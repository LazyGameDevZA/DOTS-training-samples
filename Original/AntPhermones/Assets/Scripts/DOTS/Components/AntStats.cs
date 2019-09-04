using Unity.Entities;

namespace DOTS.Components
{
    public struct AntStats : IComponentData
    {
        public float Steering;
        public float Acceleration;
        public float WallSteerStrength;
        public float GoalSteerStrength;
        public float Brightness;
    }
}
