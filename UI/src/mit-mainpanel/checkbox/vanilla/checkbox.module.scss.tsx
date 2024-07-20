// Thanks to Triton Supreme
// https://github.com/AlphaGaming7780/ExtendedRadio/blob/main/UI/game-ui/game/components/statistics-panel/menu/item/statistics-item.module.scss.tsx

import { getModule } from "cs2/modding"

const cssPath$ = "game-ui/game/components/statistics-panel/menu/item/statistics-item.module.scss"

export const StatisticsItemSCSS = {
	locked: getModule(cssPath$, "classes").locked,
	label: getModule(cssPath$, "classes").label,
}
