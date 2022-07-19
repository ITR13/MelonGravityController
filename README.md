Feel free to ask me any questions on discord: ITR#2941

### Dependencies
[VrChatUtilityKit](https://github.com/SleepyVRC/Mods/releases).  
Optional: [UIExpansionKit](https://github.com/SleepyVRC/Mods/releases) and [ActionMenuApi](https://github.com/gompoc/VRChatMods/releases)

### What it does  
Allows you to change the gravity with the press of a button.  
It reads \[Game Root Folder\]\UserData\GravityConfig.json to check for what gravity to set when what key is pressed.  

### How to use (Keybinds)
By default you press ctrl+p to set gravity to 0,0,0 (no gravity), and hold ctrl+o to set the gravity to 0,9.81,0 (float upwards)  
Each is toggled independently, so pressing ctrl+p -> (hold) ctrl+o -> (release) ctrl+o will set the gravity to 0,0,0, and pressing ctrl+p -> (hold) ctrl+o -> ctrl+p will keep it at 0,9.81,0

The config file has the fields:  
**gravity:** What gravity to set. It's possible to set diagonal or sideways gravities too.  
**trigger:** What key to press to toggle this gravity. If this is set to None then this entry is disabled  
**hold:** What key needs to be held for the trigger to activate. If set to None then only the trigger is used.  
**holdToActivate:** If set to true, then the gravity is toggled on when Trigger is held down, and off when Trigger is released

### How to use (UI)
If you have the ActionMenuAPI and the VrcUtilityKit you can use the gravity mod through the action menu  
Open the Action Menu and find "Gravity". On the bottom you can see your current grabity  
On the left side you can increase or decrease gravity in steps of 5. This can be changed in the melon-loader config.
On the right side you can set gravity to 0 or reset it to the default world gravity
