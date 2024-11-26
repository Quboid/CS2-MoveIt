using Game.Input;
using MoveIt.Actions.Select;
using MoveIt.Actions.Transform;
using MoveIt.Tool;
using QCommonLib;

namespace MoveIt.Input
{
    internal class SecondaryButton : InputButton
    {
        internal SecondaryButton(ProxyAction action) : base(action) { }
        internal SecondaryButton(string mapName, string actionName) : base(mapName, actionName) { }

        protected override void OnPress()
        {
            m_Status = ButtonStatus.Down;
            if (_MIT.MITState == MITStates.Default)
            {
                _MIT.m_MouseStartX = QCommon.MouseScreenPosition.x;
                _MIT.m_SensitivityTogglePosX = _MIT.m_MouseStartX;
            }
        }

        protected override void OnClick()
        {
            if (_MIT.MITState == MITStates.ToolActive)
            {
                _MIT.ToolboxManager.Phase = Managers.ToolboxManager.Phases.Finalise;
                return;
            }

            if (_MIT.MITState != MITStates.Default) return;

            if (_MIT.IsManipulating && _MIT.Selection.Count == 0)
            {
                _MIT.SetManipulationMode(false);
                return;
            }

            _MIT.Queue.Push(new SelectAction());
            _MIT.Queue.Do();

            Actions.Action.Phase = Actions.Phases.Cleanup;

            //MIT.Log.Debug($"SecondaryButton.OnClick {Queue.Debug()}");
        }

        protected override void OnHold()
        {
            //MIT.Log.Debug($"SecondaryButton.OnDrag");
            if (m_Status == ButtonStatus.Down && _MIT.MITState == MITStates.Default)
            {
                if (_MIT.Hovered.LastValid.IsNull) return;

                _MIT.RotationStart();
            }
            if (_MIT.MITState == MITStates.SecondaryButtonHeld)
            {
                _MIT.UpdateSensitivityMode();
            }
        }

        protected override void OnHoldEnd()
        {
            //MIT.Log.Debug($"SecondaryButton.OnHoldEnd ts:{MITState}");
            if (m_Status == ButtonStatus.Down && _MIT.Queue.Current is TransformBase ta && _MIT.MITState == MITStates.SecondaryButtonHeld)
            {
                _MIT.RotationEnd();
            }
            _MIT.ProcessSensitivityMode(false);
        }

        protected override void OnRelease()
        {
            m_Status = ButtonStatus.None;
            //MIT.Log.Debug($"SecondaryButton.OnRelease");
        }
    }
}
