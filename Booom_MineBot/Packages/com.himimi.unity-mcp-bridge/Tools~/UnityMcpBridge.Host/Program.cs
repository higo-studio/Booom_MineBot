using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var options = HostOptions.Parse(args);
var primaryProjectPath = Path.GetFullPath(options.PrimaryProjectPath ?? Directory.GetCurrentDirectory());
var unity = new UnityBridgeHub(options.IpcPort, primaryProjectPath);
unity.Start();

var sessions = new SessionManager();
unity.NotificationReceived += sessions.Broadcast;

using var listener = new HttpListener();
listener.Prefixes.Add($"http://127.0.0.1:{options.HttpPort}/mcp/");
listener.Start();
Console.WriteLine($"MCP Streamable HTTP: http://127.0.0.1:{options.HttpPort}/mcp");
Console.WriteLine($"Unity IPC: 127.0.0.1:{options.IpcPort}");
Console.WriteLine($"Primary project: {primaryProjectPath}");

while (true)
{
    HttpListenerContext context;
    try { context = await listener.GetContextAsync(); }
    catch { break; }
    _ = Task.Run(() => Router.DispatchAsync(context, unity, sessions));
}

internal static class Router
{
    public static async Task DispatchAsync(HttpListenerContext context, UnityBridgeHub unity, SessionManager sessions)
    {
        try
        {
            switch (context.Request.HttpMethod)
            {
                case "POST":
                    await HandlePostAsync(context, unity, sessions);
                    break;
                case "GET":
                    await HandleGetAsync(context, sessions);
                    break;
                case "DELETE":
                    HandleDelete(context, sessions);
                    break;
                case "OPTIONS":
                    WriteCors(context.Response);
                    context.Response.StatusCode = 204;
                    context.Response.Close();
                    break;
                default:
                    context.Response.StatusCode = 405;
                    context.Response.Close();
                    break;
            }
        }
        catch (Exception exception)
        {
            try
            {
                context.Response.StatusCode = 500;
                context.Response.ContentType = "text/plain; charset=utf-8";
                var bytes = Encoding.UTF8.GetBytes(exception.ToString());
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
            catch { }
        }
    }

    private static void WriteCors(HttpListenerResponse response)
    {
        response.Headers["Access-Control-Allow-Origin"] = "*";
        response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Mcp-Session-Id, MCP-Protocol-Version";
        response.Headers["Access-Control-Allow-Methods"] = "POST, GET, DELETE, OPTIONS";
        response.Headers["Access-Control-Expose-Headers"] = "Mcp-Session-Id";
    }

    private static async Task HandlePostAsync(HttpListenerContext context, UnityBridgeHub unity, SessionManager sessions)
    {
        WriteCors(context.Response);

        using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
        var body = await reader.ReadToEndAsync();
        if (string.IsNullOrWhiteSpace(body))
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        if (JsonNode.Parse(body) is not JsonObject requestObject)
        {
            context.Response.StatusCode = 400;
            context.Response.Close();
            return;
        }

        var method = requestObject["method"]?.GetValue<string>() ?? string.Empty;
        var isNotification = requestObject["id"] == null;
        var sessionId = context.Request.Headers["Mcp-Session-Id"];

        if (method == "initialize")
        {
            sessionId = sessions.Create();
            context.Response.Headers["Mcp-Session-Id"] = sessionId;
        }
        else if (!string.IsNullOrEmpty(sessionId) && !sessions.Touch(sessionId))
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }

        var response = await JsonRpcHandler.HandleAsync(requestObject, unity);
        if (isNotification || response == null)
        {
            context.Response.StatusCode = 202;
            context.Response.Close();
            return;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200;
        var payload = Encoding.UTF8.GetBytes(response.ToJsonString());
        await context.Response.OutputStream.WriteAsync(payload);
        context.Response.Close();
    }

    private static async Task HandleGetAsync(HttpListenerContext context, SessionManager sessions)
    {
        WriteCors(context.Response);
        var sessionId = context.Request.Headers["Mcp-Session-Id"];
        if (string.IsNullOrEmpty(sessionId) || !sessions.Touch(sessionId))
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
            return;
        }

        var accept = context.Request.Headers["Accept"] ?? string.Empty;
        if (!accept.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = 406;
            context.Response.Close();
            return;
        }

        context.Response.StatusCode = 200;
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers["Cache-Control"] = "no-cache";
        context.Response.SendChunked = true;
        await sessions.AttachStreamAsync(sessionId, context.Response);
    }

