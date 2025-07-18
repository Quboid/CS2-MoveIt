using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Tool;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays.Children
{
    internal class OverlayManipSegment : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Bezier),
                    typeof(MIO_Beziers),
                    typeof(MIO_Common),
                    typeof(MIO_DashedLines),
            });


        public OverlayManipSegment(Moveable mv) : base(OverlayTypes.MVManipSegment, mv)
        { }

        protected override bool CreateOverlayEntity()
        {
            if (_Moveable is not MVManipSegment mseg)
            {
                MIT.Log.Error($"ERROR OlayManipSeg.CreateOlayE {_Moveable} is not manip-segment.");
                return false;
            }

            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new(true)
            {
                m_Owner             = mseg.m_Entity,
                m_IsManipulatable   = mseg.IsManipulatable,
                m_IsManipChild      = mseg.IsManipChild,
            };

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.MVManipSegment));
            _MIT.EntityManager.SetComponentData(m_Entity, common);
            _MIT.EntityManager.SetComponentData<MIO_Bezier>(m_Entity, new(mseg.Curve, mseg.Width));
                
            EnqueueUpdate();

            return true;
        }

        public override bool Update()
        {
            if (_Moveable is not MVManipSegment seg) return false;
            if (m_Entity.Equals(Entity.Null)) return false;

            MIO_Common common = _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);
            UpdateCommon(ref common);
            _MIT.EntityManager.SetComponentData(m_Entity, common);

            MIO_Bezier curve = _MIT.EntityManager.GetComponentData<MIO_Bezier>(m_Entity);
            curve.Curve = seg.Curve;
            _MIT.EntityManager.SetComponentData(m_Entity, curve);

            DynamicBuffer<MIO_Beziers> curvesBuffer = _MIT.EntityManager.GetBuffer<MIO_Beziers>(m_Entity);
            DynamicBuffer<MIO_DashedLines> dashedBuffer = _MIT.EntityManager.GetBuffer<MIO_DashedLines>(m_Entity);
            curvesBuffer.Clear();
            dashedBuffer.Clear();

            // Control points and guidelines
            NativeArray<Circle3> cpPos = new(4, Allocator.Temp);
            cpPos[0] = new(CP_RADIUS, curve.Curve.a, quaternion.identity);
            cpPos[1] = new(CP_RADIUS, curve.Curve.b, quaternion.identity);
            cpPos[2] = new(CP_RADIUS, curve.Curve.c, quaternion.identity);
            cpPos[3] = new(CP_RADIUS, curve.Curve.d, quaternion.identity);

            dashedBuffer.Add(new(DrawTools.CalculateProtrudedLine(cpPos[0], cpPos[1])));
            dashedBuffer.Add(new(DrawTools.CalculateProtrudedLine(cpPos[3], cpPos[2])));

            // Outline
            var edgeGeo = _MIT.EntityManager.GetComponentData<Game.Net.EdgeGeometry>(m_Owner);
            float3 offset = new(0f);
            if (_MIT.EntityManager.HasComponent<Game.Net.Elevation>(m_Owner))
            {
                offset.y = 0.5f;
            }
            curvesBuffer.Add(new(edgeGeo.m_Start.m_Left + offset));
            curvesBuffer.Add(new(edgeGeo.m_Start.m_Right + offset));
            curvesBuffer.Add(new(edgeGeo.m_End.m_Left + offset));
            curvesBuffer.Add(new(edgeGeo.m_End.m_Right + offset));

            return true;
        }
    }
}
