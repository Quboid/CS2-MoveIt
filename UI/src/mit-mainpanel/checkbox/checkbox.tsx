import cbStyles from "./checkbox.module.scss";
import { CheckboxData } from "./cbData";
import { CheckboxState } from "./cbState";
import { Checkbox } from "./vanilla/checkbox";
import { StatisticsItemSCSS } from "./vanilla/checkbox.module.scss";

export function BuildCheckbox(data: CheckboxData, state: CheckboxState) : JSX.Element
{
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
