import { useState } from "react";
import { ButtonTheme, Button, ConfirmationDialog, Panel, Portal, FloatingButton, PanelSection, PanelSectionRow, FormattedParagraphs } from "cs2/ui";
import { bindValue, trigger, useValue } from "cs2/api";
import { game, tool, Theme } from "cs2/bindings";
import { getModule, ModuleRegistryExtend } from "cs2/modding";
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
//import { LocalizedString, useLocalization } from "cs2/l10n";
import mod from "../../mod.json";

export const MIT_ShowUI$ = bindValue<boolean>(mod.id, 'MIT_ShowUI', false);
export const MIT_ToolEnabled$ = bindValue<boolean>(mod.id, 'MIT_ToolEnabled', false);

// Getting the vanilla theme css for compatibility
const ToolBarButtonTheme: Theme | any = getModule(
    "game-ui/game/components/toolbar/components/feature-button/toolbar-feature-button.module.scss",
    "classes"
);
const ToolBarTheme: Theme | any = getModule(
    "game-ui/game/components/toolbar/toolbar.module.scss",
    "classes"
);

import iconOff from "../img/MoveIt_Off.svg";
import iconActive from "../img/MoveIt_Active.svg";
import styles from "../moveit.module.scss";

function toggle_ToolEnabled()
{
    console.log("MoveIt icon clicked");
    trigger(mod.id, 'MIT_EnableToggle');
}

export const MoveItButton : ModuleRegistryExtend = (Component) =>
{
    return (props) => {
        const { children, ...otherProps } = props || {};
        const MIT_ToolEnabled = useValue(MIT_ToolEnabled$);

        let a = iconOff;
        a = iconActive;

        const moveItIconSrc = MIT_ToolEnabled ? "coui://ui-mods/images/MoveIt_Active.svg" : "coui://ui-mods/images/MoveIt_Off.svg";
        
        return (
            <>
                <Button 
                    src={moveItIconSrc} 
                    className ={ToolBarButtonTheme.button + " " + styles.MoveItIcon} 
                    variant="icon"
                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                    selected={MIT_ToolEnabled}
                    onSelect={toggle_ToolEnabled}
                    >
                </Button>

                <div className={ToolBarTheme.divider}></div>

                <Component {...otherProps}></Component>
            </>
                    
        );
    }
}