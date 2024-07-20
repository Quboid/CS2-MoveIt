import { ModRegistrar } from "cs2/modding";
import { MoveItButton } from "mit-button/moveit-button";
import { MIT_MainPanel } from "mit-mainpanel/mainpanel";
import { MIT_DebugPanel } from "mit-debugpanel/debugPanel";
import { MIT_RebindConfirm } from "mit-rebindmkey/rebindConfirm";

const register: ModRegistrar = (moduleRegistry) => {
    // While launching game in UI development mode (include --uiDeveloperMode in the launch options)
    // - Access the dev tools by opening localhost:9444 in chrome browser.
    // - You should see a hello world output to the console.
    // - use the useModding() hook to access exposed UI, api and native coherent engine interfaces. 
    // Good luck and have fun!
    moduleRegistry.extend("game-ui/game/components/toolbar/top/toggles.tsx", "PhotoModeToggle", MoveItButton);
    moduleRegistry.append("Game", MIT_MainPanel);
    moduleRegistry.append("Game", MIT_DebugPanel);
    moduleRegistry.append("Menu", MIT_RebindConfirm);

    console.log(moduleRegistry);
}

export default register;