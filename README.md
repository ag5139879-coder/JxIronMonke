# JxIronMonke

Iron Man style thruster / flight mod for **com.JxStudios.Jxmoddinggame** (Quest + MelonLoader / LemonLoader).

## Current Features
- Procedural improved gloves (red + glowing yellow thruster pads)
- World-space HUD (Speed / Thruster status / Energy)

## How to Build (Standalone Quest – Recommended)

You do **not** need Visual Studio. Just use GitHub Actions:

### Step 1 – Upload the required DLLs
1. Go to the `refs` folder in this repository
2. Upload these files:
   - **MelonLoader.dll** (required)
   - `UnityEngine.CoreModule.dll`
   - `UnityEngine.UI.dll`
   - Any other UnityEngine.*.dll the game needs

### Step 2 – Run the build
1. Go to the **Actions** tab
2. Click **Build JxIronMonke**
3. Click **Run workflow** → **Run workflow**
4. Wait for it to finish
5. Download the artifact called `JxIronMonke-Build`
6. Inside you will find `JxIronMonke.dll`

### Step 3 – Install on Quest
Place the DLL here:
```
Android/data/com.JxStudios.Jxmoddinggame/files/Mods/
```

Then fully close and reopen the game.

## Project Structure
```
/
├── src/IronMonkeMod.cs     ← Source code
├── refs/                   ← Upload MelonLoader + Unity DLLs here
├── .github/workflows/      ← Auto-build system
├── Plugins/                ← Final built DLLs
└── Assets/                 ← Future models
```

## Notes
- Hand / Head transform names may need adjusting for Jxmoddinggame
- Check MelonLoader logs if the gloves or HUD do not appear

## Credits
- Original IronMonke concept by buzzbb
- Extended for Jxmoddinggame
