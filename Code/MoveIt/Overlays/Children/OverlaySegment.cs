using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays.Children
{
    internal class OverlaySegment : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Common),
                    typeof(MIO_Bezier),
                    typeof(MIO_Beziers),
                    typeof(MIO_Circles),
                    typeof(MIO_DashedLines),
            });


        public OverlaySegment(Moveable mv) : base(OverlayTypes.MVSegment, mv)
        { }

        protected override bool CreateOverlayEntity()
        {
            if (_Moveable is not MVSegment seg)
            {
                MIT.Log.Error($"ERROR OlaySeg.CreateOlayE {_Moveable} is not segment.");
                return false;
            }

            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new(true)
            {
                m_Owner = seg.m_Entity,
            };

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.MVSegment));
            _MIT.EntityManager.SetComponentData(m_Entity, common);
            _MIT.EntityManager.SetComponentData<MIO_Bezier>(m_Entity, new(seg.Curve, seg.Width));

            return true;
        }

        protected override bool DestroyOverlayEntity()
        {
            UpdateRelatedNodes();
            return base.DestroyOverlayEntity();
        }

        public override void EnqueueUpdate()
        {
#if IS_DEBUG
            m_Caller = QCommon.GetStackTrace(15);
#endif
            base.EnqueueUpdate();
            base.EnqueueUpdateDeferred();
            UpdateRelatedNodes();
            UpdateRelatedCPs();
        }

        private void UpdateRelatedNodes()
        {
            // If the segment has been deleted, return now
            if (!GetMoveable<MVSegment>().IsValid)
            {
                MIT.Log.Debug($"UpdateRelatedNodes called for segment {E()} that isn't valid {QCommon.GetCallerDebug()}.");
                return;
            }

            Game.Net.Edge edge = GetMoveable<MVSegment>().Edge;
            if (_MIT.Moveables.TryGet<MVNode>(new MVDefinition(Identity.Node, edge.m_Start, _MIT.IsManipulating), out var nodeA))
            {
                _MIT.QueueOverlayUpdate(nodeA.m_Overlay);
            }
            if (_MIT.Moveables.TryGet<MVNode>(new MVDefinition(Identity.Node, edge.m_End, _MIT.IsManipulating), out var nodeB))
            {
                _MIT.QueueOverlayUpdate(nodeB.m_Overlay);
            }
        }

        private void UpdateRelatedCPs()
        {
            // If the segment has been deleted, return now
            if (!GetMoveable<MVSegment>().IsValid)
            {
                MIT.Log.Error($"UpdateRelatedCPs called for segment {E()} that isn't valid.");
                return;
            }

            foreach (MVDefinition mvd in GetMoveable<MVSegment>().m_CPDefinitions)
            {
                // It won't count as existing if the selection is being cleared and a selected node is cleaned up first
                if (_MIT.ControlPointManager.GetIfExists(mvd, out var cp))
                {
                    cp.m_Overlay.EnqueueUpdate();
                }
            }
        }

        public override bool Update()
        {
            if (_Moveable is not MVSegment seg) return false;
            if (m_Entity.Equals(Entity.Null)) return false;

            MIO_Common common = _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);
            UpdateCommon(ref common);
            _MIT.EntityManager.SetComponentData(m_Entity, common);

            MIO_Bezier curve = _MIT.EntityManager.GetComponentData<MIO_Bezier>(m_Entity);
            curve.Curve = seg.Curve;
            _MIT.EntityManager.SetComponentData(m_Entity, curve);

            DynamicBuffer<MIO_Circles> cpPosBuffer = _MIT.EntityManager.GetBuffer<MIO_Circles>(m_Entity);
            DynamicBuffer<MIO_Beziers> curvesBuffer = _MIT.EntityManager.GetBuffer<MIO_Beziers>(m_Entity);
            DynamicBuffer<MIO_DashedLines> dashedBuffer = _MIT.EntityManager.GetBuffer<MIO_DashedLines>(m_Entity);
            cpPosBuffer.Clear();
            curvesBuffer.Clear();
            dashedBuffer.Clear();

            // Control points and guidelines
            NativeArray<Circle3> cpPos = new(4, Allocator.Temp);
            cpPos[0] = new(CP_RADIUS, curve.Curve.a, quaternion.identity);
            cpPos[1] = new(CP_RADIUS, curve.Curve.b, quaternion.identity);
            cpPos[2] = new(CP_RADIUS, curve.Curve.c, quaternion.identity);
            cpPos[3] = new(CP_RADIUS, curve.Curve.d, quaternion.identity);

            for (var i = 0; i < 4; i++)
            {
                cpPosBuffer.Add(new(cpPos[i]));
            }

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
