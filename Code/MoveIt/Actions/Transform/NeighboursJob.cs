using Colossal.Mathematics;
using Game.Net;
using MoveIt.Moveables;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace MoveIt.Actions.Transform
{
#if USE_BURST
    [BurstCompile]
#endif
    internal struct NeighboursJob : IJobFor
    {
        public NativeList<Neighbour> m_Neighbours;
        public ComponentLookup<Curve> gnCurve;

        public void Execute(int index)
        {
            Neighbour neighbour = m_Neighbours[index];
            RefRW<Curve> component = gnCurve.GetRefRW(neighbour.m_Entity);

            Bezier4x3 initial = neighbour.m_InitialCurve;
            Bezier4x3 offset = new(0f, initial.b - initial.a, initial.c - initial.a, initial.d - initial.a);
            float3 tValB = new(
                offset.d.x == 0 ? 0.333f : (offset.b.x / offset.d.x),
                offset.d.y == 0 ? 0.333f : (offset.b.y / offset.d.y),
                offset.d.z == 0 ? 0.333f : (offset.b.z / offset.d.z));
            float3 tValC = new(
                offset.d.x == 0 ? 0.667f : (offset.c.x / offset.d.x),
                offset.d.y == 0 ? 0.667f : (offset.c.y / offset.d.y),
                offset.d.z == 0 ? 0.667f : (offset.c.z / offset.d.z));
            Bezier4x3 tValues = new(0f, tValB, tValC, 1f);
            Bezier4x3 curve = component.ValueRO.m_Bezier;

            Bezier4x3 newCurve = new(
                curve.a,
                math.lerp(curve.a, curve.d, tValB),
                math.lerp(curve.a, curve.d, tValC),
                curve.d
                );

            //QLog.Debug($"NeighbourJob for {neighbour.m_Entity.DX()}" +
            //    $"\n   Initial:{initial.a.DX(),-25} | {initial.b.DX(),-25} | {initial.c.DX(),-25} | {initial.d.DX(),-25}" +
            //    $"\n     Curve:{curve.a.DX(),-25} | {curve.b.DX(),-25} | {curve.c.DX(),-25} | {curve.d.DX(),-25}" +
            //    $"\n  NewCurve:{newCurve.a.DX(),-25} | {newCurve.b.DX(),-25} | {newCurve.c.DX(),-25} | {newCurve.d.DX(),-25}" +
            //    $"\n    Offset:                          | {offset.b.DX(),-25} | {offset.c.DX(),-25}" +
            //    $"\n   tValues:                          | {tValues.b.DX(),-25} | {tValues.c.DX(),-25}" +
            //    $"");

            component.ValueRW.m_Bezier = newCurve;
        }
    }
}
