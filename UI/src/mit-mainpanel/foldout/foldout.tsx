import foStyles from "./foldout.module.scss";
import { FoldoutData } from "./foData";
import { FoldoutState } from "./foState";
import { FOEntryRow, FOTitleRow } from "./foRow";

export function Foldout(data: FoldoutData, state: FoldoutState) : JSX.Element
{
    var title = FOTitleRow(data.Title, state);

    var result : JSX.Element[] = [];

    if (state.IsOpen && state.Entries.length > 0)
    {
        for (var i = 0; i < data.Entries.length; i++)
        {
            result.push(FOEntryRow(data.Entries[i], state.Entries[i]));
        }

        return (
            <div className={foStyles.container}>
                {title}
                <div className={foStyles.entries}>{result}</div>
            </div>
        );
    }

    return (
        <div className={foStyles.container}>
            {title}
        </div>
    );
}
