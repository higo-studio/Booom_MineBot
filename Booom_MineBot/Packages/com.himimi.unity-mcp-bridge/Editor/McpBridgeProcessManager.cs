using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace McpBridge.Editor
{
    [InitializeOnLoad]
    internal static class McpBridgeProcessManager
    {
        static McpBridgeProcessManager()
        {
            EditorApplication.delayCall += EnsureDesiredState;
        }

        public static bool IsHostRunning()
        {
            var state = LoadState();
            if (state == null || state.ProcessId <= 0) return false;
            try { return !Process.GetProcessById(state.ProcessId).HasExited; }
            catch { return false; }
        }

        public static void EnsureDesiredState()
        {
            var settings = McpBridgeSettings.instance;
            settings.EnsureProjectScopedDefaults();
            if (!settings.Enabled) { StopHost(); return; }
            if (!settings.AutoStartHost || IsHostRunning())
            {
                McpBridgeConnection.EnsureConnected();
                return;
            }
            StartHost();
        }

        public static void StartHost()
        {
            if (!File.Exists(McpBridgePaths.HostProjectPath))
            {
                UnityEngine.Debug.LogWarning($"MCP host project missing: '{McpBridgePaths.HostProjectPath}'.");
                return;
            }

            McpBridgeSettings.instance.EnsureProjectScopedDefaults();

            if (IsHostRunning())
            {
                McpBridgeConnection.EnsureConnected();
                return;
            }

            var dotnet = ResolveDotnetPath();
            var launch = EnsurePublished(dotnet);
            if (launch == null) return;

            var settings = McpBridgeSettings.instance;
            KillOrphanHosts(settings.HttpPort, settings.IpcPort);
            var startInfo = new ProcessStartInfo
            {
                FileName = launch.FileName,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = launch.WorkingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in launch.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }
            startInfo.ArgumentList.Add("--http-port");
            startInfo.ArgumentList.Add(settings.HttpPort.ToString());
            startInfo.ArgumentList.Add("--ipc-port");
            startInfo.ArgumentList.Add(settings.IpcPort.ToString());
            startInfo.ArgumentList.Add("--primary-project-path");
            startInfo.ArgumentList.Add(McpInstanceIdentity.Get().PrimaryProjectPath);

            var process = Process.Start(startInfo);
            if (process == null) return;

            process.OutputDataReceived += (_, args) => { if (!string.IsNullOrWhiteSpace(args.Data)) UnityEngine.Debug.Log($"[McpBridge.Host] {args.Data}"); };
            process.ErrorDataReceived += (_, args) => { if (!string.IsNullOrWhiteSpace(args.Data)) UnityEngine.Debug.LogWarning($"[McpBridge.Host] {args.Data}"); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            SaveState(new HostState { ProcessId = process.Id });
            McpBridgeConnection.EnsureConnected();

            if (settings.AutoWriteCodexConfig)
            {
                McpCodexConfigWriter.Write(settings.CodexServerName, settings.HttpUrl);
            }
        }

        public static void StopHost()
        {
            var state = LoadState();
            if (state != null)
            {
                try
                {
                    var process = Process.GetProcessById(state.ProcessId);
                    if (!process.HasExited) process.Kill();
                }
                catch { }
            }
            if (File.Exists(McpBridgePaths.HostStatePath))
            {
                try { File.Delete(McpBridgePaths.HostStatePath); } catch { }
            }
        }

        private static HostLaunchInfo EnsurePublished(string dotnet)
        {
            var publishRoot = McpBridgePaths.HostPublishRoot;
            Directory.CreateDirectory(publishRoot);

            var preferredRid = GetPreferredRuntimeIdentifier();
            var preferredPublishDir = Path.Combine(publishRoot, preferredRid);
            Directory.CreateDirectory(preferredPublishDir);

            var preferredLaunch = TryBuildExistingLaunchInfo(dotnet, preferredPublishDir);
            if (preferredLaunch != null)
            {
                return preferredLaunch;
            }

            if (string.IsNullOrEmpty(dotnet))
            {
                return TryBuildExistingFrameworkDependentLaunch(dotnet, publishRoot);
            }

            UnityEngine.Debug.Log($"[McpBridge] Publishing host project for RID '{preferredRid}'...");
            var info = new ProcessStartInfo
            {
                FileName = dotnet,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            info.ArgumentList.Add("publish");
            info.ArgumentList.Add(McpBridgePaths.HostProjectPath);
            info.ArgumentList.Add("-c");
            info.ArgumentList.Add("Release");
            info.ArgumentList.Add("-r");
            info.ArgumentList.Add(preferredRid);
            info.ArgumentList.Add("--self-contained");
            info.ArgumentList.Add("true");
            info.ArgumentList.Add("/p:PublishSingleFile=true");
            info.ArgumentList.Add("-o");
            info.ArgumentList.Add(preferredPublishDir);
            info.ArgumentList.Add("--nologo");

            try
            {
                using var process = Process.Start(info);
                if (process == null) return null;
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    UnityEngine.Debug.LogError($"[McpBridge] publish failed:\n{stdout}\n{stderr}");
                    return null;
                }
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError($"[McpBridge] publish error: {exception}");
                return null;
            }

            var launch = TryBuildExistingLaunchInfo(dotnet, preferredPublishDir);
            if (launch != null)
            {
                return launch;
            }

            return TryBuildExistingFrameworkDependentLaunch(dotnet, publishRoot);
        }

        private static HostLaunchInfo TryBuildExistingLaunchInfo(string dotnet, string publishDir)
        {
            var dllPath = Path.Combine(publishDir, "UnityMcpBridge.Host.dll");
            var executablePath = GetHostExecutablePath(publishDir);

            if (!IsPublishedArtifactCurrent(dllPath) && !IsPublishedArtifactCurrent(executablePath))
            {
                return null;
            }

            if (File.Exists(executablePath))
            {
                return new HostLaunchInfo(executablePath, publishDir);
            }

            return BuildFrameworkDependentLaunch(dotnet, publishDir, dllPath);
        }

        private static HostLaunchInfo TryBuildExistingFrameworkDependentLaunch(string dotnet, string publishRoot)
        {
            var dllPath = Path.Combine(publishRoot, "UnityMcpBridge.Host.dll");
            if (!IsPublishedArtifactCurrent(dllPath))
            {
                return null;
            }

            return BuildFrameworkDependentLaunch(dotnet, publishRoot, dllPath);
        }

        private static HostLaunchInfo BuildFrameworkDependentLaunch(string dotnet, string workingDirectory, string dllPath)
        {
            if (!File.Exists(dllPath))
            {
                return null;
            }

            if (string.IsNullOrEmpty(dotnet))
            {
                UnityEngine.Debug.LogError("A framework-dependent host was published but no dotnet runtime was found.");
                return null;
            }

            return new HostLaunchInfo(dotnet, workingDirectory, dllPath);
        }

        private static bool IsPublishedArtifactCurrent(string path)
        {
            return File.Exists(path) &&
                   File.GetLastWriteTimeUtc(path) >= File.GetLastWriteTimeUtc(McpBridgePaths.HostProjectPath) &&
                   File.GetLastWriteTimeUtc(path) >= File.GetLastWriteTimeUtc(McpBridgePaths.HostProgramPath);
        }

        private static string GetHostExecutablePath(string publishDir)
        {
            var fileName = UnityEngine.Application.platform == RuntimePlatform.WindowsEditor
                ? "UnityMcpBridge.Host.exe"
                : "UnityMcpBridge.Host";
            return Path.Combine(publishDir, fileName);
        }

        private static string GetPreferredRuntimeIdentifier()
        {
            return Application.platform switch
            {
                RuntimePlatform.OSXEditor => SystemInfo.processorType.Contains("Apple", StringComparison.OrdinalIgnoreCase) ||
                                             SystemInfo.processorType.Contains("arm", StringComparison.OrdinalIgnoreCase)
                    ? "osx-arm64"
                    : "osx-x64",
                RuntimePlatform.WindowsEditor => "win-x64",
                _ => "osx-arm64"
            };
        }

        private static HostState LoadState()
        {
            if (!File.Exists(McpBridgePaths.HostStatePath)) return null;
            try { return McpBridgeJson.DeserializeObject<HostState>(File.ReadAllText(McpBridgePaths.HostStatePath)); }
            catch { return null; }
        }

        private static void SaveState(HostState state)
        {
            try { File.WriteAllText(McpBridgePaths.HostStatePath, McpBridgeJson.SerializeObject(state)); }
            catch { }
        }

        private static void KillOrphanHosts(int httpPort, int ipcPort)
        {
            var knownPid = LoadState()?.ProcessId ?? -1;
            foreach (var pid in FindPortHolders(httpPort, ipcPort))
            {
                if (pid == knownPid) continue;
                try
                {
                    var process = Process.GetProcessById(pid);
                    var name = process.ProcessName ?? string.Empty;
                    var matchesHost = name.IndexOf("UnityMcpBridge", StringComparison.OrdinalIgnoreCase) >= 0
                        || name.Equals("dotnet", StringComparison.OrdinalIgnoreCase);
                    if (!matchesHost) continue;
                    if (!process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                        UnityEngine.Debug.Log($"[McpBridge] Killed orphan host pid={pid} ({name}).");
                    }
                }
                catch { }
            }
        }

        private static IEnumerable<int> FindPortHolders(params int[] ports)
        {
            var seen = new HashSet<int>();
            var platform = Environment.OSVersion.Platform;
            if (platform is PlatformID.Unix or PlatformID.MacOSX)
            {
                foreach (var port in ports)
                {
                    var info = new ProcessStartInfo("lsof")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    info.ArgumentList.Add("-nP");
                    info.ArgumentList.Add("-iTCP:" + port);
                    info.ArgumentList.Add("-sTCP:LISTEN");
                    info.ArgumentList.Add("-t");
                    string stdout;
                    try
                    {
                        using var process = Process.Start(info);
                        if (process == null) continue;
                        stdout = process.StandardOutput.ReadToEnd();
                        process.WaitForExit(1000);
                    }
                    catch { continue; }

                    foreach (var line in stdout.Split('\n'))
                    {
                        if (int.TryParse(line.Trim(), out var pid) && seen.Add(pid)) yield return pid;
                    }
                }
            }
        }

        private static string ResolveDotnetPath()
        {
            var candidates = new[]
            {
                Environment.GetEnvironmentVariable("DOTNET_HOST_PATH"),
                Environment.GetEnvironmentVariable("DOTNET_ROOT") is { Length: > 0 } dotnetRoot ? Path.Combine(dotnetRoot, "dotnet") : null,
                "/usr/local/share/dotnet/dotnet",
                "/opt/homebrew/bin/dotnet",
                "/usr/bin/dotnet",
                Path.Combine(EditorApplication.applicationContentsPath, "NetCoreRuntime", "dotnet")
            };
            foreach (var candidate in candidates)
            {
                if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate)) return candidate;
            }
            return null;
        }

        private sealed class HostLaunchInfo
        {
            public HostLaunchInfo(string fileName, string workingDirectory, params string[] arguments)
            {
                FileName = fileName;
                WorkingDirectory = workingDirectory;
                Arguments = arguments;
            }

            public string FileName { get; }
            public string WorkingDirectory { get; }
            public IReadOnlyList<string> Arguments { get; }
        }
    }
}
