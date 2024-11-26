using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Actions.Transform
{
    internal class TransformKeyAction : TransformBase
    {
        private readonly float3 _FACTOR = new(0.25f, 0.015625f, 0.25f); // y = 1/64
        public override string Name => "TransformKeyAction";

        internal void Process(float3 direction)
        {
            if (!direction.Equals(float3.zero))
            {
                direction.x *= _FACTOR.x;
                direction.y *= _FACTOR.y;
                direction.z *= _FACTOR.z;

                Matrix4x4 matrix = default;
                matrix.SetTRS(Vector3.zero, Quaternion.AngleAxis(Camera.main.transform.localEulerAngles.y, Vector3.up), Vector3.one);
                direction = matrix.MultiplyVector(direction);
            }

            MoveDelta += direction;

            Phase = Phases.Do;
        }

        protected override bool ToolDo()
        {
            m_UpdateMove = true;
            DoFromDeltas();
            return true;
        }

        protected override void ToolDoLast()
        {
            Phase = Phases.Finalise;
        }

        public override void Archive(Phases phase, int idx)
        {
            Phase = Phases.Finalise;
            base.Archive(phase, idx);
        }
    }
}
