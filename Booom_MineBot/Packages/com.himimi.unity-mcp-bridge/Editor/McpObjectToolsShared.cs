using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityComponent = UnityEngine.Component;

namespace McpBridge.Editor
{
    internal static class McpToolResultFactory
    {
        public static ToolCallResult CreateStatus(string status, string text, bool ok)
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

    internal static class McpComponentLocator
    {
        public static bool HasComponent(GameObject gameObject, string componentType)
        {
            if (gameObject == null || string.IsNullOrWhiteSpace(componentType))
            {
                return false;
            }

            var type = UnityTypeLocator.FindType(componentType, typeof(UnityComponent));
            if (type != null)
            {
                return gameObject.GetComponent(type) != null;
            }

            return gameObject.GetComponents<UnityComponent>()
                .Any(component => MatchesComponentType(component, componentType));
        }

        public static List<UnityComponent> GetComponents(GameObject gameObject, string componentType = null, bool includeTransform = true)
        {
            if (gameObject == null)
            {
                return new List<UnityComponent>();
            }

            var components = gameObject.GetComponents<UnityComponent>()
                .Where(component => component != null)
                .Where(component => includeTransform || component is not Transform)
                .ToList();

            if (string.IsNullOrWhiteSpace(componentType))
            {
                return components;
            }

            var type = UnityTypeLocator.FindType(componentType, typeof(UnityComponent));
            if (type != null)
            {
                return components
                    .Where(component => type.IsAssignableFrom(component.GetType()))
                    .ToList();
            }

            return components
                .Where(component => MatchesComponentType(component, componentType))
                .ToList();
        }

        public static UnityComponent ResolveComponent(
            string componentId,
            string gameObjectId,
            string componentType,
            int componentIndex,
            out ToolCallResult errorResult)
        {
            errorResult = null;
            if (!string.IsNullOrWhiteSpace(componentId))
            {
                var component = UnityObjectLocator.TryResolveObject(componentId) as UnityComponent;
                if (component != null)
                {
                    return component;
                }

                errorResult = McpToolResultFactory.CreateStatus(
                    "not_found",
                    $"[Failed] Could not resolve component '{componentId}'.",
                    false);
                return null;
            }

            if (string.IsNullOrWhiteSpace(gameObjectId))
            {
                errorResult = McpToolResultFactory.CreateStatus(
                    "invalid_argument",
                    "[Failed] Provide componentId or gameObjectId to resolve a component.",
                    false);
                return null;
            }

            var gameObject = UnityObjectLocator.TryResolveObject(gameObjectId) as GameObject;
            if (gameObject == null)
            {
                errorResult = McpToolResultFactory.CreateStatus(
                    "not_found",
                    $"[Failed] Could not resolve GameObject '{gameObjectId}'.",
                    false);
                return null;
            }

            return ResolveComponent(gameObject, componentType, componentIndex, out errorResult);
        }

        public static UnityComponent ResolveComponent(
            GameObject gameObject,
            string componentType,
            int componentIndex,
            out ToolCallResult errorResult)
        {
            errorResult = null;
            if (gameObject == null)
            {
                errorResult = McpToolResultFactory.CreateStatus(
                    "not_found",
                    "[Failed] Could not resolve the target GameObject.",
                    false);
                return null;
            }

            var includeTransform = !string.IsNullOrWhiteSpace(componentType);
            var components = GetComponents(gameObject, componentType, includeTransform);
            if (components.Count == 0)
            {
                var label = string.IsNullOrWhiteSpace(componentType)
                    ? $"GameObject '{gameObject.name}'"
                    : $"component '{componentType}' on GameObject '{gameObject.name}'";
                errorResult = McpToolResultFactory.CreateStatus(
                    "not_found",
                    $"[Failed] Could not find {label}.",
                    false);
                return null;
            }

            if (componentIndex < 0 || componentIndex >= components.Count)
            {
                errorResult = McpToolResultFactory.CreateStatus(
                    "not_found",
                    $"[Failed] Component index {componentIndex} is out of range for '{gameObject.name}'.",
                    false);
                return null;
            }

            return components[componentIndex];
        }

        public static List<object> SnapshotComponents(GameObject gameObject, string componentType = null, bool includeTransform = true)
        {
            var ownerSnapshot = UnityObjectSnapshot.FromObject(gameObject).ToDictionary();
            var ownerPath = ownerSnapshot.TryGetValue("hierarchyPath", out var hierarchyPathValue)
                ? hierarchyPathValue as string ?? string.Empty
                : string.Empty;
            var ownerId = ownerSnapshot.TryGetValue("localId", out var localIdValue)
                ? localIdValue as string ?? string.Empty
                : string.Empty;

            var components = GetComponents(gameObject, componentType, includeTransform);
            var items = new List<object>(components.Count);
            for (var index = 0; index < components.Count; index++)
            {
                var component = components[index];
                var snapshot = UnityObjectSnapshot.FromObject(component).ToDictionary();
                snapshot["componentIndex"] = index;
                snapshot["gameObjectId"] = ownerId;
                snapshot["gameObjectPath"] = ownerPath;
                items.Add(snapshot);
            }

            return items;
        }

