using MoveIt.UI.Checkbox;

namespace MoveIt.UI.Foldout
{
    public class FOTitleState : FOStateBase
    {
        public FOTitleState()
            : base("none", true, null) { }

        public FOTitleState(string id, bool active, CheckboxState checkboxState = null)
            : base(id, active, checkboxState) { }
    }
}
