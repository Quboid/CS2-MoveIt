﻿using Colossal.Mathematics;
using MoveIt.Components;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Managers
{
    public class ControlPointManager
    {
        protected static readonly MIT _MIT = MIT.m_Instance;

        private static EntityArchetype _ControlPointArchetype;

        internal static int Count => _MIT.Moveables.CountOf<MVControlPoint>();
        internal bool Any => Count > 0;

        public ControlPointManager()
        {
            _ControlPointArchetype = _MIT.EntityManager.CreateArchetype(new ComponentType[] {
                ComponentType.ReadWrite<Game.Prefabs.PrefabRef>(),
                ComponentType.ReadWrite<Game.Rendering.CullingInfo>(),
                ComponentType.ReadWrite<MIT_ControlPoint>()
            });
        }

        internal static Entity CreateEntity(MVDefinition mvd)
        {
            if (_MIT.Moveables.Any<MVControlPoint>(cp => mvd.Equals(cp) && DoesMVControlPointHaveEntity(cp)))
            {
                throw new Exception($"Trying to create ControlPoint entity but it already exists ({mvd})");
            }
            if (!mvd.m_Parent.Exists(_MIT.EntityManager))
            {
                throw new Exception($"Trying to create ControlPoint entity but parent doesn't exist ({mvd})");
            }
            if (mvd.m_ParentKey < 0 || mvd.m_ParentKey > 3)
            {
                throw new Exception($"Trying to create ControlPoint entity but parent key is invalid ({mvd})");
            }

            Entity e = _MIT.EntityManager.CreateEntity(_ControlPointArchetype);

            Bezier4x3 curve = _MIT.EntityManager.GetComponentData<Game.Net.Curve>(mvd.m_Parent).m_Bezier;
            float3 position = curve.Get(mvd.m_ParentKey);

            Game.Net.Edge edge = _MIT.EntityManager.GetComponentData<Game.Net.Edge>(mvd.m_Parent);
            MIT_ControlPoint cpData = new(e, mvd.m_Parent, mvd.m_ParentKey, (mvd.m_ParentKey.IsNodeA() ? edge.m_Start : edge.m_End), position, Overlays.Overlay.CP_RADIUS * 2 /*math.max(segmentWidth / 4,  2f)*/, mvd.m_IsManipulatable);
            Game.Prefabs.PrefabRef prefabRef = new(Entity.Null);
            Game.Rendering.CullingInfo culling = new()
            {
                m_Bounds = new(cpData.m_Position - cpData.m_Diameter / 2, cpData.m_Position + cpData.m_Diameter / 2),
                m_Radius = cpData.m_Diameter / 2,
            };

            _MIT.EntityManager.SetComponentData(e, cpData);
            _MIT.EntityManager.SetComponentData(e, prefabRef);
            _MIT.EntityManager.SetComponentData(e, culling);

            //QLog.Debug($"CPM.CreateEntity2 {e.D()} ({mvd})");

            return e;
        }

        private static bool DoesMVControlPointHaveEntity(MVControlPoint cp)
            => cp.m_Entity.Exists(_MIT.EntityManager);

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
            cp.UpdateComponent();
        }

        public static void UpdateAllExisting()
        {
            foreach (MVControlPoint cp in _MIT.Moveables.GetAllOf<MVControlPoint>())
            {
                cp.UpdateComponent();
            }
        }

        public static bool HasMoveable(MVDefinition mvd)
        {
            return _MIT.Moveables.Any<MVControlPoint>(mvd.Equals);
        }

        /// <summary>
        /// Get the MVControlPoint object from a MVDefinition struct
        /// </summary>
        /// <param name="mvd">The MVDefinition struct</param>
        /// <returns>The Control Point moveable</returns>
        public MVControlPoint GetOrCreateMoveable(MVDefinition mvd)
        {
            QLog.Debug($"CPM.GetOrCreMV {mvd} exists:{HasMoveable(mvd)} {QCommon.GetCallerDebug()}");
            if (HasMoveable(mvd)) return Get(mvd);

            mvd = new(Identity.ControlPoint, Entity.Null, mvd.m_IsManipulatable, true, mvd.m_Parent, mvd.m_ParentId, mvd.m_ParentKey);
            var cp = _MIT.Moveables.Factory(mvd, Identity.ControlPoint) as MVControlPoint;
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

        public MVControlPoint Get(MVDefinition mvd)
        {
            if (!HasMoveable(mvd)) throw new Exception($"Attempted to get ControlPoint {mvd}, but doesn't exist");

            return _MIT.Moveables.First<MVControlPoint>(cp => mvd.Equals(cp));
        }

        public static MIT_ControlPoint GetData(MVControlPoint mv)
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
            foreach (MVControlPoint cp in _MIT.Moveables.GetAllOf<MVControlPoint>().Where(cp => cp.IsManipulatable == isManipulating))
            {
                result.Add(GetData(cp));
            }
            return result;
        }

        public static bool IsInUse(MVControlPoint cp)
        {
            MVDefinition mvd = cp.Definition;
            if (_MIT.Hover.Is(mvd))        return true;
            if (_MIT.Selection.Has(mvd))   return true;
            if (_MIT.Selection.Has(cp.NodeDefinition))     return true;
            if (_MIT.Selection.Has(cp.ParentDefinition))   return true;
            return false;
        }

        public void RemoveIfUnused(List<MVDefinition> mvds)
        {
            //string msg = $"CPM.RemoveIfUnused {mvds.Count}";
            foreach (MVDefinition mvd in mvds)
            {
                //msg += $"\n    {mvd}";
                if (!GetIfExists(mvd, out MVControlPoint cp)) continue;
                //msg += $"-E";
                if (cp.IsValid && IsInUse(cp)) continue;
                //msg += $"X";

                _MIT.Moveables.RemoveDo(cp);
            }
            //QLog.Debug(msg);
        }

        public void DestroyAll()
        {
            int oldCount = _MIT.m_ControlPointQuery.CalculateEntityCount();
            _MIT.EntityManager.DestroyEntity(_MIT.m_ControlPointQuery);
            MIT.Log.Info($"Removing {oldCount} entities (new count:{_MIT.m_ControlPointQuery.CalculateEntityCount()})");
        }


        internal static string DebugControlPoints()
        {
            StringBuilder sb = new();
            sb.AppendFormat("CPs:{0}", Count);
            foreach (MVControlPoint cp in _MIT.Moveables.GetAllOf<MVControlPoint>())
            {
                sb.AppendFormat("\n    {0}", cp.Definition);
            }
            return sb.ToString();
        }

        internal void DebugDumpControlPoints(string prefix = "")
        {
            MIT.Log.Bundle("CPM", prefix + DebugControlPoints());
        }
    }
}
