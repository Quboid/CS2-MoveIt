using Colossal.Mathematics;
using MoveIt.Moveables;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    internal class OverlayManipSegment : Overlay
    {
        private static EntityArchetype _Archetype = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Bezier),
                    typeof(MIO_Common),
                    typeof(MIO_Lines),
            });

        public static Entity Factory(MVManipSegment mseg)
        {
            Entity e = _Tool.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new()
            {
                m_Owner             = mseg.m_Entity,
                m_IsManipulatable   = mseg.IsManipulatable,
                m_IsManipChild      = mseg.IsChild,
            };

            MIO_Bezier Bezier = new()
            {
                Curve = mseg.Curve,
            };

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.MVManipSegment));
            _Tool.EntityManager.SetComponentData(e, common);
            _Tool.EntityManager.SetComponentData(e, Bezier);
            return e;
        }

        public OverlayManipSegment() : base(OverlayTypes.MVManipSegment)
        { }

        public override bool CreateOverlayEntity()
        {
            if (m_Moveable is not MVManipSegment mseg) return false;
            if (!base.CreateOverlayEntity()) return false;

            m_Entity = OverlayManipSegment.Factory(mseg);
            EnqueueUpdate();

            foreach (var cpd in mseg.m_CPDefinitions)
            {
                MVManipControlPoint cp = _Tool.ControlPointManager.GetOrCreate(cpd) as MVManipControlPoint;
                ((OverlayManipControlPoint)cp.m_Overlay).CreateOverlayEntityIfNoneExists();
            }

            return true;
        }

        public override bool Update()
        {
            if (m_Moveable is not MVManipSegment seg) return false;
            if (m_Entity.Equals(Entity.Null)) return false;
            
            MIO_Common common = _Tool.EntityManager.GetComponentData<MIO_Common>(m_Entity);
            UpdateCommon(ref common);
            _Tool.EntityManager.SetComponentData(m_Entity, common);

            MIO_Bezier curve = _Tool.EntityManager.GetComponentData<MIO_Bezier>(m_Entity);
            curve.Curve = seg.Curve;
            _Tool.EntityManager.SetComponentData(m_Entity, curve);

            DynamicBuffer<MIO_Lines> linesBuffer = _Tool.EntityManager.GetBuffer<MIO_Lines>(m_Entity);
            linesBuffer.Clear();

            NativeArray<Circle3> cpPos = new(4, Allocator.Temp);
            cpPos[0] = new(CP_RADIUS, curve.Curve.a, quaternion.identity);
            cpPos[1] = new(CP_RADIUS, curve.Curve.b, quaternion.identity);
            cpPos[2] = new(CP_RADIUS, curve.Curve.c, quaternion.identity);
            cpPos[3] = new(CP_RADIUS, curve.Curve.d, quaternion.identity);

            linesBuffer.Add(new(DrawTools.CalculateProtrudedLine(cpPos[0], cpPos[1])));
            linesBuffer.Add(new(DrawTools.CalculateProtrudedLine(cpPos[3], cpPos[2])));
            return true;
        }
    }
}
