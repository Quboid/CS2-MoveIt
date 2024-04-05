using Colossal.Mathematics;
using MoveIt.Moveables;
using QCommonLib;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Snapper
{
    internal struct FindCandidatesJob : IJobFor
    {
        public NativeArray<State> m_States;
        public NativeList<SnapCandidate>.ParallelWriter m_Candidates;
        public SnapLookups m_Lookups;
        public bool m_IsManipulating;

        public readonly void Execute(int index)
        {
            State s = m_States[index];

            if (m_IsManipulating)
            {
                if (s.m_Identity == QTypes.Identity.ControlPoint)
                {
                    StateControlPoint data = (StateControlPoint)s.m_Data.Get();
                    Bezier4x3 curve = m_Lookups.gnCurve.GetRefRO(data.m_Segment).ValueRO.m_Bezier;

                    #region Control Point Line
                    float3 posA = data.m_Curvekey.IsNodeA() ? curve.a : curve.d;
                    float3 posB = data.m_Curvekey.IsNodeA() ? curve.b : curve.c;
                    float3 v = posB - posA;
                    float distance = math.distance(posA, posB);
                    float multiplier = 40f / distance;
                    posA -= v * multiplier;
                    posB += v * (multiplier * 4);

                    Line3.Segment line = new(posA, posB);

                    SnapCandidate candidateLine = new()
                    {
                        m_Entity = data.m_Entity,
                        m_Type = SnapTypes.Line,
                        m_Line = line,
                    };
                    m_Candidates.AddNoResize(candidateLine);
                    #endregion

                    #region Control Point Points
                    if (data.m_Curvekey.IsMiddle())
                    {
                        SnapCandidate candidate = new()
                        {
                            m_Entity = data.m_Entity,
                            m_Type = SnapTypes.Point,
                            m_Flags = SnapFlags.SameStraight,
                            m_Point = math.lerp(curve.a, curve.d, data.m_Curvekey / 3f),
                            m_Weight = 2f,
                        };
                        m_Candidates.AddNoResize(candidate);
                    }
                    else
                    {
                        Game.Net.Edge edge = m_Lookups.gnEdge.GetRefRO(data.m_Segment).ValueRO;
                        Entity node = data.m_Curvekey.IsNodeA() ? edge.m_Start : edge.m_End;
                        if (m_Lookups.gnConnectedEdge.TryGetBuffer(node, out var buffer) && buffer.Length > 1)
                        {
                            float3 position = 0f;
                            for (int i = 0; i < buffer.Length; i++)
                            {
                                if (buffer[i].m_Edge.Equals(data.m_Segment)) continue;

                                Game.Net.Edge subEdge = m_Lookups.gnEdge.GetRefRO(buffer[i].m_Edge).ValueRO;
                                bool isStart = subEdge.m_Start.Equals(node);
                                Bezier4x3 subCurve = m_Lookups.gnCurve.GetRefRO(buffer[i].m_Edge).ValueRO.m_Bezier;
                                position += isStart ? subCurve.a : subCurve.d;
                            }
                            position /= (float)(buffer.Length - 1f);

                            SnapCandidate candidate = new()
                            {
                                m_Entity = data.m_Entity,
                                m_Type = SnapTypes.Point,
                                m_Point = position,
                                m_Weight = 2f,
                            };
                            m_Candidates.AddNoResize(candidate);
                        }
                    }
                    #endregion
                }
            }
            else
            {

                #region Straight Segment
                if (s.m_Identity == QTypes.Identity.Segment)
                {
                    StateSegment data = (StateSegment)s.m_Data.Get();

                    SnapCandidate candidate = new()
                    {
                        m_Entity = data.m_Entity,
                        m_Type = SnapTypes.Point,
                        m_Flags = SnapFlags.SameStraight,
                        m_Point = math.lerp(data.m_InitialCurve.a, data.m_InitialCurve.d, 0.5f),
                        m_Weight = 2,
                    };

                    m_Candidates.AddNoResize(candidate);
                }
                #endregion

            }
        }
    }
}
