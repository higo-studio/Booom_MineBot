using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_TESTS_FRAMEWORK
using NUnit.Framework.Interfaces;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine.TestTools;
using EditorTestMode = UnityEditor.TestTools.TestRunner.Api.TestMode;
#endif

namespace McpBridge.Editor
{
    internal static class McpTestRunner
    {
#if UNITY_TESTS_FRAMEWORK
        private static readonly object s_Gate = new();
        private const double PlayModeStartupTimeoutSeconds = 20d;
        private static TestRunSession s_Current;
#endif

        public static Task<ToolCallResult> RunAsync(
            string mode,
            string[] testNames,
            string[] groupNames,
            string[] assemblyNames,
            bool runSynchronously)
        {
#if !UNITY_TESTS_FRAMEWORK
            return Task.FromResult(new ToolCallResult
            {
                Text = "[Blocked] Unity Test Framework is not available in this project.",
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = false,
                    ["status"] = "blocked"
                }
            });
#else
            lock (s_Gate)
            {
                if (s_Current != null)
                {
                    return Task.FromResult(new ToolCallResult
                    {
                        Text = "[Busy] A Unity test run is already in progress.",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = false,
                            ["status"] = "busy"
                        }
                    });
                }
            }

            var tcs = new TaskCompletionSource<ToolCallResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("run tests", out var blockedResult))
                {
                    tcs.TrySetResult(blockedResult);
                    return;
                }

                try
                {
                    var testMode = ParseMode(mode);
                    if (RequiresCleanScenes(testMode, out var dirtyScenes))
                    {
                        tcs.TrySetResult(CreateDirtySceneBlockedResult(dirtyScenes));
                        return;
                    }

                    var callbacks = ScriptableObject.CreateInstance<TestCallbacks>();
                    var api = ScriptableObject.CreateInstance<TestRunnerApi>();

                    var filter = new Filter
                    {
                        testMode = testMode,
                        testNames = NormalizeArray(testNames),
                        groupNames = NormalizeArray(groupNames),
                        assemblyNames = NormalizeArray(assemblyNames)
                    };

                    var settings = new ExecutionSettings(filter)
                    {
                        runSynchronously = runSynchronously && filter.testMode == EditorTestMode.EditMode
                    };

                    var session = new TestRunSession(api, callbacks, tcs, testMode, EditorApplication.timeSinceStartup);
                    callbacks.Initialize(session, CleanupSession);
                    api.RegisterCallbacks(callbacks);
                    lock (s_Gate)
                    {
                        s_Current = session;
                    }

                    BeginStartupWatchdog(session);

                    session.CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(10));
                    session.CancellationTokenSource.Token.Register(() =>
                    {
                        tcs.TrySetResult(new ToolCallResult
                        {
                            Text = "[Failed] Unity test run timed out.",
                            StructuredContent = new Dictionary<string, object>
                            {
                                ["ok"] = false,
                                ["status"] = "timeout"
                            }
                        });
                        CleanupSession();
                    });

                    ScheduleExecute(session, settings);
                }
                catch (Exception exception)
                {
                    CleanupSession();
                    tcs.TrySetResult(new ToolCallResult
                    {
                        Text = $"[Failed] Unity test run could not start: {exception.Message}",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = false,
                            ["status"] = "failed"
                        }
                    });
                }
            });

            return tcs.Task;
#endif
        }

