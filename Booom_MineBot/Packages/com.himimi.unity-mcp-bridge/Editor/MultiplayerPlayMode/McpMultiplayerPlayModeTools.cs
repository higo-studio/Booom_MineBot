using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using McpBridge;

namespace McpBridge.Editor
{
    [McpPluginToolType]
    internal sealed class McpMultiplayerPlayModeTools
    {
        [McpPluginTool("unity.mppm_status", Title = "Get Multiplayer Play Mode Status")]
        [Description("Returns Unity Multiplayer Play Mode status, settings, and configured player tags from the primary Unity project.")]
        public ToolCallResult GetStatus()
        {
            return MainThread.Instance.Run(() =>
            {
                if (!MppmReflection.TryLoad(out var api, out var errorResult))
                {
                    return errorResult;
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Multiplayer Play Mode is {(api.GetIsActive() ? "active" : "inactive")}.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["isActive"] = api.GetIsActive(),
                        ["isWorkflowInitialized"] = api.GetIsWorkflowInitialized(),
                        ["showLaunchScreenOnPlayers"] = api.GetShowLaunchScreenOnPlayers(),
                        ["mutePlayers"] = api.GetMutePlayers(),
                        ["assetDatabaseRefreshTimeout"] = api.GetAssetDatabaseRefreshTimeout(),
                        ["playerTags"] = api.GetCatalogTags().Cast<object>().ToList(),
                        ["virtualProjectsConfig"] = ReadVirtualProjectsConfig()
                    }
                };
            });
        }

