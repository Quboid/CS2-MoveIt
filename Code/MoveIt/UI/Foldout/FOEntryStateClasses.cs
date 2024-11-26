using Colossal.UI.Binding;
using MoveIt.UI.Checkbox;

namespace MoveIt.UI.Foldout
{
    /// <summary>
    /// The state of an entry in any Foldout menu
    /// </summary>
    public abstract class FOEntryStateBase : FOStateBase
    {
        protected FOEntryStateBase(string rawId, bool active, CheckboxState checkboxState = null)
            : base(rawId, active, checkboxState) { }
    }

    /// <summary>
    /// The state of an entry in a Foldout menu's main entry list
    /// </summary>
    public class FOEntryState : FOEntryStateBase
    {
        public readonly FOPopoutContainerState m_PopoutState;

        public FOEntryState(string rawId, bool active, CheckboxState checkboxState = null, FOPopoutContainerState popoutState = null)
            : base(rawId, active, checkboxState)
        {
            m_PopoutState = popoutState;
        }

        protected override void WriteExtendInner(IJsonWriter writer)
        {
            if (m_PopoutState is null) return;
            writer.PropertyName("Popout");
            writer.Write(m_PopoutState);
        }
    }

    /// <summary>
    /// The state of an entry in a Foldout menu's popout list
    /// </summary>
    public class FOPopoutEntryState : FOEntryStateBase
    {
        public FOPopoutEntryState(string rawId, bool active, CheckboxState checkboxState = null) : base(rawId, active, checkboxState)
        { }
    }
}
