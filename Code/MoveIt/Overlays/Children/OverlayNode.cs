using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.PSI.Common;
using MoveIt.Moveables;
using QCommonLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;
using Unity.Mathematics;

namespace MoveIt.Overlays
{
    public class OverlayNode : Overlay
    {
        private static EntityArchetype _Archetype = _Tool.EntityManager.CreateArchetype(
            new ComponentType[] {
                    typeof(MIO_Type),
                    typeof(MIO_Common),
                    typeof(MIO_Lines),
                    typeof(MIO_Circle),
                    typeof(MIO_Circles),
            });

        public static Entity Factory(Entity owner, Circle3 circle)
        {
            Entity e = _Tool.EntityManager.CreateEntity(_Archetype);

            MIO_Common common = new()
            {
                m_Owner = owner,
            };

            _Tool.EntityManager.SetComponentData<MIO_Type>(e, new(OverlayTypes.MVNode));
            _Tool.EntityManager.SetComponentData(e, common);
            _Tool.EntityManager.SetComponentData<MIO_Circle>(e, new(circle));

            return e;
        }


        protected override Game.Objects.Transform Transform
        {
            get
            {
                var transform = m_Moveable.Transform;
                transform.m_Position = _Tool.EntityManager.TryGetComponent<Game.Net.NodeGeometry>(m_Owner, out var nodeGeo) ?
                    nodeGeo.m_Bounds.Center() :
                    _Tool.EntityManager.GetComponentData<Game.Net.Node>(m_Owner).m_Position;

                return transform;
            }
        }

        public OverlayNode() : base(OverlayTypes.MVNode) { }

        public override bool CreateOverlayEntity()
        {
            if (m_Moveable is not MVNode node) return false;
            if (!base.CreateOverlayEntity()) return false;

            m_Entity = OverlayNode.Factory(m_Moveable.m_Entity, new Circle3(m_Moveable.GetRadius(), Transform.m_Position, quaternion.identity));
            EnqueueUpdate();
            
            foreach (var cpd in node.m_CPDefinitions)
            {
                MVControlPoint cp = _Tool.ControlPointManager.GetOrCreate(cpd);
                ((OverlayControlPoint)cp.m_Overlay).CreateOverlayEntityIfNoneExists();
            }

            return true;
        }

        public override void EnqueueUpdate()
        {
            if (m_Moveable is not MVNode node) return;
            if (m_Entity.Equals(Entity.Null)) return;

            _Tool.QueueOverlayUpdate(this);

            foreach (Entity seg in node.m_Segments.Keys)
            {
                if (_Tool.Moveables.TryGet<MVSegment>(new MVDefinition(Identity.Segment, seg, m_Moveable.IsManipulatable), out var mvSeg))
                {
                    _Tool.QueueOverlayUpdate(mvSeg.m_Overlay);
                }
            }
        }

        public override bool Update()
        {
            if (m_Moveable is not MVNode node) return false;
            if (m_Entity.Equals(Entity.Null)) return false;

            MIO_Common common = _Tool.EntityManager.GetComponentData<MIO_Common>(m_Entity);
            UpdateCommon(ref common);
            _Tool.EntityManager.SetComponentData(m_Entity, common);

            MIO_Circle nodeCircle = _Tool.EntityManager.GetComponentData<MIO_Circle>(m_Entity);
            nodeCircle.Circle.position = Transform.m_Position;

            DynamicBuffer<MIO_Circles> cpPosBuffer = _Tool.EntityManager.GetBuffer<MIO_Circles>(m_Entity);
            DynamicBuffer<MIO_Lines> linesBuffer = _Tool.EntityManager.GetBuffer<MIO_Lines>(m_Entity);
            cpPosBuffer.Clear();
            linesBuffer.Clear();
            List<MIO_Circles> cpPosList = new();
            List<MIO_Lines> linesList = new();

            // Calculate all control point circles and lines
            float circleYPos = 0f;
            foreach ((Entity seg, bool isNodeA) in node.m_Segments)
            {
                Bezier4x3 curve = _Tool.EntityManager.GetComponentData<Game.Net.Curve>(seg).m_Bezier;
                circleYPos += isNodeA ? curve.a.y : curve.d.y;

                // Don't show this control point circle/line if the segment is hovered or selected in the same mode
                MVDefinition segDef = new(Identity.Segment, seg, m_Moveable.IsManipulatable);
                if (_Tool.Hover.Is(segDef)) continue;
                if (_Tool.Selection.Has(segDef)) continue;

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
            _Tool.EntityManager.SetComponentData(m_Entity, nodeCircle);

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
