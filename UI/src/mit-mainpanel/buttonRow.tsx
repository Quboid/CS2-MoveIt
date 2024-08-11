import { useState } from "react";
import classNames from "classnames";
import { Button, Tooltip } from "cs2/ui";
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
import { ButtonState, TopRowButtonStates } from "mit-mainpanel/panelState";
import { ButtonPressed } from "bindings";

import styles from "./panel.module.scss";
// Ugly code to force these images to build
import ic0 from "../img/icon_Undo_Off.svg";
import ic1 from "../img/icon_Single_Off.svg";
import ic2 from "../img/icon_Marquee_Off.svg";
import ic3 from "../img/icon_Manipulation_Off.svg";
import ic4 from "../img/icon_Redo_Off.svg";
import ic5 from "../img/icon_Undo_Disabled.svg";
import ic6 from "../img/icon_Single_Active.svg";
import ic7 from "../img/icon_Marquee_Active.svg";
import ic8 from "../img/icon_Manipulation_Active.svg";
import ic9 from "../img/icon_Redo_Disabled.svg";
import icA from "../img/icon_FoldoutOpen.svg";
import icB from "../img/icon_FoldoutClose.svg";
import icC from "../img/icon_PopoutOpen.svg";
import icD from "../img/icon_PopoutClose.svg";

export function ButtonRowTop(topRowState : TopRowButtonStates) {
    const classes = classNames({
        [styles.row]: true,
        [styles.buttonRow]: true,
    });

    // Ugly code to force these images to build
    var x = ic0; x = ic1; x = ic2; x = ic3; x = ic4; x = ic5; x = ic6; 
    x = ic7; x = ic8; x = ic9; x = icA; x = icB; x = icC; x = icD;

    return (
        <div className={classes}>
            {ButtonRowButton(TopButtonsData[0], topRowState.ButtonUndo)}
            <div className={styles.separator}></div>
            {ButtonRowButton(TopButtonsData[1], topRowState.ButtonSingle)}
            <div className={styles.separator}></div>
            {ButtonRowButton(TopButtonsData[2], topRowState.ButtonMarquee)}
            <div className={styles.separator}></div>
            {ButtonRowButton(TopButtonsData[3], topRowState.ButtonManipulation)}
            <div className={styles.separator}></div>
            {ButtonRowButton(TopButtonsData[4], topRowState.ButtonRedo)}
        </div>
    );
}

function ButtonRowButton(data : ButtonData, state : ButtonState)
{
    return (
        <div className={styles.buttonContainer}>
        <Tooltip tooltip={data.Tooltip}>
        <Button
            disabled={!state.IsEnabled}
            className={styles.button}
            src={data.GetIconPath(state)}
            id={data.Id}
            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
            onSelect={() => ButtonPressed(data.Section, data.Id)}
            variant="icon" />
        </Tooltip>
        </div>
    );
}

class ButtonData
{
    Section : string;
    Id : string;
    Icon : string;
    Tooltip : string;

    constructor(section : string, id : string, icon : string, tooltip : string)
    {
        this.Section = section;
        this.Id = id;
        this.Icon = icon;
        this.Tooltip = tooltip;
    }

    public GetIconPath(state : ButtonState) : string
    {
        if (this.Icon === null || this.Icon === "") return "";
        let postfix = !state.IsEnabled ? "Disabled" : state.IsActive ? "Active" : "Off";
    
        return `coui://ui-mods/images/icon_${this.Icon}_${postfix}.svg`;
    }
}

const TopButtonsData : ButtonData[] = [
    new ButtonData("toprow",    "undo",          "Undo",         "Undo"),
    new ButtonData("toprow",    "single",        "Single",       "Single Mode"),
    new ButtonData("toprow",    "marquee",       "Marquee",      "Marquee Mode"),
    new ButtonData("toprow",    "manipulation",  "Manipulation", "Manipulation Mode"),
    new ButtonData("toprow",    "redo",          "Redo",         "Redo"),
];