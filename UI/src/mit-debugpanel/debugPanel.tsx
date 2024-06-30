import { Portal, Panel, PanelSection, FormattedParagraphs } from "cs2/ui";
import { useValue } from "cs2/api";
import { MIT_ToolEnabled$, MIT_ShowDebugPanel$, MIT_DebugPanelContents$ } from "bindings";

import icon from "../img/MoveIt_Active.svg";
import styles from "./debugPanel.module.scss";

export const MIT_DebugPanel = () =>
{
    const toolEnabled = useValue(MIT_ToolEnabled$);
    const showDebugPanel = useValue(MIT_ShowDebugPanel$);
    const debugPanelContents = useValue(MIT_DebugPanelContents$);

    if (!toolEnabled || !showDebugPanel) return null;

    const position = {x: CalculateXPosition(), y: 0.01};

    return (
        <>
            <Portal>
                <Panel
                    draggable
                    className={styles.panel}
                    // onClose={OnClose}
                    initialPosition={position}
                    header={(
                        <div className={styles.header}>
                            <img src={icon} className={styles.headerIcon} />
                            <span className={styles.headerText}>MIT Debug</span>
                        </div>
                    )}>
                    <PanelSection className={styles.section}>
                        <FormattedParagraphs>
                            {debugPanelContents}
                        </FormattedParagraphs>
                    </PanelSection>
                </Panel>
            </Portal>
        </>
    );

    function CalculateXPosition() : number
    {
        let panelStyleWidth = 250;
        let distanceFromRight = 150;
        let uiScale = window.innerHeight / 1080;
        let uiScaledWidth = 1920 - panelStyleWidth;

        let panelX = (uiScaledWidth - distanceFromRight) / uiScaledWidth;// * uiScale;

        if (panelX < 0 || panelX > 1) { // Sanity check
            console.log(`MOVE IT: DebugPanel X position calculated as ${panelX}, falling back to 0.8. This is harmless. (UIScale:${uiScale}, UISWidth:${uiScaledWidth}) [MIT06]`)
            return 0.8;
        }

        return panelX;
    }
}
