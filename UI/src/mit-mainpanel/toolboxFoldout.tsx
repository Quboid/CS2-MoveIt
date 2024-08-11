import { FOMainEntryData, FoldoutData, FOTitleData, FOPopoutData } from "./foldout/foData";
import { Foldout } from "./foldout/foldout";
import { FoldoutState } from "./foldout/foState";

export function ToolboxFoldout(state: FoldoutState) : JSX.Element
{
    return Foldout(ToolboxData, state);
}

const ToolboxData : FoldoutData = new FoldoutData(
    new FOTitleData("toolbox", "toolboxTitle", "Toolbox", ""),
    [
        new FOMainEntryData("toolbox",  "terrainHeight",    "To Terrain Height",    ""),
        new FOMainEntryData("toolbox",  "objectHeight",     "To Object Height",     ""),
        new FOMainEntryData("toolbox",  "rotateAtCentre",   "Rotate at Centre",     ""),
        new FOMainEntryData("toolbox",  "rotateInPlace",    "Rotate in-Place",      "", undefined, undefined),
    ],
);
