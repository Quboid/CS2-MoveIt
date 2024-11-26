using Colossal.Collections;
using Colossal.Mathematics;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Searcher
{
#if USE_BURST
    [BurstCompile]
#endif
    internal struct SearcherJob : IJob
    {
        internal NativeQuadTree<Entity, Game.Common.QuadTreeBoundsXZ> m_StaticTree;
        internal NativeQuadTree<Entity, Game.Common.QuadTreeBoundsXZ> m_NetworkTree;
        internal NativeQuadTree<Game.Areas.AreaSearchItem, Game.Common.QuadTreeBoundsXZ> m_AreaTree;
        internal NativeArray<Components.MIT_ControlPoint> m_ControlPoints;
        internal Filters m_Filters;
        internal bool m_IsManipulating;
        internal QLookup m_Lookup;
        internal EntityManager m_Manager;
        internal NativeList<Entity> m_Results;
        internal float3 m_TerrainPosition;

        internal SearchTypes m_SearchType;
        internal Quad2 m_SearchRect;            // For SearchTypes.Marquee
        internal Bounds2 m_SearchOuterBounds;   // For SearchTypes.Marquee, .Bounds
        internal float3 m_SearchPoint;          // For SearchTypes.Point
        internal Line3.Segment m_SearchRay;     // For SearchTypes.Ray

        public void Execute()
        {
            // Static objects
            if ((m_Filters & Utils.FilterAllStatics) != Filters.None)
            {
                SearcherIterator iterator       = default;
                iterator.m_EntityList           = new(Allocator.Temp);
                iterator.m_Lookup               = m_Lookup;
                iterator.m_Manager              = m_Manager;
                iterator.m_IsManipulating       = m_IsManipulating;

                iterator.m_Type                 = m_SearchType;
                iterator.m_SearchQuad           = m_SearchRect;
                iterator.m_SearchOuterBounds    = m_SearchOuterBounds;
                iterator.m_SearchPoint          = default;
                iterator.m_SearchRay            = m_SearchRay;

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
            if ((m_Filters & Utils.FilterAllNetworks) != Filters.None)
            {
                SearcherIterator iterator       = default;
                iterator.m_EntityList           = new(Allocator.Temp);
                iterator.m_Lookup               = m_Lookup;
                iterator.m_Manager              = m_Manager;
                iterator.m_IsManipulating       = m_IsManipulating;

                iterator.m_Type                 = m_SearchType;
                iterator.m_SearchQuad           = m_SearchRect;
                iterator.m_SearchOuterBounds    = m_SearchOuterBounds;
                iterator.m_SearchPoint          = default;
                iterator.m_SearchRay            = m_SearchRay;

                m_NetworkTree.Iterate(ref iterator);

                foreach (Entity e in iterator.m_EntityList)
                {
                    // Nodes
                    if ((m_Filters & Filters.Nodes) == 0)
                    {
                        if (m_Manager.HasComponent<Game.Net.Node>(e) && !m_Manager.HasComponent<Game.Tools.EditorContainer>(e))
                        {
                            continue;
                        }
                    }

                    // Segments
                    if ((m_Filters & Filters.Segments) == 0)
                    {
                        if (m_Manager.HasComponent<Game.Net.Edge>(e) && m_Manager.HasComponent<Game.Net.EdgeGeometry>(e))
                        {
                            continue;
                        }
                    }

                    // Netlanes
                    if ((m_Filters & Filters.Netlanes) == 0)
                    {
                        if (m_Manager.HasComponent<Game.Net.Node>(e) && m_Manager.HasComponent<Game.Tools.EditorContainer>(e))
                        {
                            continue;
                        }
                        if (m_Manager.HasComponent<Game.Net.Edge>(e) && !m_Manager.HasComponent<Game.Net.EdgeGeometry>(e))
                        {
                            continue;
                        }
                    }

                    //if (((m_Filters & Filters.Nodes) == 0) && m_Manager.HasComponent<Game.Net.Node>(e)) continue;
                    //if (((m_Filters & Filters.Segments) == 0) && m_Manager.HasComponent<Game.Net.Edge>(e) && m_Manager.HasComponent<Game.Net.EdgeGeometry>(e)) continue;
                    //if (((m_Filters & Filters.Netlanes) == 0) &&
                    //    ((m_Manager.HasComponent<Game.Net.Edge>(e) && !m_Manager.HasComponent<Game.Net.EdgeGeometry>(e)) || 
                    //    m_Manager.HasComponent<Game.Net.AreaLane>(e)))
                    //{
                    //    continue;
                    //}

                    if (!m_Results.Contains(e))
                    {
                        m_Results.Add(e);
                    }
                }
                iterator.Dispose();
            }

            // Surfaces
            if ((m_Filters & Filters.Surfaces) != Filters.None)
            {
                SearcherIterator iterator = default;
                iterator.m_EntityList = new(Allocator.Temp);
                iterator.m_Lookup = m_Lookup;
                iterator.m_Manager = m_Manager;
                iterator.m_IsManipulating = m_IsManipulating;

                iterator.m_Type = m_SearchType;
                iterator.m_SearchQuad = m_SearchRect;
                iterator.m_SearchOuterBounds = m_SearchOuterBounds;
                iterator.m_SearchPoint = default;
                iterator.m_SearchRay = m_SearchRay;

                m_AreaTree.Iterate(ref iterator);

                for (var i = 0; i < iterator.m_EntityList.Length; i++)
                {
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
                            if (MathUtils.Intersect(m_SearchRect, circle))
                            {
                                m_Results.Add(data.m_Entity);
                            }
                            break;

                        case SearchTypes.Bounds:
                            if (MathUtils.Intersect(m_SearchOuterBounds, circle))
                            {
                                m_Results.Add(data.m_Entity);
                            }
                            break;

                        case SearchTypes.Ray:
                            Bounds1 bounds = new(data.m_Position.y - 0.1f, data.m_Position.y + 0.4f);
                            if (QIntersect.DoesLineIntersectCylinder(m_SearchRay, circle, bounds))
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
            if ((m_Filters & Utils.FilterAllStatics) == 0) return false; // Not looking for a static
            //if ((m_Filters & Utils.FilterAllStatics) == Utils.FilterAllStatics) return true; // Looking for any static - TOO BROAD, GETS MVOther/etc

            Identity identity = QTypes.GetEntityIdentity(m_Manager, e);

            bool result = false;
            switch (identity)
            {
                case Identity.Building:
                case Identity.Extension:
                case Identity.ServiceUpgrade:
                    result = (m_Filters & Filters.Buildings) != 0;
                    break;

                case Identity.Plant:
                    result = (m_Filters & Filters.Plants) != 0;
                    break;

                case Identity.Prop:
                    result = (m_Filters & Filters.Props) != 0;
                    break;

                case Identity.Decal:
                    result = (m_Filters & Filters.Decals) != 0;
                    break;

                case Identity.Surface:
                    result = (m_Filters & Filters.Surfaces) != 0;
                    break;
            }

            return result;

            //if ((m_Filters & Filters.Buildings) != 0)
            //{
            //    if (HasOr<Game.Buildings.Building, Game.Buildings.Extension>(e))
            //    {
            //        return true;
            //    }
            //}

            //if ((m_Filters & Filters.Plants) != 0)
            //{
            //    if (Has<Game.Objects.Plant>(e))
            //    {
            //        return true;
            //    }
            //}

            //if ((m_Filters & Filters.Props) != 0)
            //{
            //    if (HasOr<Game.Objects.ObjectGeometry, Game.Objects.Surface>(e))
            //    {
            //        return true;
            //    }
            //}

            //if ((m_Filters & Filters.Decals) != 0)
            //{
            //    if (Has<Game.Objects.ObjectGeometry>(e) && !Has<Game.Objects.Surface>(e))
            //    {
            //        return true;
            //    }
            //}

            //if ((m_Filters & Filters.Surfaces) != 0)
            //{
            //    if (Has<Game.Areas.Area>(e) && Has<Game.Areas.Surface>(e))
            //    {
            //        return true;
            //    }
            //}

            //return false;
        }

        private readonly bool Has<T>(Entity e) where T : IComponentData
            => m_Manager.HasComponent<T>(e);
        private readonly bool HasOr<T1, T2>(Entity e) where T1 : IComponentData where T2 : IComponentData
            => m_Manager.HasComponent<T1>(e) || m_Manager.HasComponent<T2>(e);
    }
}
