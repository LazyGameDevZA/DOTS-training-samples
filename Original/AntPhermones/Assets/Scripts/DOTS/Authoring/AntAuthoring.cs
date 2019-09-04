using DOTS.Components;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring
{
    public class AntAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<HoldingResources>(entity);
            dstManager.AddComponent<Translation2D>(entity);
            dstManager.AddComponent<Rotation2D>(entity);
            dstManager.AddComponent<Velocity>(entity);
            dstManager.AddComponent<WallSteering>(entity);
            dstManager.AddComponent<MeshColor>(entity);
            dstManager.AddComponent<TargetPosition>(entity);
            dstManager.AddComponent<AntStats>(entity);
        }
    }
}
