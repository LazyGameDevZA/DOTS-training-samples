using Unity.Entities;

namespace DOTS.Components
{
    public struct ResourceSeed : IComponentData
    {
        public Entity ResourcePrefab;
    }
}
