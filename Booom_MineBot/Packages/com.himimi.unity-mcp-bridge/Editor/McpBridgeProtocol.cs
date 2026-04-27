using System;
using System.Collections.Generic;

namespace McpBridge.Editor
{
    [Serializable]
    internal sealed class ToolDescriptor
    {
        public string Name;
        public string Title;
        public string Description;
        public object InputSchema;

        public Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["title"] = Title,
                ["description"] = Description
            };
            if (InputSchema != null) dictionary["inputSchema"] = InputSchema;
            return dictionary;
        }
    }

    internal sealed class BridgeEnvelope
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public string Method { get; set; }
        public string ToolName { get; set; }
        public Dictionary<string, object> Arguments { get; set; }
        public bool Success { get; set; }
        public object Result { get; set; }
        public string Error { get; set; }
        public List<ToolDescriptor> Tools { get; set; }
        public Dictionary<string, object> Params { get; set; }
        public UnityInstanceInfo Instance { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(Type)) dictionary["type"] = Type;
            if (!string.IsNullOrEmpty(Id)) dictionary["id"] = Id;
            if (!string.IsNullOrEmpty(Method)) dictionary["method"] = Method;
            if (!string.IsNullOrEmpty(ToolName)) dictionary["toolName"] = ToolName;
            if (Arguments != null) dictionary["arguments"] = Arguments;
            if (Success) dictionary["success"] = true;
            if (Result != null) dictionary["result"] = Result;
            if (!string.IsNullOrEmpty(Error)) dictionary["error"] = Error;
            if (Tools != null) dictionary["tools"] = Tools.ConvertAll(tool => (object)tool.ToDictionary());
            if (Params != null) dictionary["params"] = Params;
            if (Instance != null) dictionary["instance"] = Instance.ToDictionary();
            return dictionary;
        }

        public static BridgeEnvelope FromDictionary(Dictionary<string, object> dictionary)
        {
            if (dictionary == null) return null;
            var envelope = new BridgeEnvelope
            {
                Type = dictionary.TryGetValue("type", out var type) ? type as string : null,
                Id = dictionary.TryGetValue("id", out var id) ? id as string : null,
                Method = dictionary.TryGetValue("method", out var method) ? method as string : null,
                ToolName = dictionary.TryGetValue("toolName", out var toolName) ? toolName as string : null,
                Arguments = dictionary.TryGetValue("arguments", out var arguments) ? arguments as Dictionary<string, object> : null,
                Success = dictionary.TryGetValue("success", out var success) && success is bool boolValue && boolValue,
                Result = dictionary.TryGetValue("result", out var result) ? result : null,
                Error = dictionary.TryGetValue("error", out var error) ? error as string : null,
                Params = dictionary.TryGetValue("params", out var parameters) ? parameters as Dictionary<string, object> : null,
                Instance = dictionary.TryGetValue("instance", out var instance) && instance is Dictionary<string, object> instanceDictionary
                    ? UnityInstanceInfo.FromDictionary(instanceDictionary)
                    : null
            };
            return envelope;
        }
    }

    internal sealed class UnityInstanceInfo
    {
        public string InstanceId;
        public int ProcessId;
        public string ProjectPath;
        public string PrimaryProjectPath;
        public string ProjectName;
        public string ProductName;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["instanceId"] = InstanceId,
                ["processId"] = ProcessId,
                ["projectPath"] = ProjectPath,
                ["primaryProjectPath"] = PrimaryProjectPath,
                ["projectName"] = ProjectName,
                ["productName"] = ProductName
            };
        }

        public static UnityInstanceInfo FromDictionary(Dictionary<string, object> dictionary)
        {
            return new UnityInstanceInfo
            {
                InstanceId = dictionary.TryGetValue("instanceId", out var instanceId) ? instanceId as string : null,
                ProcessId = dictionary.TryGetValue("processId", out var processId) && processId is long number ? (int)number : 0,
                ProjectPath = dictionary.TryGetValue("projectPath", out var projectPath) ? projectPath as string : null,
                PrimaryProjectPath = dictionary.TryGetValue("primaryProjectPath", out var primaryProjectPath) ? primaryProjectPath as string : null,
                ProjectName = dictionary.TryGetValue("projectName", out var projectName) ? projectName as string : null,
                ProductName = dictionary.TryGetValue("productName", out var productName) ? productName as string : null
            };
        }
    }

    [Serializable]
    internal sealed class CompileState
    {
        public string RequestId;
        public bool Started;
        public bool Finished;
        public bool Succeeded;
        public string Phase;
        public bool ExitPlayModeBeforeCompile;
        public long DeadlineUtcTicks;
        public int StableEditorFrames;
        public List<CompileDiagnostic> Diagnostics = new();
    }

    [Serializable]
    internal sealed class CompileDiagnostic
    {
        public string File;
        public int Line;
        public int Column;
        public string Message;
        public string Type;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["file"] = File,
                ["line"] = Line,
                ["column"] = Column,
                ["message"] = Message,
                ["type"] = Type
            };
        }
    }

    [Serializable]
    internal sealed class HostState
    {
        public int ProcessId;
    }

    [Serializable]
    internal sealed class TestRunState
    {
        public string RequestId;
        public string Mode;
        public bool Started;
        public bool Finished;
        public bool Succeeded;
        public bool RunStarted;
        public string Status;
        public string Reason;
        public string Message;
        public string StackTrace;
        public string ResultState;
        public long DeadlineUtcTicks;
        public long StartupDeadlineUtcTicks;
        public int PassCount;
        public int FailCount;
        public int SkipCount;
        public int InconclusiveCount;
        public double Duration;
        public List<TestFailureRecord> FailedTests = new();
    }

    [Serializable]
    internal sealed class TestFailureRecord
    {
        public string Name;
        public string FullName;
        public string Message;
        public string StackTrace;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["fullName"] = FullName,
                ["message"] = Message,
                ["stackTrace"] = StackTrace
            };
        }
    }

    public sealed class ToolCallResult
    {
        public object StructuredContent { get; set; }
        public string Text { get; set; }

        public Dictionary<string, object> ToDictionary()
        {
            var dictionary = new Dictionary<string, object>();
            if (StructuredContent != null) dictionary["structuredContent"] = StructuredContent;
            if (!string.IsNullOrEmpty(Text)) dictionary["text"] = Text;
            return dictionary;
        }
    }

    internal static class JsonUtilityAdapter
    {
        public static T FromJson<T>(string json) where T : class => UnityEngine.JsonUtility.FromJson<T>(json);
        public static string ToJson<T>(T value) where T : class => UnityEngine.JsonUtility.ToJson(value);
    }
}
