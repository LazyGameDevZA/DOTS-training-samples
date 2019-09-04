using Unity.Entities;

namespace DOTS.Components
{
    public struct ObstacleSeed : IComponentData
    {
        public int RingCount;
        public float Radius;
        public float ObstaclesPerRing;
        public Entity ObstaclePrefab;
    }
}