    private static void HandleDelete(HttpListenerContext context, SessionManager sessions)
    {
        WriteCors(context.Response);
        var sessionId = context.Request.Headers["Mcp-Session-Id"];
        if (!string.IsNullOrEmpty(sessionId))
        {
            sessions.Drop(sessionId);
        }

        context.Response.StatusCode = 204;
        context.Response.Close();
    }
}

internal static class JsonRpcHandler
{
    private const string InstancesToolName = "unity.instances";
    private const string CompileToolName = "unity.compile";
    private const string TestsRunToolName = "unity.tests_run";
    private const string UnityInstanceIdArgumentName = "unityInstanceId";

    public static async Task<JsonObject?> HandleAsync(JsonObject request, UnityBridgeHub unity)
    {
        var id = request["id"];
        var method = request["method"]?.GetValue<string>() ?? string.Empty;
        var paramsNode = request["params"];

        try
        {
            return method switch
            {
                "initialize" => Success(id, new JsonObject
                {
                    ["protocolVersion"] = "2025-06-18",
                    ["capabilities"] = new JsonObject
                    {
                        ["tools"] = new JsonObject { ["listChanged"] = true }
                    },
                    ["serverInfo"] = new JsonObject
                    {
                        ["name"] = "unity-mcp-bridge",
                        ["version"] = "0.3.0"
                    }
                }),
                "notifications/initialized" => null,
                "ping" => Success(id, new JsonObject()),
                "tools/list" => await HandleToolsListAsync(id, unity),
                "tools/call" => await HandleToolCallAsync(id, paramsNode, unity),
                _ => Error(id, -32601, $"Method '{method}' is not supported.")
            };
        }
        catch (Exception exception)
        {
            return Error(id, -32000, exception.Message);
        }
    }

    private static async Task<JsonObject> HandleToolsListAsync(JsonNode? id, UnityBridgeHub unity)
    {
        JsonArray primaryTools;
        try
        {
            primaryTools = await unity.GetPrimaryToolsAsync(TimeSpan.FromSeconds(10));
        }
        catch
        {
            primaryTools = new JsonArray();
        }

        var tools = new JsonArray
        {
            BuildInstancesToolDescriptor()
        };

        foreach (var tool in primaryTools)
        {
            if (tool is JsonObject toolObject)
            {
                tools.Add(AugmentToolDescriptor(toolObject));
            }
        }

        return Success(id, new JsonObject
        {
            ["tools"] = tools
        });
    }

    private static async Task<JsonObject?> HandleToolCallAsync(JsonNode? id, JsonNode? paramsNode, UnityBridgeHub unity)
    {
        if (paramsNode is not JsonObject parameters || parameters["name"]?.GetValue<string>() is not { } toolName)
        {
            return Error(id, -32602, "Missing tool name.");
        }

        if (toolName == InstancesToolName)
        {
            return Success(id, BuildInstancesToolResult(unity));
        }

        JsonObject? arguments = null;
        string? targetInstanceId = null;
        if (parameters["arguments"] is JsonObject rawArguments)
        {
            arguments = (JsonObject)rawArguments.DeepClone();
            if (arguments[UnityInstanceIdArgumentName] is JsonNode targetNode)
            {
                targetInstanceId = targetNode.GetValue<string>();
                arguments.Remove(UnityInstanceIdArgumentName);
            }
        }

        if (IsPrimaryOnlyTool(toolName) && !string.IsNullOrWhiteSpace(targetInstanceId))
        {
            var targetInstance = unity.TryGetInstance(targetInstanceId);
            if (targetInstance == null)
            {
                return Error(id, -32002, $"Unity instance '{targetInstanceId}' is not connected.");
            }

            if (!targetInstance.IsPrimary)
            {
                return Error(id, -32003, $"{toolName} can only run on the primary Unity project instance.");
            }
        }

        var timeout = toolName switch
        {
            CompileToolName => TimeSpan.FromMinutes(5),
            TestsRunToolName => TimeSpan.FromMinutes(15),
            _ => TimeSpan.FromSeconds(30)
        };
        var resilience = GetRequestResilience(toolName);
        var response = await ForwardAsync(unity, id, "tools/call", toolName, arguments, targetInstanceId, timeout, resilience);
        return response;
    }

