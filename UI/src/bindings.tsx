import { bindValue } from "cs2/api";
import { trigger, useValue } from "cs2/api";
import { PanelState } from "mit-mainpanel/panelState";
import mod from "../mod.json";

export const MIT_ToolEnabled$           = bindValue<boolean>(mod.id, 'MIT_ToolEnabled', false);
export const MIT_PanelState$            = bindValue<PanelState>(mod.id, "MIT_PanelState", undefined);
export const MIT_ShowDebugPanel$        = bindValue<boolean>(mod.id, 'MIT_ShowDebugPanel', false);
export const MIT_DebugPanelContents$    = bindValue<string>(mod.id, 'MIT_DebugPanelContents', "Move It Debug Panel");
export const MIT_HideMoveItIcon$        = bindValue<boolean>(mod.id, 'MIT_HideMoveItIcon', false);
export const MIT_ShowRebindConfirm$     = bindValue<boolean>(mod.id, 'MIT_ShowRebindConfirm', false);
export const MIT_RebindExistingMsg$     = bindValue<string>(mod.id, 'MIT_RebindExistingMsg', "[error]");


export function ButtonPressed(id : string)
{
    trigger(mod.id, "MIT_PanelButtonPress", id);
}