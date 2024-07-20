import { CheckboxData } from "./checkbox/cbData";
import { FOEntryCBData, FOTitleCBData, FoldoutCBData } from "./foldout/foData";
import { Foldout } from "./foldout/foldout";
import { FoldoutCBState } from "./foldout/foState";

export function FiltersDropdown(state: FoldoutCBState) : JSX.Element
{
    return Foldout(FiltersData, state);
}

const FiltersData : FoldoutCBData = new FoldoutCBData(
    new FOTitleCBData("filtersTitle", "Filters", "Choose object types", new CheckboxData("filtersTitleCB", "Filters")),
    [
        new FOEntryCBData("buildings",      "Buildings",        "Select Buildings",         new CheckboxData("buildingsCB",     "Buildings")),
        new FOEntryCBData("plants",         "Plants",           "Select Plants",            new CheckboxData("plantsCB",        "Plants")),
        new FOEntryCBData("decals",         "Decals",           "Select Decals",            new CheckboxData("decalsCB",        "Decals")),
        new FOEntryCBData("props",          "Props",            "Select Props",             new CheckboxData("propsCB",         "Props")),
        new FOEntryCBData("surfaces",       "Surfaces",         "Select Surfaces",          new CheckboxData("surfacesCB",      "Surfaces")),
        new FOEntryCBData("nodes",          "Nodes",            "Select Nodes",             new CheckboxData("nodesCB",         "Nodes")),
        new FOEntryCBData("segments",       "Segments",         "Select Segments",          new CheckboxData("segmentsCB",      "Segments")),
        new FOEntryCBData("controlpoints",  "Control Points",   "Select Control Points",    new CheckboxData("controlpointsCB", "Control Points")),
    ],
);
