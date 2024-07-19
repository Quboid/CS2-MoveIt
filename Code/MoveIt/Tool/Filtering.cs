using Colossal.Entities;
using Colossal.Mathematics;
using Game.Common;
using Game.Tools;
using MoveIt.Moveables;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Tool
{
    public partial class MIT : ObjectToolBaseSystem
    {
        #region General validity checks
        internal bool IsValid(MVDefinition mvd) => IsValid(EntityManager, mvd);
        internal static bool IsValid(EntityManager Manager, MVDefinition mvd)
        {
            return IsValid(Manager, mvd.m_Entity);
        }

        internal bool IsValid(Entity e) => IsValid(EntityManager, e);
        internal static bool IsValid(EntityManager Manager, Entity e)
        {
            if (!IsValidBase(Manager, e)) return false;
            if (!(
                Manager.HasComponent<Game.Objects.Transform>(e) ||
                Manager.HasComponent<Game.Net.Edge>(e) ||
                Manager.HasComponent<Game.Net.Node>(e) ||
                Manager.HasComponent<Game.Areas.Area>(e) ||
                Manager.HasComponent<Components.MIT_ControlPoint>(e)
                )) return false;
            return true;
        }

        internal bool IsValidNetwork(Entity e) => IsValidNetwork(EntityManager, e);
        internal static bool IsValidNetwork(EntityManager Manager, Entity e)
        {
            if (!IsValidBase(Manager, e)) return false;
            if (!(
                Manager.HasComponent<Game.Net.Edge>(e) ||
                Manager.HasComponent<Game.Net.Node>(e) ||
                Manager.HasComponent<Components.MIT_ControlPoint>(e)
                )) return false;
            return true;
        }

        internal bool IsValidStatic(Entity e) => IsValidStatic(EntityManager, e);
        internal static bool IsValidStatic(EntityManager Manager, Entity e)
        {
            if (!IsValidBase(Manager, e)) return false;
            if (Manager.HasComponent<Game.Areas.Surface>(e)) return true;
            if (!Manager.HasComponent<Game.Objects.Transform>(e)) return false;
            return true;
        }

        internal bool IsValidBase(Entity e) => IsValidBase(EntityManager, e);
        internal static bool IsValidBase(EntityManager Manager, Entity e)
        {
            if (e.Equals(Entity.Null)) return false;
            if (!Manager.Exists(e)) return false;
            if (!Manager.HasComponent<Game.Prefabs.PrefabRef>(e)) return false;
            if (Manager.HasComponent<Temp>(e)) return false;
            if (Manager.HasComponent<Terrain>(e)) return false;
            if (Manager.HasComponent<Owner>(e)) return false;
            if (Manager.TryGetComponent<Game.Objects.Attached>(e, out var comp))
            {
                if (!comp.m_Parent.Equals(Entity.Null)) return false;
            }
            return true;
        }
        #endregion

        internal void UpdateMarqueeList(Input.Marquee marquee)
        {
            if (!marquee.Update(m_PointerPos))
            {
                marquee.m_Entities = new();
            }

            Bounds3 bounds = marquee.GetBounds();

            using Searcher.Searcher searcher = new(Searcher.Utils.FilterAll & ~Searcher.Filters.Segments, m_IsManipulateMode, m_PointerPos);

            searcher.SearchMarquee(bounds.xz, marquee.m_SelectArea.xz);

            if (!marquee.m_LastBounds.Equals(bounds)) marquee.m_LastBounds = bounds;
            HashSet<Entity> result = new();
            for (int i = 0; i < math.min(searcher.Count, MoveIt.Selection.SelectionBase.MAX_SELECTION_SIZE); i++)
            {
                if (IsValid(searcher.m_Results[i].m_Entity))
                {
                    result.Add(searcher.m_Results[i].m_Entity);
                }
            }
            marquee.m_EntitiesPrev = marquee.m_Entities;
            marquee.m_Entities = result;
        }


        internal bool PointInRectangle(Quad3 rectangle, float x, float z)
        {
            return PointInRectangle(rectangle, new(x, 0f, z));
        }

        internal bool PointInRectangle(Quad3 rectangle, float3 p)
        {
            return IsLeft(rectangle.a, rectangle.b, p) && IsLeft(rectangle.b, rectangle.c, p) && IsLeft(rectangle.c, rectangle.d, p) && IsLeft(rectangle.d, rectangle.a, p);
        }

        private bool IsLeft(float3 p0, float3 p1, float3 p2)
        {
            return ((p1.x - p0.x) * (p2.z - p0.z) - (p2.x - p0.x) * (p1.z - p0.z)) > 0;
        }
    }
}
