using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace DOTS.Authoring
{
    public class AddNonUniformScale : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<NonUniformScale>(entity);
        }
    }
}
