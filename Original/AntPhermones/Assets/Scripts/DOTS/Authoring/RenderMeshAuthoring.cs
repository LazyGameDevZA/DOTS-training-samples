using DOTS.Components;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring
{
    public class RenderMeshAuthoring: MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField]
        private Mesh mesh;

        [SerializeField]
        private Material material;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var renderMesh = new RenderMesh();
            renderMesh.Mesh = this.mesh;
            renderMesh.Material = this.material;

            dstManager.AddSharedComponentData(entity, renderMesh);
        }
    }
}