    private static async Task<JsonObject?> ForwardAsync(
        UnityBridgeHub unity,
        JsonNode? rpcId,
        string method,
        string? toolName,
        JsonObject? arguments,
        string? targetInstanceId,
        TimeSpan timeout,
        RequestResilience resilience = RequestResilience.None)
    {
        var envelope = new JsonObject
        {
            ["type"] = "request",
            ["id"] = Guid.NewGuid().ToString("N"),
            ["method"] = method
        };
        if (toolName != null) envelope["toolName"] = toolName;
        if (arguments != null) envelope["arguments"] = arguments;

        var deadlineUtc = DateTime.UtcNow + timeout;
        JsonObject? reply;
        while (true)
        {
            var remaining = deadlineUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                return Error(rpcId, -32001, "Unity bridge timed out.");
            }

            reply = await unity.SendRequestAsync(envelope, targetInstanceId, remaining, resilience);
            if (reply == null)
            {
                return Error(rpcId, -32001, "Unity bridge timed out.");
            }

            if (!ShouldRetryToolCall(toolName, reply, out var retryDelay))
            {
                break;
            }

            var boundedDelay = retryDelay < remaining ? retryDelay : remaining;
            if (boundedDelay > TimeSpan.Zero)
            {
                await Task.Delay(boundedDelay);
            }
        }

        var success = reply["success"]?.GetValue<bool>() ?? false;
        if (method == "tools/call")
        {
            var result = reply["result"] as JsonObject;
            var text = result?["text"]?.GetValue<string>() ??
                       (success ? string.Empty : reply["error"]?.GetValue<string>() ?? "Tool call failed.");
            var payload = new JsonObject
            {
                ["content"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "text",
                        ["text"] = text
                    }
                },
                ["isError"] = !success
            };

            if (result?["structuredContent"] is JsonNode structured)
            {
                payload["structuredContent"] = structured.DeepClone();
            }

            return Success(rpcId, payload);
        }

        if (!success)
        {
            return Error(rpcId, -32000, reply["error"]?.GetValue<string>() ?? "Unity bridge returned an error.");
        }

        return Success(rpcId, reply["result"]?.DeepClone() ?? new JsonObject());
    }

    private static bool ShouldRetryToolCall(string? toolName, JsonObject reply, out TimeSpan retryDelay)
    {
        retryDelay = TimeSpan.FromMilliseconds(750);
        if (!string.Equals(toolName, TestsRunToolName, StringComparison.Ordinal))
        {
            return false;
        }

        if (reply["success"]?.GetValue<bool>() != true)
        {
            return false;
        }

        if (reply["result"] is not JsonObject result ||
            result["structuredContent"] is not JsonObject structuredContent)
        {
            return false;
        }

        if (structuredContent["ok"]?.GetValue<bool>() != false)
        {
            return false;
        }

        var status = structuredContent["status"]?.GetValue<string>() ?? string.Empty;
        return string.Equals(status, "retry", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(status, "busy", StringComparison.OrdinalIgnoreCase);
    }

    private static RequestResilience GetRequestResilience(string toolName)
    {
        return toolName switch
        {
            CompileToolName => RequestResilience.WaitForReconnect,
            "unity.enter_play_mode" => RequestResilience.WaitForReconnectAndReplay,
            "unity.exit_play_mode" => RequestResilience.WaitForReconnectAndReplay,
            TestsRunToolName => RequestResilience.WaitForReconnect,
            _ => RequestResilience.None
        };
    }

    private static JsonObject BuildInstancesToolDescriptor()
    {
        return new JsonObject
        {
            ["name"] = InstancesToolName,
            ["title"] = "List Unity Instances",
            ["description"] = "Lists connected Unity editor instances and indicates which instance is the primary project default target.",
            ["inputSchema"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject(),
                ["required"] = new JsonArray()
            }
        };
    }

    private static JsonObject BuildInstancesToolResult(UnityBridgeHub unity)
    {
        var instances = new JsonArray();
        foreach (var instance in unity.GetInstances())
        {
            instances.Add(new JsonObject
            {
                ["instanceId"] = instance.InstanceId,
                ["processId"] = instance.ProcessId,
                ["projectPath"] = instance.ProjectPath,
                ["projectName"] = instance.ProjectName,
                ["productName"] = instance.ProductName,
                ["isPrimary"] = instance.IsPrimary
            });
        }

        return new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["text"] = $"Connected Unity instances: {instances.Count}"
                }
            },
            ["isError"] = false,
            ["structuredContent"] = new JsonObject
            {
                ["ok"] = true,
                ["instances"] = instances
            }
        };
    }

    private static JsonObject AugmentToolDescriptor(JsonObject tool)
    {
        var clone = (JsonObject)tool.DeepClone();
        if (clone["inputSchema"] is not JsonObject schema)
        {
            schema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject(),
                ["required"] = new JsonArray()
            };
            clone["inputSchema"] = schema;
        }

        if (schema["properties"] is not JsonObject properties)
        {
            properties = new JsonObject();
            schema["properties"] = properties;
        }

        var toolName = clone["name"]?.GetValue<string>() ?? string.Empty;
        if (!IsPrimaryOnlyTool(toolName))
        {
            properties[UnityInstanceIdArgumentName] = new JsonObject
            {
                ["type"] = "string",
                ["description"] = "Optional explicit Unity instance id. If omitted, the command routes to the primary project instance."
            };
        }

        return clone;
    }

    private static bool IsPrimaryOnlyTool(string toolName)
    {
        return toolName == CompileToolName ||
               toolName == "unity.enter_play_mode" ||
               toolName == "unity.exit_play_mode" ||
               toolName == TestsRunToolName ||
               toolName == "unity.package_add" ||
               toolName == "unity.package_remove" ||
               toolName == "unity.script_write" ||
               toolName == "unity.script_delete" ||
               toolName == "unity.mppm_status" ||
               toolName == "unity.mppm_players" ||
               toolName == "unity.mppm_set_active" ||
               toolName == "unity.mppm_configure" ||
               toolName == "unity.mppm_player_set_active" ||
               toolName == "unity.mppm_tag_add" ||
               toolName == "unity.mppm_tag_remove" ||
               toolName == "unity.mppm_player_tags_set";
    }

    private static JsonObject Success(JsonNode? id, JsonNode result)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["result"] = result
        };
    }

    private static JsonObject Error(JsonNode? id, int code, string message)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id?.DeepClone(),
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
    }
}

