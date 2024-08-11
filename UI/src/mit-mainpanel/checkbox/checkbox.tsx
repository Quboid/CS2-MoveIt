import cbStyles from "./checkbox.module.scss";
import { CheckboxData } from "./cbData";
import { CheckboxState } from "./cbState";
import { Checkbox } from "./vanilla/checkbox";
import { StatisticsItemSCSS } from "./vanilla/checkbox.module.scss";
import { CheckboxChanged } from "bindings";

export function BuildCheckbox(triggerEvent: boolean, type: string, data?: CheckboxData, state?: CheckboxState) : JSX.Element
{
    if (data === undefined || state === undefined) return(<></>);

    if (triggerEvent)
    {
        return (
            <div className={cbStyles.checkboxContainer}>
                <Checkbox
                    disabled={!state.IsEnabled}
                    checked={state.IsActive}
                    className={cbStyles.checkbox}
                    onChange={(value : boolean) => CheckboxChanged(type, data.Id, value)} // Now handled by label onMouseUp
                    theme={StatisticsItemSCSS}
                />
            </div>
        );
    }

    return (
        <div className={cbStyles.checkboxContainer}>
            <Checkbox
                disabled={!state.IsEnabled}
                checked={state.IsActive}
                className={cbStyles.checkbox}
                theme={StatisticsItemSCSS}
            />
        </div>
    );
}
