import { useRef } from "react";
import { Portal, Panel, PanelSection } from "cs2/ui";
import { bindValue, trigger, useValue } from "cs2/api";
import { MIT_ToolEnabled$, MIT_PanelState$ } from "bindings";
import { PanelState } from "mit-mainpanel/panelState";

import icon from "../img/MoveIt_Active.svg";
import styles from "./panel.module.scss";

import { ButtonRowTop } from "./buttonRow";
import { FiltersFoldout } from "./filtersFoldout";
import { ToolboxFoldout } from "./toolboxFoldout";

export const MIT_MainPanel = () =>
{
    const toolEnabled = useValue(MIT_ToolEnabled$);
    const panelState = useValue<PanelState>(MIT_PanelState$);
    const panelRef = useRef(null);
    const position = {x: CalculateXPosition(), y: 0.875};

    if (!toolEnabled) return null;
    if (typeof panelState === 'undefined') {
        console.log("MOVE IT: Error reading PanelState, UI will close. Bye. [MIT01]");
        return null;
    }

    return (
        <>
            <Portal>
                <Panel
                    draggable
                    className={styles.panel}
                    initialPosition={position}
                    header={(
                        <div className={styles.header}>
                            <img src={icon} className={styles.headerIcon} />
                            <span className={styles.headerText}>Move It</span>
                        </div>
                    )}>
                    <PanelSection className={styles.section}>
                        <div ref={panelRef}>
                            {ButtonRowTop(panelState.TopRow)}
                            {/* {CheckboxRow("Hello", "hello")}
                            {CheckboxRow("World", "world")} */}
                            {FiltersFoldout(panelState.FilterSection)}
                            {ToolboxFoldout(panelState.ToolboxSection)}
                        </div>
                    </PanelSection>
                </Panel>
            </Portal>
        </>
    );

    function CalculateXPosition() : number
    {
        // if (panelRef === null || panelRef.current === null) {
        //     console.log("MOVE IT: Error reading Panel reference. [MIT02]");
        //     return 0;
        // }
        // let rect = (panelRef.current as any).getBoundingClientRect();
        // if (rect === null) {
        //     console.log("MOVE IT: Error reading Panel rectangle. [MIT03]");
        //     return 0;
        // }

        let panelStyleWidth = 200;
        let xOffset = 15;
        let uiScale = window.innerHeight / 1080;
        
        let icon = document.getElementById('MoveItIcon');
        if (icon === null) {
            console.log("MOVE IT: Error reading Icon rectangle. [MIT04]");
            return 0.8;
        }

        let iconRight = (icon.offsetLeft + icon.offsetWidth) / window.innerWidth;
        let iconRightForPanelRange = iconRight * (window.innerWidth / (window.innerWidth - (panelStyleWidth * uiScale)));
        let panelWidth = (panelStyleWidth + xOffset) / (1080 * (window.innerWidth / window.innerHeight));
        let panelX = iconRightForPanelRange - panelWidth;

        if (panelX < 0 || panelX > 1) { // Sanity check
            console.log(`MOVE IT: Panel X position calculated as ${panelX}, falling back to 0.8. This is harmless. [MIT05]`)
            return 0.8;
        }

        return panelX;
    }
}
