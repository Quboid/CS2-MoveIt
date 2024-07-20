import foStyles from "./foldout.module.scss";
import { FoldoutCBData, FOTitleCBData, FOEntryCBData } from "./foData";
import { FoldoutCBState, FOEntryCBState } from "./foState";
import { FOEntryRow, FOTitleRow } from "./foRow";

export function Foldout(data: FoldoutCBData, state: FoldoutCBState) : JSX.Element
{
    var title = FOTitleRow(data.Title, state);

    var result : JSX.Element[] = [];

    if (state.IsOpen)
    {
        for (var i = 0; i < data.Entries.length; i++)
        {
            result.push(FOEntryRow(data.Entries[i], state.Entries[i]));
        }

        return (
            <div className={foStyles.dropdownContainer}>
                {title}
                <div className={foStyles.dropdownEntries}>{result}</div>
            </div>
        );
    }

    return (
        <div className={foStyles.dropdownContainer}>
            {title}
        </div>
    );
}
