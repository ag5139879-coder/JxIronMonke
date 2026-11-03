# JxIronMonke

Iron Man style thruster / flight mod for **com.JxStudios.Jxmoddinggame** (Quest + MelonLoader).

## Current Status
- Base flight/thrusters from original IronMonke
- **New**: Procedural improved gloves (red + glowing yellow thruster pads)
- **New**: World-space HUD (Speed / Thruster status / Energy)

## Features
- [x] Basic hand thrusters
- [x] Improved glove visuals (procedural for now)
- [x] World-space HUD
- [ ] Real 3D glove/suit models (replace procedural ones)
- [ ] Rockets + lock-on (next)

## Structure
```
/
├── src/
│   └── IronMonkeMod.cs     ← Main mod code (gloves + HUD)
├── Plugins/                ← Put built .dll here
├── Assets/                 ← Future real models/textures
├── BUILD.md                ← How to build & test
└── README.md
```

## Quick Start
1. Read `BUILD.md`
2. Build the mod from `src/IronMonkeMod.cs`
3. Place the DLL in the game’s MelonLoader Mods folder
4. Test on Quest

**Important**: You will probably need to update the hand/head transform names inside `FindPlayerTransforms()` to match the exact names used by Jxmoddinggame.

## Next Steps
- Replace the simple cube gloves with real low-poly models
- Hook the real thruster input from the original mod
- Add rockets

## Credits
- Original IronMonke / MelonLoader port by buzzbb
- Extended for Jxmoddinggame
