using System.Collections.Generic;
using DOTS.Components;
using Unity.Entities;
using UnityEngine;

namespace DOTS.Authoring
{
    [RequiresEntityConversion]
    public class SimulationAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public Material basePheromoneMaterial;
        public Renderer pheromoneRenderer;
        public GameObject antPrefab;
        public GameObject obstaclePrefab;
        public GameObject colonyPrefab;
        public GameObject resourcePrefab;
        public Color searchColor;
        public Color carryColor;
        public int antCount;
        public int mapSize = 128;
        public int bucketResolution;
        public float antSpeed;
        [Range(0f,1f)]
        public float antAccel;
        public float trailAddSpeed;
        [Range(0f,1f)]
        public float trailDecay;
        public float randomSteering;
        public float pheromoneSteerStrength;
        public float wallSteerStrength;
        public float goalSteerStrength;
        public float outwardStrength;
        public float inwardStrength;
        public int rotationResolution = 360;
        public int obstacleRingCount;
        [Range(0f,1f)]
        public float obstaclesPerRing;
        public float obstacleRadius;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            var antPrefab = conversionSystem.GetPrimaryEntity(this.antPrefab);
            var obstaclePrefab = conversionSystem.GetPrimaryEntity(this.obstaclePrefab);
            var colonyPrefab = conversionSystem.GetPrimaryEntity(this.colonyPrefab);
            var resourcePrefab = conversionSystem.GetPrimaryEntity(this.resourcePrefab);
            
            var antSeed = new AntSeed();
            antSeed.Count = this.antCount;
            antSeed.RandomSteering = this.randomSteering;
            antSeed.WallSteerStrength = this.wallSteerStrength;
            antSeed.GoalSteerStrength = this.goalSteerStrength;
            // Calculate as acceleration per second, ECS doesn't use FixedUpdate for physics simulation at present
            antSeed.Acceleration = this.antAccel / Time.fixedDeltaTime;
            antSeed.InitialSpeed = this.antSpeed;
            antSeed.AntPrefab = antPrefab;
            
            var antColors = new AntColors();
            antColors.Search = this.searchColor;
            antColors.Carry = this.carryColor;
            
            var obstacleSeed = new ObstacleSeed();
            obstacleSeed.ObstaclesPerRing = this.obstaclesPerRing;
            obstacleSeed.Radius = this.obstacleRadius;
            obstacleSeed.RingCount = this.obstacleRingCount;
            obstacleSeed.ObstaclePrefab = obstaclePrefab;
            var obstacleBucketResolution = new ObstacleBucketResolution{ Value = this.bucketResolution};
            
            var mapSize = new MapSize { Value = this.mapSize };
            var colonySeed = new ColonySeed { ColonyPrefab = colonyPrefab };
            var resourceSeed = new ResourceSeed{ ResourcePrefab = resourcePrefab };

            dstManager.AddComponentData(entity, antSeed);
            dstManager.AddComponentData(entity, antColors);
            dstManager.AddComponentData(entity, obstacleSeed);
            dstManager.AddComponentData(entity, obstacleBucketResolution);
            dstManager.AddComponentData(entity, mapSize);
            dstManager.AddComponentData(entity, colonySeed);
            dstManager.AddComponentData(entity, resourceSeed);
            dstManager.AddComponentData(entity, default(Colony));
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(this.antPrefab);
            referencedPrefabs.Add(this.obstaclePrefab);
            referencedPrefabs.Add(this.colonyPrefab);
            referencedPrefabs.Add(this.resourcePrefab);
        }
    }
}
