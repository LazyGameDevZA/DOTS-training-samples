using Unity.Entities;
using Unity.Mathematics;

namespace DOTS.Components
{
    public struct AntSeed : IComponentData
    {
        public int Count;
        public float RandomSteering;
        public float WallSteerStrength;
        public float GoalSteerStrength;
        public float Acceleration;
        public float InitialSpeed;
        public Entity AntPrefab;
    }
}
