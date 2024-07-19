using Colossal.Mathematics;
using MoveIt.Components;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Managers
{
    public class ControlPointManager
    {
        protected static readonly MIT _Tool = MIT.m_Instance;

        private EntityArchetype _ControlPointArchetype;

        internal int Count => _Tool.Moveables.CountOf<MVControlPoint>();
        internal bool Any => Count > 0;

        public ControlPointManager()
        {
            _ControlPointArchetype = _Tool.EntityManager.CreateArchetype(new ComponentType[] {
                ComponentType.ReadWrite<Game.Prefabs.PrefabRef>(),
                ComponentType.ReadWrite<Game.Rendering.CullingInfo>(),
                ComponentType.ReadWrite<MIT_ControlPoint>()
            });
        }

        private Entity CreateEntity(MVDefinition mvd)
        {
            if (_Tool.Moveables.Any<MVControlPoint>(cp => mvd.Equals(cp) && DoesMVControlPointHaveEntity(cp)))
            {
                throw new Exception($"Trying to create ControlPoint entity but it already exists ({mvd})");
            }

            Entity e = _Tool.EntityManager.CreateEntity(_ControlPointArchetype);

            Bezier4x3 curve = _Tool.EntityManager.GetComponentData<Game.Net.Curve>(mvd.m_Parent).m_Bezier;
            float3 position = curve.Get(mvd.m_ParentKey);

            Game.Net.Edge edge = _Tool.EntityManager.GetComponentData<Game.Net.Edge>(mvd.m_Parent);
            MIT_ControlPoint cpData = new(e, mvd.m_Parent, mvd.m_ParentKey, (mvd.m_ParentKey.IsNodeA() ? edge.m_Start : edge.m_End), position, Overlays.Overlay.CP_RADIUS * 2 /*math.max(segmentWidth / 4,  2f)*/, mvd.m_IsManipulatable);
            Game.Prefabs.PrefabRef prefabRef = new(Entity.Null);
            Game.Rendering.CullingInfo culling = new()
            {
                m_Bounds = new(cpData.m_Position - cpData.m_Diameter / 2, cpData.m_Position + cpData.m_Diameter / 2),
                m_Radius = cpData.m_Diameter / 2
            };

            _Tool.EntityManager.SetComponentData(e, cpData);
            _Tool.EntityManager.SetComponentData(e, prefabRef);
            _Tool.EntityManager.SetComponentData(e, culling);

            return e;
        }

        private static bool DoesMVControlPointHaveEntity(MVControlPoint cp)
        {
            if (cp.m_Entity.Equals(Entity.Null)) MIT.Log.Debug($"CP {cp} has null entity");
            if (!_Tool.EntityManager.Exists(cp.m_Entity)) MIT.Log.Debug($"CP {cp} has nonexistent entity");
            if (cp.m_Entity.Equals(Entity.Null)) return false;
            if (!_Tool.EntityManager.Exists(cp.m_Entity)) return false;
            return true;
        }

        /// <summary>
        /// Refresh CP map when the tool is activated, to ensure the Moveables exist and are up to date
        /// </summary>
        public void Refresh()
        { }

        /// <summary>
        /// Update a control point's component data, if it exists
        /// </summary>
        public void UpdateIfExists(MVDefinition mvd)
        {
            if (!HasMoveable(mvd)) return;

            MVControlPoint cp = Get(mvd);
            cp.RefreshComponent();
        }

        public bool HasMoveable(MVDefinition mvd)
        {
            return _Tool.Moveables.Any<MVControlPoint>(cp => mvd.Equals(cp));
        }

        /// <summary>
        /// Get the MVControlPoint object from a MVDefinition struct
        /// </summary>
        /// <param name="mvd">The MVDefinition struct</param>
        /// <returns>The Control Point</returns>
        public MVControlPoint GetOrCreate(MVDefinition mvd)
        {
            if (HasMoveable(mvd)) return Get(mvd);

            Entity e = CreateEntity(mvd);
            mvd = new(Identity.ControlPoint, e, mvd.m_IsManipulatable, true, mvd.m_Parent, mvd.m_ParentKey);
            MVControlPoint cp = _Tool.Moveables.Factory(mvd) as MVControlPoint;
            return cp;
        }

        /// <summary>
        /// Get the Moveable object from a MVDefinition struct only if it exists
        /// </summary>
        /// <param name="mvd">The MVDefinition struct</param>
        /// <returns>The Control Point</returns>
        public bool GetIfExists(MVDefinition mvd, out MVControlPoint cp)
        {
            cp = null;
            if (HasMoveable(mvd))
            {
                cp = Get(mvd);
                return true;
            }
            return false;
        }

        internal Entity RecreateEntity(MVDefinition mvd)
        {
            return CreateEntity(mvd);
        }

        public MVControlPoint Get(MVDefinition mvd)
        {
            if (!HasMoveable(mvd)) throw new Exception($"Attempted to get ControlPoint {mvd}, but doesn't exist");

            return _Tool.Moveables.First<MVControlPoint>(cp => mvd.Equals(cp));
        }

        public MIT_ControlPoint GetData(MVControlPoint mv)
        {
            try
            {
                return new(mv.m_Entity, mv.m_Parent, mv.m_ParentKey, mv.m_Node, mv.Transform.m_Position, mv.m_Diameter, mv.IsManipulatable);
            }
            catch (Exception e)
            {
                MIT.Log.Warning(e);
            }
            return new();
        }

        public HashSet<MIT_ControlPoint> GetAllData(bool isManipulating)
        {
            HashSet<MIT_ControlPoint> result = new();
            foreach (var cp in _Tool.Moveables.GetAllOf<MVControlPoint>())
            {
                if (cp.IsManipulatable == isManipulating)
                {
                    result.Add(GetData(cp));
                }
            }
            return result;
        }

        public bool IsInUse(MVControlPoint cp)
        {
            MVDefinition mvd = cp.Definition;
            if (_Tool.Hover.Is(mvd))        return true;
            if (_Tool.Selection.Has(mvd))   return true;
            if (_Tool.Selection.Has(cp.NodeDefinition))     return true;
            if (_Tool.Selection.Has(cp.SegmentDefinition))  return true;
            return false;
        }

        public void RemoveIfUnused(List<MVDefinition> mvds)
        {
            //string msg = $"CPM.RemoveIfUnused {mvds.Count}";
            foreach (var mvd in mvds)
            {
                //msg += $"\n    {mvd}";
                if (GetIfExists(mvd, out var cp))
                {
                    //msg += $"-E";
                    if (cp.IsValid && IsInUse(cp)) continue;
                    //msg += $"X";

                    _Tool.Moveables.RemoveDo(cp);
                }
            }
            //QLog.Debug(msg);
        }

        public void DestroyAll()
        {
            int oldCount = _Tool.m_ControlPointQuery.CalculateEntityCount();
            _Tool.EntityManager.DestroyEntity(_Tool.m_ControlPointQuery);
            MIT.Log.Info($"Removing {oldCount} entities (new count:{_Tool.m_ControlPointQuery.CalculateEntityCount()})");
        }


        internal string DebugControlPoints()
        {
            StringBuilder sb = new();
            sb.AppendFormat("CPs:{0}", Count);
            foreach (MVControlPoint cp in _Tool.Moveables.GetAllOf<MVControlPoint>())
            {
                sb.AppendFormat("\n    {0}", cp.Definition);
            }
            return sb.ToString();
        }

        internal void DebugDumpControlPoints(string prefix = "")
        {
            QLog.Bundle("CPM", prefix + DebugControlPoints());
        }
    }
}
