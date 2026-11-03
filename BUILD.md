# Build & Test Instructions

## Current Features in Source
- Procedural improved gloves (red with yellow thruster pads)
- Thruster pads light up while thrusting
- World-space HUD showing:
  - Speed
  - Thruster status (ONLINE / STANDBY)
  - Simple energy bar

## How to use this code

### Option A – You have a MelonLoader development setup
1. Create a new MelonLoader mod project targeting the game.
2. Copy `src/IronMonkeMod.cs` into your project.
3. Build the DLL.
4. Place the DLL in the game’s MelonLoader `Mods` (or `Plugins`) folder.
5. Test on Quest.

### Option B – You only have the old DLL
You will need to either:
- Decompile the original IronMonke.dll and merge the new glove + HUD code into it, **or**
- Replace the old mod with this new one (recommended while developing).

## Important for Quest
- Hand / Head transform names may be different in `com.JxStudios.Jxmoddinggame`.
- Open the game once with MelonLoader logging enabled and look for the real transform names.
- Update the `FindPlayerTransforms()` method with the correct names.

## Next Improvements
1. Replace the procedural cubes with real low-poly glove models (put FBX or AssetBundles in `/Assets`).
2. Hook the real thruster input from the original mod instead of the placeholder joystick check.
3. Add rocket firing later.

## Testing Checklist
- [ ] Gloves appear on both hands
- [ ] Thruster pads change color when thrusting
- [ ] HUD stays in front of the player and faces you
- [ ] No major performance drop on Quest
