using Colossal.Mathematics;
using MoveIt.Actions.Transform;
using MoveIt.Moveables;
using MoveIt.Tool;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Snapper
{
    internal enum SnapTypes
    {
        None,
        Point,
        Line,
        Curve,
        ZoneTile,
        StraightSegment,
    }

    internal enum SnapFlags
    {
        None            = 0,
        NodeContinue    = 1,
        NodeNinety      = 1 << 1,
        NodeOneEighty   = 1 << 2,
        SameStraight    = 1 << 3,
    }

    internal struct SnapCandidate
    {
        internal Entity m_Entity = Entity.Null;
        internal SnapTypes m_Type = SnapTypes.None;
        internal SnapFlags m_Flags = SnapFlags.None;
        internal float m_Weight = 1f;

        internal float3 m_Point = default;
        internal Line3.Segment m_Line = default;
        internal Bezier4x3 m_Curve = default;
        internal bool m_ZoneTile = default;

        public SnapCandidate()
        { }

        public override readonly string ToString()
        {
            string msg = $"SC:{m_Type}/{m_Flags} w:{m_Weight} {m_Entity.DX()} ";
            msg += m_Type switch
            {
                SnapTypes.Point => "Point:" + m_Point.DX(),
                SnapTypes.Line  => $"Line:{m_Line.a.DX()} => {m_Line.b.DX()}",
                SnapTypes.Curve => $"Curve:{m_Curve.a.DX()} => {m_Curve.b.DX()} => {m_Curve.c.DX()} => {m_Curve.d.DX()}",
                _ => "",
            };
            return msg;
        }

        //internal readonly Utils.IOverlay GetOverlay(OverlayFlags flags = OverlayFlags.None)
        //{
        //    UnityEngine.Color c = new(0.9f, 0f, 0.3f, 0.8f);
        //    switch (m_Type)
        //    {
        //        case SnapTypes.Point:
        //            return new Utils.Point(m_Point, c);

        //        case SnapTypes.Line:
        //            return new Utils.Line(m_Line, c);

        //        default:
        //            return new Utils.None();
        //    }
        //}
    }

    internal struct SnapResult
    {
        internal SnapCandidate m_Candidate = default;
        internal float3 m_SnapPosition = default;
        internal float3 m_Delta = default;
        internal float m_Distance = 999f;
        internal float m_Score = 0f;

        public SnapResult()
        { }

        public override readonly string ToString()
        {
            return $"{m_Candidate} - Distance:{m_Distance}, Score:{m_Score}, Pos:{m_SnapPosition.DX()} ({m_Delta.DX()})";
        }
    }


    internal class Snapper
    {
        public const float MAX_SNAP_DISTANCE = 6f;

        protected readonly MIT _MIT = MIT.m_Instance;
        protected readonly TransformBase _Action;
        protected Selection.SelectionBase Selection => _MIT.Selection;
        protected NativeList<SnapCandidate> _Candidates;
        protected SnapLookups _Lookups;

        internal SnapTypes m_SnapType;
        internal float3 m_SnapPosition;

        internal Snapper(TransformBase action)
        {
            _Action = action;
            _Lookups = SnapLookups.Get(_MIT);

            using NativeArray<State> states = new(_Action.m_Active.m_States.ToArray(), Allocator.TempJob);
            _Candidates = new(states.Length * 2, Allocator.Persistent);

            FindCandidatesJob findCandidatesJob = new()
            {
                m_States = states,
                m_Candidates = _Candidates.AsParallelWriter(),
                m_Lookups = _Lookups,
                m_IsManipulating = _MIT.IsManipulating,
            };

            JobHandle findCandidatesHandle = findCandidatesJob.Schedule(states.Length, new());
            findCandidatesHandle.Complete();
        }

        ~Snapper()
        {
            _Candidates.Dispose();
        }

        internal bool Update(out SnapResult bestResult)
        {
            _Lookups.Update(_MIT);
            using NativeList<SnapResult> results = new(_Candidates.Length, Allocator.TempJob);
            using NativeArray<State> states = new(_Action.m_Old.m_States.ToArray(), Allocator.TempJob);

            EvaluateCandidatesJob evaluateCandidatesJob = new()
            {
                m_OldStates = states,
                m_Candidates = _Candidates,
                m_Lookups = _Lookups,
                m_Results = results.AsParallelWriter(),
                m_IsManipulating = _MIT.IsManipulating,
                m_PointerPosition = _MIT.m_PointerPos,
                m_OriginalPointerPosition = _MIT.m_ClickPositionAbs,
                m_OriginalCenter = _Action.m_Center,
            };

            JobHandle checkCandidatesHandle = evaluateCandidatesJob.Schedule(_Candidates.Length, new());
            checkCandidatesHandle.Complete();

            //DebugDumpResults(results);

            float bestScore = 0.2f;
            bestResult = default;
            bool bestFound = false;
            m_SnapType = SnapTypes.None;

            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].m_Score > bestScore)
                {
                    bestScore = results[i].m_Score;
                    bestResult = results[i];
                    bestFound = true;
                    m_SnapType = bestResult.m_Candidate.m_Type;
                    m_SnapPosition = bestResult.m_SnapPosition;
                }
            }

            return bestFound;
        }

        //internal HashSet<Overlays.OverlayManager> GetOverlays(Overlays.ToolFlags toolFlags)
        //{
        //    HashSet<Overlays.OverlayManager> result = new();

        //    //if (m_SnapType != SnapTypes.None)
        //    //{
        //    //    result.Add(new Overlays.Diamond(m_SnapPosition, 2f, OverlayFlags.Tool));
        //    //}

        //    return result;
        //}

        internal void DebugDump()
        {
            string msg = $"Candidates: {_Candidates.Length}";
            foreach (var item in _Candidates)
            {
                msg += $"\n    {item}";
            }
            MIT.Log.Debug(msg);
        }

        internal void DebugDumpResults(NativeList<SnapResult> results)
        {
            string msg = $"Results: {results.Length}";
            foreach (var item in results)
            {
                msg += $"\n    {item}";
            }
            MIT.Log.Bundle("SNAP", msg);
        }
    }

    internal struct SnapLookups
    {
        public static SnapLookups m_Lookup;
        private static bool _Initialized;

        public static void Reset()
        {
            _Initialized = false;
        }

        public static ref SnapLookups Get(SystemBase system)
        {
            if (!_Initialized)
            {
                m_Lookup = new();
                m_Lookup.Init(system);
                _Initialized = true;
            }

            return ref m_Lookup;
        }

        internal BufferLookup<Game.Net.ConnectedEdge> gnConnectedEdge;
        internal ComponentLookup<Game.Net.Curve> gnCurve;
        internal ComponentLookup<Game.Net.Edge> gnEdge;
        internal ComponentLookup<Game.Net.Node> gnNode;
        internal ComponentLookup<Game.Objects.Transform> goTransform;
        internal ComponentLookup<Components.MIT_ControlPoint> MIT_ControlPoint;

        public void Init(SystemBase system)
        {
            gnConnectedEdge = system.GetBufferLookup<Game.Net.ConnectedEdge>();
            gnCurve = system.GetComponentLookup<Game.Net.Curve>();
            gnEdge = system.GetComponentLookup<Game.Net.Edge>();
            gnNode = system.GetComponentLookup<Game.Net.Node>();
            goTransform = system.GetComponentLookup<Game.Objects.Transform>();
            MIT_ControlPoint = system.GetComponentLookup<Components.MIT_ControlPoint>();
        }

        public void Update(SystemBase system)
        {
            gnConnectedEdge.Update(system);
            gnCurve.Update(system);
            gnEdge.Update(system);
            gnNode.Update(system);
            goTransform.Update(system);
            MIT_ControlPoint.Update(system);
        }
    }
}
