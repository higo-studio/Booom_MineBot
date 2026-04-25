using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using McpBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityComponent = UnityEngine.Component;
#if UNITY_TESTS_FRAMEWORK
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine.TestTools;
#endif

namespace McpBridge.Editor
{
    [McpPluginToolType]
    internal sealed class McpEditorTools
    {
        [McpPluginTool("unity.editor_state", Title = "Get Unity Editor State")]
        [Description("Returns the current Unity editor state for the targeted instance, including play mode, pause state, compile/update state, and the active scene.")]
        public ToolCallResult GetEditorState()
        {
            return MainThread.Instance.Run(() =>
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                return new ToolCallResult
                {
                    Text = $"[Success] Unity editor state: {(EditorApplication.isPlaying ? "Play Mode" : "Edit Mode")}.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["isPlaying"] = EditorApplication.isPlaying,
                        ["isPaused"] = EditorApplication.isPaused,
                        ["isCompiling"] = EditorApplication.isCompiling,
                        ["isUpdating"] = EditorApplication.isUpdating,
                        ["isPlayingOrWillChangePlaymode"] = EditorApplication.isPlayingOrWillChangePlaymode,
                        ["activeScene"] = activeScene.path,
                        ["activeSceneName"] = activeScene.name
                    }
                };
            });
        }

        [McpPluginTool("unity.enter_play_mode", Title = "Enter Unity Play Mode")]
        [Description("Enters Unity Play Mode in the targeted editor instance. Use this when a task needs the editor to be running in Play Mode before testing or inspecting runtime behavior.")]
        public ToolCallResult EnterPlayMode(
            [Description("When true, returns a blocked status instead of entering Play Mode if Unity is currently compiling or updating.")]
            bool failIfBusy = true)
        {
            return MainThread.Instance.Run(() =>
            {
                McpPlayModeTransitionGuard.Tick();
                if (EditorApplication.isPlaying)
                {
                    return CreateStatusResult("already_playing", "[Success] Unity is already in Play Mode.", true);
                }

                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    if (failIfBusy)
                    {
                        return CreateStatusResult(
                            "blocked",
                            "[Blocked] Unity is compiling or updating. Enter Play Mode after the editor becomes idle.",
                            false);
                    }
                }

                McpPlayModeTransitionGuard.MarkTransition("entering_play_mode");
                EditorApplication.isPlaying = true;
                EditorApplication.QueuePlayerLoopUpdate();
                return CreateStatusResult("started", "[Success] Unity is entering Play Mode.", true);
            });
        }

        [McpPluginTool("unity.exit_play_mode", Title = "Exit Unity Play Mode")]
        [Description("Exits Unity Play Mode in the targeted editor instance. Use this before editing assets or when runtime testing should stop without triggering a compile request.")]
        public ToolCallResult ExitPlayMode(
            [Description("When true, returns a blocked status instead of exiting Play Mode if Unity is currently compiling or updating.")]
            bool failIfBusy = true)
        {
            return MainThread.Instance.Run(() =>
            {
                McpPlayModeTransitionGuard.Tick();
                if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return CreateStatusResult("already_edit_mode", "[Success] Unity is already in Edit Mode.", true);
                }

                if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    if (failIfBusy)
                    {
                        return CreateStatusResult(
                            "blocked",
                            "[Blocked] Unity is compiling or updating. Exit Play Mode after the editor becomes idle.",
                            false);
                    }
                }

                McpPlayModeTransitionGuard.MarkTransition("exiting_play_mode");
                EditorApplication.isPlaying = false;
                EditorApplication.QueuePlayerLoopUpdate();
                return CreateStatusResult("started", "[Success] Unity is exiting Play Mode.", true);
            });
        }

        [McpPluginTool("unity.selection_get", Title = "Get Unity Selection")]
        [Description("Returns the current editor selection in the targeted Unity instance.")]
        public ToolCallResult GetSelection()
        {
            return MainThread.Instance.Run(() =>
            {
                var objects = Selection.objects ?? Array.Empty<UnityEngine.Object>();
                var items = new List<object>(objects.Length);
                foreach (var selected in objects)
                {
                    items.Add(UnityObjectSnapshot.FromObject(selected).ToDictionary());
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Unity selection contains {objects.Length} object(s).",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["count"] = objects.Length,
                        ["items"] = items
                    }
                };
            });
        }

        [McpPluginTool("unity.selection_set", Title = "Set Unity Selection")]
        [Description("Sets the current editor selection by object identifier. Use localId from selection_get or a GlobalObjectId string.")]
        public ToolCallResult SetSelection(
            [Description("One or more object identifiers returned by unity.selection_get. Accepts local numeric ids or GlobalObjectId strings.")]
            string[] objectIds)
        {
            return MainThread.Instance.Run(() =>
            {
                if (objectIds == null || objectIds.Length == 0)
                {
                    Selection.objects = Array.Empty<UnityEngine.Object>();
                    return CreateStatusResult("cleared", "[Success] Unity selection cleared.", true);
                }

                var resolved = new List<UnityEngine.Object>();
                var missing = new List<string>();
                foreach (var objectId in objectIds)
                {
                    var target = UnityObjectLocator.TryResolveObject(objectId);
                    if (target != null)
                    {
                        resolved.Add(target);
                    }
                    else
                    {
                        missing.Add(objectId);
                    }
                }

                if (missing.Count > 0)
                {
                    return new ToolCallResult
                    {
                        Text = $"[Failed] Could not resolve {missing.Count} selection target(s).",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = false,
                            ["status"] = "not_found",
                            ["missing"] = missing
                        }
                    };
                }

                Selection.objects = resolved.ToArray();
                return new ToolCallResult
                {
                    Text = $"[Success] Unity selection updated with {resolved.Count} object(s).",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["count"] = resolved.Count,
                        ["items"] = resolved.ConvertAll(target => (object)UnityObjectSnapshot.FromObject(target).ToDictionary())
                    }
                };
            });
        }

        [McpPluginTool("unity.console_logs", Title = "Read Unity Console Logs")]
        [Description("Reads recent entries from the Unity Console in the targeted editor instance. Use this to inspect editor/runtime logs, warnings, and errors without opening the Console window.")]
        public ToolCallResult GetConsoleLogs(
            [Description("Maximum number of most recent console entries to return.")]
            int limit = 50,
            [Description("Optional filter: all, log, warning, or error.")]
            string level = "all")
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPlayModeTransitionGuard.TryGetBlockedResult("collect console logs", out var blockedResult))
                {
                    return blockedResult;
                }

                var entries = UnityConsoleReader.Read(limit, level);
                var lines = new List<string>(entries.Count);
                foreach (var entry in entries)
                {
                    lines.Add($"[{entry.Level}] {entry.Message}");
                }

                return new ToolCallResult
                {
                    Text = lines.Count == 0
                        ? "[Success] Unity Console has no matching entries."
                        : string.Join("\n", lines),
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["count"] = entries.Count,
                        ["entries"] = entries.ConvertAll(entry => (object)entry.ToDictionary())
                    }
                };
            });
        }

        [McpPluginTool("unity.screenshot", Title = "Capture Unity Screenshot")]
        [Description("Captures a screenshot from the targeted Unity instance. Supported sources: game, scene, or camera.")]
        public ToolCallResult CaptureScreenshot(
            [Description("Screenshot source: game, scene, or camera.")]
            string source = "game",
            [Description("Optional camera name when source is camera.")]
            string cameraName = null,
            [Description("Optional output file name. If omitted, a temp PNG path is generated.")]
            string outputFileName = null)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("capture a screenshot", out var blockedResult))
                {
                    return blockedResult;
                }

                var texture = CaptureTexture(source, cameraName, out var failureText);
                if (texture == null)
                {
                    return CreateStatusResult("blocked", failureText ?? "[Failed] Unable to capture screenshot.", false);
                }

                try
                {
                    var directory = Path.Combine(McpBridgePaths.StateDirectory, "Screenshots");
                    Directory.CreateDirectory(directory);
                    var fileName = string.IsNullOrWhiteSpace(outputFileName)
                        ? $"unity-{source}-{DateTime.Now:yyyyMMdd-HHmmssfff}.png"
                        : SanitizeFileName(outputFileName);
                    if (!fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        fileName += ".png";
                    }

                    var path = Path.Combine(directory, fileName);
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                    return new ToolCallResult
                    {
                        Text = $"[Success] Screenshot saved to {path}",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = true,
                            ["path"] = path,
                            ["width"] = texture.width,
                            ["height"] = texture.height,
                            ["source"] = source
                        }
                    };
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            });
        }

        [McpPluginTool("unity.tests_run", Title = "Run Unity Tests")]
        [Description("Runs Unity EditMode or PlayMode tests in the targeted primary instance and returns structured results.")]
        public Task<ToolCallResult> RunTests(
            [Description("Test mode: edit, play, or all.")]
            string mode = "edit",
            [Description("Optional exact test full names to run.")]
            string[] testNames = null,
            [Description("Optional regex group names to run.")]
            string[] groupNames = null,
            [Description("Optional assembly names to run.")]
            string[] assemblyNames = null,
            [Description("When true, runs synchronously when supported by the test framework.")]
            bool runSynchronously = false)
        {
            return McpTestRunner.RunAsync(mode, testNames, groupNames, assemblyNames, runSynchronously);
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

        private static Texture2D CaptureTexture(string source, string cameraName, out string failureText)
        {
            failureText = null;
            source = string.IsNullOrWhiteSpace(source) ? "game" : source.Trim().ToLowerInvariant();
            switch (source)
            {
                case "game":
                    return ScreenCapture.CaptureScreenshotAsTexture();
                case "scene":
                    return CaptureSceneViewTexture(out failureText);
                case "camera":
                    return CaptureCameraTexture(cameraName, out failureText);
                default:
                    failureText = $"[Failed] Unsupported screenshot source '{source}'.";
                    return null;
            }
        }

        private static Texture2D CaptureSceneViewTexture(out string failureText)
        {
            failureText = null;
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                failureText = "[Failed] Scene View is not available in the targeted Unity instance.";
                return null;
            }

            return CaptureCamera(sceneView.camera, sceneView.position.width, sceneView.position.height, out failureText);
        }

        private static Texture2D CaptureCameraTexture(string cameraName, out string failureText)
        {
            failureText = null;
            Camera camera = null;
            if (!string.IsNullOrWhiteSpace(cameraName))
            {
                foreach (var candidate in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (string.Equals(candidate.name, cameraName, StringComparison.OrdinalIgnoreCase))
                    {
                        camera = candidate;
                        break;
                    }
                }
            }

            camera ??= Camera.main;
            if (camera == null)
            {
                failureText = string.IsNullOrWhiteSpace(cameraName)
                    ? "[Failed] No camera is available for screenshot capture."
                    : $"[Failed] Could not find camera '{cameraName}'.";
                return null;
            }

            var width = camera.pixelWidth > 0 ? camera.pixelWidth : 1920;
            var height = camera.pixelHeight > 0 ? camera.pixelHeight : 1080;
            return CaptureCamera(camera, width, height, out failureText);
        }

        private static Texture2D CaptureCamera(Camera camera, float width, float height, out string failureText)
        {
            failureText = null;
            var pixelWidth = Mathf.Max(1, Mathf.RoundToInt(width));
            var pixelHeight = Mathf.Max(1, Mathf.RoundToInt(height));
            var renderTexture = RenderTexture.GetTemporary(pixelWidth, pixelHeight, 24);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;

                var texture = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, pixelWidth, pixelHeight), 0, 0);
                texture.Apply();
                return texture;
            }
            catch (Exception exception)
            {
                failureText = $"[Failed] Screenshot capture failed: {exception.Message}";
                return null;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                fileName = fileName.Replace(invalid, '_');
            }

            return fileName;
        }
    }

    [InitializeOnLoad]
    internal static class McpPlayModeTransitionGuard
    {
        private static string s_State = string.Empty;
        private static double s_Deadline;

        static McpPlayModeTransitionGuard()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += Tick;
        }

        public static void MarkTransition(string state)
        {
            s_State = state ?? string.Empty;
            s_Deadline = EditorApplication.timeSinceStartup + 15.0;
        }

        public static bool TryGetBlockedResult(string operation, out ToolCallResult result)
        {
            Tick();
            if (string.IsNullOrEmpty(s_State))
            {
                result = null;
                return false;
            }

            result = new ToolCallResult
            {
                Text = $"[Retry] Unity is {s_State.Replace('_', ' ')}. Retry {operation} after the Play Mode transition completes.",
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = false,
                    ["status"] = "retry",
                    ["reason"] = s_State
                }
            };
            return true;
        }

        public static void Tick()
        {
            if (string.IsNullOrEmpty(s_State))
            {
                return;
            }

            if (EditorApplication.timeSinceStartup >= s_Deadline)
            {
                s_State = string.Empty;
                s_Deadline = 0;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            switch (change)
            {
                case PlayModeStateChange.ExitingEditMode:
                    MarkTransition("entering_play_mode");
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    MarkTransition("exiting_play_mode");
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    s_State = string.Empty;
                    s_Deadline = 0;
                    break;
            }
        }
    }

    internal static class McpEditorToolGuards
    {
        public static bool TryBlockForTransition(string operation, out ToolCallResult result)
        {
            if (McpPlayModeTransitionGuard.TryGetBlockedResult(operation, out result))
            {
                return true;
            }

            if (EditorApplication.isCompiling)
            {
                result = Create("busy", $"[Busy] Unity is compiling and cannot {operation} right now.", false);
                return true;
            }

            if (EditorApplication.isUpdating)
            {
                result = Create("busy", $"[Busy] Unity is updating and cannot {operation} right now.", false);
                return true;
            }

            result = null;
            return false;
        }

        private static ToolCallResult Create(string status, string text, bool ok)
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
    }

    internal sealed class UnityObjectSnapshot
    {
        public string LocalId;
        public string GlobalId;
        public string Name;
        public string Type;
        public string AssetPath;
        public string ScenePath;
        public string HierarchyPath;

        public static UnityObjectSnapshot FromObject(UnityEngine.Object target)
        {
            var localId = target != null ? target.GetInstanceID().ToString() : string.Empty;
            var globalId = string.Empty;
            if (target != null)
            {
                globalId = GlobalObjectId.GetGlobalObjectIdSlow(target).ToString();
            }

            return new UnityObjectSnapshot
            {
                LocalId = localId,
                GlobalId = globalId,
                Name = target != null ? target.name : string.Empty,
                Type = target != null ? target.GetType().FullName : string.Empty,
                AssetPath = target != null ? AssetDatabase.GetAssetPath(target) : string.Empty,
                ScenePath = GetScenePath(target),
                HierarchyPath = GetHierarchyPath(target)
            };
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["localId"] = LocalId,
                ["globalId"] = GlobalId,
                ["name"] = Name,
                ["type"] = Type,
                ["assetPath"] = AssetPath,
                ["scenePath"] = ScenePath,
                ["hierarchyPath"] = HierarchyPath
            };
        }

        private static string GetScenePath(UnityEngine.Object target)
        {
            return target switch
            {
                GameObject gameObject => gameObject.scene.path,
                UnityComponent component => component.gameObject.scene.path,
                _ => string.Empty
            };
        }

        private static string GetHierarchyPath(UnityEngine.Object target)
        {
            Transform transform = target switch
            {
                GameObject gameObject => gameObject.transform,
                UnityComponent component => component.transform,
                _ => null
            };

            if (transform == null)
            {
                return string.Empty;
            }

            var path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            return path;
        }
    }

    internal static class UnityObjectLocator
    {
        public static UnityEngine.Object TryResolveObject(string objectId)
        {
            if (string.IsNullOrWhiteSpace(objectId))
            {
                return null;
            }

            if (int.TryParse(objectId, out var localId))
            {
                return EditorUtility.InstanceIDToObject(localId);
            }

            if (GlobalObjectId.TryParse(objectId, out var globalObjectId))
            {
                return GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalObjectId);
            }

            return null;
        }
    }

    internal static class UnityConsoleReader
    {
        private const int ModeError = 1 << 0;
        private const int ModeAssert = 1 << 1;
        private const int ModeLog = 1 << 2;
        private const int ModeFatal = 1 << 4;
        private const int ModeWarning = 1 << 5;

        public static List<ConsoleEntry> Read(int limit, string level)
        {
            limit = Math.Max(1, Math.Min(limit, 200));
            level = string.IsNullOrWhiteSpace(level) ? "all" : level.Trim().ToLowerInvariant();

            var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor.dll");
            var logEntryType = Type.GetType("UnityEditor.LogEntry,UnityEditor.dll");
            if (logEntriesType == null || logEntryType == null)
            {
                return new List<ConsoleEntry>();
            }

            var getCount = logEntriesType.GetMethod("GetCount", BindingFlags.Public | BindingFlags.Static);
            var startGettingEntries = logEntriesType.GetMethod("StartGettingEntries", BindingFlags.Public | BindingFlags.Static);
            var endGettingEntries = logEntriesType.GetMethod("EndGettingEntries", BindingFlags.Public | BindingFlags.Static);
            var getEntryInternal = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Public | BindingFlags.Static);
            if (getCount == null || startGettingEntries == null || endGettingEntries == null || getEntryInternal == null)
            {
                return new List<ConsoleEntry>();
            }

            var count = (int)getCount.Invoke(null, null);
            var entries = new List<ConsoleEntry>(Math.Min(limit, count));
            var entryInstance = Activator.CreateInstance(logEntryType);
            var messageField = logEntryType.GetField("message", BindingFlags.Public | BindingFlags.Instance)
                               ?? logEntryType.GetField("condition", BindingFlags.Public | BindingFlags.Instance);
            var modeField = logEntryType.GetField("mode", BindingFlags.Public | BindingFlags.Instance);
            var fileField = logEntryType.GetField("file", BindingFlags.Public | BindingFlags.Instance);
            var lineField = logEntryType.GetField("line", BindingFlags.Public | BindingFlags.Instance);

            startGettingEntries.Invoke(null, null);
            try
            {
                for (var index = count - 1; index >= 0 && entries.Count < limit; index--)
                {
                    var ok = (bool)getEntryInternal.Invoke(null, new[] { index, entryInstance });
                    if (!ok)
                    {
                        continue;
                    }

                    var mode = modeField != null ? (int)modeField.GetValue(entryInstance) : 0;
                    var entryLevel = ClassifyLevel(mode);
                    if (!MatchesLevel(level, entryLevel))
                    {
                        continue;
                    }

                    entries.Add(new ConsoleEntry
                    {
                        Level = entryLevel,
                        Message = messageField?.GetValue(entryInstance) as string ?? string.Empty,
                        File = fileField?.GetValue(entryInstance) as string ?? string.Empty,
                        Line = lineField != null ? Convert.ToInt32(lineField.GetValue(entryInstance)) : 0
                    });
                }
            }
            finally
            {
                endGettingEntries.Invoke(null, null);
            }

            return entries;
        }

        private static bool MatchesLevel(string filter, string level)
        {
            return filter switch
            {
                "all" => true,
                "log" => level == "log",
                "warning" => level == "warning",
                "error" => level == "error",
                _ => true
            };
        }

        private static string ClassifyLevel(int mode)
        {
            if ((mode & (ModeError | ModeAssert | ModeFatal)) != 0)
            {
                return "error";
            }

            if ((mode & ModeWarning) != 0)
            {
                return "warning";
            }

            if ((mode & ModeLog) != 0)
            {
                return "log";
            }

            return "log";
        }
    }

    internal sealed class ConsoleEntry
    {
        public string Level;
        public string Message;
        public string File;
        public int Line;

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["level"] = Level,
                ["message"] = Message,
                ["file"] = File,
                ["line"] = Line
            };
        }
    }
}
