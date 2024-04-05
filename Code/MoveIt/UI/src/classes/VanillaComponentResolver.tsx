// Developed by Klyte45
import { FocusKey, Theme, UniqueFocusKey } from "cs2/bindings";
import { getModule } from "cs2/modding";
// import { DropdownProps, DropdownToggleProps, IconButtonProps, InfoRowProps, InfoSectionProps } from "cs2/ui";
// import { HTMLAttributes } from "react";

// type PropsToggleField = {
//     "value": any,
//     "disabled"?: boolean,
//     "onChange"?: (x: any) => any
// }

// type PropsRadioToggle = {
//     focusKey?: UniqueFocusKey | null
//     checked: boolean
//     disabled?: boolean
//     theme?: Theme | any
//     style?: CSSStyleRule
//     className?: string
// } & HTMLAttributes<any>

// type PropsRadioGroupToggleField = {
//     value: any,
//     groupValue: any,
//     disabled?: boolean,
//     onChange?: (x: any) => any,
//     onToggleSelected?: (x: any) => any,
// } & HTMLAttributes<any>

// type PropsTooltipRow = {

// }
// type PropsTooltip = {
//     tooltip: string | JSX.Element | JSX.Element[]
//     disabled?: boolean
//     theme?: Theme & any
//     direction?: "up" | "down" | "left" | "right"
//     alignment?: "left" | "right" | "center"
//     className?: string
//     children: string | JSX.Element | JSX.Element[]
// }

// type PropsEllipsesTextInput = {
//     "value"?: string,
//     "maxLength"?: number,
//     "theme"?: Theme,
//     "className"?: string
// }

const registryIndex = {
    // RadioToggle: ["game-ui/common/input/toggle/radio-toggle/radio-toggle.tsx", "RadioToggle"],
    // ToggleField: ["game-ui/menu/components/shared/game-options/toggle-field/toggle-field.tsx", "ToggleField"],
    // RadioGroupToggleField: ["game-ui/menu/components/shared/game-options/toggle-field/toggle-field.tsx", "RadioGroupToggleField"],
    // InfoSection: ["game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.tsx", "InfoSection"],
    // InfoRow: ["game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx", "InfoRow"],
    // TooltipRow: ["game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.tsx", "TooltipRow"],
    // ActiveFocusDiv: ["game-ui/common/focus/focus-div.tsx", "ActiveFocusDiv"],
    // PassiveFocusDiv: ["game-ui/common/focus/focus-div.tsx", "PassiveFocusDiv"],
    // themeToggleLine: ["game-ui/game/components/selected-info-panel/selected-info-sections/lines-section/lines-section.module.scss", "classes"],
    FOCUS_DISABLED: ["game-ui/common/focus/focus-key.ts", "FOCUS_DISABLED"],
    FOCUS_AUTO: ["game-ui/common/focus/focus-key.ts", "FOCUS_AUTO"],
    useUniqueFocusKey: ["game-ui/common/focus/focus-key.ts", "useUniqueFocusKey"],
    // Dropdown: ["game-ui/common/input/dropdown/dropdown.tsx", "Dropdown"],
    // themeDropdown: ["game-ui/menu/widgets/dropdown-field/dropdown-field.module.scss", "classes"],
    // DropdownItem: ["game-ui/common/input/dropdown/items/dropdown-item.tsx", "DropdownItem"],
    // DropdownToggle: ["game-ui/common/input/dropdown/dropdown-toggle.tsx", "DropdownToggle"],
    // IconButton: ["game-ui/common/input/button/icon-button.tsx", "IconButton"],
    // themeGamepadToolOptions: ["game-ui/game/components/tool-options/tool-button/tool-button.module.scss", "classes"],
    // Tooltip: ["game-ui/common/tooltip/tooltip.tsx", "Tooltip"],
    // EllipsisTextInput: ['game-ui/common/input/text/ellipsis-text-input/ellipsis-text-input.tsx',"EllipsisTextInput"],
}



