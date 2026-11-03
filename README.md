# JxIronMonke

Iron Man style thruster / flight mod for **com.JxStudios.Jxmoddinggame** (Quest + MelonLoader).

## Current Status
- Base: Existing `IronMonke.dll` (MelonLoader port with red/black gloves + orange thrusters)
- Goal: Better suit/glove visuals + HUD + rockets

## Planned Features
- [x] Basic hand thrusters (existing)
- [ ] Improved glove / partial suit visuals
- [ ] World-space HUD (speed, thruster status, etc.)
- [ ] Rockets + simple lock-on (later)

## Structure
```
/
├── Plugins/          # Built .dll goes here
├── src/              # C# source code
├── Assets/           # Models, materials, textures for better gloves/suit
└── README.md
```

## Requirements
- Quest
- MelonLoader (compatible with the game)
- Game: com.JxStudios.Jxmoddinggame

## Development Notes
This is a Quest (Android / IL2CPP) MelonLoader mod.  
Focus first on improved glove visuals + simple HUD before adding rockets.

## Credits
- Original IronMonke concept / MelonLoader port by buzzbb
- Extended for Jxmoddinggame
