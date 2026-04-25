using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
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
                    var callbacks = ScriptableObject.CreateInstance<TestCallbacks>();
                    callbacks.Initialize(tcs, CleanupSession);
                    var api = ScriptableObject.CreateInstance<TestRunnerApi>();
                    api.RegisterCallbacks(callbacks);

                    var filter = new Filter
                    {
                        testMode = ParseMode(mode),
                        testNames = NormalizeArray(testNames),
                        groupNames = NormalizeArray(groupNames),
                        assemblyNames = NormalizeArray(assemblyNames)
                    };

                    var settings = new ExecutionSettings(filter)
                    {
                        runSynchronously = runSynchronously && filter.testMode == EditorTestMode.EditMode
                    };

                    var session = new TestRunSession(api, callbacks, tcs);
                    lock (s_Gate)
                    {
                        s_Current = session;
                    }

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

                    api.Execute(settings);
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

        private sealed class TestRunSession
        {
            public TestRunSession(TestRunnerApi api, TestCallbacks callbacks, TaskCompletionSource<ToolCallResult> tcs)
            {
                Api = api;
                Callbacks = callbacks;
                TaskCompletionSource = tcs;
            }

            public TestRunnerApi Api { get; }
            public TestCallbacks Callbacks { get; }
            public TaskCompletionSource<ToolCallResult> TaskCompletionSource { get; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
        }

        private sealed class TestCallbacks : ScriptableObject, ICallbacks
        {
            private readonly List<object> _failedTests = new();
            private Action _cleanup;
            private TaskCompletionSource<ToolCallResult> _tcs;

            public void Initialize(TaskCompletionSource<ToolCallResult> tcs, Action cleanup)
            {
                _tcs = tcs;
                _cleanup = cleanup;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
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

                _tcs.TrySetResult(new ToolCallResult
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
        }
#endif
    }
}
