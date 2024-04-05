using MoveIt.Tool;
using QCommonLib;
using System.Text;
using Unity.Collections;
using Unity.Entities;

namespace MoveIt.Systems
{
    internal partial class MIT_RemoveOverriddenSystem : MIT_System
    {
        private EntityQuery _OverriddenEntitiesQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _OverriddenEntitiesQuery = new EntityQueryBuilder(Allocator.Persistent)
                .WithAll<Game.Common.Updated, Game.Common.Overridden>()
                .Build(EntityManager);
            RequireAnyForUpdate(_OverriddenEntitiesQuery);
        }

        protected override void OnUpdate()
        {
            EntityManager.RemoveComponent<Game.Common.Overridden>(_OverriddenEntitiesQuery);
        }

        protected override void OnDestroy()
        {
            _OverriddenEntitiesQuery.Dispose();
        }

        private void DebugDumpOverriddenEntities()
        {
            StringBuilder sb = new();

            using var entities = _OverriddenEntitiesQuery.ToEntityArray(Allocator.Temp);
            sb.AppendFormat("Overridden Entities: {0}", entities.Length);

            for (int i = 0; i < entities.Length; i++)
            {
                sb.AppendFormat("\n    {0}", entities[i].DX());
            }
            QLog.Bundle("OVER", sb.ToString());
        }
    }
}
