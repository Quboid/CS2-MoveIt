using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Searcher
{
    internal class Ray : Base
    {
        internal NativeArray<Game.Common.RaycastResult> m_VanillaResults;

        internal Ray(Filters flags, NativeArray<Game.Common.RaycastResult> vanillaResults, QTypes.Manipulate manipulate = QTypes.Manipulate.Normal) : base(flags, manipulate)
        {
            m_VanillaResults = vanillaResults;
        }

        internal (Entity e, float d)[] OnLine(Line3.Segment line, float3 position)
        {
            Search(line);
            return CalculateDistances(position);
        }

        internal void Search(Line3.Segment line)
        {
            if ((m_Flags & Filters.All) == Filters.None)
            {
                m_Results = new NativeList<Entity>(0, Allocator.Temp);
                return;
            }

            var networkTree = NetSearch.GetNetSearchTree(true, out JobHandle netSearchTreeHandle);
            netSearchTreeHandle.Complete();

            NativeList<Entity> results = new(Allocator.Temp);

            if ((m_Flags & Filters.AllObjects) != Filters.None)
            {
                if (m_VanillaResults.Length != 0)
                {
                    Entity e = m_VanillaResults[0].m_Owner;
                    if (_Tool.IsValidObject(e))
                    {
                        results.Add(e);
                    }
                }
            }

            if ((m_Flags & Filters.Segments) != Filters.None)
            {
                if (m_VanillaResults.Length != 0)
                {
                    Entity e = m_VanillaResults[0].m_Owner;
                    if (_Tool.IsValidBase(e) && _Tool.EntityManager.HasComponent<Game.Net.Edge>(e))
                    {
                        results.Add(e);
                    }
                }
            }

            if ((m_Manipulation & QTypes.Manipulate.Child) == 0f && (m_Flags & Filters.Nodes) != Filters.None)
            {
                SearcherNodeIterator iterator = default;
                iterator.m_EntityList = new(Allocator.Temp);
                iterator.m_SearchLine = line;
                networkTree.Iterate(ref iterator);

                for (int i = 0; i < iterator.m_EntityList.Length; i++)
                {
                    if (!results.Contains(iterator.m_EntityList[i]) && _Tool.EntityManager.HasComponent<Game.Net.Node>(iterator.m_EntityList[i]))
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
                    if (!_Tool.Manipulation.Has(data.m_Segment)) continue;
                    Circle2 circle = new(data.m_Diameter / 2, data.Position2D);
                    float terrainHeight = _Tool.GetTerrainHeight(data.m_Position);
                    Bounds1 bounds = new(terrainHeight - 0.1f, terrainHeight + 0.2f);

                    if (QIntersect.DoesLineIntersectCylinder(line, circle, bounds, out _))
                    {
                        results.Add(data.m_Entity);
                    }
                }
            }

            m_Results = results;
        }

        private struct SearcherNodeIterator : INativeQuadTreeIterator<Entity, Game.Common.QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, Game.Common.QuadTreeBoundsXZ>, IDisposable
        {
            public Line3.Segment m_SearchLine;
            public NativeList<Entity> m_EntityList;

            public readonly bool Intersect(Game.Common.QuadTreeBoundsXZ bounds)
            {
                return MathUtils.Intersect(bounds.m_Bounds, m_SearchLine, out _);
            }

            public void Iterate(Game.Common.QuadTreeBoundsXZ _, Entity e)
            {
                if (!_Tool.IsValid(e)) return;
                if (!_Tool.EntityManager.TryGetComponent(e, out Game.Net.Node node)) return;
                if (!_Tool.EntityManager.TryGetComponent(e, out Game.Net.NodeGeometry geoData)) return;

                Circle2 cylinderCircle = Moveables.Node.GetCircle(geoData);
                Bounds1 cylinderHeight = new(node.m_Position.y - 0.1f, node.m_Position.y + 0.2f);

                if (QIntersect.DoesLineIntersectCylinder(m_SearchLine, cylinderCircle, cylinderHeight, out List<float3> debug))
                {
                    //int c = 0;
                    //foreach (float3 pos in debug)
                    //{
                    //    Overlays.Point.Set("NodeHit" + c, pos);
                    //    c++;
                    //}
                    //Overlays.Cylinder.Set("NodeBounds", cylinderCircle, cylinderHeight);
                    m_EntityList.Add(e);
                    return;
                }
            }

            public void Dispose()
            {
                m_EntityList.Dispose();
            }
        }

        public override void Dispose()
        {
            m_VanillaResults.Dispose();
            base.Dispose();
        }

        public override JobHandle Dispose(JobHandle handle)
        {
            handle = m_VanillaResults.Dispose(handle);
            return base.Dispose(handle);
        }
    }
}
