using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace McpBridge.Editor
{
    [InitializeOnLoad]
    internal static class McpPlayModeTestTracker
    {
#if UNITY_TESTS_FRAMEWORK
        private static readonly object s_Gate = new();
        private const double RunTimeoutSeconds = 600d;
        private const double StartupTimeoutSeconds = 20d;
        private static TestRunState s_State;
        private static TrackerContext s_Context;

        static McpPlayModeTestTracker()
        {
            LoadState();
            EditorApplication.update -= TickPendingState;
            EditorApplication.update += TickPendingState;
            EditorApplication.delayCall += ResumePending;
            EditorApplication.delayCall += TryFlushPending;
        }

        public static ToolCallResult Begin(
            string requestId,
            string mode,
            string[] testNames,
            string[] groupNames,
            string[] assemblyNames,
            bool runSynchronously)
        {
            lock (s_Gate)
            {
                ClearExpiredStateLocked();
                if (s_State != null && !s_State.Finished)
                {
                    return CreateStatusResult("busy", "[Busy] A Unity PlayMode test run is already in progress.", false);
                }
            }

            ToolCallResult immediateResult = null;
            MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("run tests", out var blockedResult))
                {
                    immediateResult = blockedResult;
                    return;
                }

                if (RequiresCleanScenes(out var dirtyScenes))
                {
                    immediateResult = CreateDirtySceneBlockedResult(dirtyScenes);
                    return;
                }

                var nowUtc = DateTime.UtcNow;
                lock (s_Gate)
                {
                    s_State = new TestRunState
                    {
                        RequestId = requestId,
                        Mode = mode,
                        Started = false,
                        Finished = false,
                        Succeeded = false,
                        RunStarted = false,
                        Status = "running",
                        DeadlineUtcTicks = nowUtc.AddSeconds(RunTimeoutSeconds).Ticks,
                        StartupDeadlineUtcTicks = nowUtc.AddSeconds(StartupTimeoutSeconds).Ticks,
                        FailedTests = new List<TestFailureRecord>()
                    };
                    SaveState();
                }

                try
                {
                    EnsureContextRegistered();
                    var filter = new Filter
                    {
                        testMode = TestMode.PlayMode,
                        testNames = NormalizeArray(testNames),
                        groupNames = NormalizeArray(groupNames),
                        assemblyNames = NormalizeArray(assemblyNames)
                    };
                    var settings = new ExecutionSettings(filter)
                    {
                        runSynchronously = false
                    };

                    s_Context.Api.Execute(settings);
                    UpdateState(state => state.Started = true);
                }
                catch (Exception exception)
                {
                    CompleteTerminalStatus(
                        status: "failed",
                        ok: false,
                        reason: "start_failed",
                        message: $"[Failed] Unity PlayMode test run could not start: {exception.Message}",
                        stackTrace: exception.ToString(),
                        succeeded: false);
                }
            });

            return immediateResult;
        }

        private static void EnsureContextRegistered()
        {
            lock (s_Gate)
            {
                if (s_Context != null)
                {
                    return;
                }

                var callbacks = ScriptableObject.CreateInstance<TestCallbacks>();
                callbacks.Initialize();
                var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                api.RegisterCallbacks(callbacks);
                s_Context = new TrackerContext(api, callbacks);
            }
        }

        private static void ResumePending()
        {
            LoadState();
            lock (s_Gate)
            {
                if (s_State == null || s_State.Finished)
                {
                    return;
                }
            }

            try
            {
                EnsureContextRegistered();
            }
            catch (Exception exception)
            {
                CompleteTerminalStatus(
                    status: "failed",
                    ok: false,
                    reason: "resume_failed",
                    message: $"[Failed] Unity PlayMode test callbacks could not resume after reload: {exception.Message}",
                    stackTrace: exception.ToString(),
                    succeeded: false);
            }
        }

        private static void TickPendingState()
        {
            TestRunState state;
            lock (s_Gate)
            {
                state = s_State;
            }

            if (state == null || state.Finished)
            {
                return;
            }

            var nowTicks = DateTime.UtcNow.Ticks;
            if (!state.RunStarted && state.StartupDeadlineUtcTicks > 0 && nowTicks > state.StartupDeadlineUtcTicks)
            {
                if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.QueuePlayerLoopUpdate();
                }

                CompleteTerminalStatus(
                    status: "failed",
                    ok: false,
                    reason: "playmode_start_timeout",
                    message: $"[Failed] Unity PlayMode test run did not start within {StartupTimeoutSeconds:0} seconds. The editor is likely stuck during Play Mode transition or scene-save handling.",
                    stackTrace: string.Empty,
                    succeeded: false);
                return;
            }

            if (state.DeadlineUtcTicks > 0 && nowTicks > state.DeadlineUtcTicks)
            {
                CompleteTerminalStatus(
                    status: "timeout",
                    ok: false,
                    reason: "timeout",
                    message: "[Failed] Unity PlayMode test run timed out.",
                    stackTrace: string.Empty,
                    succeeded: false);
                return;
            }
        }

        public static void TryFlushPending()
        {
            TestRunState state;
            lock (s_Gate)
            {
                state = s_State;
            }

            if (state == null || !state.Finished || string.IsNullOrWhiteSpace(state.RequestId))
            {
                return;
            }

            var payload = new Dictionary<string, object>
            {
                ["ok"] = state.Succeeded,
                ["status"] = state.Status ?? (state.Succeeded ? "passed" : "failed"),
                ["resultState"] = state.ResultState ?? string.Empty,
                ["passCount"] = state.PassCount,
                ["failCount"] = state.FailCount,
                ["skipCount"] = state.SkipCount,
                ["inconclusiveCount"] = state.InconclusiveCount,
                ["duration"] = state.Duration,
                ["failedTests"] = state.FailedTests?.ConvertAll(record => (object)record.ToDictionary()) ?? new List<object>()
            };

            if (!string.IsNullOrWhiteSpace(state.Reason))
            {
                payload["reason"] = state.Reason;
            }

            if (!string.IsNullOrWhiteSpace(state.Message))
            {
                payload["message"] = state.Message;
            }

            if (!string.IsNullOrWhiteSpace(state.StackTrace))
            {
                payload["stackTrace"] = state.StackTrace;
            }

            var text = BuildResultText(state);
            var sent = McpBridgeConnection.TrySendResponse(new BridgeEnvelope
            {
                Type = "response",
                Id = state.RequestId,
                Success = true,
                Result = new ToolCallResult
                {
                    Text = text,
                    StructuredContent = payload
                }.ToDictionary()
            });

            if (!sent)
            {
                EditorApplication.delayCall += TryFlushPending;
                return;
            }

            ClearState();
            CleanupContext();
        }

        private static string BuildResultText(TestRunState state)
        {
            if (!string.IsNullOrWhiteSpace(state.Message) &&
                state.Status != "passed" &&
                state.Reason != null &&
                state.Reason != "run_failed")
            {
                return state.Message;
            }

            return state.Succeeded
                ? $"[Success] Unity tests passed. Passed: {state.PassCount}, Skipped: {state.SkipCount}."
                : $"[Failed] Unity tests failed. Passed: {state.PassCount}, Failed: {state.FailCount}, Skipped: {state.SkipCount}.";
        }

        private static void CompleteFromResult(ITestResultAdaptor result, List<TestFailureRecord> failedTests)
        {
            UpdateState(state =>
            {
                state.Finished = true;
                state.Succeeded = result.FailCount == 0;
                state.Status = result.FailCount == 0 ? "passed" : "failed";
                state.ResultState = result.ResultState;
                state.PassCount = result.PassCount;
                state.FailCount = result.FailCount;
                state.SkipCount = result.SkipCount;
                state.InconclusiveCount = result.InconclusiveCount;
                state.Duration = result.Duration;
                state.FailedTests = failedTests ?? new List<TestFailureRecord>();
                state.Message = null;
                state.StackTrace = null;
                state.Reason = null;
            });

            TryFlushPending();
        }

        private static void CompleteTerminalStatus(
            string status,
            bool ok,
            string reason,
            string message,
            string stackTrace,
            bool succeeded)
        {
            UpdateState(state =>
            {
                state.Finished = true;
                state.Succeeded = succeeded;
                state.Status = status;
                state.Reason = reason;
                state.Message = message;
                state.StackTrace = stackTrace;
                state.ResultState = string.Empty;
                state.PassCount = 0;
                state.FailCount = 0;
                state.SkipCount = 0;
                state.InconclusiveCount = 0;
                state.Duration = 0d;
                state.FailedTests ??= new List<TestFailureRecord>();
            });

            TryFlushPending();
        }

        private static bool RequiresCleanScenes(out List<Dictionary<string, object>> dirtyScenes)
        {
            dirtyScenes = null;
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (!scene.IsValid() || !scene.isLoaded || !scene.isDirty)
                {
                    continue;
                }

                dirtyScenes ??= new List<Dictionary<string, object>>();
                dirtyScenes.Add(new Dictionary<string, object>
                {
                    ["name"] = scene.name,
                    ["path"] = scene.path,
                    ["buildIndex"] = scene.buildIndex,
                    ["isDirty"] = scene.isDirty
                });
            }

            return dirtyScenes != null && dirtyScenes.Count > 0;
        }

        private static ToolCallResult CreateDirtySceneBlockedResult(List<Dictionary<string, object>> dirtyScenes)
        {
            var sceneLabels = new List<string>(dirtyScenes.Count);
            foreach (var scene in dirtyScenes)
            {
                var path = scene["path"] as string;
                var name = scene["name"] as string;
                sceneLabels.Add(string.IsNullOrWhiteSpace(path) ? name : path);
            }

            return CreateStatusResult(
                "blocked",
                $"[Blocked] PlayMode tests require clean scenes. Save or discard modified scenes first: {string.Join(", ", sceneLabels)}.",
                false,
                "scene_dirty",
                dirtyScenes);
        }

        private static ToolCallResult CreateStatusResult(
            string status,
            string text,
            bool ok,
            string reason = null,
            object dirtyScenes = null)
        {
            var structuredContent = new Dictionary<string, object>
            {
                ["ok"] = ok,
                ["status"] = status
            };

            if (!string.IsNullOrWhiteSpace(reason))
            {
                structuredContent["reason"] = reason;
            }

            if (dirtyScenes != null)
            {
                structuredContent["dirtyScenes"] = dirtyScenes;
            }

            return new ToolCallResult
            {
                Text = text,
                StructuredContent = structuredContent
            };
        }

        private static string[] NormalizeArray(string[] values)
        {
            return values == null || values.Length == 0 ? null : values;
        }

        private static void UpdateState(Action<TestRunState> mutate)
        {
            lock (s_Gate)
            {
                if (s_State == null)
                {
                    return;
                }

                mutate(s_State);
                SaveState();
            }
        }

        private static void LoadState()
        {
            lock (s_Gate)
            {
                if (!File.Exists(McpBridgePaths.TestRunStatePath))
                {
                    s_State = null;
                    return;
                }

                try
                {
                    s_State = McpBridgeJson.DeserializeObject<TestRunState>(File.ReadAllText(McpBridgePaths.TestRunStatePath));
                    ClearExpiredStateLocked();
                }
                catch
                {
                    s_State = null;
                }
            }
        }

        private static void SaveState()
        {
            try
            {
                File.WriteAllText(McpBridgePaths.TestRunStatePath, McpBridgeJson.SerializeObject(s_State));
            }
            catch
            {
            }
        }

        private static void ClearState()
        {
            lock (s_Gate)
            {
                s_State = null;
                try
                {
                    if (File.Exists(McpBridgePaths.TestRunStatePath))
                    {
                        File.Delete(McpBridgePaths.TestRunStatePath);
                    }
                }
                catch
                {
                }
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

            s_State = null;
            try
            {
                if (File.Exists(McpBridgePaths.TestRunStatePath))
                {
                    File.Delete(McpBridgePaths.TestRunStatePath);
                }
            }
            catch
            {
            }
        }

        private static void CleanupContext()
        {
            lock (s_Gate)
            {
                if (s_Context == null)
                {
                    return;
                }

                try
                {
                    s_Context.Callbacks.DisposeSubscriptions();
                    s_Context.Api.UnregisterCallbacks(s_Context.Callbacks);
                }
                catch
                {
                }

                try
                {
                    UnityEngine.Object.DestroyImmediate(s_Context.Callbacks);
                }
                catch
                {
                }

                try
                {
                    UnityEngine.Object.DestroyImmediate(s_Context.Api);
                }
                catch
                {
                }

                s_Context = null;
            }
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            LoadState();
            EditorApplication.delayCall += ResumePending;
            EditorApplication.delayCall += TryFlushPending;
        }

        private sealed class TrackerContext
        {
            public TrackerContext(TestRunnerApi api, TestCallbacks callbacks)
            {
                Api = api;
                Callbacks = callbacks;
            }

            public TestRunnerApi Api { get; }
            public TestCallbacks Callbacks { get; }
        }

        private sealed class TestCallbacks : ScriptableObject, IErrorCallbacks
        {
            private readonly List<TestFailureRecord> _failedTests = new();

            public void Initialize()
            {
                Application.logMessageReceived += OnLogMessageReceived;
            }

            public void DisposeSubscriptions()
            {
                Application.logMessageReceived -= OnLogMessageReceived;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                UpdateState(state => state.RunStarted = true);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                CompleteFromResult(result, new List<TestFailureRecord>(_failedTests));
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (string.IsNullOrEmpty(result.ResultState) ||
                    !result.ResultState.StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                _failedTests.Add(new TestFailureRecord
                {
                    Name = result.Name,
                    FullName = result.FullName,
                    Message = result.Message,
                    StackTrace = result.StackTrace
                });
            }

            public void OnError(string message)
            {
                CompleteTerminalStatus(
                    status: "failed",
                    ok: false,
                    reason: "run_failed",
                    message: $"[Failed] Unity test run failed: {message}",
                    stackTrace: string.Empty,
                    succeeded: false);
            }

            private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
            {
                if (!IsPlayModeSceneSaveFailure(condition, stackTrace))
                {
                    return;
                }

                if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.QueuePlayerLoopUpdate();
                }

                CompleteTerminalStatus(
                    status: "failed",
                    ok: false,
                    reason: "scene_save_during_play_mode",
                    message: "[Failed] Unity Test Runner tried to save modified scenes while entering Play Mode. Clean open scenes before running PlayMode tests.",
                    stackTrace: stackTrace ?? string.Empty,
                    succeeded: false);
            }

            private static bool IsPlayModeSceneSaveFailure(string condition, string stackTrace)
            {
                if (string.IsNullOrEmpty(condition) && string.IsNullOrEmpty(stackTrace))
                {
                    return false;
                }

                return (condition?.Contains("This cannot be used during play mode", StringComparison.OrdinalIgnoreCase) ?? false)
                    && ((stackTrace?.Contains("SaveCurrentModifiedScenesIfUserWantsTo", StringComparison.OrdinalIgnoreCase) ?? false)
                        || (stackTrace?.Contains("SaveModifiedSceneTask", StringComparison.OrdinalIgnoreCase) ?? false));
            }
        }
#endif
    }
}
