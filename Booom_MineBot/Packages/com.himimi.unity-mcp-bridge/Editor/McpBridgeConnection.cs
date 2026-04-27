using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace McpBridge.Editor
{
    [InitializeOnLoad]
    internal static class McpBridgeConnection
    {
        private static readonly object s_SendGate = new();
        private static TcpClient s_Client;
        private static StreamWriter s_Writer;
        private static CancellationTokenSource s_Cts;
        private static int s_ConnectInFlight;

        private static double s_LastHeartbeat;

        static McpBridgeConnection()
        {
            EditorApplication.delayCall += EnsureConnected;
            EditorApplication.update += Heartbeat;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void Heartbeat()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - s_LastHeartbeat < 2.0) return;
            s_LastHeartbeat = now;
            var settings = McpBridgeSettings.instance;
            settings.EnsureProjectScopedDefaults();
            if (!settings.Enabled) return;
            if (IsConnected) return;
            EnsureConnected();
        }

        public static bool IsConnected => s_Client is { Connected: true };

        public static void EnsureConnected()
        {
            var settings = McpBridgeSettings.instance;
            settings.EnsureProjectScopedDefaults();
            if (!settings.Enabled) return;
            if (IsConnected) return;
            if (Interlocked.Exchange(ref s_ConnectInFlight, 1) == 1) return;
            _ = ConnectLoopAsync();
        }

        public static bool TrySendResponse(BridgeEnvelope envelope) => SendEnvelope(envelope);

        public static void NotifyToolsChanged()
        {
            SendEnvelope(new BridgeEnvelope
            {
                Type = "notification",
                Method = "notifications/tools/list_changed",
                Params = new Dictionary<string, object>()
            });
        }

        private static async Task ConnectLoopAsync()
        {
            int port;
            List<ToolDescriptor> descriptorsSnapshot;
            try
            {
                var settings = McpBridgeSettings.instance;
                settings.EnsureProjectScopedDefaults();
                port = settings.IpcPort;
                descriptorsSnapshot = McpToolRegistry.GetEnabledDescriptors();
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"[McpBridge] snapshot settings failed: {exception.Message}");
                Interlocked.Exchange(ref s_ConnectInFlight, 0);
                return;
            }

            try
            {
                s_Cts?.Dispose();
                s_Cts = new CancellationTokenSource();
                var token = s_Cts.Token;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var client = new TcpClient();
                        await client.ConnectAsync("127.0.0.1", port);
                        var stream = client.GetStream();
                        var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
                        lock (s_SendGate)
                        {
                            s_Client = client;
                            s_Writer = writer;
                        }

                        SendEnvelope(new BridgeEnvelope
                        {
                            Type = "hello",
                            Tools = descriptorsSnapshot,
                            Instance = McpInstanceIdentity.Get()
                        });
                        UnityEngine.Debug.Log("[McpBridge] hello sent");

                        _ = ListenAsync(stream, token);
                        EditorApplication.delayCall += McpCompileTracker.TryFlushPending;
                        return;
                    }
                    catch
                    {
                        try { await Task.Delay(1000, token); }
                        catch { return; }
                    }
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"[McpBridge] connect loop aborted: {exception.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref s_ConnectInFlight, 0);
            }
        }

        private static async Task ListenAsync(Stream stream, CancellationToken token)
        {
            try
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                while (!token.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (line == null) break;
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var envelope = BridgeEnvelope.FromDictionary(McpBridgeJson.Deserialize(line) as Dictionary<string, object>);
                    if (envelope?.Type != "request") continue;
                    _ = HandleRequestAsync(envelope);
                }
            }
            catch { }
            finally
            {
                UnityEngine.Debug.Log("[McpBridge] connection reset");
                ResetConnection();
                EditorApplication.delayCall += EnsureConnected;
            }
        }

        private static void OnBeforeAssemblyReload()
        {
            UnityEngine.Debug.Log("[McpBridge] before assembly reload");
            ResetConnection();
        }

        private static async Task HandleRequestAsync(BridgeEnvelope envelope)
        {
            try
            {
                switch (envelope.Method)
                {
                    case "tools/list":
                        SendEnvelope(new BridgeEnvelope
                        {
                            Type = "response",
                            Id = envelope.Id,
                            Success = true,
                            Result = McpToolRegistry.GetEnabledDescriptors()
                        });
                        break;
                    case "tools/call":
                        await HandleToolCallAsync(envelope);
                        break;
                    default:
                        SendEnvelope(new BridgeEnvelope
                        {
                            Type = "response",
                            Id = envelope.Id,
                            Success = false,
                            Error = $"Unsupported bridge method '{envelope.Method}'."
                        });
                        break;
                }
            }
            catch (Exception exception)
            {
                SendEnvelope(new BridgeEnvelope
                {
                    Type = "response",
                    Id = envelope.Id,
                    Success = false,
                    Error = exception.ToString()
                });
            }
        }

        private static async Task HandleToolCallAsync(BridgeEnvelope envelope)
        {
            if (!McpBridgeSettings.instance.IsToolEnabled(envelope.ToolName))
            {
                SendEnvelope(new BridgeEnvelope
                {
                    Type = "response",
                    Id = envelope.Id,
                    Success = false,
                    Error = $"Tool '{envelope.ToolName}' is disabled."
                });
                return;
            }

            if (envelope.ToolName == McpCompileTracker.ToolName)
            {
                var exitPlayMode = false;
                if (envelope.Arguments != null &&
                    envelope.Arguments.TryGetValue("exitPlayMode", out var rawExitPlayMode) &&
                    rawExitPlayMode is bool boolValue)
                {
                    exitPlayMode = boolValue;
                }

                var immediateResult = McpCompileTracker.Begin(envelope.Id, exitPlayMode);
                if (immediateResult != null)
                {
                    SendEnvelope(new BridgeEnvelope
                    {
                        Type = "response",
                        Id = envelope.Id,
                        Success = true,
                        Result = immediateResult.ToDictionary()
                    });
                }

                return;
            }

            if (envelope.ToolName == "unity.tests_run" &&
                IncludesPlayMode(envelope.Arguments, out var mode))
            {
                var immediateResult = McpPlayModeTestTracker.Begin(
                    envelope.Id,
                    mode,
                    GetStringArrayArgument(envelope.Arguments, "testNames"),
                    GetStringArrayArgument(envelope.Arguments, "groupNames"),
                    GetStringArrayArgument(envelope.Arguments, "assemblyNames"),
                    GetBoolArgument(envelope.Arguments, "runSynchronously"));

                if (immediateResult != null)
                {
                    SendEnvelope(new BridgeEnvelope
                    {
                        Type = "response",
                        Id = envelope.Id,
                        Success = true,
                        Result = immediateResult.ToDictionary()
                    });
                }

                return;
            }

            var result = await McpToolRegistry.InvokeAsync(envelope.ToolName, envelope.Arguments ?? new());
            SendEnvelope(new BridgeEnvelope
            {
                Type = "response",
                Id = envelope.Id,
                Success = true,
                Result = result
            });
        }

        private static bool SendEnvelope(BridgeEnvelope envelope)
        {
            var payload = McpBridgeJson.Serialize(envelope);
            lock (s_SendGate)
            {
                if (s_Writer == null) return false;
                try { s_Writer.WriteLine(payload); return true; }
                catch { return false; }
            }
        }

        private static void ResetConnection()
        {
            lock (s_SendGate)
            {
                try { s_Writer?.Dispose(); } catch { }
                s_Writer = null;
                try { s_Client?.Dispose(); } catch { }
                s_Client = null;
            }
            s_Cts?.Cancel();
            s_Cts?.Dispose();
            s_Cts = null;
        }

        private static bool IncludesPlayMode(Dictionary<string, object> arguments, out string mode)
        {
            mode = "edit";
            if (arguments == null || !arguments.TryGetValue("mode", out var rawMode) || rawMode == null)
            {
                return false;
            }

            mode = rawMode as string ?? rawMode.ToString();
            var normalized = string.IsNullOrWhiteSpace(mode) ? "edit" : mode.Trim().ToLowerInvariant();
            return normalized is "play" or "playmode" or "all";
        }

        private static bool GetBoolArgument(Dictionary<string, object> arguments, string key)
        {
            if (arguments == null || !arguments.TryGetValue(key, out var value) || value == null)
            {
                return false;
            }

            return value switch
            {
                bool boolValue => boolValue,
                _ when bool.TryParse(value.ToString(), out var parsed) => parsed,
                _ => false
            };
        }

        private static string[] GetStringArrayArgument(Dictionary<string, object> arguments, string key)
        {
            if (arguments == null || !arguments.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            if (value is string[] stringArray)
            {
                return stringArray.Length == 0 ? null : stringArray;
            }

            if (value is IEnumerable<object> objectEnumerable)
            {
                var values = new List<string>();
                foreach (var item in objectEnumerable)
                {
                    if (item == null)
                    {
                        continue;
                    }

                    values.Add(item.ToString());
                }

                return values.Count == 0 ? null : values.ToArray();
            }

            if (value is string singleValue)
            {
                return string.IsNullOrWhiteSpace(singleValue) ? null : new[] { singleValue };
            }

            return null;
        }
    }
}
