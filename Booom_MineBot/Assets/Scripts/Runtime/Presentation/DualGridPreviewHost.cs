using System.Collections.Generic;
using Minebot.GridMining;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public enum DualGridPreviewRefreshMode : byte
    {
        [InspectorName("手动")]
        Manual = 0,
        [InspectorName("启用时")]
        OnEnable = 1,
        [InspectorName("参数变更时")]
        OnValidate = 2
    }

    [ExecuteAlways]
    public sealed class DualGridPreviewHost : MonoBehaviour
    {
        [SerializeField]
        [InspectorLabel("源地形瓦片地图")]
        private Tilemap sourceTerrainTilemap;

        [SerializeField]
        [InspectorLabel("烘焙配置")]
        private TilemapBakeProfile bakeProfile;

        [SerializeField]
        [InspectorLabel("美术集")]
        private MinebotPresentationArtSet artSet;

        [SerializeField]
        [InspectorLabel("覆盖地形配置")]
        private DualGridTerrainProfile profileOverride;

        [SerializeField]
        [InspectorLabel("预览根节点")]
        private Transform previewRoot;

        [SerializeField]
        [InspectorLabel("刷新时机")]
        private DualGridPreviewRefreshMode refreshMode = DualGridPreviewRefreshMode.Manual;

        [SerializeField]
        [InspectorLabel("自动创建缺失图层")]
        private bool createMissingLayers = true;

        private readonly DualGridRenderer renderer = new DualGridRenderer(new LayeredBinaryTerrainResolver());

        public Tilemap SourceTerrainTilemap => sourceTerrainTilemap;
        public TilemapBakeProfile BakeProfile => bakeProfile;
        public MinebotPresentationArtSet ArtSet => artSet;
        public DualGridTerrainProfile ProfileOverride => profileOverride;
        public Transform PreviewRoot => previewRoot;

#if UNITY_EDITOR
        public void Configure(
            Tilemap configuredSourceTerrainTilemap,
            TilemapBakeProfile configuredBakeProfile,
            MinebotPresentationArtSet configuredArtSet = null,
            DualGridTerrainProfile configuredProfileOverride = null,
            Transform configuredPreviewRoot = null,
            DualGridPreviewRefreshMode configuredRefreshMode = DualGridPreviewRefreshMode.Manual,
            bool configuredCreateMissingLayers = true)
        {
            sourceTerrainTilemap = configuredSourceTerrainTilemap;
            bakeProfile = configuredBakeProfile;
            artSet = configuredArtSet;
            profileOverride = configuredProfileOverride;
            previewRoot = configuredPreviewRoot;
            refreshMode = configuredRefreshMode;
            createMissingLayers = configuredCreateMissingLayers;
        }
#endif

        private void OnEnable()
        {
            if (!Application.isPlaying && refreshMode == DualGridPreviewRefreshMode.OnEnable)
            {
                RebuildPreview();
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying && refreshMode == DualGridPreviewRefreshMode.OnValidate)
            {
                RebuildPreview();
            }
        }

        public bool RebuildPreview()
        {
            if (!TryBuildContext(out MinebotPresentationAssets assets, out Tilemap[] tilemaps, out string message))
            {
                Debug.LogWarning(message, this);
                return false;
            }

            var source = new TilemapAuthoringMaterialSource(sourceTerrainTilemap, bakeProfile);
            var target = new TilemapDualGridRenderTarget(tilemaps, assets);
            renderer.RebuildAll(source, target);
            target.CompressBounds();
            return true;
        }

        public void ClearPreview()
        {
            foreach (Tilemap tilemap in GetPreviewTilemaps())
            {
                tilemap?.ClearAllTiles();
            }
        }

        public IReadOnlyList<string> ValidateConfiguration()
        {
            var issues = new List<string>();
            if (sourceTerrainTilemap == null)
            {
                issues.Add("缺少源地形瓦片地图。");
            }

            if (bakeProfile == null)
            {
                issues.Add("缺少瓦片地图烘焙配置。");
            }

            if (artSet == null && profileOverride == null)
            {
                issues.Add("缺少表现美术集或双网格配置覆盖项。");
            }

            DualGridTerrainProfile profile = profileOverride != null
                ? profileOverride
                : artSet != null ? artSet.DualGridTerrainProfile : null;
            if (profile != null)
            {
                issues.AddRange(profile.GetValidationIssues());
            }

            return issues;
        }

        private bool TryBuildContext(out MinebotPresentationAssets assets, out Tilemap[] tilemaps, out string message)
        {
            assets = null;
            tilemaps = null;
            message = string.Empty;

            IReadOnlyList<string> issues = ValidateConfiguration();
            if (issues.Count > 0)
            {
                message = string.Join("\n", issues);
                return false;
            }

            assets = MinebotPresentationAssets.Create(artSet, profileOverride);
            Transform root = ResolvePreviewRoot();
            tilemaps = createMissingLayers ? EnsureTerrainFamilyLayers(root, assets.TerrainLayoutSettings) : GetPreviewTilemaps();
            if (tilemaps == null || tilemaps.Length == 0)
            {
                message = "无法解析预览瓦片地图。";
                return false;
            }

            return true;
        }

        private Transform ResolvePreviewRoot()
        {
            if (previewRoot != null)
            {
                return previewRoot;
            }

            Transform existing = transform.Find("Dual Grid Preview Root");
            if (existing != null)
            {
                previewRoot = existing;
                return previewRoot;
            }

            var previewObject = new GameObject("Dual Grid Preview Root");
            previewObject.transform.SetParent(transform, false);
            previewRoot = previewObject.transform;
            return previewRoot;
        }

        private Tilemap[] GetPreviewTilemaps()
        {
            Transform root = ResolvePreviewRoot();
            var tilemaps = new Tilemap[DualGridTerrain.RenderLayerCount];
            TerrainRenderLayerId[] orderedLayers = DualGridTerrainLayout.OrderedLayers;
            for (int i = 0; i < orderedLayers.Length; i++)
            {
                Transform child = root.Find(DualGridTerrainLayout.GetTilemapName(orderedLayers[i]));
                tilemaps[i] = child != null ? child.GetComponent<Tilemap>() : null;
            }

            return tilemaps;
        }

        private static Tilemap[] EnsureTerrainFamilyLayers(Transform root, DualGridTerrainLayoutSettings settings)
        {
            TerrainRenderLayerId[] orderedLayers = DualGridTerrainLayout.OrderedLayers;
            var tilemaps = new Tilemap[orderedLayers.Length];
            for (int i = 0; i < orderedLayers.Length; i++)
            {
                tilemaps[i] = EnsureTilemapLayer(
                    root,
                    DualGridTerrainLayout.GetTilemapName(orderedLayers[i]),
                    DualGridTerrainLayout.GetSortingOrder(orderedLayers[i], settings),
                    settings.DisplayOffset);
            }

            return tilemaps;
        }

        private static Tilemap EnsureTilemapLayer(Transform parent, string layerName, int sortingOrder, Vector3 localPosition)
        {
            Transform layer = parent.Find(layerName);
            if (layer == null)
            {
                layer = new GameObject(layerName).transform;
                layer.SetParent(parent, false);
            }

            layer.localPosition = localPosition;
            Tilemap tilemap = layer.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                tilemap = layer.gameObject.AddComponent<Tilemap>();
            }

            TilemapRenderer rendererComponent = layer.GetComponent<TilemapRenderer>();
            if (rendererComponent == null)
            {
                rendererComponent = layer.gameObject.AddComponent<TilemapRenderer>();
            }

            rendererComponent.sortingOrder = sortingOrder;
            return tilemap;
        }
    }
}
