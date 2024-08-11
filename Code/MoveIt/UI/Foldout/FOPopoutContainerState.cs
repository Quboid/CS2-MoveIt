using System.Collections.Generic;

namespace MoveIt.UI.Foldout
{
    /// <summary>
    /// Container for all of a popout menu's data
    /// </summary>
    public class FOPopoutContainerState : FOSectionContainerStateBase
    {
        internal override List<FoldoutEntry> GetEntries()
        {
            return new();
        }
    }
}