        private static bool MatchesComponentType(UnityComponent component, string componentType)
        {
            if (component == null || string.IsNullOrWhiteSpace(componentType))
            {
                return false;
            }

            var type = component.GetType();
            return string.Equals(type.Name, componentType, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(type.FullName, componentType, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class McpObjectDataTools
    {
        public static ToolCallResult GetObjectData(UnityEngine.Object target, string label)
        {
            if (target == null)
            {
                return McpToolResultFactory.CreateStatus("not_found", $"[Failed] Could not resolve {label}.", false);
            }

            var serializedObject = new SerializedObject(target);
            var iterator = serializedObject.GetIterator();
            var properties = new List<object>();
            if (iterator.NextVisible(true))
            {
                do
                {
                    properties.Add(new Dictionary<string, object>
                    {
                        ["path"] = iterator.propertyPath,
                        ["displayName"] = iterator.displayName,
                        ["type"] = iterator.propertyType.ToString(),
                        ["value"] = SerializedPropertyValueReader.Read(iterator)
                    });
                }
                while (iterator.NextVisible(false));
            }

            return new ToolCallResult
            {
                Text = $"[Success] Read {properties.Count} serialized field(s) from {label}.",
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = true,
                    ["item"] = UnityObjectSnapshot.FromObject(target).ToDictionary(),
                    ["properties"] = properties
                }
            };
        }

        public static ToolCallResult ModifyObject(
            UnityEngine.Object target,
            Dictionary<string, object> properties,
            string label,
            bool markSceneDirty = true,
            bool recordUndo = true)
        {
            if (target == null)
            {
                return McpToolResultFactory.CreateStatus("not_found", $"[Failed] Could not resolve {label}.", false);
            }

            if (properties == null || properties.Count == 0)
            {
                return McpToolResultFactory.CreateStatus("noop", $"[Success] No property changes requested for {label}.", true);
            }

            if (recordUndo)
            {
                Undo.RecordObject(target, $"MCP Modify {label}");
            }

            var serializedObject = new SerializedObject(target);
            var changed = 0;
            foreach (var pair in properties)
            {
                var property = serializedObject.FindProperty(pair.Key);
                if (property == null)
                {
                    continue;
                }

                if (SerializedPropertyValueReader.TryWrite(property, pair.Value))
                {
                    changed++;
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
            if (markSceneDirty)
            {
                MarkSceneDirty(target);
            }

            return new ToolCallResult
            {
                Text = $"[Success] Updated {changed} serialized field(s) on {label}.",
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = true,
                    ["changedCount"] = changed,
                    ["item"] = UnityObjectSnapshot.FromObject(target).ToDictionary()
                }
            };
        }

        private static void MarkSceneDirty(UnityEngine.Object target)
        {
            switch (target)
            {
                case UnityComponent component when component.gameObject.scene.IsValid():
                    EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
                    break;
                case GameObject gameObject when gameObject.scene.IsValid():
                    EditorSceneManager.MarkSceneDirty(gameObject.scene);
                    break;
            }
        }
    }

    internal static class McpHierarchyTools
    {
        public static Dictionary<string, object> CreateGameObjectTreeSnapshot(GameObject gameObject)
        {
            var snapshot = UnityObjectSnapshot.FromObject(gameObject).ToDictionary();
            snapshot["activeSelf"] = gameObject.activeSelf;
            snapshot["components"] = McpComponentLocator.SnapshotComponents(gameObject);

            var children = new List<object>(gameObject.transform.childCount);
            for (var index = 0; index < gameObject.transform.childCount; index++)
            {
                children.Add(CreateGameObjectTreeSnapshot(gameObject.transform.GetChild(index).gameObject));
            }

            snapshot["children"] = children;
            return snapshot;
        }

        public static GameObject ResolveGameObjectByHierarchyPath(GameObject root, string hierarchyPath)
        {
            if (root == null)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(hierarchyPath))
            {
                return root;
            }

            var normalizedPath = hierarchyPath.Replace('\\', '/').Trim('/');
            if (string.Equals(normalizedPath, root.name, StringComparison.Ordinal))
            {
                return root;
            }

            if (normalizedPath.StartsWith(root.name + "/", StringComparison.Ordinal))
            {
                normalizedPath = normalizedPath.Substring(root.name.Length + 1);
            }

            var current = root.transform;
            foreach (var segment in normalizedPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var matchedChild = default(Transform);
                for (var index = 0; index < current.childCount; index++)
                {
                    var child = current.GetChild(index);
                    if (!string.Equals(child.name, segment, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    matchedChild = child;
                    break;
                }

                if (matchedChild == null)
                {
                    return null;
                }

                current = matchedChild;
            }

            return current.gameObject;
        }
    }
}
