using Game.Input;
using MoveIt.Tool;
using QCommonLib;
using System.Diagnostics;

namespace MoveIt.Input
{
    internal abstract class InputButton
    {
        protected MIT _Tool = MIT.m_Instance;

        protected readonly int DragThreshold = 250;

        internal ProxyAction Action { get; set; }
        internal long m_PressedTime;



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
            if (m_PressedTime == 0 && Action.IsPressed())
            {
                m_PressedTime = Stopwatch.GetTimestamp();
                OnPress();
            }

            if (m_PressedTime != 0)
            {
                long elapsed = QCommon.ElapsedMilliseconds(m_PressedTime);

                if (!Action.IsPressed())
                {
                    m_PressedTime = 0;

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
                else if (elapsed >= DragThreshold)
                {
                    OnHold();
                }
            }
        }

        /// <summary>
        /// Input button has been pressed, fired immediately
        /// </summary>
        internal abstract void OnPress();

        /// <summary>
        /// Input button has been released, fired for every press regardless of length (fires after OnClick() and OnHoldEnd())
        /// </summary>
        internal abstract void OnRelease();

        /// <summary>
        /// Input button is pressed for less than 250ms, fired after release within this time-frame
        /// </summary>
        internal abstract void OnClick();

        /// <summary>
        /// Input button has been held for at least 250ms, fired every frame
        /// </summary>
        internal abstract void OnHold();

        /// <summary>
        /// Input button was held for at least 250ms, fired upon release
        /// </summary>
        internal abstract void OnHoldEnd();
    }
}
