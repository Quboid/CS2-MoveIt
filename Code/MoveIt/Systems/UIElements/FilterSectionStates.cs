using MoveIt.Systems.UIElements.Checkbox;
using MoveIt.Systems.UIElements.Foldout;

namespace MoveIt.Systems.UIElements
{
    public class FilterSectionStates : FoldoutCBState
    {
        public FilterSectionStates() : base()
        {
            m_DDTitleCBState = new("filtersTitle", true, false, new CheckboxState("filtersTitleCB", true, false));
            m_DDEntryCBStates = new[] {
                new FOEntryCBState("buildings",     true, false, new CheckboxState("buildingsCB",       true, false)),
                new FOEntryCBState("plants",        true, false, new CheckboxState("plantsCB",          true, false)),
                new FOEntryCBState("decals",        true, false, new CheckboxState("decalsCB",          true, false)),
                new FOEntryCBState("props",         true, false, new CheckboxState("propsCB",           true, false)),
                new FOEntryCBState("surfaces",      true, false, new CheckboxState("surfacesCB",        true, false)),
                new FOEntryCBState("nodes",         true, false, new CheckboxState("nodesCB",           true, false)),
                new FOEntryCBState("segments",      true, false, new CheckboxState("segmentsCB",        true, false)),
                new FOEntryCBState("controlpoints", true, false, new CheckboxState("controlpointsCB",   true, false)),
            };
        }
    }
}
