# Reference DLLs

Upload the following DLLs into this `refs` folder so GitHub Actions can build the mod:

**Required:**
- `MelonLoader.dll`

**Strongly recommended:**
- `UnityEngine.CoreModule.dll`
- `UnityEngine.dll` (or the individual modules the game uses)
- `UnityEngine.UI.dll` (needed for the HUD)

### How to get them (standalone Quest)
1. After LemonLoader / MelonLoader has patched and you have run the game at least once, the managed assemblies are usually available.
2. You can also extract them from a PC MelonLoader install of a similar Unity version.
3. Upload the `.dll` files directly into this `refs` folder on GitHub.

Once the DLLs are here, go to the **Actions** tab → select **Build JxIronMonke** → **Run workflow**.
