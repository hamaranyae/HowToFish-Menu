using HarmonyLib;

namespace TrickshotMenu
{
    [HarmonyPatch(typeof(Attachments), "get_Damage")]
    internal static class DamagePatch
    {
        private static bool Prefix(Attachments __instance, ref int __result)
        {
            if (TrickshotMenuPlugin.DamageActive && __instance != null && __instance.Weapon != null)
            {
                __result = TrickshotMenuPlugin.OverrideDamage;
                return false;
            }
            return true;
        }
    }
}