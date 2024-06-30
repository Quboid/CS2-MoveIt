import { useState } from "react";
import { Button, Tooltip } from "cs2/ui";
import { trigger, useValue } from "cs2/api";
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
import { ButtonState, TopRowButtonStates } from "mit-mainpanel/panelState";
import mod from "../../mod.json";

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

export function ButtonRowTop(topRowState : TopRowButtonStates) {
    const classes: string = `${styles.row} ${styles.buttonRow}`;

    // Ugly code to force these images to build
    var x = ic0; x = ic1; x = ic2; x = ic3; x = ic4; x = ic5; x = ic6; x = ic7; x = ic8; x = ic9;

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
    if (!state.IsEnabled)
    {
        return (
            <div className={styles.buttonContainer}>
            <Tooltip tooltip={data.Tooltip}>
            <Button
                disabled
                className={styles.button}
                src={data.GetIconPath(state)}
                id={data.Id}
                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                onSelect={() => ButtonPressed(data.Id)}
                variant="icon" />
            </Tooltip>
            </div>
        );
    }

    return (
        <div className={styles.buttonContainer}>
        <Tooltip tooltip={data.Tooltip}>
        <Button
            className={styles.button}
            src={data.GetIconPath(state)}
            id={data.Id}
            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
            onSelect={() => ButtonPressed(data.Id)}
            variant="icon" />
        </Tooltip>
        </div>
    );
}

function ButtonPressed(id : string)
{
    trigger(mod.id, "MIT_PanelButtonPress", id);
}

class ButtonData
{
    Id : string;
    Icon : string;
    Tooltip : string;

    constructor(id : string, icon : string, tooltip : string)
    {
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
    new ButtonData("undo",          "Undo",         "Undo"),
    new ButtonData("single",        "Single",       "Single Mode"),
    new ButtonData("marquee",       "Marquee",      "Marquee Mode"),
    new ButtonData("manipulation",  "Manipulation", "Manipulation Mode"),
    new ButtonData("redo",          "Redo",         "Redo"),
];