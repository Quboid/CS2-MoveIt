﻿using MoveIt.Systems.UIElements.Checkbox;

namespace MoveIt.Systems.UIElements.Foldout
{
    public class FOEntryState : StateBase
    {
        public FOEntryState(string id, bool enabled, bool active)
            : base(id, enabled, active) { }
    }

    public class FOEntryCBState : FOStateCBBase
    {
        public FOEntryCBState(string id, bool enabled, bool active, CheckboxState checkboxState)
            : base(id, enabled, active, checkboxState) { }
    }
}
