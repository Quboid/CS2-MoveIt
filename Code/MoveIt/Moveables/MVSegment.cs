using Colossal.Mathematics;
using MoveIt.Actions.Transform;
using MoveIt.Overlays.Children;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Moveables
{
    public class MVSegment : Moveable
    {
        public override bool IsManipulatable => false;

        internal static float GetDefaultWidth(Entity e)
        {
            if (!_MIT.EntityManager.Exists(e))
            {
                MIT.Log.Error($"Segment.GetDefaultWidth - entity {e.D()} doesn't exist!\n{QCommon.GetStackTrace()}");
                return 0f;
            }
            Game.Prefabs.PrefabRef segPrefab = _MIT.EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(e);
            Game.Prefabs.NetGeometryData geoData = _MIT.EntityManager.GetComponentData<Game.Prefabs.NetGeometryData>(segPrefab);
            return geoData.m_DefaultWidth;
        }

        internal List<MVDefinition> m_CPDefinitions;

        internal float Width        => GetDefaultWidth(m_Entity);
        internal Bezier4x3 Curve    => _MIT.EntityManager.GetComponentData<Game.Net.Curve>(m_Entity).m_Bezier;
        internal Game.Net.Edge Edge => _MIT.EntityManager.GetComponentData<Game.Net.Edge>(m_Entity);

        public override Game.Objects.Transform Transform => new()
            {
                m_Position = Curve.Position(),
                m_Rotation = new quaternion(),
            };

        public MVSegment(Entity e) : base(e, Identity.Segment)
        {
            m_CPDefinitions = new();
            for (short i = 0; i < CURVE_CPS; i++)
            {
                MVDefinition mvd = new(Identity.ControlPoint, Entity.Null, IsManipulatable, true, m_Entity, m_Identity, i);
                MVControlPoint cp = _MIT.ControlPointManager.GetOrCreateMoveable(mvd);
                m_CPDefinitions.Add(cp.Definition);
            }

            m_Overlay = new OverlaySegment(this);

            RefreshFromAbstract();
        }

        protected MVSegment(Entity e, Identity identity) : base(e, identity)
        { } // Pass-thru for children


        internal override bool Refresh()
        {
            if (!IsValid) return false;
            if (!IsOverlayValid) return false;

            //QLog.Debug($"Olay-EnqueueUpdate {GetType().Name}.{System.Reflection.MethodBase.GetCurrentMethod().Name} {E()} caller:{QCommon.GetCallingMethodName()}");
            m_Overlay.EnqueueUpdate();
            return true;
        }

        internal override List<MVDefinition> GetAllChildren()
            => m_CPDefinitions;

        internal override List<MVDefinition> GetChildrenToTransform()
            => new() { m_CPDefinitions[1], m_CPDefinitions[2] };

        internal override List<T> GetChildMoveablesForOverlays<T>() 
        {
            List<T> result = new();
            m_CPDefinitions.ForEach(cpd => result.Add(_MIT.ControlPointManager.GetOrCreateMoveable(cpd) as T));
            return result;
        }

        internal override void MoveIt(TransformBase action, State state, bool move, bool rotate)
        {
            if (!move && !rotate) return;

            Bezier4x3 originalCurve = state.m_InitialCurve;
            float3 oldPos = originalCurve.Position();
            float3 newPos = state.m_Position;

            MVControlPoint cpB = _MIT.ControlPointManager.Get(m_CPDefinitions[1]);
            MVControlPoint cpC = _MIT.ControlPointManager.Get(m_CPDefinitions[2]);
            State newB = new(_MIT.EntityManager, ref QLookupFactory.Get(), cpB, state.m_MoveDelta, state.m_AngleDelta, state.m_InitialCenter);
            State newC = new(_MIT.EntityManager, ref QLookupFactory.Get(), cpC, state.m_MoveDelta, state.m_AngleDelta, state.m_InitialCenter);

            if ((action.m_Snapper.m_SnapType & Snapper.SnapTypes.StraightSegment) > 0)
            {
                newB.m_Position = math.lerp(originalCurve.a, originalCurve.d, 1 / 3f);
                newC.m_Position = math.lerp(originalCurve.d, originalCurve.a, 1 / 3f);
            }
            else
            {
                float3 O_N_Offset = newPos - oldPos;
                newB.m_Position = originalCurve.b + O_N_Offset;
                newC.m_Position = originalCurve.c + O_N_Offset;
            }

            cpB.MoveIt(action, newB, move, rotate);
            cpC.MoveIt(action, newC, move, rotate);

            // Should read new curve and save that state instead?
            state.Transform(move, rotate);
        }

        protected List<MVControlPoint> InitialiseOverlayCircles()
        {
            if (m_CPDefinitions.Count < 4)
            {
                return new();
            }
            List<MVControlPoint> cps = new();

            m_CPDefinitions.ForEach(cpd => cps.Add(_MIT.ControlPointManager.GetOrCreateMoveable(cpd)));

            return cps;
        }

        internal float GetAngleRelative(float3 pos)
        {
            Bezier4x3 curve = Curve;
            float2 mag = (curve.d - curve.a).XZ();
            float angle = math.atan2(mag.x, mag.y) * Mathf.Rad2Deg + 90f;
            //float angleRaw = angle;
            bool isLeft = ((curve.d.x - curve.a.x) * (pos.z - curve.a.z) - (pos.x - curve.a.x) * (curve.d.z - curve.a.z)) > 0;
            if (!isLeft) angle += 180;
            angle %= 360;

            //QLog.XDebug($"Segment {m_Entity.DX()} angle:{angle} ({angleRaw}) isLeft:{isLeft}");

            return angle;
        }

        public override void Dispose()
        {
            _MIT.ControlPointManager.RemoveIfUnused(m_CPDefinitions);
            base.Dispose();
        }
    }
}