        [McpPluginTool("unity.mppm_players", Title = "List Multiplayer Play Mode Players")]
        [Description("Lists Multiplayer Play Mode player slots, activation state, launch state, tags, and virtual project identifiers.")]
        public ToolCallResult GetPlayers()
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("inspect Multiplayer Play Mode players", out var blocked))
                {
                    return blocked;
                }

                if (!MppmReflection.TryLoad(out var api, out var errorResult))
                {
                    return errorResult;
                }

                var players = api.GetPlayers().Select(player => (object)player).ToList();
                return new ToolCallResult
                {
                    Text = $"[Success] Found {players.Count} Multiplayer Play Mode player slot(s).",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["items"] = players
                    }
                };
            });
        }

        [McpPluginTool("unity.mppm_set_active", Title = "Set Multiplayer Play Mode Active")]
        [Description("Enables or disables Unity Multiplayer Play Mode in the primary project.")]
        public ToolCallResult SetActive(
            [Description("When true, enables Multiplayer Play Mode. When false, disables it.")]
            bool active)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("change Multiplayer Play Mode active state", out var blocked))
                {
                    return blocked;
                }

                if (!MppmReflection.TryLoad(out var api, out var errorResult))
                {
                    return errorResult;
                }

                api.SetIsActive(active);
                return new ToolCallResult
                {
                    Text = $"[Success] Multiplayer Play Mode {(active ? "enabled" : "disabled")}.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["isActive"] = api.GetIsActive()
                    }
                };
            });
        }

        [McpPluginTool("unity.mppm_configure", Title = "Configure Multiplayer Play Mode")]
        [Description("Updates Multiplayer Play Mode settings such as launch screen behavior, mute state, and asset refresh timeout.")]
        public ToolCallResult Configure(
            [Description("Optional setting for whether clone players show the Unity launch screen.")]
            bool? showLaunchScreenOnPlayers = null,
            [Description("Optional setting for whether clone players should be muted.")]
            bool? mutePlayers = null,
            [Description("Optional asset database refresh timeout in seconds.")]
            int assetDatabaseRefreshTimeout = -1)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("configure Multiplayer Play Mode", out var blocked))
                {
                    return blocked;
                }

                if (!MppmReflection.TryLoad(out var api, out var errorResult))
                {
                    return errorResult;
                }

                if (showLaunchScreenOnPlayers.HasValue)
                {
                    api.SetShowLaunchScreenOnPlayers(showLaunchScreenOnPlayers.Value);
                }

                if (mutePlayers.HasValue)
                {
                    api.SetMutePlayers(mutePlayers.Value);
                }

                if (assetDatabaseRefreshTimeout >= 0)
                {
                    api.SetAssetDatabaseRefreshTimeout(assetDatabaseRefreshTimeout);
                }

                return new ToolCallResult
                {
                    Text = "[Success] Multiplayer Play Mode settings updated.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["showLaunchScreenOnPlayers"] = api.GetShowLaunchScreenOnPlayers(),
                        ["mutePlayers"] = api.GetMutePlayers(),
                        ["assetDatabaseRefreshTimeout"] = api.GetAssetDatabaseRefreshTimeout()
                    }
                };
            });
        }

        [McpPluginTool("unity.mppm_player_set_active", Title = "Set Multiplayer Play Mode Player Active")]
        [Description("Activates or deactivates a Multiplayer Play Mode player slot. Player 1 is the main editor and cannot be deactivated.")]
        public ToolCallResult SetPlayerActive(
            [Description("Player slot index from 1 to 4.")]
            int playerIndex,
            [Description("When true, activates the player slot. When false, deactivates it.")]
            bool active)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("change Multiplayer Play Mode player state", out var blocked))
                {
                    return blocked;
                }

                if (!MppmReflection.TryLoad(out var api, out var errorResult))
                {
                    return errorResult;
                }

                var result = api.SetPlayerActive(playerIndex, active, out var error);
                if (!result)
                {
                    return CreateStatus("failed", $"[Failed] Could not {(active ? "activate" : "deactivate")} player {playerIndex}: {error}", false);
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Player {playerIndex} {(active ? "activated" : "deactivated")}.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["player"] = api.GetPlayer(playerIndex)
                    }
                };
            });
        }

        [McpPluginTool("unity.mppm_tag_add", Title = "Add Multiplayer Play Mode Tag")]
        [Description("Adds a tag to the Multiplayer Play Mode project-wide tag catalog.")]
        public ToolCallResult AddTag(
            [Description("Tag name to add to the project-wide MPPM catalog.")]
            string tag)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("add Multiplayer Play Mode tags", out var blocked))
                {
                    return blocked;
                }

                if (!MppmReflection.TryLoad(out var api, out var errorResult))
                {
                    return errorResult;
                }

                var success = api.AddCatalogTag(tag, out var error);
                if (!success)
                {
                    return CreateStatus("failed", $"[Failed] Could not add MPPM tag '{tag}': {error}", false);
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Added Multiplayer Play Mode tag '{tag}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["playerTags"] = api.GetCatalogTags().Cast<object>().ToList()
                    }
                };
            });
        }

        [McpPluginTool("unity.mppm_tag_remove", Title = "Remove Multiplayer Play Mode Tag")]
        [Description("Removes a tag from the Multiplayer Play Mode project-wide tag catalog and strips it from players using it.")]
        public ToolCallResult RemoveTag(
            [Description("Tag name to remove from the project-wide MPPM catalog.")]
            string tag)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("remove Multiplayer Play Mode tags", out var blocked))
                {
                    return blocked;
                }

                if (!MppmReflection.TryLoad(out var api, out var errorResult))
                {
                    return errorResult;
                }

                var success = api.RemoveCatalogTag(tag, out var error);
                if (!success)
                {
                    return CreateStatus("failed", $"[Failed] Could not remove MPPM tag '{tag}': {error}", false);
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Removed Multiplayer Play Mode tag '{tag}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["playerTags"] = api.GetCatalogTags().Cast<object>().ToList()
                    }
                };
            });
        }

        [McpPluginTool("unity.mppm_player_tags_set", Title = "Set Multiplayer Play Mode Player Tags")]
        [Description("Replaces all tags on a specific Multiplayer Play Mode player slot.")]
        public ToolCallResult SetPlayerTags(
            [Description("Player slot index from 1 to 4.")]
            int playerIndex,
            [Description("The exact set of tags that should remain on the player after the update.")]
            string[] tags)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("set Multiplayer Play Mode player tags", out var blocked))
                {
                    return blocked;
                }

                if (!MppmReflection.TryLoad(out var api, out var errorResult))
                {
                    return errorResult;
                }

                var success = api.SetPlayerTags(playerIndex, tags ?? Array.Empty<string>(), out var error);
                if (!success)
                {
                    return CreateStatus("failed", $"[Failed] Could not update tags for player {playerIndex}: {error}", false);
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Updated tags for player {playerIndex}.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["player"] = api.GetPlayer(playerIndex)
                    }
                };
            });
        }

        private static Dictionary<string, object> ReadVirtualProjectsConfig()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "ProjectSettings", "VirtualProjectsConfig.json");
            if (!File.Exists(path))
            {
                return new Dictionary<string, object>
                {
                    ["exists"] = false
                };
            }

            var parsed = McpBridgeJson.Deserialize(File.ReadAllText(path)) as Dictionary<string, object>;
            return new Dictionary<string, object>
            {
                ["exists"] = true,
                ["path"] = "ProjectSettings/VirtualProjectsConfig.json",
                ["data"] = parsed ?? new Dictionary<string, object>()
            };
        }

        private static ToolCallResult CreateStatus(string status, string text, bool ok)
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

    internal static class MppmReflection
    {
        private const string SettingsTypeName = "Unity.Multiplayer.Playmode.Workflow.Editor.MultiplayerPlayModeSettings";
        private const string WorkflowTypeName = "Unity.Multiplayer.Playmode.Workflow.Editor.MultiplayerPlaymode";

        public static bool TryLoad(out MppmApi api, out ToolCallResult errorResult)
        {
            var settingsType = FindType(SettingsTypeName);
            var workflowType = FindType(WorkflowTypeName);
            if (settingsType == null || workflowType == null)
            {
                api = null;
                errorResult = new ToolCallResult
                {
                    Text = "[Failed] Unity Multiplayer Play Mode package is not available in the current editor domain.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["status"] = "not_available"
                    }
                };
                return false;
            }

            api = new MppmApi(settingsType, workflowType);
            errorResult = null;
            return true;
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }

    internal sealed class MppmApi
    {
        private readonly Type m_SettingsType;
        private readonly Type m_WorkflowType;
        private readonly PropertyInfo m_PlayerTagsProperty;
        private readonly PropertyInfo m_PlayersProperty;

        public MppmApi(Type settingsType, Type workflowType)
        {
            m_SettingsType = settingsType;
            m_WorkflowType = workflowType;
            m_PlayerTagsProperty = GetProperty(m_WorkflowType, "PlayerTags");
            m_PlayersProperty = GetProperty(m_WorkflowType, "Players");
        }

        public bool GetIsActive() => (bool)InvokeStatic(m_SettingsType, "GetIsMppmActive");
        public void SetIsActive(bool active) => InvokeStatic(m_SettingsType, "SetIsMppmActive", active);
        public bool GetShowLaunchScreenOnPlayers() => (bool)GetProperty(m_SettingsType, "ShowLaunchScreenOnPlayers").GetValue(null);
        public void SetShowLaunchScreenOnPlayers(bool value) => GetProperty(m_SettingsType, "ShowLaunchScreenOnPlayers").SetValue(null, value);
        public bool GetMutePlayers() => (bool)GetProperty(m_SettingsType, "MutePlayers").GetValue(null);
        public void SetMutePlayers(bool value) => GetProperty(m_SettingsType, "MutePlayers").SetValue(null, value);
        public int GetAssetDatabaseRefreshTimeout() => (int)GetProperty(m_SettingsType, "AssetDatabaseRefreshTimeout").GetValue(null);
        public void SetAssetDatabaseRefreshTimeout(int value) => GetProperty(m_SettingsType, "AssetDatabaseRefreshTimeout").SetValue(null, value);
        public bool GetIsWorkflowInitialized() => (bool)(GetProperty(m_WorkflowType, "IsVirtualProjectWorkflowInitialized")?.GetValue(null) ?? false);

        public string[] GetCatalogTags()
        {
            var tagCatalog = m_PlayerTagsProperty.GetValue(null);
            if (tagCatalog == null)
            {
                return Array.Empty<string>();
            }

            return ((IEnumerable)(GetProperty(tagCatalog.GetType(), "Tags").GetValue(tagCatalog) as IEnumerable))?.Cast<object>().Select(item => item?.ToString() ?? string.Empty).ToArray()
                   ?? Array.Empty<string>();
        }

        public List<Dictionary<string, object>> GetPlayers()
        {
            var players = m_PlayersProperty.GetValue(null) as IEnumerable;
            if (players == null)
            {
                return new List<Dictionary<string, object>>();
            }

            return players.Cast<object>().Select(ToPlayerDictionary).ToList();
        }

        public Dictionary<string, object> GetPlayer(int playerIndex)
        {
            var players = m_PlayersProperty.GetValue(null) as IEnumerable;
            if (players == null)
            {
                return null;
            }

            foreach (var player in players.Cast<object>())
            {
                var stateJson = GetField(player.GetType(), "m_PlayerStateJson").GetValue(player);
                var index = (int)GetProperty(stateJson.GetType(), "Index").GetValue(stateJson);
                if (index == playerIndex)
                {
                    return ToPlayerDictionary(player);
                }
            }

            return null;
        }

        public bool SetPlayerActive(int playerIndex, bool active, out string error)
        {
            error = null;
            var player = FindPlayer(playerIndex);
            if (player == null)
            {
                error = "player_not_found";
                return false;
            }

            var methodName = active ? "Activate" : "Deactivate";
            var method = player.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (method == null)
            {
                error = "method_not_found";
                return false;
            }

            var parameters = method.GetParameters();
            object[] args;
            if (methodName == "Activate" && parameters.Length == 2)
            {
                args = new object[] { null, null };
            }
            else if (parameters.Length == 1)
            {
                args = new object[] { null };
            }
            else
            {
                error = "unexpected_signature";
                return false;
            }

            var success = (bool)method.Invoke(player, args);
            if (!success)
            {
                error = args[0]?.ToString() ?? "unknown";
            }

            return success;
        }

        public bool AddCatalogTag(string tag, out string error)
        {
            error = null;
            var tagCatalog = m_PlayerTagsProperty.GetValue(null);
            if (tagCatalog == null)
            {
                error = "player_tags_unavailable";
                return false;
            }

            var method = tagCatalog.GetType().GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var args = new object[] { tag, null };
            var success = (bool)method.Invoke(tagCatalog, args);
            if (!success)
            {
                error = args[1]?.ToString() ?? "unknown";
            }

            return success;
        }

        public bool RemoveCatalogTag(string tag, out string error)
        {
            error = null;
            var tagCatalog = m_PlayerTagsProperty.GetValue(null);
            if (tagCatalog == null)
            {
                error = "player_tags_unavailable";
                return false;
            }

            var method = tagCatalog.GetType().GetMethod("Remove", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var args = new object[] { tag, null, null };
            var success = (bool)method.Invoke(tagCatalog, args);
            if (!success)
            {
                error = args[2]?.ToString() ?? "unknown";
            }

            return success;
        }

        public bool SetPlayerTags(int playerIndex, string[] tags, out string error)
        {
            error = null;
            var player = FindPlayer(playerIndex);
            if (player == null)
            {
                error = "player_not_found";
                return false;
            }

            var currentTags = ((IEnumerable)(GetProperty(player.GetType(), "Tags").GetValue(player) as IEnumerable))?.Cast<object>().Select(item => item?.ToString() ?? string.Empty).ToList()
                              ?? new List<string>();
            var desiredTags = new HashSet<string>(tags ?? Array.Empty<string>(), StringComparer.Ordinal);

            foreach (var currentTag in currentTags)
            {
                if (!desiredTags.Contains(currentTag))
                {
                    var removeMethod = player.GetType().GetMethod("RemoveTag", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    var removeArgs = new object[] { currentTag, null };
                    if (!(bool)removeMethod.Invoke(player, removeArgs))
                    {
                        error = removeArgs[1]?.ToString() ?? "remove_failed";
                        return false;
                    }
                }
            }

            foreach (var desiredTag in desiredTags)
            {
                if (currentTags.Contains(desiredTag))
                {
                    continue;
                }

                var addMethod = player.GetType().GetMethod("AddTag", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var addArgs = new object[] { desiredTag, null };
                if (!(bool)addMethod.Invoke(player, addArgs))
                {
                    error = addArgs[1]?.ToString() ?? "add_failed";
                    return false;
                }
            }

            return true;
        }

        private object FindPlayer(int playerIndex)
        {
            var players = m_PlayersProperty.GetValue(null) as IEnumerable;
            if (players == null)
            {
                return null;
            }

            foreach (var player in players.Cast<object>())
            {
                var stateJson = GetField(player.GetType(), "m_PlayerStateJson").GetValue(player);
                var index = (int)GetProperty(stateJson.GetType(), "Index").GetValue(stateJson);
                if (index == playerIndex)
                {
                    return player;
                }
            }

            return null;
        }

        private static Dictionary<string, object> ToPlayerDictionary(object player)
        {
            var playerType = player.GetType();
            var stateJson = GetField(playerType, "m_PlayerStateJson").GetValue(player);
            var stateJsonType = stateJson.GetType();
            var typeDependentInfo = GetProperty(playerType, "TypeDependentPlayerInfo").GetValue(player);

            return new Dictionary<string, object>
            {
                ["index"] = GetProperty(stateJsonType, "Index").GetValue(stateJson),
                ["name"] = GetProperty(playerType, "Name").GetValue(player)?.ToString() ?? string.Empty,
                ["type"] = GetProperty(playerType, "Type").GetValue(player)?.ToString() ?? string.Empty,
                ["active"] = (bool)GetProperty(stateJsonType, "Active").GetValue(stateJson),
                ["state"] = GetProperty(playerType, "PlayerState").GetValue(player)?.ToString() ?? string.Empty,
                ["tags"] = ((IEnumerable)(GetProperty(playerType, "Tags").GetValue(player) as IEnumerable))?.Cast<object>().Select(item => item?.ToString() ?? string.Empty).Cast<object>().ToList()
                           ?? new List<object>(),
                ["virtualProjectIdentifier"] = typeDependentInfo == null ? null : GetProperty(typeDependentInfo.GetType(), "VirtualProjectIdentifier")?.GetValue(typeDependentInfo)?.ToString(),
                ["playerIdentifier"] = GetProperty(playerType, "PlayerIdentifier").GetValue(player)?.ToString() ?? string.Empty
            };
        }

        private static PropertyInfo GetProperty(Type type, string name)
        {
            return type.GetProperty(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static FieldInfo GetField(Type type, string name)
        {
            return type.GetField(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        }

        private static object InvokeStatic(Type type, string methodName, params object[] args)
        {
            var method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return method.Invoke(null, args);
        }
    }
}
