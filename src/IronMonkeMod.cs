using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(IronMonke.IronMonkeMod), "JxIronMonke", "1.2.0", "Jx")]
[assembly: MelonGame(null, null)]

namespace IronMonke
{
    public class IronMonkeMod : MelonMod
    {
        private bool hasLogged;

        public override void OnApplicationStart()
        {
            MelonLogger.Msg("========================================");
            MelonLogger.Msg("JxIronMonke v1.2.0 loaded successfully!");
            MelonLogger.Msg("========================================");
        }

        public override void OnUpdate()
        {
            // Keep completely empty for now so nothing can crash during update
            if (!hasLogged)
            {
                hasLogged = true;
                MelonLogger.Msg("JxIronMonke OnUpdate is running");
            }
        }
    }
}
