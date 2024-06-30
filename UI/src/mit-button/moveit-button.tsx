import { useState } from "react";
import { Button } from "cs2/ui";
import { bindValue, trigger, useValue } from "cs2/api";
import { game, tool, Theme } from "cs2/bindings";
import { getModule, ModuleRegistryExtend } from "cs2/modding";
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
import { MIT_ToolEnabled$, MIT_HideMoveItIcon$ } from "bindings";
//import { LocalizedString, useLocalization } from "cs2/l10n";
import mod from "../../mod.json";

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
import styles from "./moveit-button.module.scss";

function toggle_ToolEnabled()
{
    console.log("MoveIt icon clicked");
    trigger(mod.id, 'MIT_EnableToggle');
}

export const MoveItButton : ModuleRegistryExtend = (Component) =>
{
    return (props) => {
        const toolEnabled = useValue(MIT_ToolEnabled$);
        const hideIcon = useValue(MIT_HideMoveItIcon$);
        const { children, ...otherProps } = props || {};

        if (hideIcon) return(<><Component {...otherProps}></Component></>);

        var yeet = iconOff;
        yeet = iconActive;

        const moveItIconSrc = toolEnabled ? "coui://ui-mods/images/MoveIt_Active.svg" : "coui://ui-mods/images/MoveIt_Off.svg";
        
        return (
            <>
                <Button 
                    id="MoveItIcon"
                    src={moveItIconSrc} 
                    className ={ToolBarButtonTheme.button + " " + styles.MoveItIcon} 
                    variant="icon"
                    focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                    selected={toolEnabled}
                    onSelect={toggle_ToolEnabled}
                    >
                </Button>

                <div className={ToolBarTheme.divider}></div>

                <Component {...otherProps}></Component>
            </>
                    
        );
    }
}