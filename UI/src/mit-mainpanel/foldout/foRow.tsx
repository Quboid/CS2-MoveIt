import foStyles from "./foldout.module.scss";
import classNames from "classnames";
import { Button, Tooltip } from "cs2/ui";
import { ButtonPressed } from "bindings";
import { FOTitleCBData, FOEntryCBData } from "./foData";
import { FoldoutCBState, FOEntryCBState } from "./foState";
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
import { BuildCheckbox } from "mit-mainpanel/checkbox/checkbox";

export function FOTitleRow(data: FOTitleCBData, state: FoldoutCBState) : JSX.Element {
    const classes = classNames({
        [foStyles.row]: true,
        [foStyles.dropdownTitle]: true,
        [foStyles.dropdownTitleOpen]: state.IsOpen,
        [foStyles.dropdownTitleClosed]: !state.IsOpen,
    });

    return (
        <div className={classes}>
        <Button
            className={foStyles.dropdownTitleBtn}
            // src={data.GetIconPath(state)}
            id={data.Id}
            focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
            onSelect={() => ButtonPressed(data.Id)}
            variant="icon">
                {data.Label}
        </Button>
        </div>
    );
}

export function FOEntryRow(data: FOEntryCBData, state: FOEntryCBState) : JSX.Element
{
    const classes = classNames({
        [foStyles.row]: true,
        [foStyles.dropdownEntry]: true,
    });

    return (
        <div className={classes}>
            {BuildCheckbox(data.Checkbox, state.Checkbox)}<span>{data.Label}</span>
        </div>
    );
}
