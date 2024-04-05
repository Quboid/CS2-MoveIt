using Colossal.Mathematics;
using MoveIt.Moveables;
using QCommonLib;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Snapper
{
    internal struct EvaluateCandidatesJob : IJobFor
    {
        public NativeArray<State> m_OldStates;
        public NativeList<SnapCandidate> m_Candidates;
        public NativeList<SnapResult>.ParallelWriter m_Results;
        public SnapLookups m_Lookups;
        public float3 m_PointerPosition;
        public float3 m_OriginalPointerPosition;
        public float3 m_OriginalCenter;
        public bool m_IsManipulating;

        public readonly void Execute(int index)
        {
            SnapCandidate candidate = m_Candidates[index];
            State originalState = GetState(index);
            float3 offsetPointer = originalState.m_Position - m_OriginalPointerPosition;
            float3 offsetCenter = originalState.m_Position - m_OriginalCenter;
            SnapResult result = new();
            
            switch (candidate.m_Type)
            {
                case SnapTypes.Point:
                    {
                        result.m_Distance = math.distance(candidate.m_Point.XZ(), m_PointerPosition.XZ() + offsetCenter.XZ());
                        result.m_Score = result.m_Distance > Snapper.MAX_SNAP_DISTANCE ? 0f : candidate.m_Weight / (result.m_Distance + 1);
                        result.m_Candidate = candidate;
                        result.m_SnapPosition = candidate.m_Point;
                        result.m_Delta = result.m_SnapPosition - originalState.m_Position;
                        m_Results.AddNoResize(result);
                    
                        break;
                    }

                case SnapTypes.Line:
                    {
                        Line2.Segment line = candidate.m_Line.xz;
                        result.m_Distance = MathUtils.Distance(line, m_PointerPosition.XZ() + offsetPointer.XZ(), out float t);
                        result.m_Score = result.m_Distance > Snapper.MAX_SNAP_DISTANCE ? 0f : candidate.m_Weight / (result.m_Distance + 1);
                        result.m_Candidate = candidate;
                        result.m_SnapPosition = math.lerp(candidate.m_Line.a, candidate.m_Line.b, t);
                        result.m_Delta = result.m_SnapPosition - originalState.m_Position;
                        m_Results.AddNoResize(result);

                        break;
                    }

                case SnapTypes.Curve:
                    {
                        break;
                    }

                case SnapTypes.ZoneTile:
                    {
                        break;
                    }

                default:
                    {
                        break;
                    }
            }
        }

        private readonly State GetState(int index)
        {
            foreach (var s in m_OldStates)
            {
                if (s.m_Entity.Equals(m_Candidates[index].m_Entity))
                {
                    return s;
                }
            }
            throw new System.Exception($"GetState failed to find state for entity {m_Candidates[index].m_Entity.D()}!");
        }
    }
}
