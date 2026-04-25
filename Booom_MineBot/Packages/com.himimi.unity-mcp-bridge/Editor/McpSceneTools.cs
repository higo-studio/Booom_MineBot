using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using McpBridge;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityComponent = UnityEngine.Component;

namespace McpBridge.Editor
{
    [McpPluginToolType]
    internal sealed class McpSceneTools
    {
        [McpPluginTool("unity.scene_list_opened", Title = "List Open Unity Scenes")]
        [Description("Lists the scenes currently open in the targeted Unity editor instance.")]
        public ToolCallResult ListOpenedScenes()
        {
            return MainThread.Instance.Run(() =>
            {
                var items = new List<object>();
                for (var index = 0; index < SceneManager.sceneCount; index++)
                {
                    items.Add(SceneSnapshot.FromScene(SceneManager.GetSceneAt(index)).ToDictionary());
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Found {items.Count} open scene(s).",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["items"] = items
                    }
                };
            });
        }

        [McpPluginTool("unity.scene_get_data", Title = "Get Unity Scene Data")]
        [Description("Returns the root objects for a scene. If scenePath is omitted, uses the active scene.")]
        public ToolCallResult GetSceneData(
            [Description("Optional scene path. If omitted, the active scene is used.")]
            string scenePath = null)
        {
            return MainThread.Instance.Run(() =>
            {
                var scene = ResolveScene(scenePath);
                if (!scene.IsValid())
                {
                    return CreateStatus("not_found", $"[Failed] Could not find scene '{scenePath ?? "<active>"}'.", false);
                }

                var roots = new List<object>();
                foreach (var root in scene.GetRootGameObjects())
                {
                    roots.Add(UnityObjectSnapshot.FromObject(root).ToDictionary());
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Scene '{scene.path}' contains {roots.Count} root object(s).",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["scene"] = SceneSnapshot.FromScene(scene).ToDictionary(),
                        ["roots"] = roots
                    }
                };
            });
        }

        [McpPluginTool("unity.scene_open", Title = "Open Unity Scene")]
        [Description("Opens a scene in the Unity editor.")]
        public ToolCallResult OpenScene(
            [Description("Project-relative path to the scene asset.")]
            string scenePath,
            [Description("When true, opens the scene additively instead of replacing currently open scenes.")]
            bool additive = false)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("open a scene", out var blocked))
                {
                    return blocked;
                }

                var mode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
                var scene = EditorSceneManager.OpenScene(scenePath, mode);
                return new ToolCallResult
                {
                    Text = $"[Success] Opened scene '{scene.path}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["scene"] = SceneSnapshot.FromScene(scene).ToDictionary()
                    }
                };
            });
        }

        [McpPluginTool("unity.scene_save", Title = "Save Unity Scene")]
        [Description("Saves the active scene or a specific open scene. When outputPath is provided, performs Save As.")]
        public ToolCallResult SaveScene(
            [Description("Optional currently open scene path. If omitted, saves the active scene.")]
            string scenePath = null,
            [Description("Optional output path for Save As.")]
            string outputPath = null)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("save a scene", out var blocked))
                {
                    return blocked;
                }

                var scene = ResolveScene(scenePath);
                if (!scene.IsValid())
                {
                    return CreateStatus("not_found", $"[Failed] Could not find scene '{scenePath ?? "<active>"}'.", false);
                }

                var finalPath = string.IsNullOrWhiteSpace(outputPath) ? scene.path : outputPath;
                var saved = EditorSceneManager.SaveScene(scene, finalPath, true);
                return new ToolCallResult
                {
                    Text = saved ? $"[Success] Saved scene '{finalPath}'." : $"[Failed] Could not save scene '{finalPath}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = saved,
                        ["scene"] = SceneSnapshot.FromScene(SceneManager.GetSceneByPath(finalPath)).ToDictionary()
                    }
                };
            });
        }

        [McpPluginTool("unity.scene_set_active", Title = "Set Active Unity Scene")]
        [Description("Sets the active scene for the Unity editor.")]
        public ToolCallResult SetActiveScene(
            [Description("Path to an open scene.")]
            string scenePath)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("set the active scene", out var blocked))
                {
                    return blocked;
                }

                var scene = SceneManager.GetSceneByPath(scenePath);
                if (!scene.IsValid())
                {
                    return CreateStatus("not_found", $"[Failed] Could not find open scene '{scenePath}'.", false);
                }

                EditorSceneManager.SetActiveScene(scene);
                return new ToolCallResult
                {
                    Text = $"[Success] Active scene set to '{scene.path}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["scene"] = SceneSnapshot.FromScene(scene).ToDictionary()
                    }
                };
            });
        }

        [McpPluginTool("unity.scene_create", Title = "Create Unity Scene")]
        [Description("Creates and saves a new empty scene asset.")]
        public ToolCallResult CreateScene(
            [Description("Project-relative path for the new scene asset.")]
            string scenePath,
            [Description("When true, keeps currently open scenes and creates the new scene additively.")]
            bool additive = false)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("create a scene", out var blocked))
                {
                    return blocked;
                }

                var setup = additive ? NewSceneSetup.EmptyScene : NewSceneSetup.EmptyScene;
                var mode = additive ? NewSceneMode.Additive : NewSceneMode.Single;
                var scene = EditorSceneManager.NewScene(setup, mode);
                EditorSceneManager.SaveScene(scene, scenePath, true);
                scene = SceneManager.GetSceneByPath(scenePath);
                return new ToolCallResult
                {
                    Text = $"[Success] Created scene '{scenePath}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["scene"] = SceneSnapshot.FromScene(scene).ToDictionary()
                    }
                };
            });
        }

        [McpPluginTool("unity.gameobject_find", Title = "Find Unity GameObjects")]
        [Description("Finds GameObjects by name, tag, or component type in the targeted editor instance.")]
        public ToolCallResult FindGameObjects(
            [Description("Optional exact object name.")]
            string name = null,
            [Description("Optional exact tag.")]
            string tag = null,
            [Description("Optional full component type name, such as UnityEngine.Camera.")]
            string componentType = null,
            [Description("Optional scene path to limit the search.")]
            string scenePath = null,
            [Description("When true, includes inactive objects.")]
            bool includeInactive = true)
        {
            return MainThread.Instance.Run(() =>
            {
                var matches = new List<object>();
                foreach (var gameObject in EnumerateGameObjects(scenePath, includeInactive))
                {
                    if (!string.IsNullOrWhiteSpace(name) && !string.Equals(gameObject.name, name, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(tag) && !string.Equals(gameObject.tag, tag, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(componentType) && gameObject.GetComponent(componentType) == null)
                    {
                        continue;
                    }

                    matches.Add(UnityObjectSnapshot.FromObject(gameObject).ToDictionary());
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Found {matches.Count} matching GameObject(s).",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["items"] = matches
                    }
                };
            });
        }

        [McpPluginTool("unity.gameobject_create", Title = "Create Unity GameObject")]
        [Description("Creates a new GameObject in the targeted scene.")]
        public ToolCallResult CreateGameObject(
            [Description("Name for the new object.")]
            string name = "GameObject",
            [Description("Optional primitive type: empty, cube, sphere, capsule, cylinder, plane, quad.")]
            string primitiveType = "empty",
            [Description("Optional parent object identifier.")]
            string parentObjectId = null,
            [Description("Optional scene path. If omitted, uses the active scene.")]
            string scenePath = null)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("create a GameObject", out var blocked))
                {
                    return blocked;
                }

                var gameObject = CreateGameObjectInternal(name, primitiveType);
                var scene = ResolveScene(scenePath);
                if (scene.IsValid())
                {
                    SceneManager.MoveGameObjectToScene(gameObject, scene);
                }

                if (!string.IsNullOrWhiteSpace(parentObjectId))
                {
                    var parent = UnityObjectLocator.TryResolveObject(parentObjectId) as GameObject;
                    if (parent == null)
                    {
                        UnityEngine.Object.DestroyImmediate(gameObject);
                        return CreateStatus("not_found", $"[Failed] Could not resolve parent '{parentObjectId}'.", false);
                    }

                    Undo.SetTransformParent(gameObject.transform, parent.transform, "MCP Set Parent");
                }

                Undo.RegisterCreatedObjectUndo(gameObject, "MCP Create GameObject");
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
                return new ToolCallResult
                {
                    Text = $"[Success] Created GameObject '{gameObject.name}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["item"] = UnityObjectSnapshot.FromObject(gameObject).ToDictionary(),
                        ["sceneDirty"] = gameObject.scene.isDirty
                    }
                };
            });
        }

        [McpPluginTool("unity.gameobject_destroy", Title = "Destroy Unity GameObject")]
        [Description("Destroys a GameObject by identifier.")]
        public ToolCallResult DestroyGameObject(
            [Description("GameObject identifier.")]
            string objectId)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("destroy a GameObject", out var blocked))
                {
                    return blocked;
                }

                var gameObject = UnityObjectLocator.TryResolveObject(objectId) as GameObject;
                if (gameObject == null)
                {
                    return CreateStatus("not_found", $"[Failed] Could not resolve GameObject '{objectId}'.", false);
                }

                var scene = gameObject.scene;
                Undo.DestroyObjectImmediate(gameObject);
                if (scene.IsValid())
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                return CreateStatus("destroyed", "[Success] GameObject destroyed.", true);
            });
        }

        [McpPluginTool("unity.gameobject_duplicate", Title = "Duplicate Unity GameObject")]
        [Description("Duplicates a GameObject by identifier.")]
        public ToolCallResult DuplicateGameObject(
            [Description("GameObject identifier.")]
            string objectId)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("duplicate a GameObject", out var blocked))
                {
                    return blocked;
                }

                var gameObject = UnityObjectLocator.TryResolveObject(objectId) as GameObject;
                if (gameObject == null)
                {
                    return CreateStatus("not_found", $"[Failed] Could not resolve GameObject '{objectId}'.", false);
                }

                var duplicate = UnityEngine.Object.Instantiate(gameObject, gameObject.transform.parent);
                duplicate.name = gameObject.name;
                Undo.RegisterCreatedObjectUndo(duplicate, "MCP Duplicate GameObject");
                EditorSceneManager.MarkSceneDirty(duplicate.scene);
                return new ToolCallResult
                {
                    Text = $"[Success] Duplicated GameObject '{duplicate.name}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["item"] = UnityObjectSnapshot.FromObject(duplicate).ToDictionary(),
                        ["sceneDirty"] = duplicate.scene.isDirty
                    }
                };
            });
        }

        [McpPluginTool("unity.gameobject_modify", Title = "Modify Unity GameObject")]
        [Description("Updates a GameObject's basic fields and transform.")]
        public ToolCallResult ModifyGameObject(
            [Description("GameObject identifier.")]
            string objectId,
            [Description("Optional new name.")]
            string name = null,
            [Description("Optional active state.")]
            bool? activeSelf = null,
            [Description("Optional tag.")]
            string tag = null,
            [Description("Optional layer. Use -1 to leave unchanged.")]
            int layer = -1,
            [Description("Optional local position as [x,y,z].")]
            double[] localPosition = null,
            [Description("Optional local rotation Euler angles as [x,y,z].")]
            double[] localEulerAngles = null,
            [Description("Optional local scale as [x,y,z].")]
            double[] localScale = null)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("modify a GameObject", out var blocked))
                {
                    return blocked;
                }

                var gameObject = UnityObjectLocator.TryResolveObject(objectId) as GameObject;
                if (gameObject == null)
                {
                    return CreateStatus("not_found", $"[Failed] Could not resolve GameObject '{objectId}'.", false);
                }

                Undo.RecordObject(gameObject, "MCP Modify GameObject");
                Undo.RecordObject(gameObject.transform, "MCP Modify Transform");

                if (!string.IsNullOrWhiteSpace(name))
                {
                    gameObject.name = name;
                }

                if (activeSelf.HasValue)
                {
                    gameObject.SetActive(activeSelf.Value);
                }

                if (!string.IsNullOrWhiteSpace(tag))
                {
                    gameObject.tag = tag;
                }

                if (layer >= 0)
                {
                    gameObject.layer = layer;
                }

                ApplyVector(localPosition, vector => gameObject.transform.localPosition = vector);
                ApplyVector(localEulerAngles, vector => gameObject.transform.localEulerAngles = vector);
                ApplyVector(localScale, vector => gameObject.transform.localScale = vector);

                EditorUtility.SetDirty(gameObject);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
                return new ToolCallResult
                {
                    Text = $"[Success] Updated GameObject '{gameObject.name}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["item"] = UnityObjectSnapshot.FromObject(gameObject).ToDictionary(),
                        ["sceneDirty"] = gameObject.scene.isDirty
                    }
                };
            });
        }

        [McpPluginTool("unity.gameobject_set_parent", Title = "Set Unity GameObject Parent")]
        [Description("Reparents a GameObject in the hierarchy.")]
        public ToolCallResult SetParent(
            [Description("GameObject identifier.")]
            string objectId,
            [Description("Optional parent GameObject identifier. Omit or empty to unparent.")]
            string parentObjectId = null,
            [Description("When true, keeps world transform while reparenting.")]
            bool worldPositionStays = true)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("reparent a GameObject", out var blocked))
                {
                    return blocked;
                }

                var gameObject = UnityObjectLocator.TryResolveObject(objectId) as GameObject;
                if (gameObject == null)
                {
                    return CreateStatus("not_found", $"[Failed] Could not resolve GameObject '{objectId}'.", false);
                }

                Transform parentTransform = null;
                if (!string.IsNullOrWhiteSpace(parentObjectId))
                {
                    parentTransform = (UnityObjectLocator.TryResolveObject(parentObjectId) as GameObject)?.transform;
                    if (parentTransform == null)
                    {
                        return CreateStatus("not_found", $"[Failed] Could not resolve parent '{parentObjectId}'.", false);
                    }
                }

                Undo.SetTransformParent(gameObject.transform, parentTransform, "MCP Set Parent");
                if (parentTransform == null)
                {
                    gameObject.transform.SetParent(null, worldPositionStays);
                }
                else
                {
                    gameObject.transform.SetParent(parentTransform, worldPositionStays);
                }

                EditorSceneManager.MarkSceneDirty(gameObject.scene);
                return new ToolCallResult
                {
                    Text = $"[Success] Updated parent for '{gameObject.name}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["item"] = UnityObjectSnapshot.FromObject(gameObject).ToDictionary()
                    }
                };
            });
        }

        [McpPluginTool("unity.component_add", Title = "Add Unity Component")]
        [Description("Adds a component to a GameObject.")]
        public ToolCallResult AddComponent(
            [Description("Target GameObject identifier.")]
            string gameObjectId,
            [Description("Full component type name, such as UnityEngine.Camera.")]
            string componentType)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("add a component", out var blocked))
                {
                    return blocked;
                }

                var gameObject = UnityObjectLocator.TryResolveObject(gameObjectId) as GameObject;
                if (gameObject == null)
                {
                    return CreateStatus("not_found", $"[Failed] Could not resolve GameObject '{gameObjectId}'.", false);
                }

                var type = UnityTypeLocator.FindType(componentType, typeof(UnityComponent));
                if (type == null)
                {
                    return CreateStatus("not_found", $"[Failed] Could not find component type '{componentType}'.", false);
                }

                var component = Undo.AddComponent(gameObject, type);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
                return new ToolCallResult
                {
                    Text = $"[Success] Added component '{type.FullName}' to '{gameObject.name}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["item"] = UnityObjectSnapshot.FromObject(component).ToDictionary()
                    }
                };
            });
        }

        [McpPluginTool("unity.component_destroy", Title = "Destroy Unity Component")]
        [Description("Destroys a component by identifier.")]
        public ToolCallResult DestroyComponent(
            [Description("Component identifier.")]
            string componentId)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("destroy a component", out var blocked))
                {
                    return blocked;
                }

                var component = UnityObjectLocator.TryResolveObject(componentId) as UnityComponent;
                if (component == null)
                {
                    return CreateStatus("not_found", $"[Failed] Could not resolve component '{componentId}'.", false);
                }

                var scene = component.gameObject.scene;
                Undo.DestroyObjectImmediate(component);
                EditorSceneManager.MarkSceneDirty(scene);
                return CreateStatus("destroyed", "[Success] Component destroyed.", true);
            });
        }

        [McpPluginTool("unity.component_get", Title = "Get Unity Component Data")]
        [Description("Returns serialized data for a component.")]
        public ToolCallResult GetComponentData(
            [Description("Component identifier.")]
            string componentId)
        {
            return MainThread.Instance.Run(() => GetObjectDataInternal(componentId, "component"));
        }

        [McpPluginTool("unity.component_modify", Title = "Modify Unity Component")]
        [Description("Updates serialized fields on a component. Property keys use SerializedProperty paths.")]
        public ToolCallResult ModifyComponent(
            [Description("Component identifier.")]
            string componentId,
            [Description("Map of SerializedProperty paths to primitive values.")]
            Dictionary<string, object> properties)
        {
            return MainThread.Instance.Run(() => ModifyObjectInternal(componentId, properties, "component"));
        }

        [McpPluginTool("unity.component_list_all", Title = "List Unity Component Types")]
        [Description("Lists known component types available in the current editor domain.")]
        public ToolCallResult ListAllComponentTypes()
        {
            return MainThread.Instance.Run(() =>
            {
                var items = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly =>
                    {
                        try { return assembly.GetTypes(); }
                        catch { return Array.Empty<Type>(); }
                    })
                    .Where(type => type != null && !type.IsAbstract && typeof(UnityComponent).IsAssignableFrom(type))
                    .Select(type => type.FullName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .Cast<object>()
                    .ToList();

                var count = items.Count;
                return new ToolCallResult
                {
                    Text = $"[Success] Found {count} component type(s).",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["items"] = items
                    }
                };
            });
        }

        [McpPluginTool("unity.object_get_data", Title = "Get Unity Object Data")]
        [Description("Returns serialized data for any Unity object resolved by identifier.")]
        public ToolCallResult GetObjectData(
            [Description("Unity object identifier.")]
            string objectId)
        {
            return MainThread.Instance.Run(() => GetObjectDataInternal(objectId, "object"));
        }

        [McpPluginTool("unity.object_modify", Title = "Modify Unity Object")]
        [Description("Updates serialized fields on any Unity object resolved by identifier. Property keys use SerializedProperty paths.")]
        public ToolCallResult ModifyObject(
            [Description("Unity object identifier.")]
            string objectId,
            [Description("Map of SerializedProperty paths to primitive values.")]
            Dictionary<string, object> properties)
        {
            return MainThread.Instance.Run(() => ModifyObjectInternal(objectId, properties, "object"));
        }

        private static ToolCallResult GetObjectDataInternal(string objectId, string label)
        {
            var target = UnityObjectLocator.TryResolveObject(objectId);
            if (target == null)
            {
                return CreateStatus("not_found", $"[Failed] Could not resolve {label} '{objectId}'.", false);
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

        private static ToolCallResult ModifyObjectInternal(string objectId, Dictionary<string, object> properties, string label)
        {
            if (McpEditorToolGuards.TryBlockForTransition($"modify a {label}", out var blocked))
            {
                return blocked;
            }

            var target = UnityObjectLocator.TryResolveObject(objectId);
            if (target == null)
            {
                return CreateStatus("not_found", $"[Failed] Could not resolve {label} '{objectId}'.", false);
            }

            if (properties == null || properties.Count == 0)
            {
                return CreateStatus("noop", $"[Success] No property changes requested for {label}.", true);
            }

            Undo.RecordObject(target, $"MCP Modify {label}");
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

            if (target is UnityComponent component)
            {
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
            }
            else if (target is GameObject gameObject)
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
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

        private static IEnumerable<GameObject> EnumerateGameObjects(string scenePath, bool includeInactive)
        {
            if (!string.IsNullOrWhiteSpace(scenePath))
            {
                var scene = SceneManager.GetSceneByPath(scenePath);
                if (!scene.IsValid())
                {
                    yield break;
                }

                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive))
                    {
                        yield return child.gameObject;
                    }
                }

                yield break;
            }

            foreach (var gameObject in UnityEngine.Object.FindObjectsByType<GameObject>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (EditorUtility.IsPersistent(gameObject))
                {
                    continue;
                }

                yield return gameObject;
            }
        }

        private static Scene ResolveScene(string scenePath)
        {
            if (string.IsNullOrWhiteSpace(scenePath))
            {
                return SceneManager.GetActiveScene();
            }

            var scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.IsValid())
            {
                return scene;
            }

            return default;
        }

        private static GameObject CreateGameObjectInternal(string name, string primitiveType)
        {
            primitiveType = string.IsNullOrWhiteSpace(primitiveType) ? "empty" : primitiveType.Trim().ToLowerInvariant();
            if (primitiveType == "empty")
            {
                return new GameObject(name);
            }

            if (Enum.TryParse<PrimitiveType>(primitiveType, true, out var parsed))
            {
                var created = GameObject.CreatePrimitive(parsed);
                created.name = name;
                return created;
            }

            return new GameObject(name);
        }

        private static void ApplyVector(double[] values, Action<Vector3> apply)
        {
            if (values == null || values.Length < 3)
            {
                return;
            }

            apply(new Vector3((float)values[0], (float)values[1], (float)values[2]));
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

    internal sealed class SceneSnapshot
    {
        public string Path;
        public string Name;
        public bool IsLoaded;
        public bool IsDirty;
        public bool IsActive;

        public static SceneSnapshot FromScene(Scene scene)
        {
            return new SceneSnapshot
            {
                Path = scene.path,
                Name = scene.name,
                IsLoaded = scene.isLoaded,
                IsDirty = scene.isDirty,
                IsActive = scene == SceneManager.GetActiveScene()
            };
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["path"] = Path,
                ["name"] = Name,
                ["isLoaded"] = IsLoaded,
                ["isDirty"] = IsDirty,
                ["isActive"] = IsActive
            };
        }
    }

    internal static class UnityTypeLocator
    {
        public static Type FindType(string typeName, Type assignableTo = null)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type;
                try { type = assembly.GetType(typeName, false); }
                catch { type = null; }
                if (type == null)
                {
                    continue;
                }

                if (assignableTo == null || assignableTo.IsAssignableFrom(type))
                {
                    return type;
                }
            }

            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try { return assembly.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .FirstOrDefault(type =>
                    type != null &&
                    (assignableTo == null || assignableTo.IsAssignableFrom(type)) &&
                    (string.Equals(type.FullName, typeName, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(type.Name, typeName, StringComparison.OrdinalIgnoreCase)));
        }
    }

    internal static class SerializedPropertyValueReader
    {
        public static object Read(SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Boolean => property.boolValue,
                SerializedPropertyType.Integer => property.intValue,
                SerializedPropertyType.Float => property.floatValue,
                SerializedPropertyType.String => property.stringValue,
                SerializedPropertyType.Enum => property.enumDisplayNames != null && property.enumValueIndex >= 0 && property.enumValueIndex < property.enumDisplayNames.Length
                    ? property.enumDisplayNames[property.enumValueIndex]
                    : property.enumValueIndex,
                SerializedPropertyType.Vector2 => ToArray(property.vector2Value),
                SerializedPropertyType.Vector3 => ToArray(property.vector3Value),
                SerializedPropertyType.Vector4 => ToArray(property.vector4Value),
                SerializedPropertyType.Color => ToArray(property.colorValue),
                SerializedPropertyType.ObjectReference => property.objectReferenceValue != null
                    ? UnityObjectSnapshot.FromObject(property.objectReferenceValue).ToDictionary()
                    : null,
                _ => property.displayName
            };
        }

        public static bool TryWrite(SerializedProperty property, object rawValue)
        {
            try
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        property.boolValue = Convert.ToBoolean(rawValue);
                        return true;
                    case SerializedPropertyType.Integer:
                        property.intValue = Convert.ToInt32(rawValue);
                        return true;
                    case SerializedPropertyType.Float:
                        property.floatValue = Convert.ToSingle(rawValue);
                        return true;
                    case SerializedPropertyType.String:
                        property.stringValue = rawValue?.ToString() ?? string.Empty;
                        return true;
                    case SerializedPropertyType.Enum:
                        if (rawValue is string enumName)
                        {
                            var index = Array.IndexOf(property.enumNames, enumName);
                            if (index >= 0)
                            {
                                property.enumValueIndex = index;
                                return true;
                            }
                        }

                        property.enumValueIndex = Convert.ToInt32(rawValue);
                        return true;
                    case SerializedPropertyType.Vector2:
                        if (TryReadVector(rawValue, out var vector2Values, 2))
                        {
                            property.vector2Value = new Vector2((float)vector2Values[0], (float)vector2Values[1]);
                            return true;
                        }

                        return false;
                    case SerializedPropertyType.Vector3:
                        if (TryReadVector(rawValue, out var vector3Values, 3))
                        {
                            property.vector3Value = new Vector3((float)vector3Values[0], (float)vector3Values[1], (float)vector3Values[2]);
                            return true;
                        }

                        return false;
                    case SerializedPropertyType.Vector4:
                        if (TryReadVector(rawValue, out var vector4Values, 4))
                        {
                            property.vector4Value = new Vector4((float)vector4Values[0], (float)vector4Values[1], (float)vector4Values[2], (float)vector4Values[3]);
                            return true;
                        }

                        return false;
                    case SerializedPropertyType.Color:
                        if (TryReadVector(rawValue, out var colorValues, 4))
                        {
                            property.colorValue = new Color((float)colorValues[0], (float)colorValues[1], (float)colorValues[2], (float)colorValues[3]);
                            return true;
                        }

                        return false;
                    case SerializedPropertyType.ObjectReference:
                        var referenceId = rawValue?.ToString();
                        property.objectReferenceValue = UnityObjectLocator.TryResolveObject(referenceId);
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static object ToArray(Vector2 vector)
        {
            return new[] { vector.x, vector.y };
        }

        private static object ToArray(Vector3 vector)
        {
            return new[] { vector.x, vector.y, vector.z };
        }

        private static object ToArray(Vector4 vector)
        {
            return new[] { vector.x, vector.y, vector.z, vector.w };
        }

        private static object ToArray(Color color)
        {
            return new[] { color.r, color.g, color.b, color.a };
        }

        private static bool TryReadVector(object rawValue, out double[] values, int count)
        {
            values = null;
            if (rawValue is not IList<object> list || list.Count < count)
            {
                if (rawValue is not IList<double> doubles || doubles.Count < count)
                {
                    return false;
                }

                values = doubles.Take(count).ToArray();
                return true;
            }

            values = new double[count];
            for (var index = 0; index < count; index++)
            {
                values[index] = Convert.ToDouble(list[index]);
            }

            return true;
        }
    }
}
