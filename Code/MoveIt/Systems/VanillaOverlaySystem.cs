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
            if (_AllHighlightedEntitiesQuery.CalculateEntityCount() > 0)
            {
                using var tempEntities = _AllHighlightedEntitiesQuery.ToEntityArray(Allocator.Temp);

                for (int i = 0; i < tempEntities.Length; i++)
                {
                    EntityManager.RemoveComponent<Highlighted>(tempEntities[i]);
                    EntityManager.AddComponent<Game.Common.Updated>(tempEntities[i]);
                    EntityManager.AddComponent<Game.Common.BatchesUpdated>(tempEntities[i]);
                }
            }
        }

        protected override void OnDestroy()
        {
            _AllHighlightedEntitiesQuery.Dispose();
        }
    }
}
