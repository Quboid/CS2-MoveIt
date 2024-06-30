using Colossal.Collections;
using Colossal.Mathematics;
using MoveIt.QAccessor;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace MoveIt.Searcher
{
    [BurstCompile]
    internal struct MarqueeJob : IJob
    {
        internal NativeQuadTree<Entity, Game.Common.QuadTreeBoundsXZ> m_StaticTree;
        internal NativeQuadTree<Entity, Game.Common.QuadTreeBoundsXZ> m_NetworkTree;
        internal NativeArray<Components.MIT_ControlPoint> m_ControlPoints;
        internal Filters m_Filters;
        internal bool m_IsManipulating;
        internal QLookup m_Lookup;
        internal EntityManager m_Manager;
        internal NativeList<Entity> m_Results;
        internal Quad2 m_SearchArea;
        internal Bounds2 m_SearchBounds;
        internal SearchTypes m_SearchType;

        public void Execute()
        {
            // Static objects
            if ((m_Filters & Filters.AllStatics) != Filters.None)
            {
                SearcherIterator iterator = default;
                iterator.m_EntityList           = new(Allocator.Temp);
                iterator.m_Type                 = m_SearchType;
                iterator.m_SearchPoint          = default;
                iterator.m_SearchRect           = m_SearchArea;
                iterator.m_SearchOuterBounds    = m_SearchBounds;
                iterator.m_Lookup               = m_Lookup;
                iterator.m_Manager              = m_Manager;

                m_StaticTree.Iterate(ref iterator);
                for (int i = 0; i < iterator.m_EntityList.Length; i++)
                {
                    Entity e = iterator.m_EntityList[i];
                    if (!m_Results.Contains(e) && FilterStatic(e))
                    {
                        m_Results.Add(e);
                    }
                }
                iterator.Dispose();
            }

            // Networks
            if ((m_Filters & Filters.AllNets) != Filters.None)
            {
                SearcherIterator iterator = default;
                iterator.m_EntityList           = new(Allocator.Temp);
                iterator.m_Type                 = m_SearchType;
                iterator.m_SearchPoint          = default;
                iterator.m_SearchRect           = m_SearchArea;
                iterator.m_SearchOuterBounds    = m_SearchBounds;
                iterator.m_Lookup               = m_Lookup;
                iterator.m_Manager              = m_Manager;
                m_NetworkTree.Iterate(ref iterator);
                for (int i = 0; i < iterator.m_EntityList.Length; i++)
                {
                    if ((m_Filters & Filters.Segments) == 0 && m_Manager.HasComponent<Game.Net.Edge>(iterator.m_EntityList[i])) continue;
                    if ((m_Filters & Filters.Nodes) == 0 && m_Manager.HasComponent<Game.Net.Node>(iterator.m_EntityList[i])) continue;

                    if (!m_Results.Contains(iterator.m_EntityList[i]))
                    {
                        m_Results.Add(iterator.m_EntityList[i]);
                    }
                }
                iterator.Dispose();
            }

            // Control Points
            if ((m_Filters & Filters.ControlPoints) != Filters.None && m_IsManipulating && m_ControlPoints.Length > 0)
            {
                foreach (Components.MIT_ControlPoint data in m_ControlPoints)
                {
                    if (data.m_IsManipulatable != m_IsManipulating) continue;
                    Circle2 circle = new(data.m_Diameter / 2, data.Position2D);

                    switch (m_SearchType)
                    {
                        case SearchTypes.Marquee:
                            if (MathUtils.Intersect(m_SearchArea, circle))
                            {
                                m_Results.Add(data.m_Entity);
                            }
                            break;

                        case SearchTypes.Bounds:
                            if (MathUtils.Intersect(m_SearchBounds, circle))
                            {
                                m_Results.Add(data.m_Entity);
                            }
                            break;
                    }
                }
            }
        }


        private readonly bool FilterStatic(Entity e)
        {
            if ((m_Filters & Filters.AllStatics) == 0) return false; // Not looking for a static
            if ((m_Filters & Filters.AllStatics) == Filters.AllStatics) return true; // Looking for any static

            if ((m_Filters & Filters.Buildings) != 0)
            {
                if (HasOr<Game.Buildings.Building, Game.Buildings.Extension>(e))
                {
                    return true;
                }
            }

            if ((m_Filters & Filters.Plants) != 0)
            {
                if (Has<Game.Objects.Plant>(e))
                {
                    return true;
                }
            }

            if ((m_Filters & Filters.Props) != 0)
            {
                if (HasOr<Game.Objects.ObjectGeometry, Game.Objects.Surface>(e))
                {
                    return true;
                }
            }

            if ((m_Filters & Filters.Decals) != 0)
            {
                if (Has<Game.Objects.ObjectGeometry>(e) && !Has<Game.Objects.Surface>(e))
                {
                    return true;
                }
            }

            return false;
        }
        private readonly bool Has<T>(Entity e) where T : IComponentData
            => m_Manager.HasComponent<T>(e);
        private readonly bool HasOr<T1, T2>(Entity e) where T1 : IComponentData where T2 : IComponentData
            => m_Manager.HasComponent<T1>(e) || m_Manager.HasComponent<T2>(e);
    }
}
