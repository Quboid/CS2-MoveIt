using Colossal.Mathematics;
using MoveIt.Moveables;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    internal class OverlaySegment : Overlay
    {
        private static EntityArchetype _Archetype = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Common),
                    typeof(MIO_Bezier),
                    typeof(MIO_Beziers),
                    typeof(MIO_Circles),
                    typeof(MIO_DashedLines),
            });

        public static Entity Factory(Entity owner, Bezier4x3 curve, float width)
        {
            Entity e = _Tool.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new()
            {
                m_Owner = owner,
            };

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.MVSegment));
            _Tool.EntityManager.SetComponentData(e, common);
            _Tool.EntityManager.SetComponentData<MIO_Bezier>(e, new(curve, width));
            return e;
        }

        public OverlaySegment() : base(OverlayTypes.MVSegment)
        { }

        public override bool CreateOverlayEntity()
        {
            if (m_Moveable is not MVSegment seg) return false;
            if (!base.CreateOverlayEntity()) return false;

            m_Entity = OverlaySegment.Factory(m_Moveable.m_Entity, seg.Curve, seg.Width);
            EnqueueUpdate();

            foreach (var cpd in seg.m_CPDefinitions)
            {
                MVControlPoint cp = _Tool.ControlPointManager.GetOrCreate(cpd);
                ((OverlayControlPoint)cp.m_Overlay).CreateOverlayEntityIfNoneExists();
            }

            return true;
        }

        public override bool DestroyOverlayEntity()
        {
            UpdateRelatedOverlays();
            return base.DestroyOverlayEntity();
        }

        public override void EnqueueUpdate()
        {
            base.EnqueueUpdate();
            base.EnqueueUpdateDeferred();
            UpdateRelatedOverlays();
        }

        private void UpdateRelatedOverlays()
        {
            // If the segment has been deleted, return now
            if (!GetMoveable<MVSegment>().IsValid) return;

            Game.Net.Edge edge = GetMoveable<MVSegment>().Edge;
            if (_Tool.Moveables.TryGet<MVNode>(new MVDefinition(Identity.Node, edge.m_Start, _Tool.IsManipulating), out var nodeA))
            {
                _Tool.QueueOverlayUpdate(nodeA.m_Overlay);
            }
            if (_Tool.Moveables.TryGet<MVNode>(new MVDefinition(Identity.Node, edge.m_End, _Tool.IsManipulating), out var nodeB))
            {
                _Tool.QueueOverlayUpdate(nodeB.m_Overlay);
            }

            foreach (var mvd in GetMoveable<MVSegment>().m_CPDefinitions)
            {
                // It won't count as existing if the selection is being cleared and a selected node is cleaned up first
                if (_Tool.ControlPointManager.GetIfExists(mvd, out var cp))
                {
                    cp.m_Overlay.EnqueueUpdate();
                }
            }
        }

        public override bool Update()
        {
            if (m_Moveable is not MVSegment seg) return false;
            if (m_Entity.Equals(Entity.Null)) return false;

            MIO_Common common = _Tool.EntityManager.GetComponentData<MIO_Common>(m_Entity);
            UpdateCommon(ref common);
            _Tool.EntityManager.SetComponentData(m_Entity, common);

            MIO_Bezier curve = _Tool.EntityManager.GetComponentData<MIO_Bezier>(m_Entity);
            curve.Curve = seg.Curve;
            _Tool.EntityManager.SetComponentData(m_Entity, curve);

            DynamicBuffer<MIO_Circles> cpPosBuffer = _Tool.EntityManager.GetBuffer<MIO_Circles>(m_Entity);
            DynamicBuffer<MIO_Beziers> curvesBuffer = _Tool.EntityManager.GetBuffer<MIO_Beziers>(m_Entity);
            DynamicBuffer<MIO_DashedLines> dashedBuffer = _Tool.EntityManager.GetBuffer<MIO_DashedLines>(m_Entity);
            cpPosBuffer.Clear();
            curvesBuffer.Clear();
            dashedBuffer.Clear();

            // Control points and guidelines
            NativeArray<Circle3> cpPos = new(4, Allocator.Temp);
            cpPos[0] = new(CP_RADIUS, curve.Curve.a, quaternion.identity);
            cpPos[1] = new(CP_RADIUS, curve.Curve.b, quaternion.identity);
            cpPos[2] = new(CP_RADIUS, curve.Curve.c, quaternion.identity);
            cpPos[3] = new(CP_RADIUS, curve.Curve.d, quaternion.identity);

            for (int i = 0; i < 4; i++)
            {
                cpPosBuffer.Add(new(cpPos[i]));
            }

            dashedBuffer.Add(new(DrawTools.CalculateProtrudedLine(cpPos[0], cpPos[1])));
            dashedBuffer.Add(new(DrawTools.CalculateProtrudedLine(cpPos[3], cpPos[2])));

            // Outline
            var edgeGeo = _Tool.EntityManager.GetComponentData<Game.Net.EdgeGeometry>(m_Owner);
            float3 offset = new(0f);
            if (_Tool.EntityManager.HasComponent<Game.Net.Elevation>(m_Owner))
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
