using Colossal.Entities;
using Colossal.Mathematics;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays.Children
{
    public class OverlayNode : Overlay
    {
        private static readonly EntityArchetype _Archetype = _MIT.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Common),
                    typeof(MIO_Lines),
                    typeof(MIO_Circle),
                    typeof(MIO_Circles),
            });


        protected override Game.Objects.Transform Transform
        {
            get
            {
                var transform = _Moveable.Transform;
                transform.m_Position = _MIT.EntityManager.TryGetComponent<Game.Net.NodeGeometry>(m_Owner, out var nodeGeo) ?
                    nodeGeo.m_Bounds.Center() :
                    _MIT.EntityManager.GetComponentData<Game.Net.Node>(m_Owner).m_Position;

                return transform;
            }
        }

        public OverlayNode(Moveable mv) : base(OverlayTypes.MVNode, mv) { }

        protected override bool CreateOverlayEntity()
        {
            if (_Moveable is not MVNode)
            {
                MIT.Log.Error($"ERROR OlayNode.CreateOlayE {_Moveable} is not node.");
                return false;
            }

            Circle3 circle = new(_Moveable.GetRadius(), Transform.m_Position, quaternion.identity);
            m_Entity = _MIT.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new()
            {
                m_Owner = _Moveable.m_Entity,
            };

            _MIT.EntityManager.SetComponentData<MIO_Type>(m_Entity, new(OverlayTypes.MVNode));
            _MIT.EntityManager.SetComponentData(m_Entity, common);
            _MIT.EntityManager.SetComponentData<MIO_Circle>(m_Entity, new(circle));

            EnqueueUpdate();

            return true;
        }

        public override void EnqueueUpdate()
        {
#if IS_DEBUG
            m_Caller = QCommon.GetStackTrace(15);
#endif
            if (_Moveable is not MVNode node) return;
            if (m_Entity.Equals(Entity.Null)) return;

            _MIT.QueueOverlayUpdate(this);

            foreach (Entity seg in node.m_Segments.Keys)
            {
                if (_MIT.Moveables.TryGet<MVSegment>(new MVDefinition(Identity.Segment, seg, _Moveable.IsManipulatable), out var mvSeg))
                {
                    _MIT.QueueOverlayUpdate(mvSeg.m_Overlay);
                }
            }
        }

        public override bool Update()
        {
            if (_Moveable is not MVNode node) return false;
            if (!m_Entity.Exists(_MIT.EntityManager))
            {
                QLog.Debug($"OlayNode.Update {m_Entity.D()} (for {m_Owner.DX()}) doesn't exist! Queued by:\n{m_Caller}");
                return false;
            }

            MIO_Common common = _MIT.EntityManager.GetComponentData<MIO_Common>(m_Entity);
            UpdateCommon(ref common);
            _MIT.EntityManager.SetComponentData(m_Entity, common);

            MIO_Circle nodeCircle = _MIT.EntityManager.GetComponentData<MIO_Circle>(m_Entity);
            nodeCircle.Circle.position = Transform.m_Position;

            DynamicBuffer<MIO_Circles> cpPosBuffer = _MIT.EntityManager.GetBuffer<MIO_Circles>(m_Entity);
            DynamicBuffer<MIO_Lines> linesBuffer = _MIT.EntityManager.GetBuffer<MIO_Lines>(m_Entity);
            cpPosBuffer.Clear();
            linesBuffer.Clear();
            List<MIO_Circles> cpPosList = new();
            List<MIO_Lines> linesList = new();

            // Calculate all control point circles and lines
            float circleYPos = 0f;
            foreach ((Entity seg, bool isNodeA) in node.m_Segments)
            {
                Bezier4x3 curve = _MIT.EntityManager.GetComponentData<Game.Net.Curve>(seg).m_Bezier;
                circleYPos += isNodeA ? curve.a.y : curve.d.y;

                // Don't show this control point circle/line if the segment is hovered or selected in the same mode
                MVDefinition segDef = new(Identity.Segment, seg, _Moveable.IsManipulatable);
                if (_MIT.Hover.Is(segDef)) continue;
                if (_MIT.Selection.Has(segDef)) continue;

                MVDefinition cpdA = node.m_CPDefinitions.First(mvd => mvd.m_Parent.Equals(seg) && mvd.m_ParentKey.IsEnd());
                MVDefinition cpdB = node.m_CPDefinitions.First(mvd => mvd.m_Parent.Equals(seg) && mvd.m_ParentKey.IsMiddle());
                cpPosList.Add(new MIO_Circles(new(CP_RADIUS, curve.Get(cpdA.m_ParentKey), quaternion.identity)));
                cpPosList.Add(new MIO_Circles(new(CP_RADIUS, curve.Get(cpdB.m_ParentKey), quaternion.identity)));

                Line3.Segment line = isNodeA ? new(curve.a, curve.b) : new(curve.d, curve.c);
                float cutStart = QIntersect.IntersectionsBetweenLineAndCircleCut(nodeCircle.Circle.XZ(), line, true, CP_RADIUS);
                float cutEnd = QIntersect.IntersectionsBetweenLineAndCircleCut(new(CP_RADIUS, (isNodeA ? curve.b : curve.c).XZ()), line, false);
                linesList.Add(new MIO_Lines(DrawTools.CalculcateSegmentEndLine(curve, cutStart, cutEnd, isNodeA)));
            }

            // Calculate the node circle's overlay height based on the control points
            nodeCircle.Circle.position.y = node.m_Segments.Count > 0 ? circleYPos / node.m_Segments.Count : nodeCircle.Circle.position.y;
            _MIT.EntityManager.SetComponentData(m_Entity, nodeCircle);

            // Remove circles hidden by node circle and lines where both circles are hidden
            float2 pos = nodeCircle.Circle.position.XZ();
            for (int i = 0; i < cpPosList.Count; i += 2)
            {
                bool a = math.distance(cpPosList[i].Circle.position.XZ(), pos) > nodeCircle.Circle.radius;
                bool b = math.distance(cpPosList[i + 1].Circle.position.XZ(), pos) > nodeCircle.Circle.radius;

                if (a) cpPosBuffer.Add(cpPosList[i]);
                if (b) cpPosBuffer.Add(cpPosList[i + 1]);
                if (a || b) linesBuffer.Add(linesList[i / 2]);
            }

            return true;
        }
    }
}
