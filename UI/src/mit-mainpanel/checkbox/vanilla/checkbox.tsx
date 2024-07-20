// Thanks to Triton Supreme
// https://github.com/AlphaGaming7780/ExtendedRadio/blob/main/UI/game-ui/common/input/toggle/checkbox/checkbox.tsx

import { getModule } from "cs2/modding"

const cbPath$ = "game-ui/common/input/toggle/checkbox/checkbox.tsx"

export type PropsCheckbox = {
    checked?: boolean,
    disabled?: boolean,
    theme?: any,
    className?: string,
    [key: string]: any;
}

const CheckboxModule = getModule(cbPath$, "Checkbox")

export function Checkbox(propsCheckbox: PropsCheckbox): JSX.Element {
    return <CheckboxModule {...propsCheckbox} />
}
