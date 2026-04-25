using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;

namespace McpBridge.Editor
{
    [InitializeOnLoad]
    internal static class McpCompileTracker
    {
        public const string ToolName = "unity.compile";

        private static readonly object s_Gate = new();
        private static CompileState s_State;
        private const string PhaseWaitingForPlayModeExit = "waiting_for_playmode_exit";
        private const string PhaseWaitingForEditorSettle = "waiting_for_editor_settle";
        private const string PhaseRequestedCompile = "requested_compile";
        private const string PhaseCompiling = "compiling";
        private const string PhaseBlocked = "blocked";
        private const string PhaseCompleted = "completed";
        private const double ExitPlayModeTimeoutSeconds = 30.0;
        private const double CompileTimeoutSeconds = 120.0;
        private const int RequiredStableEditorFrames = 2;

        static McpCompileTracker()
        {
            CompilationPipeline.compilationStarted += _ => UpdateState(state =>
            {
                state.Started = true;
                state.Phase = PhaseCompiling;
            });
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            CompilationPipeline.compilationFinished += _ => OnCompilationFinished();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += TickPendingState;
            LoadState();
            EditorApplication.delayCall += ResumePending;
            EditorApplication.delayCall += TryFlushPending;
        }

        public static ToolCallResult Begin(string requestId, bool exitPlayMode)
        {
            lock (s_Gate)
            {
                ClearExpiredStateLocked();
                if (s_State != null && !s_State.Finished)
                {
                    return CreateStatusResult(
                        "busy",
                        "[Busy] A Unity compile request is already running.",
                        false);
                }

                s_State = new CompileState
                {
                    RequestId = requestId,
                    Started = false,
                    Finished = false,
                    Succeeded = false,
                    Phase = exitPlayMode ? PhaseWaitingForPlayModeExit : PhaseRequestedCompile,
                    ExitPlayModeBeforeCompile = exitPlayMode,
                    DeadlineUtcTicks = DateTime.UtcNow.AddSeconds(exitPlayMode ? ExitPlayModeTimeoutSeconds : CompileTimeoutSeconds).Ticks,
                    StableEditorFrames = 0,
                    Diagnostics = new List<CompileDiagnostic>()
                };
                SaveState();
            }

            Log($"begin requestId={requestId} exitPlayMode={exitPlayMode} isPlaying={EditorApplication.isPlaying} isPlayingOrWillChange={EditorApplication.isPlayingOrWillChangePlaymode}");

            McpBridge.MainThread.Instance.Run(() =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    if (!exitPlayMode)
                    {
                        CompleteBlockedRequest();
                        return;
                    }

                    if (EditorApplication.isPlaying)
                    {
                        Log("requesting exit from Play Mode");
                        EditorApplication.isPlaying = false;
                    }

                    EditorApplication.QueuePlayerLoopUpdate();
                    EditorApplication.delayCall += WaitForPlayModeExitAndCompile;
                    return;
                }

                RequestCompile();
            });

