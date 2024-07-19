using Colossal.Mathematics;
using MoveIt.Actions;
using MoveIt.Overlays;
using MoveIt.QAccessor;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Moveables
{
    public class MVSegment : Moveable
    {
        public override bool IsManipulatable => false;

        internal static float GetDefaultWidth(Entity e)
        {
            if (!_Tool.EntityManager.Exists(e))
            {
                MIT.Log.Error($"Segment.GetDefaultWidth - entity {e.D()} doesn't exist!\n{QCommon.GetStackTrace()}");
                return 0f;
            }
            Game.Prefabs.PrefabRef segPrefab = _Tool.EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(e);
            Game.Prefabs.NetGeometryData geoData = _Tool.EntityManager.GetComponentData<Game.Prefabs.NetGeometryData>(segPrefab);
            return geoData.m_DefaultWidth;
        }

        internal List<MVDefinition> m_CPDefinitions;

        internal float Width        => GetDefaultWidth(m_Entity);
        internal Bezier4x3 Curve    => _Tool.EntityManager.GetComponentData<Game.Net.Curve>(m_Entity).m_Bezier;
        internal Game.Net.Edge Edge => _Tool.EntityManager.GetComponentData<Game.Net.Edge>(m_Entity);

        public override Game.Objects.Transform Transform => new()
            {
                m_Position = Curve.Position(),
                m_Rotation = new quaternion(),
            };

        public MVSegment(Entity e) : base(e, Identity.Segment, ObjectType.Normal)
        {
            m_Overlay = Factory.Create<OverlaySegment>(this, OverlayTypes.MVSegment);
            Refresh();
        }

        public MVSegment(Entity e, Identity identity, ObjectType objectType) : base(e, identity, objectType)
        { } // Pass-thru for children


        internal override bool Refresh()
        {
            if (!IsValid) return false;
            if (!IsOverlayValid) return false;

            m_CPDefinitions = new();
            for (short i = 0; i < CURVE_CPS; i++)
            {
                MVDefinition mvd = new(Identity.ControlPoint, Entity.Null, IsManipulatable, IsManaged, m_Entity, i);
                MVControlPoint cp = _Tool.ControlPointManager.GetOrCreate(mvd);
                m_CPDefinitions.Add(cp.Definition);
            }
            m_Overlay.EnqueueUpdate();
            return true;
        }

        internal override List<MVDefinition> GetAllChildren() => m_CPDefinitions;

        internal override List<MVDefinition> GetChildrenToTransform() => new() { m_CPDefinitions[1], m_CPDefinitions[2] };

        internal override List<T> GetChildMoveablesForOverlays<T>() 
        {
            List<T> result = new();
            m_CPDefinitions.ForEach(cpd => result.Add(_Tool.ControlPointManager.GetOrCreate(cpd) as T));
            return result;
        }

        internal override void MoveIt(TransformAction action, State state, bool move, bool rotate)
        {
            if (!move && !rotate) return;

            Bezier4x3 originalCurve = state.m_InitialCurve;
            float3 oldPos = originalCurve.Position();
            float3 newPos = state.m_Position;

            MVControlPoint cpB = _Tool.ControlPointManager.Get(m_CPDefinitions[1]);
            MVControlPoint cpC = _Tool.ControlPointManager.Get(m_CPDefinitions[2]);
            State newB = new(_Tool.EntityManager, ref QLookupFactory.Get(), cpB);
            State newC = new(_Tool.EntityManager, ref QLookupFactory.Get(), cpC);
            //State newB = action.GetState(m_CPDefinitions[1]);
            //State newC = action.GetState(m_CPDefinitions[2]);

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

            //action.SetState(m_CPDefinitions[1], newB);
            //action.SetState(m_CPDefinitions[2], newC);
            cpB.MoveIt(action, newB, move, rotate);
            cpC.MoveIt(action, newC, move, rotate);

            // Should read new curve and save that state instead?
            state.Transform(move, rotate);
        }

        internal override void UpdateYOffset()
        {
            m_YOffset = 0f;
        }

        protected List<MVControlPoint> InitialiseOverlayCircles()
        {
            if (m_CPDefinitions.Count < 4)
            {
                return new();
            }
            List<MVControlPoint> cps = new();

            m_CPDefinitions.ForEach(cpd => cps.Add(_Tool.ControlPointManager.GetOrCreate(cpd)));

            return cps;
        }

        public override void Dispose()
        {
            _Tool.ControlPointManager.RemoveIfUnused(m_CPDefinitions);
            base.Dispose();
        }
    }
}
