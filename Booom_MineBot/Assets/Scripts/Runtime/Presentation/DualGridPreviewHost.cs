using System.Collections.Generic;
using Minebot.GridMining;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Minebot.Presentation
{
    public enum DualGridPreviewRefreshMode : byte
    {
        Manual = 0,
        OnEnable = 1,
        OnValidate = 2
    }

    [ExecuteAlways]
    public sealed class DualGridPreviewHost : MonoBehaviour
    {
        [SerializeField]
        private Tilemap sourceTerrainTilemap;

        [SerializeField]
        private TilemapBakeProfile bakeProfile;

        [SerializeField]
        private MinebotPresentationArtSet artSet;

        [SerializeField]
        private DualGridTerrainProfile profileOverride;

        [SerializeField]
        private Transform previewRoot;

        [SerializeField]
        private DualGridPreviewRefreshMode refreshMode = DualGridPreviewRefreshMode.Manual;

        [SerializeField]
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
                issues.Add("Missing source terrain tilemap.");
            }

            if (bakeProfile == null)
            {
                issues.Add("Missing tilemap bake profile.");
            }

            if (artSet == null && profileOverride == null)
            {
                issues.Add("Missing presentation art set or dual-grid profile override.");
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
                message = "Unable to resolve preview tilemaps.";
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
