using DOTS.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Profiling;
using Unity.Transforms;
using UnityEngine;

namespace DOTS.Systems
{
    public class RenderMeshSystem : ComponentSystem
    {
        private EntityQuery renderMeshWithoutColors;
        private EntityQuery renderMeshWithColors;
        private EntityQuery renderMeshWithoutProps;
        
        private ProfilerMarker withoutColorsMarker;
        private ProfilerMarker withColorsMarker;
        private static readonly int color = Shader.PropertyToID("_Color");

        public RenderMeshSystem()
        {
            this.withoutColorsMarker = new ProfilerMarker("RenderMeshSystem.RenderMeshWithoutColors");
            this.withColorsMarker = new ProfilerMarker("RenderMeshSystem.RenderMeshWithColors");
        }

        protected override void OnCreate()
        {
            this.renderMeshWithoutColors = this.Entities
                .WithAll(typeof(RenderMesh), typeof(LocalToWorld))
                .WithNone(typeof(MeshColor))
                .ToEntityQuery();

            this.renderMeshWithColors = this.Entities
                .WithAll(typeof(RenderMesh), typeof(MatProps), typeof(LocalToWorld), typeof(MeshColor))
                .ToEntityQuery();

            this.renderMeshWithoutProps = this.Entities
                .WithAll(typeof(RenderMesh), typeof(LocalToWorld), typeof(MeshColor))
                .WithNone(typeof(MatProps))
                .ToEntityQuery();
        }

        protected override void OnUpdate()
        {
            ArchetypeChunkSharedComponentType<RenderMesh> renderMeshType = this.GetArchetypeChunkSharedComponentType<RenderMesh>();
            ArchetypeChunkSharedComponentType<MatProps> matPropsType = this.GetArchetypeChunkSharedComponentType<MatProps>();
            ArchetypeChunkComponentType<LocalToWorld> localToWorldType = this.GetArchetypeChunkComponentType<LocalToWorld>();
            ArchetypeChunkComponentType<MeshColor> meshColorType = this.GetArchetypeChunkComponentType<MeshColor>();
            ArchetypeChunkEntityType archetypeChunkEntityType = this.GetArchetypeChunkEntityType();

            this.AddMatPropsForColorMesh(archetypeChunkEntityType);

            this.withColorsMarker.Begin();
            this.RenderMeshWithColors(renderMeshType, matPropsType, localToWorldType, meshColorType);
            this.withColorsMarker.End();
            
            this.withoutColorsMarker.Begin();
            this.RenderMeshWithoutColors(renderMeshType, localToWorldType);
            this.withoutColorsMarker.End();
        }

        private void AddMatPropsForColorMesh(ArchetypeChunkEntityType archetypeChunkEntityType)
        {
            var chunks = this.renderMeshWithoutProps.CreateArchetypeChunkArray(Allocator.TempJob);

            for(int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var matProps = new MatProps{ Value = new MaterialPropertyBlock() };

                var entities = chunk.GetNativeArray(archetypeChunkEntityType);

                for(int j = 0; j < entities.Length; j++)
                {
                    this.PostUpdateCommands.AddSharedComponent(entities[j], matProps);
                }
            }
            
            chunks.Dispose();
        }

        private unsafe void RenderMeshWithColors(
            ArchetypeChunkSharedComponentType<RenderMesh> renderMeshType,
            ArchetypeChunkSharedComponentType<MatProps> matPropsType,
            ArchetypeChunkComponentType<LocalToWorld> localToWorldType,
            ArchetypeChunkComponentType<MeshColor> meshColorType)
        {
            var chunks = this.renderMeshWithColors.CreateArchetypeChunkArray(Allocator.TempJob);

            for(int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var renderMesh = chunk.GetSharedComponentData(renderMeshType, this.EntityManager);
                var matProps = chunk.GetSharedComponentData(matPropsType, this.EntityManager);
                var localToWorldArray = chunk.GetNativeArray(localToWorldType);
                var colorArray = chunk.GetNativeArray(meshColorType);

                var matrix4x4Array = new Matrix4x4[chunk.Count];
                var vector4Array = new Vector4[chunk.Count];
                fixed(void* matrixPointer = &matrix4x4Array[0], vector4Pointer = &vector4Array[0])
                {
                    var localToWorldPointer = localToWorldArray.GetUnsafeReadOnlyPtr();
                    UnsafeUtility.MemCpy(matrixPointer, localToWorldPointer, sizeof(Matrix4x4) * chunk.Count);

                    var colorPointer = colorArray.GetUnsafeReadOnlyPtr();
                    UnsafeUtility.MemCpy(vector4Pointer, colorPointer, sizeof(Vector4) * chunk.Count);
                }
                
                matProps.Value.SetVectorArray(color, vector4Array);

                Graphics.DrawMeshInstanced(renderMesh.Mesh, 0, renderMesh.Material, matrix4x4Array, chunk.Count, matProps.Value);
            }

            chunks.Dispose();
        }

        private unsafe void RenderMeshWithoutColors(
            ArchetypeChunkSharedComponentType<RenderMesh> renderMeshType,
            ArchetypeChunkComponentType<LocalToWorld> localToWorldType)
        {
            var chunks = this.renderMeshWithoutColors.CreateArchetypeChunkArray(Allocator.TempJob);

            for(int i = 0; i < chunks.Length; i++)
            {
                var chunk = chunks[i];
                var renderMesh = chunk.GetSharedComponentData(renderMeshType, this.EntityManager);
                var localToWorldArray = chunk.GetNativeArray(localToWorldType);

                var matrix4x4Array = new Matrix4x4[chunk.Count];
                fixed(void* matrixPointer = &matrix4x4Array[0])
                {
                    var localToWorldPointer = localToWorldArray.GetUnsafeReadOnlyPtr();

                    UnsafeUtility.MemCpy(matrixPointer, localToWorldPointer, sizeof(Matrix4x4) * chunk.Count);
                }

                Graphics.DrawMeshInstanced(renderMesh.Mesh, 0, renderMesh.Material, matrix4x4Array);
            }

            chunks.Dispose();
        }
    }
}
