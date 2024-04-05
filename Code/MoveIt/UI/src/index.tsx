import { ModRegistrar } from "cs2/modding";
import { MoveItButton } from "moveit-button/moveit-button";

const register: ModRegistrar = (moduleRegistry) => {
    // While launching game in UI development mode (include --uiDeveloperMode in the launch options)
    // - Access the dev tools by opening localhost:9444 in chrome browser.
    // - You should see a hello world output to the console.
    // - use the useModding() hook to access exposed UI, api and native coherent engine interfaces. 
    // Good luck and have fun!
    
    moduleRegistry.extend("game-ui/game/components/toolbar/top/toggles.tsx", "PhotoModeToggle", MoveItButton);

    console.log(moduleRegistry);
}

export default register;