[Flags]
internal enum RequestResilience
{
    None = 0,
    WaitForReconnect = 1,
    ReplayOnReconnect = 2,
    WaitForReconnectAndReplay = WaitForReconnect | ReplayOnReconnect
}

internal sealed class SessionManager
{
    private readonly ConcurrentDictionary<string, Session> _sessions = new();

    public string Create()
    {
        var id = Guid.NewGuid().ToString("N");
        _sessions[id] = new Session();
        return id;
    }

    public bool Touch(string id) => _sessions.ContainsKey(id);

    public void Drop(string id)
    {
        if (_sessions.TryRemove(id, out var session))
        {
            session.Close();
        }
    }

    public async Task AttachStreamAsync(string id, HttpListenerResponse response)
    {
        if (!_sessions.TryGetValue(id, out var session))
        {
            response.StatusCode = 404;
            response.Close();
            return;
        }

        await session.HoldAsync(response);
    }

    public void Broadcast(JsonObject notification)
    {
        var payload = notification.ToJsonString();
        foreach (var session in _sessions.Values)
        {
            session.Send(payload);
        }
    }

    private sealed class Session
    {
        private readonly List<HttpListenerResponse> _streams = new();
        private readonly object _gate = new();
        private readonly TaskCompletionSource _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task HoldAsync(HttpListenerResponse response)
        {
            lock (_gate) { _streams.Add(response); }
            await _closed.Task;
            try { response.Close(); } catch { }
            lock (_gate) { _streams.Remove(response); }
        }

        public void Send(string jsonPayload)
        {
            var frame = Encoding.UTF8.GetBytes($"event: message\ndata: {jsonPayload}\n\n");
            lock (_gate)
            {
                foreach (var stream in _streams.ToArray())
                {
                    try
                    {
                        stream.OutputStream.Write(frame, 0, frame.Length);
                        stream.OutputStream.Flush();
                    }
                    catch
                    {
                        try { stream.Close(); } catch { }
                        _streams.Remove(stream);
                    }
                }
            }
        }

        public void Close()
        {
            _closed.TrySetResult();
        }
    }
}

internal sealed class UnityBridgeHub
{
    private static readonly TimeSpan ReconnectGracePeriod = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ReconnectTimeout = TimeSpan.FromSeconds(30);
    private readonly int _port;
    private readonly string _primaryProjectPath;
    private readonly ConcurrentDictionary<string, PendingRequest> _pending = new();
    private readonly ConcurrentDictionary<string, UnityConnection> _connections = new();
    private readonly ConcurrentDictionary<TcpClient, string> _socketToInstance = new();
    private readonly object _connectionSignalGate = new();
    private TaskCompletionSource<bool> _connectionSignal = CreateConnectionSignal();

    public UnityBridgeHub(int port, string primaryProjectPath)
    {
        _port = port;
        _primaryProjectPath = NormalizePath(primaryProjectPath);
    }

    public event Action<JsonObject>? NotificationReceived;

    public void Start()
    {
        var listener = new TcpListener(IPAddress.Loopback, _port);
        listener.Start();
        _ = Task.Run(async () =>
        {
            while (true)
            {
                var client = await listener.AcceptTcpClientAsync();
                _ = Task.Run(() => ReadLoopAsync(client));
            }
        });
    }

