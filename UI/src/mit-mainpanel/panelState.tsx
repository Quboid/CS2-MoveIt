import { FoldoutCBState } from "./foldout/foState";

export interface ButtonState
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
}

export interface TopRowButtonStates
{
    ButtonUndo : ButtonState;
    ButtonSingle : ButtonState;
    ButtonMarquee : ButtonState;
    ButtonManipulation : ButtonState;
    ButtonRedo : ButtonState;
}

export interface PanelState
{
    TopRow : TopRowButtonStates;
    FilterSection : FoldoutCBState;
}