export class VanillaComponentResolver {
    public static get instance(): VanillaComponentResolver { return this._instance ??= new VanillaComponentResolver() }
    private static _instance?: VanillaComponentResolver


    private cachedData: Partial<Record<keyof typeof registryIndex, any>> = {}
    private updateCache(entry: keyof typeof registryIndex) {
        const entryData = registryIndex[entry];
        return this.cachedData[entry] = getModule(entryData[0], entryData[1])
    }

    // public get RadioToggle(): (props: PropsRadioToggle) => JSX.Element { return this.cachedData["RadioToggle"] ?? this.updateCache("RadioToggle") }
    // public get ToggleField(): (props: PropsToggleField) => JSX.Element { return this.cachedData["ToggleField"] ?? this.updateCache("ToggleField") }
    // public get RadioGroupToggleField(): (props: PropsRadioGroupToggleField) => JSX.Element { return this.cachedData["RadioGroupToggleField"] ?? this.updateCache("RadioGroupToggleField") }
    // public get InfoSection(): (props: InfoSectionProps & { children: JSX.Element | JSX.Element[] }) => JSX.Element { return this.cachedData["InfoSection"] ?? this.updateCache("InfoSection") }
    // public get InfoRow(): (props: InfoRowProps) => JSX.Element { return this.cachedData["InfoRow"] ?? this.updateCache("InfoRow") }
    // public get TooltipRow(): (props: PropsTooltipRow) => JSX.Element { return this.cachedData["TooltipRow"] ?? this.updateCache("TooltipRow") }
    // public get ActiveFocusDiv(): (props: any) => JSX.Element { return this.cachedData["ActiveFocusDiv"] ?? this.updateCache("ActiveFocusDiv") }
    // public get PassiveFocusDiv(): (props: any) => JSX.Element { return this.cachedData["PassiveFocusDiv"] ?? this.updateCache("PassiveFocusDiv") }
    // public get Dropdown(): (props: DropdownProps) => JSX.Element { return this.cachedData["Dropdown"] ?? this.updateCache("Dropdown") }
    // //public get DropdownItem(): (props: DropdownItemProps<T>) => JSX.Element { return this.cachedData["DropdownItem"] ?? this.updateCache("DropdownItem") }
    // public get DropdownToggle(): (props: DropdownToggleProps) => JSX.Element { return this.cachedData["DropdownToggle"] ?? this.updateCache("DropdownToggle") }
    // public get IconButton(): (props: IconButtonProps) => JSX.Element { return this.cachedData["IconButton"] ?? this.updateCache("IconButton") }
    // public get Tooltip(): (props: PropsTooltip) => JSX.Element { return this.cachedData["Tooltip"] ?? this.updateCache("Tooltip") }
    // public get EllipsisTextInput(): (props: PropsEllipsesTextInput) => JSX.Element { return this.cachedData["EllipsisTextInput"] ?? this.updateCache("EllipsisTextInput") }


    // public get themeToggleLine(): Theme | any { return this.cachedData["themeToggleLine"] ?? this.updateCache("themeToggleLine") }
    // public get themeDropdown(): Theme | any { return this.cachedData["themeDropdown"] ?? this.updateCache("themeDropdown") }
    // public get themeGamepadToolOptions(): Theme | any { return this.cachedData["themeGamepadToolOptions"] ?? this.updateCache("themeGamepadToolOptions") }


    public get FOCUS_DISABLED(): UniqueFocusKey { return this.cachedData["FOCUS_DISABLED"] ?? this.updateCache("FOCUS_DISABLED") }
    public get FOCUS_AUTO(): UniqueFocusKey { return this.cachedData["FOCUS_AUTO"] ?? this.updateCache("FOCUS_AUTO") }
    public get useUniqueFocusKey(): (focusKey: FocusKey, debugName: string) => UniqueFocusKey | null { return this.cachedData["useUniqueFocusKey"] ?? this.updateCache("useUniqueFocusKey") }
} 
