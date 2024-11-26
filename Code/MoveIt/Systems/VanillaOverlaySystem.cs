using Game.Tools;
using Unity.Collections;
using Unity.Entities;

namespace MoveIt.Systems
{
    internal partial class MIT_VanillaOverlaySystem : MIT_System
    {
        private EntityQuery _AllHighlightedEntitiesQuery;

        protected override void OnCreate()
        {
            base.OnCreate();

            _AllHighlightedEntitiesQuery = new EntityQueryBuilder(Allocator.Persistent)
                .WithAll<Highlighted>()
                .Build(EntityManager);
        }

        protected override void OnUpdate()
        {
            // Hide the vanilla game overlay ("MeshLayer.Outline")
            if (_AllHighlightedEntitiesQuery.CalculateEntityCount() <= 0) return;
            
            using NativeArray<Entity> tempEntities = _AllHighlightedEntitiesQuery.ToEntityArray(Allocator.Temp);

            foreach (Entity e in tempEntities)
            {
                EntityManager.RemoveComponent<Highlighted>(e);
                EntityManager.AddComponent<Game.Common.Updated>(e);
                EntityManager.AddComponent<Game.Common.BatchesUpdated>(e);
            }
        }

        protected override void OnDestroy()
        {
            _AllHighlightedEntitiesQuery.Dispose();
        }
    }
}
