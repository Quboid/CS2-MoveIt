
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
import { bindValue, trigger, useValue } from "cs2/api";
import { Portal, Panel, PanelSection, FormattedParagraphs, Button, Tooltip } from "cs2/ui";
import { MIT_RebindExistingMsg$, MIT_ShowRebindConfirm$ } from "bindings"
import icon from "../img/MoveIt_Active.svg";
import stylesMain from "../mit-mainpanel/panel.module.scss";
import styles from "./rebindConfirm.module.scss";
import mod from "../../mod.json";

function ButtonPressed(doRebind : boolean)
{
    trigger(mod.id, "MIT_ShowRebindConfirm", doRebind);
}

export const MIT_RebindConfirm = () =>
{
    const showRebindConfirm = useValue(MIT_ShowRebindConfirm$);
    const rebindExistingMsg = useValue(MIT_RebindExistingMsg$);

    if (!showRebindConfirm) return null;

    return (
        <>
            <Portal>
                <Panel
                    draggable
                    className={styles.panel}
                    header={(
                        <div className={styles.header}>
                            <img src={icon} className={stylesMain.headerIcon} />
                            <span className={stylesMain.headerText}>Move It - Use 'M' Key?</span>
                        </div>
                    )}>
                    <PanelSection className={styles.section}>
                        <FormattedParagraphs>
                            {rebindExistingMsg}
                        </FormattedParagraphs>
                    {/* </PanelSection>
                    <PanelSection className={styles.section}> */}
                        <div className={styles.buttonRow}>
                            <div className={styles.buttonContainer}>
                            <Tooltip tooltip="Yes, Move It will use 'M'">
                            <Button
                                className={styles.button}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                onSelect={() => ButtonPressed(true)}
                                variant="primary">Yes</Button>
                            </Tooltip>
                            </div>
                            <div className={styles.buttonContainer}>
                            <Tooltip tooltip="No, Move It will use 'Shift + M'">
                            <Button
                                className={styles.button}
                                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                                onSelect={() => ButtonPressed(false)}
                                variant="primary">No</Button>
                            </Tooltip>
                            </div>
                        </div>
                    </PanelSection>
                </Panel>
            </Portal>
        </>
    )
}