            return null;
        }

        private static void WaitForPlayModeExitAndCompile()
        {
            lock (s_Gate)
            {
                if (s_State == null || s_State.Finished) return;
                s_State.Phase = PhaseWaitingForPlayModeExit;
                SaveState();
            }

            Log($"waiting_for_playmode_exit isPlaying={EditorApplication.isPlaying} isPlayingOrWillChange={EditorApplication.isPlayingOrWillChangePlaymode}");

            if (HasTimedOut())
            {
                CompleteTerminalStatus(
                    "timeout",
                    "[Failed] Timed out while waiting for Unity to exit Play Mode before compiling.",
                    false,
                    true);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                EditorApplication.delayCall += WaitForPlayModeExitAndCompile;
                return;
            }

            BeginEditorSettle();
        }

        private static void WaitForEditorSettleAndCompile()
        {
            CompileState state;
            lock (s_Gate) { state = s_State; }
            if (state == null || state.Finished) return;

            Log($"waiting_for_editor_settle stableFrames={state.StableEditorFrames} isPlaying={EditorApplication.isPlaying} isPlayingOrWillChange={EditorApplication.isPlayingOrWillChangePlaymode} isUpdating={EditorApplication.isUpdating} isCompiling={EditorApplication.isCompiling}");

            if (HasTimedOut())
            {
                CompleteTerminalStatus(
                    "timeout",
                    "[Failed] Timed out while waiting for the Unity editor to settle after exiting Play Mode.",
                    false,
                    true);
                return;
            }

            if (EditorApplication.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isUpdating ||
                EditorApplication.isCompiling)
            {
                UpdateState(current => current.StableEditorFrames = 0);
                EditorApplication.delayCall += WaitForEditorSettleAndCompile;
                return;
            }

            var stableEnough = false;
            UpdateState(current =>
            {
                current.Phase = PhaseWaitingForEditorSettle;
                current.StableEditorFrames++;
                stableEnough = current.StableEditorFrames >= RequiredStableEditorFrames;
            });

            if (!stableEnough)
            {
                EditorApplication.delayCall += WaitForEditorSettleAndCompile;
                return;
            }

            RequestCompile();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            Log($"playModeStateChanged={change}");
            if (change != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            CompileState state;
            lock (s_Gate) { state = s_State; }
            if (state == null || state.Finished)
            {
                return;
            }

            if (state.Phase == PhaseWaitingForPlayModeExit && state.ExitPlayModeBeforeCompile)
            {
                BeginEditorSettle();
            }
        }

        private static void TickPendingState()
        {
            CompileState state;
            lock (s_Gate) { state = s_State; }
            if (state == null || state.Finished)
            {
                return;
            }

            if (state.Phase == PhaseWaitingForPlayModeExit &&
                !EditorApplication.isPlaying &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Log("tick_pending_state -> begin_editor_settle");
                BeginEditorSettle();
            }
        }

        private static void BeginEditorSettle()
        {
            Log("begin_editor_settle");
            UpdateState(state =>
            {
                state.Phase = PhaseWaitingForEditorSettle;
                state.StableEditorFrames = 0;
                state.DeadlineUtcTicks = DateTime.UtcNow.AddSeconds(ExitPlayModeTimeoutSeconds).Ticks;
            });
            EditorApplication.QueuePlayerLoopUpdate();
            EditorApplication.delayCall += WaitForEditorSettleAndCompile;
        }

        private static void RequestCompile()
        {
            Log("request_compile");
            UpdateState(state =>
            {
                state.Phase = PhaseRequestedCompile;
                state.DeadlineUtcTicks = DateTime.UtcNow.AddSeconds(CompileTimeoutSeconds).Ticks;
                state.StableEditorFrames = 0;
            });
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            CompilationPipeline.RequestScriptCompilation();
            EditorApplication.delayCall += FinalizeIfIdle;
        }

        private static void FinalizeIfIdle()
        {
            lock (s_Gate)
            {
                if (s_State == null || s_State.Started || s_State.Finished) return;
            }

            Log($"finalize_if_idle isCompiling={EditorApplication.isCompiling} isUpdating={EditorApplication.isUpdating}");

            if (HasTimedOut())
            {
                CompleteTerminalStatus(
                    "timeout",
                    "[Failed] Timed out while waiting for Unity compilation to complete.",
                    false,
                    true);
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += FinalizeIfIdle;
                return;
            }
            UpdateState(state =>
            {
                state.Started = true;
                state.Finished = true;
                state.Succeeded = true;
                state.Phase = PhaseCompleted;
            });
            TryFlushPending();
        }

        private static void CompleteBlockedRequest()
        {
            UpdateState(state =>
            {
                state.Started = true;
                state.Finished = true;
                state.Succeeded = false;
                state.Phase = PhaseBlocked;
            });

            var sent = McpBridgeConnection.TrySendResponse(new BridgeEnvelope
            {
                Type = "response",
                Id = s_State?.RequestId,
                Success = true,
                Result = CreateStatusResult(
                    "blocked",
                    "[Blocked] Unity is in Play Mode. Call unity.compile with exitPlayMode=true to exit Play Mode and compile.",
                    false).ToDictionary()
            });

            if (!sent)
            {
                EditorApplication.delayCall += CompleteBlockedRequest;
                return;
            }

            ClearState();
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, CompilerMessage[] messages)
        {
            UpdateState(state =>
            {
                foreach (var message in messages.Where(candidate => candidate.type == CompilerMessageType.Error))
                {
                    state.Diagnostics.Add(new CompileDiagnostic
                    {
                        File = message.file,
                        Line = message.line,
                        Column = message.column,
                        Message = message.message,
                        Type = message.type.ToString()
                    });
                }
            });
        }

        private static void OnCompilationFinished()
        {
            Log("compilation_finished");
            UpdateState(state =>
            {
                state.Finished = true;
                state.Succeeded = state.Diagnostics.Count == 0;
                state.Phase = PhaseCompleted;
            });

            if (s_State != null)
            {
                TryFlushPending();
            }
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            Log("did_reload_scripts");
            LoadState();
            EditorApplication.delayCall += ResumePending;
            EditorApplication.delayCall += TryFlushPending;
        }

        private static void ResumePending()
        {
            CompileState state;
            lock (s_Gate) { state = s_State; }
            if (state == null || state.Finished) return;

            Log($"resume_pending phase={state.Phase} exitPlayMode={state.ExitPlayModeBeforeCompile} started={state.Started} finished={state.Finished}");

            if (string.IsNullOrEmpty(state.Phase))
            {
                if (state.ExitPlayModeBeforeCompile && EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.delayCall += WaitForPlayModeExitAndCompile;
                    return;
                }

                if (!state.Started)
                {
                    EditorApplication.delayCall += RequestCompile;
                    return;
                }
            }

            switch (state.Phase)
            {
                case PhaseWaitingForPlayModeExit:
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        EditorApplication.delayCall += BeginEditorSettle;
                    }
                    else
                    {
                        EditorApplication.delayCall += WaitForPlayModeExitAndCompile;
                    }
                    break;
                case PhaseWaitingForEditorSettle:
                    EditorApplication.delayCall += WaitForEditorSettleAndCompile;
                    break;
                case PhaseRequestedCompile:
                case PhaseCompiling:
                    EditorApplication.delayCall += FinalizeIfIdle;
                    break;
            }
        }

        public static void TryFlushPending()
        {
            CompileState state;
            lock (s_Gate) { state = s_State; }
            if (state == null || !state.Finished || string.IsNullOrWhiteSpace(state.RequestId)) return;

            var diagnostics = state.Diagnostics.Select(diagnostic => $"{diagnostic.File}:{diagnostic.Line}:{diagnostic.Column} {diagnostic.Message}");
            var text = state.Succeeded
                ? (state.ExitPlayModeBeforeCompile
                    ? "[Success] Unity exited Play Mode and compile completed."
                    : "[Success] Unity compile completed.")
                : $"[Failed] Unity compile reported {state.Diagnostics.Count} error(s).\n{string.Join("\n", diagnostics)}";

            var sent = McpBridgeConnection.TrySendResponse(new BridgeEnvelope
            {
                Type = "response",
                Id = state.RequestId,
                Success = state.Succeeded,
                Result = new ToolCallResult
                {
                    Text = text,
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = state.Succeeded,
                        ["diagnostics"] = state.Diagnostics.ConvertAll(diagnostic => (object)diagnostic.ToDictionary())
                    }
                }.ToDictionary(),
                Error = state.Succeeded ? null : text
            });

            if (!sent)
            {
                EditorApplication.delayCall += TryFlushPending;
                return;
            }

            ClearState();
        }

        private static bool HasTimedOut()
        {
            CompileState state;
            lock (s_Gate) { state = s_State; }
            return state != null && state.DeadlineUtcTicks > 0 && DateTime.UtcNow.Ticks > state.DeadlineUtcTicks;
        }

        private static void UpdateState(Action<CompileState> mutate)
        {
            lock (s_Gate)
            {
                if (s_State == null) return;
                mutate(s_State);
                SaveState();
            }
        }

        private static void LoadState()
        {
            lock (s_Gate)
            {
                if (!File.Exists(McpBridgePaths.CompileStatePath)) { s_State = null; return; }
                try
                {
                    s_State = McpBridgeJson.DeserializeObject<CompileState>(File.ReadAllText(McpBridgePaths.CompileStatePath));
                    ClearExpiredStateLocked();
                }
                catch { s_State = null; }
            }
        }

        private static void SaveState()
        {
            try { File.WriteAllText(McpBridgePaths.CompileStatePath, McpBridgeJson.SerializeObject(s_State)); }
            catch { }
        }

        private static void DeleteState()
        {
            try { if (File.Exists(McpBridgePaths.CompileStatePath)) File.Delete(McpBridgePaths.CompileStatePath); }
            catch { }
        }

        private static void ClearState()
        {
            lock (s_Gate)
            {
                s_State = null;
                DeleteState();
            }
        }

        private static void ClearExpiredStateLocked()
        {
            if (s_State == null || s_State.Finished)
            {
                return;
            }

            if (s_State.DeadlineUtcTicks <= 0 || DateTime.UtcNow.Ticks <= s_State.DeadlineUtcTicks)
            {
                return;
            }

            Log($"clearing expired state phase={s_State.Phase}");
            s_State = null;
            DeleteState();
        }

        private static ToolCallResult CreateStatusResult(string status, string text, bool ok)
        {
            return new ToolCallResult
            {
                Text = text,
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = ok,
                    ["status"] = status,
                    ["diagnostics"] = new List<object>()
                }
            };
        }

        private static void CompleteTerminalStatus(string status, string text, bool ok, bool success)
        {
            Log($"complete_terminal_status status={status} success={success}");
            UpdateState(state =>
            {
                state.Started = true;
                state.Finished = true;
                state.Succeeded = success;
                state.Phase = PhaseCompleted;
            });

            var sent = McpBridgeConnection.TrySendResponse(new BridgeEnvelope
            {
                Type = "response",
                Id = s_State?.RequestId,
                Success = true,
                Result = CreateStatusResult(status, text, ok).ToDictionary()
            });

            if (!sent)
            {
                EditorApplication.delayCall += () => CompleteTerminalStatus(status, text, ok, success);
                return;
            }

            ClearState();
        }

        private static void Log(string message)
        {
            UnityEngine.Debug.Log($"[McpCompileTracker] {message}");
        }
    }
}
