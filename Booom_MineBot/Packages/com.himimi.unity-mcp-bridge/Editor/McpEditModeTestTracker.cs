using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace McpBridge.Editor
{
    [InitializeOnLoad]
    internal static class McpEditModeTestTracker
    {
#if UNITY_TESTS_FRAMEWORK
        private static readonly object s_Gate = new();
        private const double RunTimeoutSeconds = 600d;
        private const double StartupTimeoutSeconds = 10d;
        private static TestRunState s_State;
        private static TrackerContext s_Context;

        static McpEditModeTestTracker()
        {
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
                if (s_State != null && !s_State.Finished)
                {
                    return CreateStatusResult("busy", "[Busy] A Unity EditMode test run is already in progress.", false);
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

                var nowUtc = DateTime.UtcNow;
                lock (s_Gate)
                {
                    s_State = new TestRunState
                    {
                        RequestId = requestId,
                        Mode = NormalizeMode(mode),
                        CreatedUtcTicks = nowUtc.Ticks,
                        Started = false,
                        Finished = false,
                        Succeeded = false,
                        RunStarted = false,
                        Status = "running",
                        DeadlineUtcTicks = nowUtc.AddSeconds(RunTimeoutSeconds).Ticks,
                        StartupDeadlineUtcTicks = nowUtc.AddSeconds(StartupTimeoutSeconds).Ticks,
                        FailedTests = new List<TestFailureRecord>()
                    };
                }

                try
                {
                    TryDeletePreviousTestResults();
                    EnsureContextRegistered();
                    var filter = new Filter
                    {
                        testMode = TestMode.EditMode,
                        testNames = NormalizeArray(testNames),
                        groupNames = NormalizeArray(groupNames),
                        assemblyNames = NormalizeArray(assemblyNames)
                    };
                    var settings = new ExecutionSettings(filter)
                    {
                        runSynchronously = runSynchronously
                    };

                    StartWatchdogs(requestId);
                    s_Context.Api.Execute(settings);
                    UpdateState(state => state.Started = true);
                }
                catch (Exception exception)
                {
                    CompleteTerminalStatus(
                        status: "failed",
                        ok: false,
                        reason: "start_failed",
                        message: $"[Failed] Unity EditMode test run could not start: {exception.Message}",
                        stackTrace: exception.ToString(),
                        succeeded: false);
                }
            });

            return immediateResult;
        }

        public static string GetPendingRequestId()
        {
            lock (s_Gate)
            {
                return s_State != null && !s_State.Finished
                    ? s_State.RequestId
                    : null;
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

            CleanupContext();
            ClearState();
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

        private static void StartWatchdogs(string requestId)
        {
            _ = MonitorStartupAsync(requestId);
            _ = MonitorTimeoutAsync(requestId);
        }

        private static async Task MonitorStartupAsync(string requestId)
        {
            await Task.Delay(TimeSpan.FromSeconds(StartupTimeoutSeconds));

            var shouldFail = false;
            lock (s_Gate)
            {
                shouldFail = s_State != null &&
                             !s_State.Finished &&
                             string.Equals(s_State.RequestId, requestId, StringComparison.Ordinal) &&
                             !s_State.RunStarted;
            }

            if (!shouldFail)
            {
                return;
            }

            TryCompleteFromResultFile();

            lock (s_Gate)
            {
                shouldFail = s_State != null &&
                             !s_State.Finished &&
                             string.Equals(s_State.RequestId, requestId, StringComparison.Ordinal) &&
                             !s_State.RunStarted;
            }

            if (!shouldFail)
            {
                return;
            }

            MainThread.Instance.Run(() => CompleteTerminalStatus(
                status: "failed",
                ok: false,
                reason: "editmode_start_timeout",
                message: $"[Failed] Unity EditMode test run did not start within {StartupTimeoutSeconds:0} seconds.",
                stackTrace: string.Empty,
                succeeded: false));
        }

        private static async Task MonitorTimeoutAsync(string requestId)
        {
            await Task.Delay(TimeSpan.FromSeconds(RunTimeoutSeconds));

            var shouldFail = false;
            lock (s_Gate)
            {
                shouldFail = s_State != null &&
                             !s_State.Finished &&
                             string.Equals(s_State.RequestId, requestId, StringComparison.Ordinal);
            }

            if (!shouldFail)
            {
                return;
            }

            TryCompleteFromResultFile();

            lock (s_Gate)
            {
                shouldFail = s_State != null &&
                             !s_State.Finished &&
                             string.Equals(s_State.RequestId, requestId, StringComparison.Ordinal);
            }

            if (!shouldFail)
            {
                return;
            }

            MainThread.Instance.Run(() => CompleteTerminalStatus(
                status: "timeout",
                ok: false,
                reason: "timeout",
                message: "[Failed] Unity EditMode test run timed out.",
                stackTrace: string.Empty,
                succeeded: false));
        }

        private static void TryDeletePreviousTestResults()
        {
            try
            {
                if (File.Exists(McpBridgePaths.TestResultsPath))
                {
                    File.Delete(McpBridgePaths.TestResultsPath);
                }
            }
            catch
            {
            }
        }

        private static void TryCompleteFromResultFile()
        {
            TestRunState state;
            lock (s_Gate)
            {
                state = s_State;
            }

            if (state == null || state.Finished || state.CreatedUtcTicks <= 0)
            {
                return;
            }

            var testResultsPath = McpBridgePaths.TestResultsPath;
            if (!File.Exists(testResultsPath))
            {
                return;
            }

            try
            {
                var lastWriteUtc = File.GetLastWriteTimeUtc(testResultsPath);
                if (lastWriteUtc.Ticks < state.CreatedUtcTicks)
                {
                    return;
                }

                if (!TryParseTestResults(testResultsPath, out var parsed))
                {
                    return;
                }

                UpdateState(current =>
                {
                    current.Finished = true;
                    current.RunStarted = true;
                    current.Succeeded = parsed.FailCount == 0;
                    current.Status = parsed.FailCount == 0 ? "passed" : "failed";
                    current.ResultState = parsed.ResultState;
                    current.PassCount = parsed.PassCount;
                    current.FailCount = parsed.FailCount;
                    current.SkipCount = parsed.SkipCount;
                    current.InconclusiveCount = parsed.InconclusiveCount;
                    current.Duration = parsed.Duration;
                    current.FailedTests = parsed.FailedTests;
                    current.Message = null;
                    current.StackTrace = null;
                    current.Reason = null;
                });

                MainThread.Instance.Run(TryFlushPending);
            }
            catch
            {
            }
        }

        private static bool TryParseTestResults(string path, out ParsedTestRun parsed)
        {
            parsed = default;

            var document = XDocument.Load(path, LoadOptions.None);
            var root = document.Root;
            if (root == null || !string.Equals(root.Name.LocalName, "test-run", StringComparison.Ordinal))
            {
                return false;
            }

            parsed = new ParsedTestRun(
                GetAttribute(root, "result"),
                ParseIntAttribute(root, "passed"),
                ParseIntAttribute(root, "failed"),
                ParseIntAttribute(root, "skipped"),
                ParseIntAttribute(root, "inconclusive"),
                ParseDoubleAttribute(root, "duration"),
                root
                    .Descendants()
                    .Where(element =>
                        string.Equals(element.Name.LocalName, "test-case", StringComparison.Ordinal) &&
                        GetAttribute(element, "result").StartsWith("Failed", StringComparison.OrdinalIgnoreCase))
                    .Select(element => new TestFailureRecord
                    {
                        Name = GetAttribute(element, "name"),
                        FullName = GetAttribute(element, "fullname"),
                        Message = element.Element(XName.Get("failure"))?.Element(XName.Get("message"))?.Value ?? string.Empty,
                        StackTrace = element.Element(XName.Get("failure"))?.Element(XName.Get("stack-trace"))?.Value ?? string.Empty
                    })
                    .ToList());

            return true;
        }

        private static string GetAttribute(XElement element, string name)
        {
            return element.Attribute(name)?.Value ?? string.Empty;
        }

        private static int ParseIntAttribute(XElement element, string name)
        {
            return int.TryParse(GetAttribute(element, name), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        private static double ParseDoubleAttribute(XElement element, string name)
        {
            return double.TryParse(GetAttribute(element, name), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0d;
        }

        private static void CompleteFromResult(ITestResultAdaptor result, List<TestFailureRecord> failedTests)
        {
            UpdateState(state =>
            {
                state.Finished = true;
                state.RunStarted = true;
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

        private static void UpdateState(Action<TestRunState> mutate)
        {
            lock (s_Gate)
            {
                if (s_State == null)
                {
                    return;
                }

                mutate(s_State);
            }
        }

        private static void ClearState()
        {
            lock (s_Gate)
            {
                s_State = null;
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
                    if (s_Context.Callbacks != null)
                    {
                        UnityEngine.Object.DestroyImmediate(s_Context.Callbacks);
                    }
                }
                catch
                {
                }

                try
                {
                    if (s_Context.Api != null)
                    {
                        UnityEngine.Object.DestroyImmediate(s_Context.Api);
                    }
                }
                catch
                {
                }

                s_Context = null;
            }
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

        private static string NormalizeMode(string mode)
        {
            return string.IsNullOrWhiteSpace(mode) ? "edit" : mode.Trim().ToLowerInvariant();
        }

        private static string[] NormalizeArray(string[] values)
        {
            return values == null || values.Length == 0 ? null : values;
        }

        private static ToolCallResult CreateStatusResult(string status, string text, bool ok)
        {
            return new ToolCallResult
            {
                Text = text,
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = ok,
                    ["status"] = status
                }
            };
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

        private readonly struct ParsedTestRun
        {
            public ParsedTestRun(
                string resultState,
                int passCount,
                int failCount,
                int skipCount,
                int inconclusiveCount,
                double duration,
                List<TestFailureRecord> failedTests)
            {
                ResultState = resultState;
                PassCount = passCount;
                FailCount = failCount;
                SkipCount = skipCount;
                InconclusiveCount = inconclusiveCount;
                Duration = duration;
                FailedTests = failedTests;
            }

            public string ResultState { get; }
            public int PassCount { get; }
            public int FailCount { get; }
            public int SkipCount { get; }
            public int InconclusiveCount { get; }
            public double Duration { get; }
            public List<TestFailureRecord> FailedTests { get; }
        }

        private sealed class TestCallbacks : ScriptableObject, ICallbacks
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
                CompleteFromResult(result, _failedTests);
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

            private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
            {
                if (type != LogType.Exception)
                {
                    return;
                }

                if (_failedTests.Any(item => item.Message == condition && item.StackTrace == stackTrace))
                {
                    return;
                }

                _failedTests.Add(new TestFailureRecord
                {
                    Name = "Exception",
                    FullName = "Exception",
                    Message = condition,
                    StackTrace = stackTrace
                });
            }
        }
#else
        public static ToolCallResult Begin(
            string requestId,
            string mode,
            string[] testNames,
            string[] groupNames,
            string[] assemblyNames,
            bool runSynchronously)
        {
            return CreateStatusResult("blocked", "[Blocked] Unity Test Framework is not available in this project.", false);
        }

        public static string GetPendingRequestId() => null;

        public static void TryFlushPending()
        {
        }

        private static ToolCallResult CreateStatusResult(string status, string text, bool ok)
        {
            return new ToolCallResult
            {
                Text = text,
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = ok,
                    ["status"] = status
                }
            };
        }
#endif
    }
}
