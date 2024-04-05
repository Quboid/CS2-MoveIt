using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using QCommonLib;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Searcher
{
    internal class Marquee : Base
    {
        internal Marquee(Filters flags, QTypes.Manipulate manipulate = QTypes.Manipulate.Normal) : base(flags, manipulate) { }

        /// <summary>
        /// Run the marquee search
        /// </summary>
        /// <param name="rect">The area to search within</param>
        /// <param name="outer">A map-align rectangle for quickly exluding irrelevant condidates</param>
        internal void Search(Quad2 rect, Bounds2 outer)
        {
            SearchMarquee(rect, outer);
        }

        private void SearchMarquee(Quad2 rect, Bounds2 outer)
        {
            if ((m_Flags & Filters.All) == Filters.None)
            {
                m_Results = new NativeList<Entity>(0, Allocator.Temp);
                return;
            }

            var objectTree = ObjSearch.GetStaticSearchTree(true, out JobHandle objSearchTreeHandle);
            objSearchTreeHandle.Complete();
            var networkTree = NetSearch.GetNetSearchTree(true, out JobHandle netSearchTreeHandle);
            netSearchTreeHandle.Complete();

            NativeList<Entity> results = new(Allocator.Temp);

            if ((m_Flags & Filters.AllObjects) != Filters.None)
            {
                SearcherIterator iterator = default;
                iterator.m_EntityList = new(Allocator.Temp);
                iterator.m_Type = SelectionTypes.Marquee;
                iterator.m_SearchPoint = default;
                iterator.m_SearchRect = rect;
                iterator.m_SearchOuterBounds = outer;
                objectTree.Iterate(ref iterator);
                for (int i = 0; i < iterator.m_EntityList.Length; i++)
                {
                    if (!results.Contains(iterator.m_EntityList[i]))
                    {
                        results.Add(iterator.m_EntityList[i]);
                    }
                }
                iterator.Dispose();
            }

            if ((m_Flags & Filters.AllNets) != Filters.None)
            {
                SearcherIterator iterator = default;
                iterator.m_EntityList = new(Allocator.Temp);
                iterator.m_Type = SelectionTypes.Marquee;
                iterator.m_SearchPoint = default;
                iterator.m_SearchRect = rect;
                iterator.m_SearchOuterBounds = outer;
                networkTree.Iterate(ref iterator);
                for (int i = 0; i < iterator.m_EntityList.Length; i++)
                {
                    if ((m_Flags & Filters.Segments) == 0 && _Tool.EntityManager.HasComponent<Game.Net.Edge>(iterator.m_EntityList[i])) continue;
                    if ((m_Flags & Filters.Nodes) == 0 && _Tool.EntityManager.HasComponent<Game.Net.Node>(iterator.m_EntityList[i])) continue;

                    if (!results.Contains(iterator.m_EntityList[i]))
                    {
                        results.Add(iterator.m_EntityList[i]);
                    }
                }
                iterator.Dispose();
            }

            if ((m_Flags & Filters.ControlPoints) != Filters.None && (m_Manipulation & QTypes.Manipulate.Child) != 0f && _Tool.ControlPointManager.Any)
            {
                foreach (Components.MIT_ControlPoint data in _Tool.ControlPointManager)
                {
                    Circle2 circle = new(data.m_Diameter / 2, data.Position2D);
                    if (MathUtils.Intersect(rect, circle))
                    {
                        results.Add(data.m_Entity);
                    }
                }
            }

            m_Results = results;
        }


        /// <summary>
        /// Derived from Game.Common.RaycastSystem.FindEntitiesFromTreeJob.FindEdgesIterator
        /// </summary>
        private struct SearcherIterator : INativeQuadTreeIterator<Entity, Game.Common.QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, Game.Common.QuadTreeBoundsXZ>, IDisposable
        {
            public SelectionTypes m_Type;
            public Quad2 m_SearchRect;
            public Bounds2 m_SearchOuterBounds;
            public float2 m_SearchPoint;
            public NativeList<Entity> m_EntityList;

            public readonly bool Intersect(Game.Common.QuadTreeBoundsXZ bounds)
            {
                if (m_Type == SelectionTypes.Point)
                {
                    return MathUtils.Intersect(bounds.m_Bounds.xz, m_SearchPoint);
                }
                return MathUtils.Intersect(bounds.m_Bounds.xz, m_SearchRect);
            }

            public void Iterate(Game.Common.QuadTreeBoundsXZ bounds, Entity e)
            {
                if (!_Tool.IsValid(e)) return;
                if (!MathUtils.Intersect(m_SearchOuterBounds, bounds.m_Bounds.xz)) return;

                QAccessor.QObjectSimple obj = new(e, _Tool);
                var prefab = obj.GetComponent<Game.Prefabs.PrefabRef>().m_Prefab;

                if (obj.m_Identity == QTypes.Identity.Building)
                {
                    Quad2 objRect = CalculateBuildingCorners(ref obj, prefab);

                    switch (m_Type)
                    {
                        case SelectionTypes.Point:
                            if (MathUtils.Intersect(objRect, m_SearchPoint))
                            {
                                m_EntityList.Add(e);
                                return;
                            }
                            break;

                        case SelectionTypes.Marquee:
                            if (MathUtils.Intersect(objRect, m_SearchRect))
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

                if (obj.m_Identity == QTypes.Identity.Node)
                {
                    if (!_Tool.EntityManager.TryGetComponent(e, out Game.Net.NodeGeometry geoData)) return;

                    Circle2 circle = Moveables.Node.GetCircle(geoData);

                    switch (m_Type)
                    {
                        case SelectionTypes.Point:
                            if (MathUtils.Intersect(circle, m_SearchPoint))
                            {
                                m_EntityList.Add(e);
                                return;
                            }
                            break;

                        case SelectionTypes.Marquee:
                            if (MathUtils.Intersect(m_SearchRect, circle))
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

                if (_Tool.EntityManager.TryGetComponent<Game.Prefabs.ObjectGeometryData>(prefab, out var objGeoData))
                {
                    if ((objGeoData.m_Flags & Game.Objects.GeometryFlags.Circular) > 0)
                    {
                        float2 pos = objBounds.Center();
                        float radius = math.max(objGeoData.m_Size.x, objGeoData.m_Size.z) / 2;
                        Circle2 circle = new(radius, pos);

                        switch (m_Type)
                        {
                            case SelectionTypes.Point:
                                if (MathUtils.Intersect(circle, m_SearchPoint))
                                {
                                    m_EntityList.Add(e);
                                    return;
                                }
                                break;

                            case SelectionTypes.Marquee:
                                if (MathUtils.Intersect(m_SearchRect, circle))
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
                    case SelectionTypes.Point:
                        if (MathUtils.Intersect(objBounds, m_SearchPoint))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    case SelectionTypes.Marquee:
                        if (MathUtils.Intersect(objBounds, m_SearchRect))
                        {
                            m_EntityList.Add(e);
                            return;
                        }
                        break;

                    default:
                        break;
                }
            }

            private readonly Quad2 CalculateBuildingCorners(ref QAccessor.QObjectSimple obj, Entity prefab)
            {
                int2 lotSize = _Tool.EntityManager.GetComponentData<Game.Prefabs.BuildingData>(prefab).m_LotSize;
                float offX = lotSize.x * 4;
                float offZ = lotSize.y * 4;
                float2 position = obj.Parent.Position.XZ();
                quaternion q = obj.Parent.Rotation;

                Quad2 result = new(
                    RotateAroundPivot(position, q, new(-offX, 0, -offZ)),
                    RotateAroundPivot(position, q, new(offX, 0, -offZ)),
                    RotateAroundPivot(position, q, new(offX, 0, offZ)),
                    RotateAroundPivot(position, q, new(-offX, 0, offZ)));
                return result;
            }

            private readonly float2 RotateAroundPivot(float2 position, quaternion q, float3 offset)
            {
                float3 newPos = math.mul(q, offset);
                return position + new float2(newPos.x, newPos.z);
            }

            public void Dispose()
            {
                m_EntityList.Dispose();
            }
        }
    }
}
