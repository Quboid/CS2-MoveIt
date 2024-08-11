import { CheckboxState } from "mit-mainpanel/checkbox/cbState";

// Main Foldout menu

export interface FoldoutState
{
    IsOpen : boolean;
    Title : FOTitleState;
    Entries: FOMainEntryState[];
}

export interface FOTitleState
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
    Checkbox? : CheckboxState;
}

export interface FOEntryStateBase
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
    Checkbox? : CheckboxState;
}

export interface FOMainEntryState extends FOEntryStateBase
{
    Popout? : FOPopoutState;
}

// Popout submenu

export interface FOPopoutState
{
    IsOpen : boolean;
    Entries: FOPopoutEntryState[];
}

export interface FOPopoutEntryState extends FOEntryStateBase
{ }