    public async Task<JsonArray> GetPrimaryToolsAsync(TimeSpan timeout)
    {
        var reply = await SendRequestAsync(new JsonObject
        {
            ["type"] = "request",
            ["id"] = Guid.NewGuid().ToString("N"),
            ["method"] = "tools/list"
        }, null, timeout, RequestResilience.WaitForReconnectAndReplay);

        if (reply?["success"]?.GetValue<bool>() != true)
        {
            return new JsonArray();
        }

        return reply["result"] as JsonArray ?? new JsonArray();
    }

    public IReadOnlyList<UnityInstanceSnapshot> GetInstances()
    {
        return _connections.Values
            .Select(connection => new UnityInstanceSnapshot
            {
                InstanceId = connection.InstanceId,
                ProcessId = connection.ProcessId,
                ProjectPath = connection.ProjectPath,
                ProjectName = connection.ProjectName,
                ProductName = connection.ProductName,
                IsPrimary = connection.IsPrimary(_primaryProjectPath)
            })
            .OrderByDescending(instance => instance.IsPrimary)
            .ThenBy(instance => instance.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public UnityInstanceSnapshot? TryGetInstance(string instanceId)
    {
        if (!_connections.TryGetValue(instanceId, out var connection))
        {
            return null;
        }

        return new UnityInstanceSnapshot
        {
            InstanceId = connection.InstanceId,
            ProcessId = connection.ProcessId,
            ProjectPath = connection.ProjectPath,
            ProjectName = connection.ProjectName,
            ProductName = connection.ProductName,
            IsPrimary = connection.IsPrimary(_primaryProjectPath)
        };
    }

    public async Task<JsonObject?> SendRequestAsync(JsonObject envelope, string? targetInstanceId, TimeSpan timeout, RequestResilience resilience)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var reconnectCts = CreateReconnectWaitCancellationSource(timeoutCts.Token, resilience, timeout);
        var waitToken = reconnectCts?.Token ?? timeoutCts.Token;
        var connection = await WaitForTargetAsync(targetInstanceId, resilience, waitToken);
        if (connection == null)
        {
            throw new InvalidOperationException(targetInstanceId == null
                ? "No Unity instance is connected to the MCP bridge before the request timeout."
                : $"Unity instance '{targetInstanceId}' did not reconnect before the request timeout.");
        }

        var requestId = envelope["id"]!.GetValue<string>();
        if (TryGetInFlightRequest(connection.InstanceId, out var inFlight))
        {
            throw new InvalidOperationException(
                $"Unity instance '{connection.InstanceId}' is already handling '{inFlight.ToolName}' " +
                $"for {inFlight.Elapsed.TotalSeconds:0.0}s. The editor may be blocked by a modal dialog, " +
                "a Play Mode transition, or a previous stalled request.");
        }

        var pending = new PendingRequest(connection.InstanceId, envelope, targetInstanceId, resilience);
        _pending[requestId] = pending;

        if (!connection.TrySend(envelope))
        {
            _pending.TryRemove(requestId, out _);
            throw new InvalidOperationException($"Failed to send request to Unity instance '{connection.InstanceId}'.");
        }

        using (timeoutCts.Token.Register(() => pending.TrySetTimeout()))
        {
            var result = await pending.Task;
            _pending.TryRemove(requestId, out _);
            return result;
        }
    }

    private static CancellationTokenSource? CreateReconnectWaitCancellationSource(
        CancellationToken parentToken,
        RequestResilience resilience,
        TimeSpan requestTimeout)
    {
        if ((resilience & RequestResilience.WaitForReconnect) == 0 ||
            requestTimeout <= ReconnectTimeout)
        {
            return null;
        }

        var reconnectCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken);
        reconnectCts.CancelAfter(ReconnectTimeout);
        return reconnectCts;
    }

