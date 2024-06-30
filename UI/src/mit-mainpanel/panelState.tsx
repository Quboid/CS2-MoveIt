export interface ButtonState
{
    Id : string;
    IsEnabled : boolean;
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
}
