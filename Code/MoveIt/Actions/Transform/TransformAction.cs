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
                if (_MIT.UsingPrecisionMode)
                {
                    float3 mouseDeltaBefore = _MIT.m_SensitivityTogglePosAbs - _MIT.m_ClickPositionAbs;
                    float3 mouseDeltaAfter = (_MIT.m_PointerPos - _MIT.m_SensitivityTogglePosAbs) / 6f;
                    newMoveDelta = mouseDeltaBefore + mouseDeltaAfter;
                }
                else
                {
                    newMoveDelta = _MIT.m_PointerPos - _MIT.m_ClickPositionAbs;
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

                float mouseTravel;
                if (_MIT.UsingPrecisionMode)
                {
                    float mouseRotateBefore = _MIT.m_SensitivityTogglePosX - _MIT.m_MouseStartX;
                    float mouseRotateAfter = (QCommon.MouseScreenPosition.x - _MIT.m_SensitivityTogglePosX) / 6;
                    mouseTravel = mouseRotateBefore + mouseRotateAfter;
                }
                else
                {
                    mouseTravel = QCommon.MouseScreenPosition.x - _MIT.m_MouseStartX;
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

            // If nothing has changed this frame, end now
            if (!m_UpdateMove && !m_UpdateRotate)
            {
                return false;
            }

            MoveDelta = newMoveDelta;
            AngleDelta = newAngleDelta;

            DoFromDeltas();

            return true;
        }
    }
}
