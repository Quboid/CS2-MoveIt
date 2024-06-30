using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using MoveIt.QAccessor;
using MoveIt.Tool;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Searcher
{
    /// <summary>
    /// Derived from Game.Common.RaycastSystem.FindEntitiesFromTreeJob.FindEdgesIterator
    /// </summary>
    internal struct SearcherIterator : INativeQuadTreeIterator<Entity, Game.Common.QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, Game.Common.QuadTreeBoundsXZ>, IDisposable
    {
        public SearchTypes m_Type;
        public Quad2 m_SearchRect;
        public Bounds2 m_SearchOuterBounds;
        public float2 m_SearchPoint;
        public NativeList<Entity> m_EntityList;
        public QLookup m_Lookup;
        public EntityManager m_Manager;

        public readonly bool Intersect(Game.Common.QuadTreeBoundsXZ bounds)
        {
            bool result = m_Type switch
            {
                SearchTypes.Point    => MathUtils.Intersect(bounds.m_Bounds.xz, m_SearchPoint),
                SearchTypes.Marquee  => MathUtils.Intersect(bounds.m_Bounds.xz, m_SearchRect),
                SearchTypes.Bounds   => MathUtils.Intersect(bounds.m_Bounds.xz, m_SearchOuterBounds),
                _ => throw new NotImplementedException(),
            };

            return result;
        }

        public void Iterate(Game.Common.QuadTreeBoundsXZ bounds, Entity e)
        {
            if (!MIT.IsValid(m_Manager, e)) return;
            if (!MathUtils.Intersect(m_SearchOuterBounds, bounds.m_Bounds.xz)) return;
            if (m_EntityList.Length >= (Selection.SelectionBase.MAX_SELECTION_SIZE * 2)) return;

            QObjectSimple obj = new(m_Manager, ref m_Lookup, e);
            var prefab = m_Manager.GetComponentData<Game.Prefabs.PrefabRef>(e).m_Prefab;

            if (obj.m_Identity == Identity.Building)
            {
                Quad2 objRect = SearcherBase.CalculateBuildingCorners(m_Manager, ref obj, prefab);

                switch (m_Type)
                {
                    case SearchTypes.Point:
                        if (MathUtils.Intersect(objRect, m_SearchPoint))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    case SearchTypes.Marquee:
                        if (MathUtils.Intersect(objRect, m_SearchRect))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    case SearchTypes.Bounds:
                        if (MathUtils.Intersect(m_SearchOuterBounds, objRect))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    default:
                        break;
                }

                return;
            }

            if (obj.m_Identity == Identity.Node)
            {
                if (!m_Manager.TryGetComponent(e, out Game.Net.Node node)) return;

                Circle2 circle = GetCircle(m_Manager, e, node);

                switch (m_Type)
                {
                    case SearchTypes.Point:
                        if (MathUtils.Intersect(circle, m_SearchPoint))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    case SearchTypes.Marquee:
                        if (MathUtils.Intersect(m_SearchRect, circle))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    case SearchTypes.Bounds:
                        if (MathUtils.Intersect(m_SearchOuterBounds, circle))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    default:
                        break;
                }
                return;
            }


            Bounds2 objBounds = bounds.m_Bounds.xz;

            if (m_Manager.TryGetComponent<Game.Prefabs.ObjectGeometryData>(prefab, out var objGeoData))
            {
                if ((objGeoData.m_Flags & Game.Objects.GeometryFlags.Circular) > 0)
                {
                    float2 pos = objBounds.Center();
                    float radius = math.max(objGeoData.m_Size.x, objGeoData.m_Size.z) / 2;
                    Circle2 circle = new(radius, pos);

                    switch (m_Type)
                    {
                        case SearchTypes.Point:
                            if (MathUtils.Intersect(circle, m_SearchPoint))
                            {
                                m_EntityList.Add(e);
                                return;
                            }
                            break;

                        case SearchTypes.Marquee:
                            if (MathUtils.Intersect(m_SearchRect, circle))
                            {
                                m_EntityList.Add(e);
                                return;
                            }
                            break;

                        case SearchTypes.Bounds:
                            if (MathUtils.Intersect(m_SearchOuterBounds, circle))
                            {
                                m_EntityList.Add(e);
                                return;
                            }
                            break;

                        default:
                            break;
                    }

                    return;
                }
            }

            switch (m_Type)
            {
                case SearchTypes.Point:
                    if (MathUtils.Intersect(objBounds, m_SearchPoint))
                    {
                        m_EntityList.Add(e);
                        return;
                    }
                    break;

                case SearchTypes.Marquee:
                    if (MathUtils.Intersect(objBounds, m_SearchRect))
                    {
                        m_EntityList.Add(e);
                        return;
                    }
                    break;

                case SearchTypes.Bounds:
                    if (MathUtils.Intersect(m_SearchOuterBounds, objBounds))
                    {
                        m_EntityList.Add(e);
                        return;
                    }
                    break;

                default:
                    break;
            }
        }

        internal static Circle2 GetCircle(EntityManager manager, Entity e, Game.Net.Node node)
        {
            if (manager.TryGetComponent(e, out Game.Net.NodeGeometry geoData))
            {
                return Moveables.MVNode.GetCircle(geoData);
            }
            return Moveables.MVNode.GetCircle(node);
        }

        public void Dispose()
        {
            m_EntityList.Dispose();
        }
    }
}
