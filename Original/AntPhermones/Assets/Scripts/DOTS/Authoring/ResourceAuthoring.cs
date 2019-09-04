using DOTS.Components;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring
{
    public class ResourceAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<Resource>(entity);
            dstManager.AddComponent<Translation2D>(entity);
        }
    }
}
