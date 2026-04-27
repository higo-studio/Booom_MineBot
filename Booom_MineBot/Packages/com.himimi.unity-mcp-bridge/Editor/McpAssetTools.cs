using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using McpBridge;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityComponent = UnityEngine.Component;

namespace McpBridge.Editor
{
    [McpPluginToolType]
    internal sealed class McpAssetTools
    {
        [McpPluginTool("unity.asset_find", Title = "Find Unity Assets")]
        [Description("Finds assets in the Unity project by AssetDatabase search filter.")]
        public ToolCallResult FindAssets(
            [Description("AssetDatabase search filter, for example 't:Prefab Player'.")]
            string filter = "",
            [Description("Optional folders to limit the search.")]
            string[] folders = null)
        {
            return MainThread.Instance.Run(() =>
            {
                var guids = AssetDatabase.FindAssets(filter ?? string.Empty, folders);
                var items = new List<object>(guids.Length);
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    items.Add(AssetSnapshot.FromPath(path).ToDictionary());
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Found {items.Count} asset(s).",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["items"] = items
                    }
                };
            });
        }

        [McpPluginTool("unity.asset_read", Title = "Read Unity Asset Metadata")]
        [Description("Reads metadata for an asset path and loads the main asset object when possible.")]
        public ToolCallResult ReadAsset(
            [Description("Project-relative asset path.")]
            string assetPath)
        {
            return MainThread.Instance.Run(() =>
            {
                var normalizedPath = ProjectPathGuards.RequireProjectAssetPath(assetPath, out var errorResult);
                if (normalizedPath == null)
                {
                    return errorResult;
                }

                if (!File.Exists(Path.GetFullPath(normalizedPath)) && !AssetDatabase.IsValidFolder(normalizedPath))
                {
                    return CreateStatus("not_found", $"[Failed] Asset '{normalizedPath}' does not exist.", false);
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Read asset '{normalizedPath}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["asset"] = AssetSnapshot.FromPath(normalizedPath).ToDictionary()
                    }
                };
            });
        }

        [McpPluginTool("unity.asset_move", Title = "Move Unity Asset")]
        [Description("Moves or renames an asset within the Unity project.")]
        public ToolCallResult MoveAsset(
            [Description("Source project-relative asset path.")]
            string assetPath,
            [Description("Destination project-relative asset path.")]
            string destinationPath)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("move assets", out var blocked))
                {
                    return blocked;
                }

                var source = ProjectPathGuards.RequireProjectAssetPath(assetPath, out var sourceError);
                if (source == null) return sourceError;
                var destination = ProjectPathGuards.RequireProjectAssetPath(destinationPath, out var destinationError);
                if (destination == null) return destinationError;

                var error = AssetDatabase.MoveAsset(source, destination);
                if (!string.IsNullOrEmpty(error))
                {
                    return CreateStatus("failed", $"[Failed] Could not move asset: {error}", false);
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Moved asset to '{destination}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["asset"] = AssetSnapshot.FromPath(destination).ToDictionary()
                    }
                };
            });
        }

        [McpPluginTool("unity.asset_copy", Title = "Copy Unity Asset")]
        [Description("Copies an asset within the Unity project.")]
        public ToolCallResult CopyAsset(
            [Description("Source project-relative asset path.")]
            string assetPath,
            [Description("Destination project-relative asset path.")]
            string destinationPath)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("copy assets", out var blocked))
                {
                    return blocked;
                }

                var source = ProjectPathGuards.RequireProjectAssetPath(assetPath, out var sourceError);
                if (source == null) return sourceError;
                var destination = ProjectPathGuards.RequireProjectAssetPath(destinationPath, out var destinationError);
                if (destination == null) return destinationError;

                var copied = AssetDatabase.CopyAsset(source, destination);
                if (!copied)
                {
                    return CreateStatus("failed", $"[Failed] Could not copy asset to '{destination}'.", false);
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Copied asset to '{destination}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["asset"] = AssetSnapshot.FromPath(destination).ToDictionary()
                    }
                };
            });
        }

        [McpPluginTool("unity.asset_delete", Title = "Delete Unity Asset")]
        [Description("Deletes an asset from the Unity project.")]
        public ToolCallResult DeleteAsset(
            [Description("Project-relative asset path.")]
            string assetPath)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("delete assets", out var blocked))
                {
                    return blocked;
                }

                var path = ProjectPathGuards.RequireProjectAssetPath(assetPath, out var errorResult);
                if (path == null) return errorResult;

                var deleted = AssetDatabase.DeleteAsset(path);
                return CreateStatus(deleted ? "deleted" : "failed", deleted
                    ? $"[Success] Deleted asset '{path}'."
                    : $"[Failed] Could not delete asset '{path}'.", deleted);
            });
        }

        [McpPluginTool("unity.asset_refresh", Title = "Refresh Unity Assets")]
        [Description("Refreshes the Unity AssetDatabase.")]
        public ToolCallResult RefreshAssets()
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("refresh assets", out var blocked))
                {
                    return blocked;
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                return CreateStatus("refreshed", "[Success] Unity AssetDatabase refreshed.", true);
            });
        }

        [McpPluginTool("unity.prefab_instantiate", Title = "Instantiate Unity Prefab")]
        [Description("Instantiates a prefab asset into the active or target scene.")]
        public ToolCallResult InstantiatePrefab(
            [Description("Prefab asset path.")]
            string assetPath,
            [Description("Optional target scene path.")]
            string scenePath = null,
            [Description("Optional parent object identifier.")]
            string parentObjectId = null)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("instantiate prefabs", out var blocked))
                {
                    return blocked;
                }

                var path = ProjectPathGuards.RequireProjectAssetPath(assetPath, out var errorResult);
                if (path == null) return errorResult;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    return CreateStatus("not_found", $"[Failed] Could not load prefab '{path}'.", false);
                }

                var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                {
                    return CreateStatus("failed", $"[Failed] Could not instantiate prefab '{path}'.", false);
                }

                if (!string.IsNullOrWhiteSpace(scenePath))
                {
                    var scene = SceneManager.GetSceneByPath(scenePath);
                    if (scene.IsValid())
                    {
                        SceneManager.MoveGameObjectToScene(instance, scene);
                    }
                }

                if (!string.IsNullOrWhiteSpace(parentObjectId))
                {
                    var parent = UnityObjectLocator.TryResolveObject(parentObjectId) as GameObject;
                    if (parent != null)
                    {
                        instance.transform.SetParent(parent.transform, true);
                    }
                }

                Undo.RegisterCreatedObjectUndo(instance, "MCP Instantiate Prefab");
                EditorSceneManager.MarkSceneDirty(instance.scene);
                return new ToolCallResult
                {
                    Text = $"[Success] Instantiated prefab '{path}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["item"] = UnityObjectSnapshot.FromObject(instance).ToDictionary(),
                        ["prefabPath"] = path
                    }
                };
            });
        }

        [McpPluginTool("unity.prefab_create", Title = "Create Unity Prefab")]
        [Description("Saves a scene GameObject as a prefab asset.")]
        public ToolCallResult CreatePrefab(
            [Description("GameObject identifier.")]
            string gameObjectId,
            [Description("Destination prefab asset path.")]
            string assetPath)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("create prefabs", out var blocked))
                {
                    return blocked;
                }

                var gameObject = UnityObjectLocator.TryResolveObject(gameObjectId) as GameObject;
                if (gameObject == null)
                {
                    return CreateStatus("not_found", $"[Failed] Could not resolve GameObject '{gameObjectId}'.", false);
                }

                var path = ProjectPathGuards.RequireProjectAssetPath(assetPath, out var errorResult);
                if (path == null) return errorResult;

                var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, path, InteractionMode.AutomatedAction, out var success);
                if (!success || prefab == null)
                {
                    return CreateStatus("failed", $"[Failed] Could not save prefab '{path}'.", false);
                }

                return new ToolCallResult
                {
                    Text = $"[Success] Saved prefab '{path}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["asset"] = AssetSnapshot.FromPath(path).ToDictionary()
                    }
                };
            });
        }

        [McpPluginTool("unity.prefab_get_data", Title = "Get Unity Prefab Data")]
        [Description("Loads a prefab asset through LoadPrefabContents and returns its hierarchy and components.")]
        public ToolCallResult GetPrefabData(
            [Description("Prefab asset path.")]
            string assetPath)
        {
            return MainThread.Instance.Run(() =>
                WithPrefabContents(assetPath, "inspect prefabs", (path, root) => new ToolCallResult
                {
                    Text = $"[Success] Read prefab '{path}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["prefabPath"] = path,
                        ["asset"] = AssetSnapshot.FromPath(path).ToDictionary(),
                        ["root"] = McpHierarchyTools.CreateGameObjectTreeSnapshot(root)
                    }
                }));
        }

        [McpPluginTool("unity.prefab_gameobject_modify", Title = "Modify Unity Prefab GameObject")]
        [Description("Loads a prefab asset and updates a GameObject inside it by hierarchy path.")]
        public ToolCallResult ModifyPrefabGameObject(
            [Description("Prefab asset path.")]
            string assetPath,
            [Description("Optional hierarchy path inside the prefab. Omit to target the prefab root.")]
            string gameObjectPath = null,
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
                EditPrefabContents(assetPath, "modify prefabs", (path, root) =>
                {
                    var gameObject = ResolvePrefabGameObject(root, gameObjectPath, out var errorResult);
                    if (gameObject == null)
                    {
                        return errorResult;
                    }

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
                    EditorUtility.SetDirty(gameObject.transform);
                    return new ToolCallResult
                    {
                        Text = $"[Success] Updated prefab GameObject '{gameObject.name}'.",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = true,
                            ["item"] = UnityObjectSnapshot.FromObject(gameObject).ToDictionary()
                        }
                    };
                }));
        }

        [McpPluginTool("unity.prefab_gameobject_create", Title = "Create Unity Prefab GameObject")]
        [Description("Loads a prefab asset and creates a child GameObject inside it.")]
        public ToolCallResult CreatePrefabGameObject(
            [Description("Prefab asset path.")]
            string assetPath,
            [Description("Name for the new object.")]
            string name = "GameObject",
            [Description("Optional primitive type: empty, cube, sphere, capsule, cylinder, plane, quad.")]
            string primitiveType = "empty",
            [Description("Optional parent hierarchy path inside the prefab. Omit to parent under the prefab root.")]
            string parentPath = null)
        {
            return MainThread.Instance.Run(() =>
                EditPrefabContents(assetPath, "modify prefabs", (path, root) =>
                {
                    ToolCallResult errorResult = null;
                    var parent = string.IsNullOrWhiteSpace(parentPath)
                        ? root
                        : ResolvePrefabGameObject(root, parentPath, out errorResult);
                    if (parent == null)
                    {
                        return errorResult;
                    }

                    var gameObject = CreateGameObject(name, primitiveType);
                    gameObject.transform.SetParent(parent.transform, false);
                    EditorUtility.SetDirty(gameObject);
                    return new ToolCallResult
                    {
                        Text = $"[Success] Created prefab GameObject '{gameObject.name}'.",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = true,
                            ["item"] = UnityObjectSnapshot.FromObject(gameObject).ToDictionary(),
                            ["parent"] = UnityObjectSnapshot.FromObject(parent).ToDictionary()
                        }
                    };
                }));
        }

        [McpPluginTool("unity.prefab_gameobject_destroy", Title = "Destroy Unity Prefab GameObject")]
        [Description("Loads a prefab asset and destroys a child GameObject inside it.")]
        public ToolCallResult DestroyPrefabGameObject(
            [Description("Prefab asset path.")]
            string assetPath,
            [Description("Hierarchy path inside the prefab.")]
            string gameObjectPath)
        {
            return MainThread.Instance.Run(() =>
                EditPrefabContents(assetPath, "modify prefabs", (path, root) =>
                {
                    var gameObject = ResolvePrefabGameObject(root, gameObjectPath, out var errorResult);
                    if (gameObject == null)
                    {
                        return errorResult;
                    }

                    if (gameObject == root)
                    {
                        return CreateStatus("blocked", "[Blocked] Destroying the prefab root is not supported.", false);
                    }

                    UnityEngine.Object.DestroyImmediate(gameObject);
                    return CreateStatus("destroyed", "[Success] Prefab GameObject destroyed.", true);
                }));
        }

        [McpPluginTool("unity.prefab_gameobject_set_parent", Title = "Set Unity Prefab GameObject Parent")]
        [Description("Loads a prefab asset and reparents a GameObject inside it.")]
        public ToolCallResult SetPrefabGameObjectParent(
            [Description("Prefab asset path.")]
            string assetPath,
            [Description("Hierarchy path for the target GameObject.")]
            string gameObjectPath,
            [Description("Optional hierarchy path for the new parent. Omit to parent under the prefab root.")]
            string parentPath = null,
            [Description("When true, keeps world transform while reparenting.")]
            bool worldPositionStays = true)
        {
            return MainThread.Instance.Run(() =>
                EditPrefabContents(assetPath, "modify prefabs", (path, root) =>
                {
                    var gameObject = ResolvePrefabGameObject(root, gameObjectPath, out var targetError);
                    if (gameObject == null)
                    {
                        return targetError;
                    }

                    if (gameObject == root)
                    {
                        return CreateStatus("blocked", "[Blocked] Reparenting the prefab root is not supported.", false);
                    }

                    ToolCallResult parentError = null;
                    var parent = string.IsNullOrWhiteSpace(parentPath)
                        ? root
                        : ResolvePrefabGameObject(root, parentPath, out parentError);
                    if (parent == null)
                    {
                        return parentError;
                    }

                    if (parent == gameObject)
                    {
                        return CreateStatus("invalid_argument", "[Failed] A GameObject cannot become its own parent.", false);
                    }

                    gameObject.transform.SetParent(parent.transform, worldPositionStays);
                    EditorUtility.SetDirty(gameObject.transform);
                    return new ToolCallResult
                    {
                        Text = $"[Success] Updated parent for prefab GameObject '{gameObject.name}'.",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = true,
                            ["item"] = UnityObjectSnapshot.FromObject(gameObject).ToDictionary(),
                            ["parent"] = UnityObjectSnapshot.FromObject(parent).ToDictionary()
                        }
                    };
                }));
        }

        [McpPluginTool("unity.prefab_component_add", Title = "Add Unity Prefab Component")]
        [Description("Loads a prefab asset and adds a component to a GameObject inside it.")]
        public ToolCallResult AddPrefabComponent(
            [Description("Prefab asset path.")]
            string assetPath,
            [Description("Optional hierarchy path inside the prefab. Omit to target the prefab root.")]
            string gameObjectPath = null,
            [Description("Component type name. Supports both short names like Camera and full names like UnityEngine.Camera.")]
            string componentType = null)
        {
            return MainThread.Instance.Run(() =>
                EditPrefabContents(assetPath, "modify prefabs", (path, root) =>
                {
                    var gameObject = ResolvePrefabGameObject(root, gameObjectPath, out var errorResult);
                    if (gameObject == null)
                    {
                        return errorResult;
                    }

                    var type = UnityTypeLocator.FindType(componentType, typeof(UnityComponent));
                    if (type == null)
                    {
                        return CreateStatus("not_found", $"[Failed] Could not find component type '{componentType}'.", false);
                    }

                    var component = gameObject.AddComponent(type);
                    EditorUtility.SetDirty(component);
                    return new ToolCallResult
                    {
                        Text = $"[Success] Added prefab component '{type.FullName}' to '{gameObject.name}'.",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = true,
                            ["item"] = UnityObjectSnapshot.FromObject(component).ToDictionary()
                        }
                    };
                }));
        }

        [McpPluginTool("unity.prefab_component_destroy", Title = "Destroy Unity Prefab Component")]
        [Description("Loads a prefab asset and destroys a component inside it by GameObject plus component type/index.")]
        public ToolCallResult DestroyPrefabComponent(
            [Description("Prefab asset path.")]
            string assetPath,
            [Description("Optional hierarchy path inside the prefab. Omit to target the prefab root.")]
            string gameObjectPath = null,
            [Description("Optional component type name when resolving the component from its owner.")]
            string componentType = null,
            [Description("Zero-based component index within the filtered matches on the GameObject.")]
            int componentIndex = 0)
        {
            return MainThread.Instance.Run(() =>
                EditPrefabContents(assetPath, "modify prefabs", (path, root) =>
                {
                    var gameObject = ResolvePrefabGameObject(root, gameObjectPath, out var errorResult);
                    if (gameObject == null)
                    {
                        return errorResult;
                    }

                    var component = McpComponentLocator.ResolveComponent(gameObject, componentType, componentIndex, out var componentError);
                    if (component == null)
                    {
                        return componentError;
                    }

                    UnityEngine.Object.DestroyImmediate(component);
                    return CreateStatus("destroyed", "[Success] Prefab component destroyed.", true);
                }));
        }

        [McpPluginTool("unity.prefab_component_get", Title = "Get Unity Prefab Component Data")]
        [Description("Loads a prefab asset and returns serialized data for a component inside it.")]
        public ToolCallResult GetPrefabComponentData(
            [Description("Prefab asset path.")]
            string assetPath,
            [Description("Optional hierarchy path inside the prefab. Omit to target the prefab root.")]
            string gameObjectPath = null,
            [Description("Optional component type name when resolving the component from its owner.")]
            string componentType = null,
            [Description("Zero-based component index within the filtered matches on the GameObject.")]
            int componentIndex = 0)
        {
            return MainThread.Instance.Run(() =>
                WithPrefabContents(assetPath, "inspect prefabs", (path, root) =>
                {
                    var gameObject = ResolvePrefabGameObject(root, gameObjectPath, out var errorResult);
                    if (gameObject == null)
                    {
                        return errorResult;
                    }

                    var component = McpComponentLocator.ResolveComponent(gameObject, componentType, componentIndex, out var componentError);
                    return component != null
                        ? McpObjectDataTools.GetObjectData(component, "prefab component")
                        : componentError;
                }));
        }

        [McpPluginTool("unity.prefab_component_modify", Title = "Modify Unity Prefab Component")]
        [Description("Loads a prefab asset and updates serialized fields on a component inside it.")]
        public ToolCallResult ModifyPrefabComponent(
            [Description("Prefab asset path.")]
            string assetPath,
            [Description("Optional hierarchy path inside the prefab. Omit to target the prefab root.")]
            string gameObjectPath = null,
            [Description("Optional component type name when resolving the component from its owner.")]
            string componentType = null,
            [Description("Zero-based component index within the filtered matches on the GameObject.")]
            int componentIndex = 0,
            [Description("Map of SerializedProperty paths to primitive values.")]
            Dictionary<string, object> properties = null)
        {
            return MainThread.Instance.Run(() =>
                EditPrefabContents(assetPath, "modify prefabs", (path, root) =>
                {
                    var gameObject = ResolvePrefabGameObject(root, gameObjectPath, out var errorResult);
                    if (gameObject == null)
                    {
                        return errorResult;
                    }

                    var component = McpComponentLocator.ResolveComponent(gameObject, componentType, componentIndex, out var componentError);
                    return component != null
                        ? McpObjectDataTools.ModifyObject(component, properties, "prefab component", markSceneDirty: false, recordUndo: false)
                        : componentError;
                }));
        }

        [McpPluginTool("unity.script_read", Title = "Read Script File")]
        [Description("Reads a project script or text-based source file.")]
        public ToolCallResult ReadScript(
            [Description("Project-relative script path.")]
            string path)
        {
            return MainThread.Instance.Run(() =>
            {
                var fullPath = ProjectPathGuards.RequireProjectTextFile(path, out var assetPath, out var errorResult);
                if (fullPath == null) return errorResult;

                return new ToolCallResult
                {
                    Text = $"[Success] Read script '{assetPath}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["path"] = assetPath,
                        ["content"] = File.ReadAllText(fullPath)
                    }
                };
            });
        }

        [McpPluginTool("unity.script_write", Title = "Write Script File")]
        [Description("Writes or creates a project script or text-based source file and refreshes the AssetDatabase.")]
        public ToolCallResult WriteScript(
            [Description("Project-relative script path.")]
            string path,
            [Description("Complete file content.")]
            string content)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("write scripts", out var blocked))
                {
                    return blocked;
                }

                var fullPath = ProjectPathGuards.RequireProjectTextFile(path, out var assetPath, out var errorResult, allowMissing: true);
                if (fullPath == null) return errorResult;

                Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory());
                File.WriteAllText(fullPath, content ?? string.Empty);
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                return CreateStatus("written", $"[Success] Wrote script '{assetPath}'.", true);
            });
        }

        [McpPluginTool("unity.script_delete", Title = "Delete Script File")]
        [Description("Deletes a project script or text-based source file and refreshes the AssetDatabase.")]
        public ToolCallResult DeleteScript(
            [Description("Project-relative script path.")]
            string path)
        {
            return MainThread.Instance.Run(() =>
            {
                if (McpPrimaryInstanceGuards.TryBlock("delete scripts", out var blocked))
                {
                    return blocked;
                }

                var fullPath = ProjectPathGuards.RequireProjectTextFile(path, out var assetPath, out var errorResult);
                if (fullPath == null) return errorResult;

                File.Delete(fullPath);
                var metaPath = fullPath + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                return CreateStatus("deleted", $"[Success] Deleted script '{assetPath}'.", true);
            });
        }

        [McpPluginTool("unity.package_list", Title = "List Unity Packages")]
        [Description("Lists packages installed in the current project.")]
        public Task<ToolCallResult> ListPackages()
        {
            return McpPackageRunner.RunListAsync();
        }

        [McpPluginTool("unity.package_search", Title = "Search Unity Packages")]
        [Description("Searches the package registry for a package name or keyword.")]
        public Task<ToolCallResult> SearchPackages(
            [Description("Package name or keyword.")]
            string query)
        {
            return McpPackageRunner.RunSearchAsync(query);
        }

        [McpPluginTool("unity.package_add", Title = "Add Unity Package")]
        [Description("Adds a package to the primary Unity project.")]
        public Task<ToolCallResult> AddPackage(
            [Description("Package identifier, for example 'com.unity.test-framework@1.6.0'.")]
            string packageId)
        {
            return McpPackageRunner.RunAddAsync(packageId);
        }

        [McpPluginTool("unity.package_remove", Title = "Remove Unity Package")]
        [Description("Removes a package from the primary Unity project.")]
        public Task<ToolCallResult> RemovePackage(
            [Description("Package name, for example 'com.unity.test-framework'.")]
            string packageName)
        {
            return McpPackageRunner.RunRemoveAsync(packageName);
        }

        private static ToolCallResult WithPrefabContents(string assetPath, string operation, Func<string, GameObject, ToolCallResult> action)
        {
            if (McpEditorToolGuards.TryBlockForTransition(operation, out var blocked))
            {
                return blocked;
            }

            var path = ProjectPathGuards.RequireProjectAssetPath(assetPath, out var errorResult);
            if (path == null)
            {
                return errorResult;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                return CreateStatus("not_found", $"[Failed] Could not load prefab '{path}'.", false);
            }

            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                if (root == null)
                {
                    return CreateStatus("failed", $"[Failed] Could not open prefab contents for '{path}'.", false);
                }

                return action(path, root);
            }
            catch (Exception exception)
            {
                return CreateStatus("failed", $"[Failed] Prefab operation failed: {exception.Message}", false);
            }
            finally
            {
                if (root != null)
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static ToolCallResult EditPrefabContents(string assetPath, string operation, Func<string, GameObject, ToolCallResult> action)
        {
            if (McpPrimaryInstanceGuards.TryBlock(operation, out var blocked))
            {
                return blocked;
            }

            return WithPrefabContents(assetPath, operation, (path, root) =>
            {
                var result = action(path, root);
                if (!IsOk(result))
                {
                    return result;
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out var success);
                if (!success || prefab == null)
                {
                    return CreateStatus("failed", $"[Failed] Could not save prefab '{path}'.", false);
                }

                return AttachPrefabMetadata(result, path);
            });
        }

        private static GameObject ResolvePrefabGameObject(GameObject root, string gameObjectPath, out ToolCallResult errorResult)
        {
            errorResult = null;
            if (string.IsNullOrWhiteSpace(gameObjectPath))
            {
                return root;
            }

            var gameObject = McpHierarchyTools.ResolveGameObjectByHierarchyPath(root, gameObjectPath);
            if (gameObject != null)
            {
                return gameObject;
            }

            errorResult = CreateStatus("not_found", $"[Failed] Could not find prefab GameObject '{gameObjectPath}'.", false);
            return null;
        }

        private static bool IsOk(ToolCallResult result)
        {
            return result?.StructuredContent is Dictionary<string, object> dictionary &&
                   dictionary.TryGetValue("ok", out var okValue) &&
                   okValue is bool ok &&
                   ok;
        }

        private static ToolCallResult AttachPrefabMetadata(ToolCallResult result, string path)
        {
            if (result?.StructuredContent is Dictionary<string, object> dictionary)
            {
                dictionary["prefabPath"] = path;
                dictionary["asset"] = AssetSnapshot.FromPath(path).ToDictionary();
            }

            return result;
        }

        private static void ApplyVector(double[] values, Action<Vector3> apply)
        {
            if (values == null || values.Length < 3)
            {
                return;
            }

            apply(new Vector3((float)values[0], (float)values[1], (float)values[2]));
        }

        private static GameObject CreateGameObject(string name, string primitiveType)
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

        private static ToolCallResult CreateStatus(string status, string text, bool ok)
        {
            return McpToolResultFactory.CreateStatus(status, text, ok);
        }
    }

    internal static class ProjectPathGuards
    {
        private static readonly string[] s_TextExtensions = { ".cs", ".asmdef", ".shader", ".hlsl", ".cginc", ".uxml", ".uss", ".txt", ".json", ".md" };

        public static string RequireProjectAssetPath(string path, out ToolCallResult errorResult)
        {
            errorResult = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                errorResult = CreateStatus("invalid_path", "[Failed] A project-relative asset path is required.", false);
                return null;
            }

            var normalized = path.Replace('\\', '/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) &&
                !normalized.StartsWith("Packages/", StringComparison.Ordinal) &&
                !normalized.StartsWith("ProjectSettings/", StringComparison.Ordinal))
            {
                errorResult = CreateStatus("invalid_path", $"[Failed] Path '{normalized}' is outside the Unity project scope.", false);
                return null;
            }

            return normalized;
        }

        public static string RequireProjectTextFile(string path, out string assetPath, out ToolCallResult errorResult, bool allowMissing = false)
        {
            assetPath = RequireProjectAssetPath(path, out errorResult);
            if (assetPath == null)
            {
                return null;
            }

            var extension = Path.GetExtension(assetPath);
            if (Array.IndexOf(s_TextExtensions, extension) < 0)
            {
                errorResult = CreateStatus("invalid_path", $"[Failed] File extension '{extension}' is not allowed for script tools.", false);
                return null;
            }

            var fullPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
            var projectRoot = Path.GetFullPath(Directory.GetCurrentDirectory()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                errorResult = CreateStatus("invalid_path", "[Failed] Path escapes the project root.", false);
                return null;
            }

            if (!allowMissing && !File.Exists(fullPath))
            {
                errorResult = CreateStatus("not_found", $"[Failed] File '{assetPath}' does not exist.", false);
                return null;
            }

            return fullPath;
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

    public static class McpPrimaryInstanceGuards
    {
        public static bool TryBlock(string operation, out ToolCallResult result)
        {
            if (!McpInstanceIdentity.IsPrimaryInstance())
            {
                result = new ToolCallResult
                {
                    Text = $"[Blocked] Only the primary Unity project instance can {operation}.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = false,
                        ["status"] = "blocked"
                    }
                };
                return true;
            }

            if (McpEditorToolGuards.TryBlockForTransition(operation, out result))
            {
                return true;
            }

            return false;
        }
    }

    internal sealed class AssetSnapshot
    {
        public string Path;
        public string Guid;
        public string Type;
        public bool Exists;
        public bool IsFolder;

        public static AssetSnapshot FromPath(string path)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            return new AssetSnapshot
            {
                Path = path,
                Guid = AssetDatabase.AssetPathToGUID(path),
                Type = asset != null ? asset.GetType().FullName : string.Empty,
                Exists = File.Exists(global::System.IO.Path.GetFullPath(path)) || AssetDatabase.IsValidFolder(path),
                IsFolder = AssetDatabase.IsValidFolder(path)
            };
        }

        public Dictionary<string, object> ToDictionary()
        {
            return new Dictionary<string, object>
            {
                ["path"] = Path,
                ["guid"] = Guid,
                ["type"] = Type,
                ["exists"] = Exists,
                ["isFolder"] = IsFolder
            };
        }
    }

    internal static class McpPackageRunner
    {
        public static Task<ToolCallResult> RunListAsync()
        {
            return RunAsync(
                () => Client.List(true),
                request =>
                {
                    var list = (ListRequest)request;
                    var items = list.Result.Select(package => (object)new Dictionary<string, object>
                    {
                        ["name"] = package.name,
                        ["displayName"] = package.displayName,
                        ["version"] = package.version,
                        ["source"] = package.source.ToString()
                    }).ToList();

                    return new ToolCallResult
                    {
                        Text = $"[Success] Listed {items.Count} package(s).",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = true,
                            ["items"] = items
                        }
                    };
                });
        }

        public static Task<ToolCallResult> RunSearchAsync(string query)
        {
            return RunAsync(
                () => string.IsNullOrWhiteSpace(query) ? Client.SearchAll() : Client.Search(query),
                request =>
                {
                    var search = (SearchRequest)request;
                    var items = search.Result.Select(package => (object)new Dictionary<string, object>
                    {
                        ["name"] = package.name,
                        ["displayName"] = package.displayName,
                        ["version"] = package.version,
                        ["description"] = package.description
                    }).ToList();

                    return new ToolCallResult
                    {
                        Text = $"[Success] Found {items.Count} package(s).",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = true,
                            ["items"] = items
                        }
                    };
                });
        }

        public static Task<ToolCallResult> RunAddAsync(string packageId)
        {
            if (!McpInstanceIdentity.IsPrimaryInstance())
            {
                return Task.FromResult(Blocked("add packages"));
            }

            return RunAsync(
                () => Client.Add(packageId),
                request =>
                {
                    var add = (AddRequest)request;
                    return new ToolCallResult
                    {
                        Text = $"[Success] Added package '{add.Result.name}@{add.Result.version}'.",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = true,
                            ["name"] = add.Result.name,
                            ["version"] = add.Result.version
                        }
                    };
                });
        }

        public static Task<ToolCallResult> RunRemoveAsync(string packageName)
        {
            if (!McpInstanceIdentity.IsPrimaryInstance())
            {
                return Task.FromResult(Blocked("remove packages"));
            }

            return RunAsync(
                () => Client.Remove(packageName),
                request => new ToolCallResult
                {
                    Text = $"[Success] Removed package '{packageName}'.",
                    StructuredContent = new Dictionary<string, object>
                    {
                        ["ok"] = true,
                        ["name"] = packageName
                    }
                });
        }

        private static Task<ToolCallResult> RunAsync(Func<Request> start, Func<Request, ToolCallResult> onSuccess)
        {
            var tcs = new TaskCompletionSource<ToolCallResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            MainThread.Instance.Run(() =>
            {
                if (McpEditorToolGuards.TryBlockForTransition("run a package operation", out var blocked))
                {
                    tcs.TrySetResult(blocked);
                    return;
                }

                Request request;
                try
                {
                    request = start();
                }
                catch (Exception exception)
                {
                    tcs.TrySetResult(new ToolCallResult
                    {
                        Text = $"[Failed] Package operation could not start: {exception.Message}",
                        StructuredContent = new Dictionary<string, object>
                        {
                            ["ok"] = false,
                            ["status"] = "failed"
                        }
                    });
                    return;
                }

                void Poll()
                {
                    if (!request.IsCompleted)
                    {
                        return;
                    }

                    EditorApplication.update -= Poll;
                    if (request.Status == StatusCode.Success)
                    {
                        tcs.TrySetResult(onSuccess(request));
                    }
                    else
                    {
                        tcs.TrySetResult(new ToolCallResult
                        {
                            Text = $"[Failed] Package operation failed: {request.Error.message}",
                            StructuredContent = new Dictionary<string, object>
                            {
                                ["ok"] = false,
                                ["status"] = "failed",
                                ["errorCode"] = request.Error.errorCode
                            }
                        });
                    }
                }

                EditorApplication.update += Poll;
            });

            return tcs.Task;
        }

        private static ToolCallResult Blocked(string operation)
        {
            return new ToolCallResult
            {
                Text = $"[Blocked] Only the primary Unity project instance can {operation}.",
                StructuredContent = new Dictionary<string, object>
                {
                    ["ok"] = false,
                    ["status"] = "blocked"
                }
            };
        }
    }
}
