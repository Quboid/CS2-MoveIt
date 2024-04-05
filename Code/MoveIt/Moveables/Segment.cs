using Colossal.Mathematics;
using MoveIt.Actions;
using MoveIt.Managers;
using MoveIt.Overlays;
using MoveIt.Tool;
using QCommonLib;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Moveables
{
    public class Segment : Moveable
    {
        internal static float GetDefaultWidth(Entity e)
        {
            if (!_Tool.EntityManager.Exists(e))
            {
                MIT.Log.Error($"Segment.GetDefaultWidth - entity {e.D()} doesn't exist!");
                return 0f;
            }
            Game.Prefabs.PrefabRef segPrefab = _Tool.EntityManager.GetComponentData<Game.Prefabs.PrefabRef>(e);
            Game.Prefabs.NetGeometryData geoData = _Tool.EntityManager.GetComponentData<Game.Prefabs.NetGeometryData>(segPrefab);
            return geoData.m_DefaultWidth;
        }

        internal List<CPDefinition> m_CPDefinitions;
        private NativeArray<Circle2> _OverlayCircles;

        internal Bezier4x3 Curve => _Tool.EntityManager.GetComponentData<Game.Net.Curve>(m_Entity).m_Bezier;
        internal Game.Net.Edge Edge => _Tool.EntityManager.GetComponentData<Game.Net.Edge>(m_Entity);

        public override Game.Objects.Transform Transform
        {
            get
            {
                return new Game.Objects.Transform()
                {
                    m_Position = Curve.Position(),
                    m_Rotation = new quaternion(),
                };
            }
        }

        public Segment(Entity e) : base(e, QTypes.Identity.Segment, QTypes.ObjectType.Normal, QTypes.Manipulate.Normal | QTypes.Manipulate.Parent)
        {
            Refresh();
        }

        internal override bool Refresh()
        {
            if (!base.Refresh()) return false;

            m_CPDefinitions = new();
            for (short i = 0; i < CURVE_CPS; i++)
            {
                CPDefinition cpd = new(i <= 1 ? Edge.m_Start : Edge.m_End, m_Entity, i);
                m_CPDefinitions.Add(cpd);

                _Tool.ControlPointManager.Factory(cpd);
            }
            return true;
        }

        internal override List<T> GetChildren<T>()
        {
            List<T> result = new()
            {
                _Tool.ControlPointManager.GetOrCreate(m_CPDefinitions[1]) as T,
                _Tool.ControlPointManager.GetOrCreate(m_CPDefinitions[2]) as T
            };
            return result;
        }

        internal List<ControlPoint> GetChildrenFull()
        {
            List<ControlPoint> result = new();
            foreach (CPDefinition cpd in m_CPDefinitions)
            {
                result.Add(_Tool.ControlPointManager.GetOrCreate(cpd));
            }
            return result;
        }

        internal override void MoveIt(TransformAction action, State state, bool move, bool rotate)
        {
            if (!move && !rotate) return;

            Bezier4x3 originalCurve = state.m_Data.Segment.m_InitialCurve;
            float3 oldPos = originalCurve.Position();
            float3 newPos = state.m_Position;

            //string msg = "";
            ControlPoint cpB = _Tool.ControlPointManager.Get(m_CPDefinitions[1]);
            ControlPoint cpC = _Tool.ControlPointManager.Get(m_CPDefinitions[2]);
            State newB = action.GetState(m_CPDefinitions[1]);
            State newC = action.GetState(m_CPDefinitions[2]);

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

            action.SetState(m_CPDefinitions[1], newB);
            action.SetState(m_CPDefinitions[2], newC);
            cpB.MoveIt(action, newB, move, rotate);
            cpC.MoveIt(action, newC, move, rotate);

            state.Transform(move, rotate);
        }

        internal override void UpdateYOffset()
        {
            m_YOffset = 0f;
        }

        public override void OnHover()
        {
            base.OnHover();
        }

        public override void OnUnhover()
        {
            GetChildrenFull().ForEach(cp => cp.DisposeIfUnused());

            base.OnUnhover();
        }

        public override void OnSelect()
        {
            base.OnSelect();
        }

        public override void OnDeselect()
        {
            GetChildrenFull().ForEach(cp => cp.DisposeIfUnused());

            base.OnDeselect();
        }

        private List<ControlPoint> InitialiseOverlayCircles()
        {
            if (m_CPDefinitions.Count < 4)
            {
                return new();
            }
            List<ControlPoint> cps = new();

            m_CPDefinitions.ForEach(cpd => cps.Add(_Tool.ControlPointManager.GetOrCreate(cpd)));
            _OverlayCircles.Dispose();
            _OverlayCircles = new(4, Allocator.Persistent);

            for (int i = 0; i < CURVE_CPS; i++)
            {
                _OverlayCircles[i] = cps[i].GetCircle();
            }

            return cps;
        }

        internal override HashSet<Utils.IOverlay> GetOverlays(OverlayFlags flags)
        {
            HashSet<Utils.IOverlay> result = new();
            List<ControlPoint> cps = InitialiseOverlayCircles();
            if (cps.Count == 0)
            {
                return new();
            }

            for (int i = 0; i < CURVE_CPS; i++)
            {
                Circle2 c = _OverlayCircles[i];
                c.radius = 1f;
                _OverlayCircles[i] = c;
            }
            result.Add(new OverlaySegmentNormal(Curve, flags, _OverlayCircles));

            return result;
        }

        internal override HashSet<Utils.IOverlay> GetManipulationOverlays(OverlayFlags flags)
        {
            HashSet<Utils.IOverlay> result = new();
            List<ControlPoint> cps = InitialiseOverlayCircles();
            if (cps.Count == 0)
            {
                return new();
            }

            result.Add(new OverlaySegmentManipulating(Curve, flags, _OverlayCircles));

            for (int i = 0; i < CURVE_CPS; i++)
            {
                if (cps[i].m_Status <= CPStatus.Visible)
                {
                    result.Add(cps[i].GetOverlayFromParent(flags));
                }
            }

            return result;
        }


        public struct OverlaySegmentNormal : Utils.IOverlay
        {
            public Utils.OverlayCommon Common { get; set; }

            public Bezier4x3 m_Curve;
            public NativeArray<Circle2> m_CPCircles;

            public OverlaySegmentNormal(Bezier4x3 curve, OverlayFlags flags, NativeArray<Circle2> circles)
            {
                Common = new()
                {
                    Flags = flags,
                    Manipulatable = QTypes.Manipulate.Normal,
                };
                m_Curve = curve;
                m_CPCircles = circles;
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                Line2 line;

                line = DrawTools.CalculateProtrudedLine(m_CPCircles[0], m_CPCircles[1]);
                Common.DrawTool.LineDashedSimple(Common, Colors.Get(Common, Colors.Styles.Foreground), line);
                line = DrawTools.CalculateProtrudedLine(m_CPCircles[3], m_CPCircles[2]);
                Common.DrawTool.LineDashedSimple(Common, Colors.Get(Common, Colors.Styles.Foreground), line);

                Common.DrawTool.CircleSimple(Common, Colors.Get(Common, Colors.Styles.Foreground), m_CPCircles[0]);
                Common.DrawTool.CircleSimple(Common, Colors.Get(Common, Colors.Styles.Foreground), m_CPCircles[3]);

                Common.DrawTool.CircleSimple(Common, Colors.Get(Common, Colors.Styles.Foreground), m_CPCircles[1]);
                Common.DrawTool.CircleSimple(Common, Colors.Get(Common, Colors.Styles.Foreground), m_CPCircles[2]);
            }

            public readonly void Dispose() { }
            public readonly JobHandle Dispose(JobHandle handle) => handle;
        }


        public struct OverlaySegmentManipulating : Utils.IOverlay
        {
            public Utils.OverlayCommon Common { get; set; }

            public Bezier4x3 m_Curve;
            public NativeArray<Circle2> m_CPCircles;

            public OverlaySegmentManipulating(Bezier4x3 curve, OverlayFlags flags, NativeArray<Circle2> circles)
            {
                Common = new()
                {
                    Flags = flags,
                    Manipulatable = QTypes.Manipulate.Parent,
                };
                m_Curve = curve;
                m_CPCircles = circles;
            }

            public readonly void Draw(ToolFlags toolFlags)
            {
                Color fg = Colors.Get(Common, Common.Flags == OverlayFlags.Hovering ? Colors.Styles.Background : Colors.Styles.Foreground);

                Line3.Segment line = DrawTools.CalculateNodeProtrusionCurve(m_Curve, m_CPCircles[0], m_CPCircles[1], true);
                Common.DrawTool.LineSimple(Common, Colors.Get(Common, Colors.Styles.Background), line);
                Common.DrawTool.LineDashedSimple(Common, fg, line);
                line = DrawTools.CalculateNodeProtrusionCurve(m_Curve, m_CPCircles[3], m_CPCircles[2], false);
                Common.DrawTool.LineSimple(Common, Colors.Get(Common, Colors.Styles.Background), line);
                Common.DrawTool.LineDashedSimple(Common, fg, line);
            }

            public readonly void Dispose() { }

            public readonly JobHandle Dispose(JobHandle handle) => handle;
        }


        public override void Dispose()
        {
            _OverlayCircles.Dispose();
        }

        ~Segment()
        {
            _OverlayCircles.Dispose();
        }
    }
}
