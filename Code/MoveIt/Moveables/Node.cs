using Colossal.IO.AssetDatabase.Internal;
using Colossal.Mathematics;
using MoveIt.Actions;
using MoveIt.Managers;
using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Moveables
{
    public class Node : Moveable
    {
        internal Dictionary<Entity, bool> m_Segments;
        internal List<CPDefinition> m_CPDefinitions;
        private NativeArray<Line3.Segment> _OverlayLines;
        private NativeArray<float3> _OverlayCPPositions;

        public override Game.Objects.Transform Transform
        {
            get
            {
                var netTransform = GetComponent<Game.Net.Node>();
                return new()
                {
                    m_Position = netTransform.m_Position,
                    m_Rotation = netTransform.m_Rotation,
                };
            }
        }

        public override Game.Objects.Transform OverlayTransform
        {
            get
            {
                var netGeoData = GetComponent<Game.Net.NodeGeometry>();
                var transform = Transform;
                transform.m_Position = netGeoData.m_Bounds.Center();
                return transform;
            }
        }

        public Node(Entity e) : base(e, QTypes.Identity.Node, QTypes.ObjectType.Normal)
        {
            Refresh();
        }

        internal override bool Refresh()
        {
            if (!base.Refresh()) return false;

            m_Segments = new();
            m_CPDefinitions = new();

            if (TryGetBuffer<Game.Net.ConnectedEdge>(out var buffer, true))
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    bool isStart = _Tool.EntityManager.GetComponentData<Game.Net.Edge>(buffer[i].m_Edge).m_Start.Equals(m_Entity);
                    m_Segments.Add(buffer[i].m_Edge, isStart);

                    CPDefinition cpd = new(m_Entity, buffer[i].m_Edge, (short)(isStart ? 0 : 3));
                    m_CPDefinitions.Add(cpd);
                    _Tool.ControlPointManager.Factory(cpd);

                    cpd = new(m_Entity, buffer[i].m_Edge, (short)(isStart ? 1 : 2));
                    m_CPDefinitions.Add(cpd);
                    _Tool.ControlPointManager.Factory(cpd);
                }
            }

            return true;
        }

        internal override List<T> GetChildren<T>()
        {
            List<T> result = new();
            foreach (CPDefinition cpd in m_CPDefinitions)
            {
                result.Add(_Tool.ControlPointManager.GetOrCreate(cpd) as T);
            }
            return result;
        }

        internal override void MoveIt(TransformAction action, State nodeState, bool move, bool rotate)
        {
            if (!move && !rotate) return;

            Matrix4x4 matrix = default;
            matrix.SetTRS(action.m_Center + action.MoveDelta, Quaternion.Euler(0f, action.AngleDelta, 0f), Vector3.one);

            for (int i = 0; i < m_CPDefinitions.Count; i++)
            {
                ControlPoint cp = _Tool.ControlPointManager.GetOrCreate(m_CPDefinitions[i]);
                State cpState = action.GetState(m_CPDefinitions[i]);

                cpState.m_Position = (float3)matrix.MultiplyPoint(cpState.m_InitialPosition - action.m_Center);
                cpState.m_Angle = (cpState.m_InitialAngle + action.AngleDelta + 360) % 360;

                action.SetState(m_CPDefinitions[i], cpState);
                //QLog.Debug($"{Time.frameCount} N.MoveIt {i}/{m_CPDefinitions.Count}/{_Tool.ControlPointManager.m_ControlPoints.Count} {m_Entity.DX()} ({m_CPDefinitions[i]}) cp:{cp.m_Entity.DX()}:{cp.m_CurveKey} newCPS:{cpState.m_Entity.D()}");
                cp.MoveIt(action, cpState, move, rotate);
            }

            nodeState.Transform(move, rotate);
        }

        public override void OnHover()
        {
            base.OnHover();
        }

        public override void OnUnhover()
        {
            base.OnUnhover();

            GetChildren<ControlPoint>().ForEach(cp => cp.DisposeIfUnused());
        }

        public override void OnSelect()
        {
            base.OnSelect();
        }

        public override void OnDeselect()
        {
            GetChildren<ControlPoint>().ForEach(cp => cp.DisposeIfUnused());

            base.OnDeselect();
        }

        internal float GetDiameter()
        {
            Game.Net.NodeGeometry geoData = _Tool.EntityManager.GetComponentData<Game.Net.NodeGeometry>(m_Entity);
            float x = geoData.m_Bounds.max.x - geoData.m_Bounds.min.x;
            float z = geoData.m_Bounds.max.z - geoData.m_Bounds.min.z;
            return math.max(6f, math.min(x, z));
        }

        internal static Circle2 GetCircle(Game.Net.NodeGeometry geoData)
        {
            float x = geoData.m_Bounds.max.x - geoData.m_Bounds.min.x;
            float z = geoData.m_Bounds.max.z - geoData.m_Bounds.min.z;
            float diameter = math.max(6f, math.min(x, z));
            return new(diameter / 2, geoData.m_Bounds.xz.Center());
        }

        internal override Utils.IOverlay GetOverlay(OverlayFlags flags = OverlayFlags.None)
        {
            _OverlayLines.Dispose();
            _OverlayLines = new(m_Segments.Count, Allocator.Persistent);
            _OverlayCPPositions.Dispose();
            _OverlayCPPositions = new(m_CPDefinitions.Count, Allocator.Persistent);

            float diameter = GetDiameter();

            int cCPs = 0;
            int cLines = 0;
            foreach ((Entity seg, bool isNodeA) in m_Segments)
            {
                Bezier4x3 curve = _Tool.EntityManager.GetComponentData<Game.Net.Curve>(seg).m_Bezier;
                CPDefinition cpdA = m_CPDefinitions.First(cpd => cpd.m_Segment.Equals(seg) && cpd.m_CurveKey.IsEnd());
                CPDefinition cpdB = m_CPDefinitions.First(cpd => cpd.m_Segment.Equals(seg) && cpd.m_CurveKey.IsMiddle());
                _OverlayCPPositions[cCPs++] = curve.Get(cpdA.m_CurveKey);
                _OverlayCPPositions[cCPs++] = curve.Get(cpdB.m_CurveKey);

                Line3.Segment line = isNodeA ? new(curve.a, curve.b) : new(curve.d, curve.c);
                float cutStart = QIntersect.IntersectionsBetweenLineAndCircleCut(new(diameter / 2, OverlayTransform.m_Position.XZ()), line, true);
                float cutEnd = QIntersect.IntersectionsBetweenLineAndCircleCut(new(1f, (isNodeA ? curve.b : curve.c).XZ()), line, false);

                _OverlayLines[cLines++] = DrawTools.CalculcateSegmentEndLine(curve, cutStart, cutEnd, isNodeA);
            }


            return new OverlayNode(OverlayTransform, diameter, _OverlayLines, _OverlayCPPositions, flags);
        }


        public struct OverlayNode : Utils.IOverlay
        {
            public Utils.OverlayCommon Common { get; set; }

            public float m_Diameter;
            public Color m_CustomColor;
            public NativeArray<Line3.Segment> m_Lines;
            public NativeArray<float3> m_CPPositions;

            public OverlayNode(Game.Objects.Transform transform, float diameter, NativeArray<Line3.Segment> lines, NativeArray<float3> CPPositions, OverlayFlags flags)
            {
                Common = new()
                {
                    Transform = transform,
                    Flags = flags,
                };
                m_Diameter = diameter;
                m_CustomColor = default;
                m_Lines = lines;
                m_CPPositions = CPPositions;
            }

            public void Draw(ToolFlags toolFlags)
            {
                Common.DrawTool.CircleSimple(Common, Colors.Get(Common, Colors.Styles.Foreground), m_Diameter);
                if ((toolFlags & ToolFlags.Manipulation) == ToolFlags.Manipulation) return;
                if ((Common.Flags & OverlayFlags.Selected) == OverlayFlags.None) return;

                Utils.OverlayCommon common = new()
                {
                    Width = Common.Width * 0.9f,
                };
                for (int i = 0; i < m_CPPositions.Length; i++)
                {
                    if (math.distance(m_CPPositions[i].XZ(), Common.Transform.m_Position.XZ()) < (m_Diameter / 2)) continue;
                    common.Transform = new(m_CPPositions[i], default);
                    Common.DrawTool.CircleSimple(common, Colors.Get(Common, Colors.Styles.Background), 2f);
                }
                for (int i = 0; i < m_Lines.Length; i++)
                {
                    Common.DrawTool.LineDashedSimple(common, Colors.Get(Common, Colors.Styles.Background), m_Lines[i]);
                }
            }

            public readonly void Dispose() { }

            public readonly JobHandle Dispose(JobHandle handle)
            {
                return handle;
            }
        }


        public override void Dispose()
        {
            _OverlayCPPositions.Dispose();
            _OverlayLines.Dispose();
        }

        ~Node()
        {
            _OverlayCPPositions.Dispose();
            _OverlayLines.Dispose();
        }

        internal string DebugNode()
        {
            string msg = $"{m_Entity.DX()} CPs:{m_CPDefinitions.Count}";

            return msg;
        }
    }
}
