using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using MoveIt.Moveables;
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

        internal Ray(Filters flags, NativeArray<Game.Common.RaycastResult> vanillaResults, bool isManipulating = false) : base(flags, isManipulating)
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

            // Non-network objects
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

            // Segments
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

            // Nodes
            if (!m_IsManipulating && (m_Flags & Filters.Nodes) != Filters.None)
            {
                SearcherNodeIterator iterator = default;
                iterator.m_EntityList = new(Allocator.Temp);
                iterator.m_SearchLine = line;
                networkTree.Iterate(ref iterator);

                //string msg = $"Results:{results.Length} Entities: {iterator.m_EntityList.Length} ";
                for (int i = 0; i < iterator.m_EntityList.Length; i++)
                {
                    //msg += $" {iterator.m_EntityList[i].DX()}:{results.Contains(iterator.m_EntityList[i])}:{_Tool.EntityManager.HasComponent<Game.Net.Node>(iterator.m_EntityList[i])}";
                    if (!results.Contains(iterator.m_EntityList[i]) && _Tool.EntityManager.HasComponent<Game.Net.Node>(iterator.m_EntityList[i]))
                    {
                        //msg += "!";
                        results.Add(iterator.m_EntityList[i]);
                    }
                }
                //if (results.Length > 0) QLog.Debug(msg);
                iterator.Dispose();
            }

            // Control Points
            if ((m_Flags & Filters.ControlPoints) != Filters.None && m_IsManipulating && _Tool.ControlPointManager.Any)
            {
                foreach (Components.MIT_ControlPoint data in _Tool.ControlPointManager.GetAllData())
                {
                    if (data.m_IsManipulatable != m_IsManipulating) continue;
                    if (!_Tool.Selection.Has(new MVDefinition(Identity.Segment, data.m_Parent, data.m_IsManipulatable))) continue;

                    Circle2 circle = new(data.m_Diameter / 2, data.Position2D);
                    Bounds1 bounds = new(data.m_Position.y - 0.1f, data.m_Position.y + 0.4f);
                    //Overlays.Cylinder.Set("NodeBounds" + c++, circle, bounds);

                    if (QIntersect.DoesLineIntersectCylinder(line, circle, bounds, out _))
                    {
                        results.Add(data.m_Entity);
                    }
                }
            }

            m_Results = results;
            //if (m_Results.Length > 0) DebugDumpResults(true, $"vanilla:{m_VanillaResults.Length}, ");
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

                Circle2 cylinderCircle = GetCircle(e, node);
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

            private readonly Circle2 GetCircle(Entity e, Game.Net.Node node)
            {
                if (_Tool.EntityManager.TryGetComponent(e, out Game.Net.NodeGeometry geoData))
                {
                    return MVNode.GetCircle(geoData);
                }
                return MVNode.GetCircle(node);
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


        #region Debug

        protected string DebugResults(bool full = false)
        {
            string msg = $"Results: {m_Results.Length}";
            if (full)
            {
                for (int i = 0; i < m_Results.Length; i++)
                {
                    msg += $"\n    {m_Results[i].DX(true)}";
                }
            }
            else
            {
                msg += "  ";
                for (int i = 0; i < m_Results.Length; i++)
                {
                    msg += $"{m_Results[i].DX()}, ";
                }
            }
            return msg;
        }

        protected void DebugDumpResults(bool full = false, string prefix = "")
        {
            QLog.Debug(prefix + DebugResults(full));
        }

        #endregion
    }
}
