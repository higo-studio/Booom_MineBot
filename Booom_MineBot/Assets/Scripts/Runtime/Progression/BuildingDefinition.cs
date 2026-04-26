using Minebot.Common;
using Minebot.GridMining;
using UnityEngine;

namespace Minebot.Progression
{
    [CreateAssetMenu(menuName = "Minebot/Progression/Building Definition")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        [SerializeField]
        private string id = "building";

        [SerializeField]
        private string displayName = "建筑";

        [SerializeField]
        private ResourceAmount cost = ResourceAmount.Zero;

        [SerializeField]
        private Vector2Int footprintSize = Vector2Int.one;

        [SerializeField]
        private TerrainKind allowedTerrain = TerrainKind.Empty;

        [SerializeField]
        private GameObject prefab;

        [SerializeField]
        private Vector2 colliderSize = Vector2.one;

        public string Id => string.IsNullOrWhiteSpace(id) ? name : id;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
        public ResourceAmount Cost => cost;
        public Vector2Int FootprintSize => new Vector2Int(Mathf.Max(1, footprintSize.x), Mathf.Max(1, footprintSize.y));
        public TerrainKind AllowedTerrain => allowedTerrain;
        public GameObject Prefab => prefab;
        public Vector2 ColliderSize => colliderSize.x > 0f && colliderSize.y > 0f ? colliderSize : (Vector2)FootprintSize;

        public static BuildingDefinition CreateRuntime(
            string id,
            string displayName,
            ResourceAmount cost,
            Vector2Int footprintSize,
            TerrainKind allowedTerrain = TerrainKind.Empty,
            GameObject prefab = null,
            Vector2? colliderSize = null)
        {
            BuildingDefinition definition = CreateInstance<BuildingDefinition>();
            definition.name = displayName;
            definition.id = id;
            definition.displayName = displayName;
            definition.cost = cost;
            definition.footprintSize = new Vector2Int(Mathf.Max(1, footprintSize.x), Mathf.Max(1, footprintSize.y));
            definition.allowedTerrain = allowedTerrain;
            definition.prefab = prefab;
            definition.colliderSize = colliderSize ?? (Vector2)definition.footprintSize;
            return definition;
        }
    }
}
