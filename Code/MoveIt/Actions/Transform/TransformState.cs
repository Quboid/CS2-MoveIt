using MoveIt.Moveables;
using Unity.Collections;
using Unity.Mathematics;

namespace MoveIt.Actions.Transform
{
    internal class TransformStateOld : TransformState
    {
        internal TransformStateOld(int length) : base(length) { }
    }

    internal class TransformStateNew : TransformState
    {
        internal TransformStateNew(int length) : base(length) { }
    }

    internal abstract class TransformState : ActionState
    {
        internal NativeArray<State> m_States;
        internal float3 m_MoveDelta = 0f;
        internal float m_AngleDelta = 0f;

        internal int Count => m_States.Length;

        internal TransformState(int length)
        {
            m_States = new NativeArray<State>(length, Allocator.Persistent);
        }

        public override void Dispose()
        {
            if (m_States.IsCreated)
            {
                for (int i = 0; i < m_States.Length; i++)
                {
                    m_States[i].Dispose();
                }
            }
            m_States.Dispose();
        }

        public override string ToString()
        {
            return $"[TrState:{(this is TransformStateNew ? "New" : "Old")},#{m_States.Length}]";
        }
    }
}
