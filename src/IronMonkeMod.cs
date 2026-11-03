using MelonLoader;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.2.1", "Jx")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    public class IronMonkeMod : MelonMod
    {
        public override void OnApplicationStart()
        {
            MelonLogger.Msg("JxIronMonke 1.2.1 started OK");
        }
    }
}
