using Game.Input;
using MoveIt.Tool;
using QCommonLib;
using System.Diagnostics;
using Unity.Mathematics;

namespace MoveIt.Input
{
    internal abstract class InputButton
    {
        protected MIT _MIT = MIT.m_Instance;

        protected readonly int DragThreshold = 250;

        protected ProxyAction Action { get; set; }
        protected long m_PressedTime;
        protected ButtonStatus m_Status     = ButtonStatus.None;
        protected float2 m_PressedPosition  = float.MaxValue;
        protected bool m_IsDragging         = false;

        internal bool Enabled
        {
            get => Action.enabled;
            set => Action.shouldBeEnabled = value;
        }

        internal InputButton(ProxyAction action)
        {
            Action = action;
        }

        internal InputButton(string mapName, string actionName)
        {
            Action = InputManager.instance.FindAction(mapName, actionName);
        }

        internal void Update()
        {
            if (_MIT.UIHasFocus) return;

            if (m_PressedTime == 0 && Action.WasPressedThisFrame())
            {
                m_PressedTime = Stopwatch.GetTimestamp();
                m_PressedPosition = QCommon.MouseScreenPosition;
                m_IsDragging = false;
                OnPress();
            }

            if (m_PressedTime != 0)
            {
                long elapsed = QCommon.ElapsedMilliseconds(m_PressedTime);

                if (!Action.IsPressed())
                {
                    m_PressedTime = 0;

                    if (m_IsDragging)
                    {
                        OnDragEnd();
                        m_IsDragging = false;
                    }

                    if (elapsed < DragThreshold)
                    {
                        OnClick();
                    }
                    else
                    {
                        OnHoldEnd();
                    }

                    OnRelease();
                }
                else 
                {
                    if (!m_IsDragging)
                    {
                        if (math.distance(QCommon.MouseScreenPosition, m_PressedPosition) > 4f)
                        {
                            m_IsDragging = true;
                            OnDragStart();
                        }
                    }

                    if (m_IsDragging)
                    {
                        OnDrag();
                    }

                    if (elapsed >= DragThreshold)
                    {
                        OnHold();
                    }
                }
            }
        }

        /// <summary>
        /// Input button has been pressed, fired immediately
        /// </summary>
        internal abstract void OnPress();

        /// <summary>
        /// Input button has been released, fired for every press regardless of length (fires after OnClick and OnHoldEnd)
        /// </summary>
        internal abstract void OnRelease();

        /// <summary>
        /// Input button is pressed for less than 250ms, fired after release within this time-frame (after OnDragEnd, instead of OnHoldEnd, before OnRelease)
        /// </summary>
        internal abstract void OnClick();

        /// <summary>
        /// Input button is being held and has moved since press, fired once (before OnHold)
        /// </summary>
        internal virtual void OnDragStart() { }

        /// <summary>
        /// Input button is being held and has moved since press, fired every frame (before OnHold)
        /// </summary>
        internal virtual void OnDrag() { }

        /// <summary>
        /// Input button was being held and had moved since press, has now been released, fired once (before OnClick, OnHoldEnd, and OnRelease)
        /// </summary>
        internal virtual void OnDragEnd() { }

        /// <summary>
        /// Input button has been held for at least 250ms, fired every frame (after OnDrag)
        /// </summary>
        internal abstract void OnHold();

        /// <summary>
        /// Input button was held for at least 250ms, fired upon release (after OnDragEnd, instead of OnClick, before OnRelease)
        /// </summary>
        internal abstract void OnHoldEnd();

        protected enum ButtonStatus
        {
            None,
            Down,
        }
    }
}
