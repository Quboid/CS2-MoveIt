import { CheckboxState } from "mit-mainpanel/checkbox/cbState";

export interface FoldoutCBState
{
    IsOpen : boolean;
    Title : FOTitleCBState;
    Entries: FOEntryCBState[];
}

export interface FOTitleCBState
{
    Id : string;
    /// <summary>
    /// IsEnabled: is this widget able to be interacted with?
    /// </summary>
    IsEnabled : boolean;
    /// <summary>
    /// IsActive: is this widget ticked/open/etc?
    /// </summary>
    IsActive : boolean;
    Checkbox : CheckboxState;
}

export interface FOEntryCBState
{
    Id : string;
    /// <summary>
    /// IsEnabled: is this widget able to be interacted with?
    /// </summary>
    IsEnabled : boolean;
    /// <summary>
    /// IsActive: is this widget ticked/open/etc?
    /// </summary>
    IsActive : boolean;
    Checkbox : CheckboxState;
}