    private async Task ReadLoopAsync(TcpClient client)
    {
        try
        {
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            while (true)
            {
                var line = await reader.ReadLineAsync();
                if (line == null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (JsonNode.Parse(line) is not JsonObject node)
                {
                    continue;
                }

                var type = node["type"]?.GetValue<string>();
                switch (type)
                {
                    case "hello":
                        RegisterConnection(client, node);
                        break;
                    case "response":
                        if (node["id"]?.GetValue<string>() is { } id &&
                            _pending.TryRemove(id, out var pending))
                        {
                            pending.TrySetResult(node);
                        }
                        break;
                    case "notification":
                        var method = node["method"]?.GetValue<string>() ?? string.Empty;
                        var jsonRpc = new JsonObject
                        {
                            ["jsonrpc"] = "2.0",
                            ["method"] = method
                        };
                        if (node["params"] is JsonNode parameters)
                        {
                            jsonRpc["params"] = parameters.DeepClone();
                        }
                        NotificationReceived?.Invoke(jsonRpc);
                        break;
                }
            }
        }
        catch
        {
        }
        finally
        {
            UnregisterConnection(client);
            try { client.Dispose(); } catch { }
        }
    }

    private void RegisterConnection(TcpClient client, JsonObject hello)
    {
        string instanceId;
        int processId;
        string projectPath;
        string primaryProjectPath;
        string projectName;
        string productName;

        if (hello["instance"] is JsonObject instance)
        {
            instanceId = instance["instanceId"]?.GetValue<string>() ?? string.Empty;
            processId = instance["processId"]?.GetValue<int>() ?? 0;
            projectPath = instance["projectPath"]?.GetValue<string>() ?? string.Empty;
            primaryProjectPath = instance["primaryProjectPath"]?.GetValue<string>() ?? string.Empty;
            projectName = instance["projectName"]?.GetValue<string>() ?? string.Empty;
            productName = instance["productName"]?.GetValue<string>() ?? string.Empty;
        }
        else
        {
            instanceId = $"legacy:{client.Client.RemoteEndPoint}";
            processId = 0;
            projectPath = $"[legacy] {client.Client.RemoteEndPoint}";
            primaryProjectPath = string.Empty;
            projectName = "Legacy Unity Instance";
            productName = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(projectPath))
        {
            projectPath = $"[unknown] {instanceId}";
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            projectName = Path.GetFileName(projectPath);
        }

        if (instanceId.StartsWith("legacy:", StringComparison.Ordinal))
        {
            Console.WriteLine($"Ignoring legacy Unity connection '{instanceId}' because it does not declare a primary project path.");
            try { client.Dispose(); } catch { }
            return;
        }

        if (string.IsNullOrWhiteSpace(primaryProjectPath))
        {
            primaryProjectPath = projectPath;
        }

        var normalizedPrimaryProjectPath = NormalizePath(primaryProjectPath);
        if (!PathsEqual(normalizedPrimaryProjectPath, _primaryProjectPath))
        {
            Console.WriteLine(
                $"Ignoring Unity instance '{instanceId}' from unrelated project family '{normalizedPrimaryProjectPath}'. Expected '{_primaryProjectPath}'.");
            try { client.Dispose(); } catch { }
            return;
        }

        var connection = new UnityConnection(
            instanceId,
            processId,
            projectPath,
            normalizedPrimaryProjectPath,
            projectName,
            productName,
            client,
            new StreamWriter(client.GetStream(), new UTF8Encoding(false)) { AutoFlush = true });

        if (_connections.TryGetValue(instanceId, out var previous))
        {
            previous.Dispose();
        }

        _connections[instanceId] = connection;
        _socketToInstance[client] = instanceId;
        SignalConnectionChanged();
        ResumePendingRequests(instanceId);
        RetryPendingRequests(instanceId);

        Console.WriteLine($"Unity connected: {instanceId} ({projectPath})");
    }

    private void UnregisterConnection(TcpClient client)
    {
        if (_socketToInstance.TryRemove(client, out var instanceId) &&
            _connections.TryRemove(instanceId, out var connection))
        {
            FailPendingRequests(instanceId);
            connection.Dispose();
            SignalConnectionChanged();
            Console.WriteLine($"Unity disconnected: {instanceId}");
        }
    }

    private void FailPendingRequests(string instanceId)
    {
        foreach (var pair in _pending)
        {
            if (pair.Value.InstanceId != instanceId)
            {
                continue;
            }

            pair.Value.TryHandleDisconnect(instanceId, ReconnectGracePeriod, ReconnectTimeout);
        }
    }

    private void ResumePendingRequests(string instanceId)
    {
        foreach (var pair in _pending)
        {
            pair.Value.TryHandleReconnect(instanceId);
        }
    }

    private void RetryPendingRequests(string instanceId)
    {
        foreach (var pair in _pending)
        {
            var pending = pair.Value;
            if (!pending.ShouldReplayOnReconnect(instanceId))
            {
                continue;
            }

            var connection = ResolveTarget(pending.TargetInstanceId);
            if (connection == null)
            {
                continue;
            }

            if (connection.TrySend((JsonObject)pending.Envelope.DeepClone()))
            {
                pending.MarkReplayed(connection.InstanceId);
            }
        }
    }

    private UnityConnection? ResolveTarget(string? targetInstanceId)
    {
        if (!string.IsNullOrWhiteSpace(targetInstanceId))
        {
            return _connections.TryGetValue(targetInstanceId, out var explicitConnection) ? explicitConnection : null;
        }

        var primary = _connections.Values.FirstOrDefault(connection =>
            PathsEqual(connection.ProjectPath, _primaryProjectPath));
        if (primary != null)
        {
            return primary;
        }

        return _connections.Values
            .OrderBy(connection => connection.ProjectPath, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();
    }

    private async Task<UnityConnection?> WaitForTargetAsync(string? targetInstanceId, RequestResilience resilience, CancellationToken cancellationToken)
    {
        var connection = ResolveTarget(targetInstanceId);
        if (connection != null || resilience == RequestResilience.None)
        {
            return connection;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            Task waitTask;
            lock (_connectionSignalGate)
            {
                waitTask = _connectionSignal.Task;
            }

            var completed = await Task.WhenAny(waitTask, Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();

            connection = ResolveTarget(targetInstanceId);
            if (connection != null)
            {
                return connection;
            }

            if (completed == waitTask)
            {
                lock (_connectionSignalGate)
                {
                    if (_connectionSignal.Task.IsCompleted)
                    {
                        _connectionSignal = CreateConnectionSignal();
                    }
                }
            }
        }

        return null;
    }

    private bool TryGetInFlightRequest(string instanceId, out PendingRequest pendingRequest)
    {
        foreach (var pair in _pending)
        {
            var pending = pair.Value;
            if (pending.Task.IsCompleted || !string.Equals(pending.InstanceId, instanceId, StringComparison.Ordinal))
            {
                continue;
            }

            pendingRequest = pending;
            return true;
        }

        pendingRequest = null!;
        return false;
    }

    private static string NormalizePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static bool PathsEqual(string left, string right)
    {
        return string.Equals(NormalizePath(left), NormalizePath(right), StringComparison.OrdinalIgnoreCase);
    }

    private void SignalConnectionChanged()
    {
        lock (_connectionSignalGate)
        {
            _connectionSignal.TrySetResult(true);
            _connectionSignal = CreateConnectionSignal();
        }
    }

    private static TaskCompletionSource<bool> CreateConnectionSignal()
    {
        return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class UnityConnection : IDisposable
    {
        private readonly object _gate = new();
        private readonly TcpClient _client;
        private readonly StreamWriter _writer;

        public UnityConnection(string instanceId, int processId, string projectPath, string primaryProjectPath, string projectName, string productName, TcpClient client, StreamWriter writer)
        {
            InstanceId = instanceId;
            ProcessId = processId;
            ProjectPath = NormalizePath(projectPath);
            PrimaryProjectPath = NormalizePath(primaryProjectPath);
            ProjectName = projectName;
            ProductName = productName;
            _client = client;
            _writer = writer;
        }

        public string InstanceId { get; }
        public int ProcessId { get; }
        public string ProjectPath { get; }
        public string PrimaryProjectPath { get; }
        public string ProjectName { get; }
        public string ProductName { get; }

        public bool IsPrimary(string primaryProjectPath)
        {
            if (ProjectPath.StartsWith("[legacy]", StringComparison.Ordinal))
            {
                return false;
            }

            return PathsEqual(ProjectPath, primaryProjectPath);
        }

        public bool TrySend(JsonObject envelope)
        {
            lock (_gate)
            {
                try
                {
                    _writer.WriteLine(envelope.ToJsonString());
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public void Dispose()
        {
            try { _writer.Dispose(); } catch { }
            try { _client.Dispose(); } catch { }
        }
    }

    private sealed class PendingRequest
    {
        private readonly object _gate = new();
        private readonly TaskCompletionSource<JsonObject?> _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private CancellationTokenSource? _disconnectTimeoutCts;

        public PendingRequest(string instanceId, JsonObject envelope, string? targetInstanceId, RequestResilience resilience)
        {
            InstanceId = instanceId;
            Envelope = (JsonObject)envelope.DeepClone();
            TargetInstanceId = targetInstanceId;
            Resilience = resilience;
            ToolName = envelope["toolName"]?.GetValue<string>() ?? envelope["method"]?.GetValue<string>() ?? "unknown";
            StartedAtUtc = DateTime.UtcNow;
        }

        public string InstanceId { get; private set; }
        public string? TargetInstanceId { get; }
        public JsonObject Envelope { get; }
        public RequestResilience Resilience { get; }
        public string ToolName { get; }
        public DateTime StartedAtUtc { get; }
        public TimeSpan Elapsed => DateTime.UtcNow - StartedAtUtc;
        public Task<JsonObject?> Task => _tcs.Task;
        private bool WaitingForReconnect => (Resilience & RequestResilience.WaitForReconnect) != 0;
        private bool ReplayOnReconnect => (Resilience & RequestResilience.ReplayOnReconnect) != 0;

        public void TrySetResult(JsonObject result)
        {
            ClearDisconnectWait();
            _tcs.TrySetResult(result);
        }

        public void TrySetTimeout()
        {
            ClearDisconnectWait();
            _tcs.TrySetResult(null);
        }

        public bool TryHandleDisconnect(string instanceId, TimeSpan reconnectGracePeriod, TimeSpan reconnectTimeout)
        {
            if (!WaitingForReconnect)
            {
                ClearDisconnectWait();
                _tcs.TrySetResult(new JsonObject
                {
                    ["type"] = "response",
                    ["success"] = false,
                    ["error"] = BuildDisconnectError(instanceId)
                });
                return false;
            }

            lock (_gate)
            {
                if (_tcs.Task.IsCompleted)
                {
                    return false;
                }

                if (_disconnectTimeoutCts != null)
                {
                    return true;
                }

                _disconnectTimeoutCts = new CancellationTokenSource();
                _ = MonitorReconnectAsync(instanceId, reconnectGracePeriod, reconnectTimeout, _disconnectTimeoutCts.Token);
            }

            return true;
        }

        public void TryHandleReconnect(string instanceId)
        {
            if (!string.Equals(InstanceId, instanceId, StringComparison.Ordinal))
            {
                return;
            }

            ClearDisconnectWait();
        }

        public bool ShouldReplayOnReconnect(string instanceId)
        {
            return ReplayOnReconnect &&
                   !_tcs.Task.IsCompleted &&
                   string.Equals(InstanceId, instanceId, StringComparison.Ordinal);
        }

        public void MarkReplayed(string instanceId)
        {
            InstanceId = instanceId;
        }

        private async Task MonitorReconnectAsync(
            string instanceId,
            TimeSpan reconnectGracePeriod,
            TimeSpan reconnectTimeout,
            CancellationToken cancellationToken)
        {
            try
            {
                if (reconnectGracePeriod > TimeSpan.Zero)
                {
                    await global::System.Threading.Tasks.Task.Delay(reconnectGracePeriod, cancellationToken);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine(
                        $"Unity instance '{instanceId}' disconnected while handling '{ToolName}'. Waiting up to {reconnectTimeout.TotalSeconds:0}s for reconnect.");
                }

                var remainingTimeout = reconnectTimeout - reconnectGracePeriod;
                if (remainingTimeout > TimeSpan.Zero)
                {
                    await global::System.Threading.Tasks.Task.Delay(remainingTimeout, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                lock (_gate)
                {
                    if (_disconnectTimeoutCts == null || _disconnectTimeoutCts.Token != cancellationToken)
                    {
                        return;
                    }

                    _disconnectTimeoutCts.Dispose();
                    _disconnectTimeoutCts = null;
                }

                _tcs.TrySetResult(new JsonObject
                {
                    ["type"] = "response",
                    ["success"] = false,
                    ["error"] = BuildDisconnectError(instanceId)
                });
            }
            catch (OperationCanceledException)
            {
            }
        }

        private void ClearDisconnectWait()
        {
            CancellationTokenSource? toDispose = null;
            lock (_gate)
            {
                toDispose = _disconnectTimeoutCts;
                _disconnectTimeoutCts = null;
            }

            if (toDispose == null)
            {
                return;
            }

            try { toDispose.Cancel(); } catch { }
            toDispose.Dispose();
        }

        private static string BuildDisconnectError(string instanceId)
        {
            return $"Unity instance '{instanceId}' disconnected while handling the request. This usually happens during Play Mode transitions or domain reload. Retry after the editor reconnects.";
        }
    }
}

internal sealed class UnityInstanceSnapshot
{
    public string InstanceId { get; init; } = string.Empty;
    public int ProcessId { get; init; }
    public string ProjectPath { get; init; } = string.Empty;
    public string ProjectName { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
}

internal sealed class HostOptions
{
    public int HttpPort { get; private init; }
    public int IpcPort { get; private init; }
    public string? PrimaryProjectPath { get; private init; }

    public static HostOptions Parse(string[] args)
    {
        var httpPort = 63811;
        var ipcPort = 63812;
        string? primaryProjectPath = null;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--http-port":
                    httpPort = int.Parse(args[++index]);
                    break;
                case "--ipc-port":
                    ipcPort = int.Parse(args[++index]);
                    break;
                case "--primary-project-path":
                    primaryProjectPath = args[++index];
                    break;
            }
        }

        return new HostOptions
        {
            HttpPort = httpPort,
            IpcPort = ipcPort,
            PrimaryProjectPath = primaryProjectPath
        };
    }
}
