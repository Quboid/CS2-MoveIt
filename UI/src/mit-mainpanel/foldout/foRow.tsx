import foStyles from "./foldout.module.scss";
import classNames from "classnames";
import { Button, HTMLImageElement } from "cs2/ui";
import { LabelMouseUp } from "bindings";
import { FOTitleData, FOMainEntryData } from "./foData";
import { FoldoutState, FOMainEntryState, FOPopoutState } from "./foState";
import { VanillaComponentResolver } from "classes/VanillaComponentResolver";
import { BuildCheckbox } from "mit-mainpanel/checkbox/checkbox";

export function FOTitleRow(data: FOTitleData, state: FoldoutState) : JSX.Element {
    var showCB = data.Checkbox !== undefined && state.Title.Checkbox !== undefined;
    if (showCB)
    {
        showCB = state.IsOpen ? (data.CBWhenOpen === true) : (data.CBWhenClosed === true);
    }
    const classes = classNames({
        [foStyles.row]: true,
        [foStyles.title]: true,
        [foStyles.titleOpen]: state.IsOpen,
        [foStyles.titleClosed]: !state.IsOpen,
    });
    const labelClasses = classNames({
        [foStyles.label]: true,
        [foStyles.labelCB]: showCB,
    })

    return (
        <div className={classes}>
            {showCB && (<div className={foStyles.checkbox}>{BuildCheckbox(true, data.Section, data.Checkbox, state.Title.Checkbox)}</div>)}
            <Button
                className={foStyles.titleBtn}
                src={GetFoldoutArrow(state)}
                id={data.Id}
                focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                onMouseUpCapture={(e) => LabelMouseUp(data.Section, data.Id, e.button)}
                variant="icon">
                    <div className={labelClasses}>{data.Label}</div>
            </Button>
        </div>
    );
}

export function FOEntryRow(data: FOMainEntryData, state: FOMainEntryState) : JSX.Element
{
    const classes = classNames({
        [foStyles.row]: true,
        [foStyles.entry]: true,
        [foStyles.active]: state.IsActive,
        [foStyles.inactive]: !state.IsActive,
    });
    const showCB = data.Checkbox !== undefined && state.Checkbox !== undefined;
    const showPO = true;//data.Popout !== undefined && state.Popout !== undefined;
    const labelClasses = classNames({
        [foStyles.label]: true,
        [foStyles.labelCB]: showCB,
        [foStyles.labelPO]: showPO,
    })

    return (
        <div className={classes}>
            {showCB && (<div
                className={foStyles.checkbox}
                onMouseUp={(e) => LabelMouseUp(data.Section, data.RawId, e.button)}
                >
                    {BuildCheckbox(false, data.Section, data.Checkbox, state.Checkbox)}
                </div>
            )}
            <div
                className={labelClasses}
                onMouseUp={(e) => LabelMouseUp(data.Section, data.RawId, e.button)}
                >
                    {data.Label}
            </div>
            {showPO && (<div
                className={foStyles.checkbox}
                >
                    <img src={GetPopoutArrow(state)} />
                </div>)}
                
                {
                // <Button
                //     className={foStyles.popoutBtn}
                //     src={GetPopoutArrow(state)}
                //     id={data.Id}
                //     focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
                //     onMouseUpCapture={(e) => LabelMouseUp(data.Section, data.RawId, e.button)}
                //     variant="icon">
                //         <div className={labelClasses}>{data.Label}</div>
                // </Button>
                }

        </div>
    );
}

function GetFoldoutArrow(state : FoldoutState) : string
{
    if (state.IsOpen)
    {
        return `coui://ui-mods/images/icon_FoldoutOpen.svg`;
    }
    return `coui://ui-mods/images/icon_FoldoutClose.svg`;
}

function GetPopoutArrow(state : FOMainEntryState) : string
{
    if (state.Popout?.IsOpen)
    {
        return `coui://ui-mods/images/icon_PopoutOpen.svg`;
    }
    return `coui://ui-mods/images/icon_PopoutClose.svg`;
}