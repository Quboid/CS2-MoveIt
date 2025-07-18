using System;
using System.Linq;
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

    [Flags]
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
        internal Entity m_Entity;
        internal SnapTypes m_Type;
        internal SnapFlags m_Flags;
        internal float m_Weight;

        internal float3 m_Point;
        internal Line3.Segment m_Line;
        internal Bezier4x3 m_Curve;
        internal bool m_ZoneTile;

        public SnapCandidate(bool _)
        {
            m_Entity = Entity.Null;
            m_Type = SnapTypes.None;
            m_Flags = SnapFlags.None;
            m_Weight = 1f;

            m_Point = default;
            m_Line = default;
            m_Curve = default;
            m_ZoneTile = default;
        }

        public readonly override string ToString()
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
        internal SnapCandidate m_Candidate;
        internal float3 m_SnapPosition;
        internal float3 m_Delta;
        internal float m_Distance;
        internal float m_Score;

        // Create new struct with default values, needs pointless parameter in C# 9.0
        public SnapResult(bool _)
        {
            m_Candidate = default;
            m_SnapPosition = default;
            m_Delta = default;
            m_Distance = 999f;
            m_Score = 0f;
        }

        public readonly override string ToString()
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

            var bestScore = 0.2f;
            bestResult = default;
            var bestFound = false;
            m_SnapType = SnapTypes.None;

            for (var i = 0; i < results.Length; i++)
            {
                if (!(results[i].m_Score > bestScore)) continue;
                
                bestScore = results[i].m_Score;
                bestResult = results[i];
                bestFound = true;
                m_SnapType = bestResult.m_Candidate.m_Type;
                m_SnapPosition = bestResult.m_SnapPosition;
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
            var msg = $"Candidates: {_Candidates.Length}";
            for (int i = 0; i < _Candidates.Length; i++)
            {
                msg += "\n    " + _Candidates[i].ToString();
            }
            MIT.Log.Debug(msg);
        }

        internal void DebugDumpResults(NativeList<SnapResult> results)
        {
            var msg = $"Results: {results.Length}";
            for (int i = 0; i < results.Length; i++)
            {
                msg += "\n    " + results[i].ToString();
            }
            MIT.Log.Bundle("SNAP", msg);
        }
    }

    internal struct SnapLookups
    {
        private static SnapLookups _Lookup;
        private static bool _Initialized;

        public static void Reset()
        {
            _Initialized = false;
        }

        public static ref SnapLookups Get(SystemBase system)
        {
            if (_Initialized) return ref _Lookup;
            
            _Lookup = new();
            _Lookup.Init(system);
            _Initialized = true;

            return ref _Lookup;
        }

        internal BufferLookup<Game.Net.ConnectedEdge> gnConnectedEdge;
        internal ComponentLookup<Game.Net.Curve> gnCurve;
        internal ComponentLookup<Game.Net.Edge> gnEdge;
        internal ComponentLookup<Game.Net.Node> gnNode;
        internal ComponentLookup<Game.Objects.Transform> goTransform;
        internal ComponentLookup<Components.MIT_ControlPoint> MIT_ControlPoint;

        private void Init(SystemBase system)
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
