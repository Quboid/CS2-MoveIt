using MoveIt.Systems.UIElements.Checkbox;

namespace MoveIt.Systems.UIElements.Foldout
{
    public class FOTitleState : StateBase
    {
        public FOTitleState(string id, bool enabled, bool active)
            : base(id, enabled, active) { }
    }

    public class FOTitleCBState : FOStateCBBase
    {
        public FOTitleCBState(string id, bool enabled, bool active, CheckboxState checkboxState)
            : base(id, enabled, active, checkboxState) { }
    }
}
