using System;
using System.Collections.Generic;

namespace McpBridge.Editor
{
    internal static class McpToolConventions
    {
        private static readonly Dictionary<string, string> s_Groups = new(StringComparer.Ordinal)
        {
            ["unity.instances"] = "工作流",
            ["unity.compile"] = "工作流",
            ["unity.editor_state"] = "工作流",
            ["unity.enter_play_mode"] = "工作流",
            ["unity.exit_play_mode"] = "工作流",
            ["unity.selection_get"] = "工作流",
            ["unity.selection_set"] = "工作流",
            ["unity.console_logs"] = "工作流",
            ["unity.screenshot"] = "工作流",
            ["unity.tests_run"] = "工作流",
            ["unity.mppm_status"] = "多人播放模式",
            ["unity.mppm_players"] = "多人播放模式",
            ["unity.mppm_set_active"] = "多人播放模式",
            ["unity.mppm_configure"] = "多人播放模式",
            ["unity.mppm_player_set_active"] = "多人播放模式",
            ["unity.mppm_tag_add"] = "多人播放模式",
            ["unity.mppm_tag_remove"] = "多人播放模式",
            ["unity.mppm_player_tags_set"] = "多人播放模式"
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
                return "其他";
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
                return "场景 / 对象";
            }

            if (toolName.StartsWith("unity.asset_", StringComparison.Ordinal) ||
                toolName.StartsWith("unity.prefab_", StringComparison.Ordinal) ||
                toolName.StartsWith("unity.script_", StringComparison.Ordinal) ||
                toolName.StartsWith("unity.package_", StringComparison.Ordinal))
            {
                return "资源 / 脚本 / 包";
            }

            if (IsReflectionTool(toolName))
            {
                return "反射";
            }

            return "其他";
        }
    }
}
