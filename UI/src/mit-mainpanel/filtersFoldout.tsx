import { CheckboxData } from "./checkbox/cbData";
import { FOMainEntryData, FOTitleData, FoldoutData } from "./foldout/foData";
import { Foldout } from "./foldout/foldout";
import { FoldoutState } from "./foldout/foState";

export function FiltersFoldout(state: FoldoutState) : JSX.Element
{
    return Foldout(FiltersData, state);
}

const FiltersData : FoldoutData = new FoldoutData(
    new FOTitleData("filters", "filtersAll", "Filters", "Choose object types", new CheckboxData("filtersAll", "Filters"), false, true),
    [
        new FOMainEntryData("filters", "buildings",     "Buildings",        "Select Buildings",         new CheckboxData("buildings",       "Buildings")),
        new FOMainEntryData("filters", "plants",        "Plants",           "Select Plants",            new CheckboxData("plants",          "Plants")),
        new FOMainEntryData("filters", "decals",        "Decals",           "Select Decals",            new CheckboxData("decals",          "Decals")),
        new FOMainEntryData("filters", "props",         "Props",            "Select Props",             new CheckboxData("props",           "Props")),
        new FOMainEntryData("filters", "surfaces",      "Surfaces",         "Select Surfaces",          new CheckboxData("surfaces",        "Surfaces")),
        new FOMainEntryData("filters", "nodes",         "Nodes",            "Select Nodes",             new CheckboxData("nodes",           "Nodes")),
        new FOMainEntryData("filters", "segments",      "Segments",         "Select Segments",          new CheckboxData("segments",        "Segments")),
    ],
);
