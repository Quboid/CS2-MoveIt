using MoveIt.Tool;
using QCommonLib;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Actions
{
    internal class TransformKeyAction : TransformAction
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

            m_HotkeyPressed = true;
            MoveDelta += direction;
            QLog.Debug($"TKA.Process dir:{direction.DX()}, mD:{MoveDelta.DX()}");
            _Tool.ToolAction = ToolActions.Do;
            _Tool.CreationPhase = CreationPhases.Create;
        }
    }
}
