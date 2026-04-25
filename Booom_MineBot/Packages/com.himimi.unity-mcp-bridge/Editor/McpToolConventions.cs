using System;
using System.Collections.Generic;

namespace McpBridge.Editor
{
    internal static class McpToolConventions
    {
        private static readonly Dictionary<string, string> s_Groups = new(StringComparer.Ordinal)
        {
            ["unity.instances"] = "Workflow",
            ["unity.compile"] = "Workflow",
            ["unity.editor_state"] = "Workflow",
            ["unity.enter_play_mode"] = "Workflow",
            ["unity.exit_play_mode"] = "Workflow",
            ["unity.selection_get"] = "Workflow",
            ["unity.selection_set"] = "Workflow",
            ["unity.console_logs"] = "Workflow",
            ["unity.screenshot"] = "Workflow",
            ["unity.tests_run"] = "Workflow",
            ["unity.mppm_status"] = "Multiplayer Play Mode",
            ["unity.mppm_players"] = "Multiplayer Play Mode",
            ["unity.mppm_set_active"] = "Multiplayer Play Mode",
            ["unity.mppm_configure"] = "Multiplayer Play Mode",
            ["unity.mppm_player_set_active"] = "Multiplayer Play Mode",
            ["unity.mppm_tag_add"] = "Multiplayer Play Mode",
            ["unity.mppm_tag_remove"] = "Multiplayer Play Mode",
            ["unity.mppm_player_tags_set"] = "Multiplayer Play Mode"
        };

        public static bool IsReflectionTool(string toolName)
        {
            return !string.IsNullOrWhiteSpace(toolName) &&
                   toolName.StartsWith("unity.reflection_", StringComparison.Ordinal);
        }

        public static bool IsPrimaryOnlyTool(string toolName)
        {
            return toolName switch
            {
                "unity.compile" => true,
                "unity.enter_play_mode" => true,
                "unity.exit_play_mode" => true,
                "unity.tests_run" => true,
                "unity.package_add" => true,
                "unity.package_remove" => true,
                "unity.script_write" => true,
                "unity.script_delete" => true,
                "unity.mppm_status" => true,
                "unity.mppm_players" => true,
                "unity.mppm_set_active" => true,
                "unity.mppm_configure" => true,
                "unity.mppm_player_set_active" => true,
                "unity.mppm_tag_add" => true,
                "unity.mppm_tag_remove" => true,
                "unity.mppm_player_tags_set" => true,
                _ => false
            };
        }

        public static string GetGroup(string toolName)
        {
            if (string.IsNullOrWhiteSpace(toolName))
            {
                return "Other";
            }

            if (s_Groups.TryGetValue(toolName, out var group))
            {
                return group;
            }

            if (toolName.StartsWith("unity.scene_", StringComparison.Ordinal) ||
                toolName.StartsWith("unity.gameobject_", StringComparison.Ordinal) ||
                toolName.StartsWith("unity.component_", StringComparison.Ordinal) ||
                toolName.StartsWith("unity.object_", StringComparison.Ordinal))
            {
                return "Scene / Object";
            }

            if (toolName.StartsWith("unity.asset_", StringComparison.Ordinal) ||
                toolName.StartsWith("unity.prefab_", StringComparison.Ordinal) ||
                toolName.StartsWith("unity.script_", StringComparison.Ordinal) ||
                toolName.StartsWith("unity.package_", StringComparison.Ordinal))
            {
                return "Asset / Script / Package";
            }

            if (IsReflectionTool(toolName))
            {
                return "Reflection";
            }

            return "Other";
        }
    }
}