#if UNITY_TESTS_FRAMEWORK
        private static void CleanupSession()
        {
            lock (s_Gate)
            {
                if (s_Current == null)
                {
                    return;
                }

                try
                {
                    if (s_Current.StartupWatchdog != null)
                    {
                        EditorApplication.update -= s_Current.StartupWatchdog;
                    }
                }
                catch
                {
                }

                try
                {
                    s_Current.Callbacks?.DisposeSubscriptions();
                    s_Current.Api.UnregisterCallbacks(s_Current.Callbacks);
                }
                catch
                {
                }

                try
                {
                    if (s_Current.Callbacks != null)
                    {
                        UnityEngine.Object.DestroyImmediate(s_Current.Callbacks);
                    }
                }
                catch
                {
                }

                try
                {
                    if (s_Current.Api != null)
                    {
                        UnityEngine.Object.DestroyImmediate(s_Current.Api);
                    }
                }
                catch
                {
                }

                s_Current.CancellationTokenSource?.Dispose();
                s_Current = null;
            }
        }

        private static string[] NormalizeArray(string[] values)
        {
            return values == null || values.Length == 0 ? null : values;
        }

        private static EditorTestMode ParseMode(string mode)
        {
            mode = string.IsNullOrWhiteSpace(mode) ? "edit" : mode.Trim().ToLowerInvariant();
            return mode switch
            {
                "edit" or "editmode" => EditorTestMode.EditMode,
                "play" or "playmode" => EditorTestMode.PlayMode,
                "all" => EditorTestMode.EditMode | EditorTestMode.PlayMode,
                _ => EditorTestMode.EditMode
            };
        }

        private static bool RequiresCleanScenes(EditorTestMode mode, out List<Dictionary<string, object>> dirtyScenes)
        {
            dirtyScenes = null;
            if ((mode & EditorTestMode.PlayMode) == 0)
            {
                return false;
            }

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

            return new ToolCallResult
            {
                Text = $"[Blocked] PlayMode tests require clean scenes. Save or discard modified scenes first: {string.Join(", ", sceneLabels)}.",
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = false,
                    ["status"] = "blocked",
                    ["reason"] = "scene_dirty",
                    ["dirtyScenes"] = dirtyScenes
                }
            };
        }

        private static void BeginStartupWatchdog(TestRunSession session)
        {
            if ((session.Mode & EditorTestMode.PlayMode) == 0)
            {
                return;
            }

            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                lock (s_Gate)
                {
                    if (!ReferenceEquals(s_Current, session))
                    {
                        EditorApplication.update -= callback;
                        return;
                    }
                }

                if (session.TaskCompletionSource.Task.IsCompleted || session.RunStarted)
                {
                    EditorApplication.update -= callback;
                    return;
                }

                if (EditorApplication.timeSinceStartup - session.CreatedAtEditorTime < PlayModeStartupTimeoutSeconds)
                {
                    return;
                }

                EditorApplication.update -= callback;
                if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.QueuePlayerLoopUpdate();
                }

                session.TaskCompletionSource.TrySetResult(new ToolCallResult
                {
                    Text = $"[Failed] Unity PlayMode test run did not start within {PlayModeStartupTimeoutSeconds:0} seconds. The editor is likely stuck during Play Mode transition or scene-save handling.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["status"] = "failed",
                        ["reason"] = "playmode_start_timeout"
                    }
                });
                CleanupSession();
            };

            session.StartupWatchdog = callback;
            EditorApplication.update += callback;
        }

        private static void ScheduleExecute(TestRunSession session, ExecutionSettings settings)
        {
            EditorApplication.CallbackFunction delayedStart = null;
            delayedStart = () =>
            {
                EditorApplication.delayCall -= delayedStart;

                lock (s_Gate)
                {
                    if (!ReferenceEquals(s_Current, session) || session.TaskCompletionSource.Task.IsCompleted)
                    {
                        return;
                    }
                }

                if (McpEditorToolGuards.TryBlockForTransition("run tests", out var blockedResult))
                {
                    session.TaskCompletionSource.TrySetResult(blockedResult);
                    CleanupSession();
                    return;
                }

                try
                {
                    session.Api.Execute(settings);
                }
                catch (Exception exception)
                {
                    CleanupSession();
                    session.TaskCompletionSource.TrySetResult(new ToolCallResult
                    {
                        Text = $"[Failed] Unity test run could not start: {exception.Message}",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = false,
                            ["status"] = "failed"
                        }
                    });
                }
            };

            EditorApplication.delayCall += delayedStart;
        }

        private sealed class TestRunSession
        {
            public TestRunSession(
                TestRunnerApi api,
                TestCallbacks callbacks,
                TaskCompletionSource<ToolCallResult> tcs,
                EditorTestMode mode,
                double createdAtEditorTime)
            {
                Api = api;
                Callbacks = callbacks;
                TaskCompletionSource = tcs;
                Mode = mode;
                CreatedAtEditorTime = createdAtEditorTime;
            }

            public TestRunnerApi Api { get; }
            public TestCallbacks Callbacks { get; }
            public TaskCompletionSource<ToolCallResult> TaskCompletionSource { get; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
            public EditorTestMode Mode { get; }
            public double CreatedAtEditorTime { get; }
            public bool RunStarted { get; set; }
            public EditorApplication.CallbackFunction StartupWatchdog { get; set; }
        }

        private sealed class TestCallbacks : ScriptableObject, ICallbacks
        {
            private readonly List<object> _failedTests = new();
            private Action _cleanup;
            private TestRunSession _session;

            public void Initialize(TestRunSession session, Action cleanup)
            {
                _session = session;
                _cleanup = cleanup;
                Application.logMessageReceived += OnLogMessageReceived;
            }

            public void DisposeSubscriptions()
            {
                Application.logMessageReceived -= OnLogMessageReceived;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                if (_session != null)
                {
                    _session.RunStarted = true;
                }
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                var payload = new Dictionary<string, object>
                {
                    ["ok"] = result.FailCount == 0,
                    ["status"] = result.FailCount == 0 ? "passed" : "failed",
                    ["resultState"] = result.ResultState,
                    ["passCount"] = result.PassCount,
                    ["failCount"] = result.FailCount,
                    ["skipCount"] = result.SkipCount,
                    ["inconclusiveCount"] = result.InconclusiveCount,
                    ["duration"] = result.Duration,
                    ["failedTests"] = _failedTests
                };

                var text = result.FailCount == 0
                    ? $"[Success] Unity tests passed. Passed: {result.PassCount}, Skipped: {result.SkipCount}."
                    : $"[Failed] Unity tests failed. Passed: {result.PassCount}, Failed: {result.FailCount}, Skipped: {result.SkipCount}.";

                _session.TaskCompletionSource.TrySetResult(new ToolCallResult
                {
                    Text = text,
                    StructuredContent = payload
                });
                _cleanup?.Invoke();
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

                _failedTests.Add(new Dictionary<string, object>
                {
                    ["name"] = result.Name,
                    ["fullName"] = result.FullName,
                    ["message"] = result.Message,
                    ["stackTrace"] = result.StackTrace
                });
            }

            private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
            {
                if (_session?.TaskCompletionSource == null || _session.TaskCompletionSource.Task.IsCompleted)
                {
                    return;
                }

                if (!IsPlayModeSceneSaveFailure(condition, stackTrace))
                {
                    return;
                }

                if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                    EditorApplication.QueuePlayerLoopUpdate();
                }

                _session.TaskCompletionSource.TrySetResult(new ToolCallResult
                {
                    Text = "[Failed] Unity Test Runner tried to save modified scenes while entering Play Mode. Clean open scenes before running PlayMode tests.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["status"] = "failed",
                        ["reason"] = "scene_save_during_play_mode",
                        ["message"] = condition ?? string.Empty,
                        ["stackTrace"] = stackTrace ?? string.Empty
                    }
                });
                _cleanup?.Invoke();
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
