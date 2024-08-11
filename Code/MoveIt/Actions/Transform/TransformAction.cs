using MoveIt.Tool;
using QCommonLib;
using Unity.Mathematics;
using UnityEngine;

namespace MoveIt.Actions.Transform
{
    internal class TransformAction : TransformBase
    {
        public override string Name => "Transform";

        protected override bool ToolDo()
        {
            float3 newMoveDelta = MoveDelta;
            float newAngleDelta = AngleDelta;

            if (_MIT.MITState == MITStates.ApplyButtonHeld)
            {
                float y = MoveDelta.y;
                if (_MIT.IsLowSensitivity)
                {
                    float3 mouseDeltaBefore = _MIT.m_SensitivityTogglePosAbs - _MIT.m_ClickPositionAbs;
                    float3 mouseDeltaAfter = (_MIT.m_PointerPos - _MIT.m_SensitivityTogglePosAbs) / 5f;
                    newMoveDelta = mouseDeltaBefore + mouseDeltaAfter;
                }
                else
                {
                    newMoveDelta = _MIT.m_PointerPos - _MIT.m_ClickPositionAbs;// - m_dragStartRelative;
                }
                newMoveDelta.y = y;

                // Snapping
                if (QKeyboard.Alt)
                {
                    if (m_Snapper.Update(out var snapResult))
                    {
                        newMoveDelta = snapResult.m_Delta;
                    }
                }
            }
            else if (_MIT.MITState == MITStates.SecondaryButtonHeld)
            {
                // Rotation value, 1 = full 360 (uses screen height, not width, to adapt to ultrawide)
                //float angle = (float)(mouseTravel) / (float)(Screen.height * 1.5f) * _MIT.RotationDirection;

                float mouseTravel;
                if (_MIT.IsLowSensitivity)
                {
                    float mouseRotateBefore = _MIT.m_SensitivityTogglePosX - _MIT.m_MouseStartX;// _MIT.m_SensitivityAngleOffset;
                    float mouseRotateAfter = (QCommon.MouseScreenPosition.x - _MIT.m_SensitivityTogglePosX) / 5;
                    mouseTravel = mouseRotateBefore + mouseRotateAfter;// / Screen.width * 1.2f;

                    //newAngle = ushort.MaxValue * 9.58738E-05f * mouseTravel;
                }
                else
                {
                    mouseTravel = QCommon.MouseScreenPosition.x - _MIT.m_MouseStartX;
                    //newAngle = ushort.MaxValue * 9.58738E-05f * (QCommon.MouseScreenPosition.x - _MIT.m_MouseStartX) / Screen.width * 1.2f;
                }
                float angle = mouseTravel / (float)(Screen.height * 1.5f) * _MIT.RotationDirection;


                // Snapping
                if (QKeyboard.Alt)
                {
                    // Snap to 45 degrees
                    angle = Mathf.Round(angle * 8f) / 8;
                }

                newAngleDelta = angle * 360;
            }

            // If nothing has changed this frame, end now
            m_UpdateMove = false;
            m_UpdateRotate = false;
            if (!MoveDelta.Equals(newMoveDelta))
            {
                m_UpdateMove = true;
            }

            if (!AngleDelta.Equals(newAngleDelta))
            {
                m_UpdateRotate = true;
            }

            if (!m_UpdateMove && !m_UpdateRotate)
            {
                return false;
            }

            MoveDelta = newMoveDelta;
            AngleDelta = newAngleDelta;

            DoFromAngleAndMoveDeltas();

            return true;
        }
    }
}
