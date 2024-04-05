using Colossal.Mathematics;
using MoveIt.Components;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Managers
{
    public enum CPStatus
    {
        None = 0,
        Visible = 1,
        Selected = 2,
        Hovering = 4,
    }

    public struct CPDefinition
    {
        public Entity m_Segment;
        public short m_CurveKey;

        public CPDefinition(Entity node, Entity segment, short curveKey)
        {
            m_Segment = segment;
            m_CurveKey = curveKey;
        }

        public CPDefinition(ControlPoint cp)
        {
            m_Segment = cp.m_Segment;
            m_CurveKey = cp.m_CurveKey;
        }

        public CPDefinition(StateControlPoint scp)
        {
            m_Segment = scp.m_Segment;
            m_CurveKey = scp.m_Curvekey;
        }

        public readonly override string ToString()
        {
            return $"s:{m_Segment.D()}:{m_CurveKey}";
        }
    }

    public class ControlPointManager : IEnumerable<MIT_ControlPoint>
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

        private EntityArchetype _ControlPointArchetype;
        internal List<ControlPoint> m_ControlPoints;

        internal bool Any => m_ControlPoints.Count > 0;

        public ControlPointManager()
        {
            _ControlPointArchetype = _Tool.EntityManager.CreateArchetype(new ComponentType[] {
                ComponentType.ReadWrite<Game.Prefabs.PrefabRef>(),
                ComponentType.ReadWrite<Game.Rendering.CullingInfo>(),
                ComponentType.ReadWrite<MIT_ControlPoint>()
            });

            m_ControlPoints = new();
        }

        public ControlPoint Factory(CPDefinition cpd)
        {
            return Factory(cpd.m_Segment, cpd.m_CurveKey);
        }

        public ControlPoint Factory(Entity segment, short curveKey)
        {
            if (m_ControlPoints.Any(cp => cp.m_CurveKey == curveKey && cp.m_Segment == segment))
            {
                ControlPoint result = m_ControlPoints.First(cp => cp.m_CurveKey == curveKey && cp.m_Segment == segment);
                return result;
            }

            Entity e = _Tool.EntityManager.CreateEntity(_ControlPointArchetype);

            float segmentWidth = Segment.GetDefaultWidth(segment);
            Bezier4x3 curve = _Tool.EntityManager.GetComponentData<Game.Net.Curve>(segment).m_Bezier;
            float3 position = curve.Get(curveKey);

            Game.Net.Edge edge = _Tool.EntityManager.GetComponentData<Game.Net.Edge>(segment);
            MIT_ControlPoint cpData = new(e, segment, (curveKey.IsNodeA() ? edge.m_Start : edge.m_End), position, math.max(segmentWidth / 4, 2f), curveKey);
            _Tool.EntityManager.SetComponentData(e, cpData);
            Game.Prefabs.PrefabRef prefabRef = new(Entity.Null);
            _Tool.EntityManager.SetComponentData(e, prefabRef);
            Game.Rendering.CullingInfo culling = new()
            {
                m_Bounds = new(cpData.m_Position - cpData.m_Diameter / 2, cpData.m_Position + cpData.m_Diameter / 2),
                m_Radius = cpData.m_Diameter / 2
            };

            ControlPoint cp = (ControlPoint)Moveable.Factory(e);
            Add(cp);
            return cp;
        }

        public void Add(ControlPoint cp)
        {
            if (Has(cp.m_Entity)) throw new Exception($"ControlPoint for {cp.m_Entity.D()} already exists!");
            m_ControlPoints.Add(cp);
        }

        public void RemoveFromList(ControlPoint cp)
        {
            m_ControlPoints.Remove(cp);
        }

        public void Clear()
        {
            while (m_ControlPoints.Count > 0)
            {
                m_ControlPoints[0].Dispose();
            }

            if (m_ControlPoints.Count > 0)
            {
                MIT.Log.Warning($"{m_ControlPoints.Count} control points still exist after cleanup!");
            }
        }

        /// <summary>
        /// Refresh CPs when the tool is activated, remove any that are not valid
        /// </summary>
        public void Refresh()
        {
            HashSet<ControlPoint> remove = new();
            foreach (ControlPoint cp in m_ControlPoints)
            {
                if (!cp.Refresh())
                {
                    remove.Add(cp);
                }
            }

            foreach (ControlPoint cp in remove)
            {
                cp.Dispose();
            }
        }

        /// <summary>
        /// Update a control point's component data, if it exists
        /// </summary>
        public void Update(CPDefinition cpd)
        {
            if (!Has(cpd)) return;

            ControlPoint cp = Get(cpd);
            cp.RefreshComponent();
        }

        public bool Has(ControlPoint cp)
        {
            return m_ControlPoints.Contains(cp);
        }

        public bool Has(Entity e)
        {
            return m_ControlPoints.Any(cp => cp.m_Entity == e);
        }

        public bool Has(Entity seg, short curveKey)
        {
            return m_ControlPoints.Any(cp => cp.m_Segment == seg && cp.m_CurveKey == curveKey);
        }

        public bool Has(CPDefinition cpd)
        {
            return m_ControlPoints.Any(cp => cp.m_Segment == cpd.m_Segment && cp.m_CurveKey == cpd.m_CurveKey);
        }

        /// <summary>
        /// Get the Moveable object from an existing control point entity
        /// </summary>
        /// <param name="e">The existing control point entity</param>
        /// <returns>The Control Point</returns>
        public ControlPoint GetOrCreate(Entity e)
        {
            if (Has(e))
            {
                return Get(e);
            }
            return new(e);
        }

        /// <summary>
        /// Get the Moveable object from a ControlPointDefinition struct
        /// </summary>
        /// <param name="cpd">The ControlPointDefinition struct</param>
        /// <returns>The Control Point</returns>
        public ControlPoint GetOrCreate(CPDefinition cpd)
        {
            if (Has(cpd))
            {
                return Get(cpd);
            }
            return Factory(cpd);
        }

        /// <summary>
        /// Get the Moveable object from a ControlPointDefinition struct if it already exists
        /// </summary>
        /// <param name="cpd">The ControlPointDefinition to look for</param>
        /// <param name="cp">The Moveable if it exists</param>
        /// <returns>Does this Control Point exist?</returns>
        public bool TryGet(CPDefinition cpd, out ControlPoint cp)
        {
            if (!Has(cpd.m_Segment, cpd.m_CurveKey))
            {
                cp = null;
                return false;
            }

            cp = m_ControlPoints.First(cp => cp.m_Segment == cpd.m_Segment && cp.m_CurveKey == cpd.m_CurveKey);
            return true;
        }

        public ControlPoint Get(Entity e)
        {
            if (!Has(e)) throw new Exception($"Attempted to get ControlPoint {e.D()}, but doesn't exist");

            return m_ControlPoints.First(cp => cp.m_Entity == e);
        }

        public ControlPoint Get(CPDefinition cpd)
        {
            if (!Has(cpd)) throw new Exception($"Attempted to get ControlPoint {cpd}, but doesn't exist");

            return m_ControlPoints.First(cp => cp.m_Segment == cpd.m_Segment && cp.m_CurveKey == cpd.m_CurveKey);
        }

        public MIT_ControlPoint GetData(int index)
        {
            return GetData(m_ControlPoints[index]);
        }

        public MIT_ControlPoint GetData(Entity e)
        {
            return GetData(Get(e));
        }

        public MIT_ControlPoint GetData(ControlPoint mv)
        {
            try
            {
                return new(mv.m_Entity, mv.m_Segment, mv.m_Node, mv.Transform.m_Position, mv.m_Diameter, mv.m_CurveKey);
            }
            catch (Exception e)
            {
                MIT.Log.Warning(e);
            }
            return new();
        }

        #region Enumeration
        public IEnumerator<MIT_ControlPoint> GetEnumerator() => new Enumeration(this);
        IEnumerator IEnumerable.GetEnumerator() => new Enumeration(this);
        private class Enumeration : IEnumerator<MIT_ControlPoint>
        {
            private int _Position = -1;
            private readonly ControlPointManager _ControlPointManager;

            public Enumeration(ControlPointManager cpm)
            {
                _ControlPointManager = cpm;
            }

            public MIT_ControlPoint Current => _ControlPointManager.GetData(_Position);

            object IEnumerator.Current => Current;

            public void Dispose()
            { }

            public bool MoveNext()
            {
                _Position++;
                return _Position < _ControlPointManager.m_ControlPoints.Count;
            }

            public void Reset()
            {
                _Position = -1;
            }
        }
        #endregion

        internal string DebugControlPoints()
        {
            StringBuilder sb = new();
            sb.AppendFormat("CPs:{0}", m_ControlPoints.Count);
            foreach (ControlPoint cp in m_ControlPoints)
            {
                sb.AppendFormat(" {0}", cp.m_Entity.D());
            }
            return sb.ToString();
        }

        internal void DebugDumpControlPoints()
        {
            QLog.Bundle("CPM", DebugControlPoints());
        }
    }
}
