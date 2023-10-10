# CacheGetClapped

A [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader) mod for [Resonite](https://resonite.com/) 

Smart cache management for Resonite. Detects cached files that have been unused for a specified number of days, and promptly deletes them whenever the game is closed. Also allows a maximum cache size to be specified, automatically cleaning up unused cache files. These numbers can be easily altered to match your needs.

PLEASE UNDERSTAND THAT ANY MOD, PROGRAM, OR APPLICATION THAT DELETES FILES HAS THE RISK OF REMOVING THE WRONG INFORMATION. THOUGH I HAVE TESTED THIS NUMEROUS TIMES, I CANNOT GUARANTEE ON DIFFERENT DEVICES THIS WILL WORK AS INTENDED. USE AT YOUR OWN RISK

## Installation
1. Install [ResoniteModLoader](https://github.com/resonite-modding-group/ResoniteModLoader).
2. Place [CacheGetClapped.dll](https://github.com/dfgHiatus/CacheGetClapped/releases/latest/download/CacheGetClapped.dll) into your `rml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\Resonite\rml_mods` for a default install. You can create it if it's missing, or if you launch the game once with ResoniteModLoader installed it will create the folder for you.
3. Start the game. If you want to verify that the mod is working you can check your Resonite logs.

Note: The first time this mod runs, it may take Resonite extra time to shutdown while it deletes a large amount of old cached files. After this, it should not be an issue. 
