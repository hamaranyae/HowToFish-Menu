using BepInEx.Configuration;
using HarmonyLib;

namespace TrickshotMenu
{
    [HarmonyPatch(typeof(LocalCasino), "GetRouletteColorFromBall")]
    internal static class RoulettePatch
    {
        [HarmonyPostfix]
        private static void ForceWin(ref BetColor __result)
        {
            try
            {
                ConfigEntry<bool> opt = TrickshotMenuPlugin.AlwaysWinRoulette;
                if (opt == null || !opt.Value)
                {
                    return;
                }
                CasinoManager c = CasinoManager.Instance;
                if (c == null || !CasinoManager.HasPlacedBet)
                {
                    return;
                }
                if (Server.Instance == null || !Server.Instance.IsServerInitialized)
                {
                    return;
                }
                int betColor = TrickshotMenuPlugin.ReadRouletteBetColor();
                if (betColor >= 0 && betColor <= 2)
                {
                    __result = (BetColor)betColor;
                }
            }
            catch
            {
                // never break the hosting logic
            }
        }
    }
}