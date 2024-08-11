using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Areas;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Searcher
{
    internal struct SearcherIterator : IDisposable,
        INativeQuadTreeIterator<Entity, Game.Common.QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, Game.Common.QuadTreeBoundsXZ>,
        INativeQuadTreeIterator<AreaSearchItem, Game.Common.QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<AreaSearchItem, Game.Common.QuadTreeBoundsXZ>
    {
        public NativeList<Entity> m_EntityList;
        public QLookup m_Lookup;
        public EntityManager m_Manager;
        public bool m_IsManipulating;

        public SearchTypes m_Type;
        public Quad2 m_SearchQuad;              // For SearchTypes.Marquee
        public Bounds2 m_SearchOuterBounds;     // For SearchTypes.Marquee, .Bounds
        public float2 m_SearchPoint;            // For SearchTypes.Point
        public Line3.Segment m_SearchRay;       // For SearchTypes.Ray

        /// <summary>
        /// Does the given <see cref="bounds">bounds</see> intersect the search area?
        /// </summary>
        /// <param name="bounds">The QuadTree bounds</param>
        /// <returns>Does bounds intersect search area?</returns>
        public readonly bool Intersect(Game.Common.QuadTreeBoundsXZ bounds)
        {
            Bounds2 b = bounds.m_Bounds.xz;
            bool result = m_Type switch
            {
                SearchTypes.Marquee => MathUtils.Intersect(b, m_SearchQuad),
                SearchTypes.Bounds  => MathUtils.Intersect(b, m_SearchOuterBounds),
                SearchTypes.Point   => MathUtils.Intersect(b, m_SearchPoint),
                SearchTypes.Ray     => QIntersect.DoesLineIntersectBounds3(m_SearchRay, bounds.m_Bounds, out _),
                _ => throw new NotImplementedException(),
            };

            return result;
        }

        /// <summary>
        /// Check if the <see cref="e">entity's</see> <see cref="bounds">bounding box</see> intersects the search area
        /// </summary>
        /// <param name="bounds">The entity's bounds</param>
        /// <param name="e">The entity to check and, if valid, add to result</param>
        public void Iterate(Game.Common.QuadTreeBoundsXZ bounds, Entity e)
        {
            if (!MIT.IsValid(m_Manager, e)) return;
            if (m_EntityList.Length >= (Selection.SelectionBase.MAX_SELECTION_SIZE * 2)) return;
            if (m_Type == SearchTypes.Marquee || m_Type == SearchTypes.Bounds)
            {
                if (!MathUtils.Intersect(m_SearchOuterBounds, bounds.m_Bounds.xz)) return;
            }

            QObjectSimple obj = new(m_Manager, ref m_Lookup, e);
            var prefab = m_Manager.GetComponentData<Game.Prefabs.PrefabRef>(e).m_Prefab;

            // Building
            if (!m_IsManipulating && (obj.m_Identity == Identity.Building || obj.m_Identity == Identity.Extension))
            {
                Quad2 objRect = Utils.CalculateBuildingCorners(m_Manager, ref obj, prefab);

                switch (m_Type)
                {
                    case SearchTypes.Marquee:
                        if (MathUtils.Intersect(objRect, m_SearchQuad))
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

                    case SearchTypes.Point:
                        if (MathUtils.Intersect(objRect, m_SearchPoint))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    case SearchTypes.Ray:
                        float y = obj.m_Parent.Position.y;
                        Quad3 quad3 = new(
                            new float3(objRect.a.x, y, objRect.a.y),
                            new float3(objRect.b.x, y, objRect.b.y),
                            new float3(objRect.c.x, y, objRect.c.y),
                            new float3(objRect.d.x, y, objRect.d.y));

                        if (QIntersect.DoesLineIntersectQuad3(m_SearchRay, quad3, out _))
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


            // Node
            if (!m_IsManipulating && obj.m_Identity == Identity.Node)
            {
                if (!m_Manager.TryGetComponent(e, out Game.Net.Node node)) return;

                Circle2 circle = Utils.GetCircle(m_Manager, e, node);

                switch (m_Type)
                {
                    case SearchTypes.Marquee:
                        if (MathUtils.Intersect(m_SearchQuad, circle))
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

                    case SearchTypes.Point:
                        if (MathUtils.Intersect(circle, m_SearchPoint))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    case SearchTypes.Ray:
                        Bounds1 cylinderHeight = new(node.m_Position.y - 0.1f, node.m_Position.y + 0.2f);

                        if (QIntersect.DoesLineIntersectCylinder(m_SearchRay, circle, cylinderHeight))
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


            // Segments
            if (obj.m_Identity == Identity.Segment || obj.m_Identity == Identity.NetLane)
            {
                switch (m_Type)
                {
                    case SearchTypes.Marquee:
                        if (MathUtils.Intersect(objBounds, m_SearchQuad))
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

                    case SearchTypes.Point:
                        // TODO Remember to implement if ever needed
                        return;

                    case SearchTypes.Ray:
                        // Handled by vanilla raycast results
                        return;

                    default:
                        break;
                }
                return;
            }


            // A circular object
            if (!m_IsManipulating && m_Manager.TryGetComponent<Game.Prefabs.ObjectGeometryData>(prefab, out var objGeoData))
            {
                if ((objGeoData.m_Flags & Game.Objects.GeometryFlags.Circular) > 0)
                {
                    float2 pos = objBounds.Center();
                    float radius = math.max(objGeoData.m_Size.x, objGeoData.m_Size.z) / 2;
                    Circle2 circle = new(radius, pos);

                    switch (m_Type)
                    {
                        case SearchTypes.Marquee:
                            if (MathUtils.Intersect(m_SearchQuad, circle))
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

                        case SearchTypes.Point:
                            if (MathUtils.Intersect(circle, m_SearchPoint))
                            {
                                m_EntityList.Add(e);
                                return;
                            }
                            break;

                        case SearchTypes.Ray:
                            Bounds1 cylinderHeight = new(obj.m_Parent.Position.y, obj.m_Parent.Position.y + 1f);

                            if (QIntersect.DoesLineIntersectCylinder(m_SearchRay, circle, cylinderHeight))
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


            // Any other object
            if (!m_IsManipulating)
            {
                switch (m_Type)
                {
                    case SearchTypes.Marquee:
                        if (MathUtils.Intersect(objBounds, m_SearchQuad))
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

                    case SearchTypes.Point:
                        if (MathUtils.Intersect(objBounds, m_SearchPoint))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    case SearchTypes.Ray:
                        if (QIntersect.DoesLineIntersectBounds3(m_SearchRay, bounds.m_Bounds, out _))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    default:
                        break;
                }
            }
        }


        /// <summary>
        /// Check if the <see cref="areaSearchItem">area search item's</see> <see cref="bounds">bounding box</see> intersects the search area
        /// </summary>
        /// <param name="bounds">The entity's bounds</param>
        /// <param name="areaSearchItem">The entity to check and, if valid, add to result</param>
        public void Iterate(Game.Common.QuadTreeBoundsXZ bounds, AreaSearchItem areaSearchItem)
        {
            Entity e = areaSearchItem.m_Area;

            if (!MIT.IsValid(m_Manager, e)) return;
            if (m_EntityList.Length >= (Selection.SelectionBase.MAX_SELECTION_SIZE * 2)) return;
            if (m_Type == SearchTypes.Marquee || m_Type == SearchTypes.Bounds)
            {
                if (!MathUtils.Intersect(m_SearchOuterBounds, bounds.m_Bounds.xz)) return;
            }
            else
            {
                return;
            }

            if (m_Manager.HasComponent<Game.Common.Owner>(e)) return;
            if (!m_Manager.HasComponent<Surface>(e)) return;

            if (m_Manager.TryGetBuffer<Node>(e, true, out var nodes) && m_Manager.TryGetBuffer<Triangle>(e, true, out var triangles))
            {
                Triangle2 tri = AreaUtils.GetTriangle2(nodes, triangles[areaSearchItem.m_Triangle]);
                if (!MathUtils.Intersect(m_SearchOuterBounds, tri)) return;
                if (m_Type == SearchTypes.Marquee && !MathUtils.Intersect(m_SearchQuad, tri)) return;

                m_EntityList.Add(e);
            }
        }

        public void Dispose()
        {
            m_EntityList.Dispose();
        }
    }
}
