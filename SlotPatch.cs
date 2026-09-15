using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace TrickshotMenu
{
    [HarmonyPatch(typeof(SlotMachineManager), nameof(SlotMachineManager.RollRandom))]
    internal static class SlotPatch
    {
        [HarmonyPrefix]
        private static void ForceRarity(Player roller)
        {
            try
            {
                ConfigEntry<string> opt = TrickshotMenuPlugin.SlotForce;
                if (opt == null)
                {
                    return;
                }
                string mode = opt.Value;
                if (mode == "OFF")
                {
                    SlotMachineManager.SetCheatSkin(null, byte.MaxValue);
                    return;
                }
                Rarity want = mode == "RED" ? Rarity.Rare : Rarity.Legendary;
                Item[] avail = SlotMachine.AvailableItems;
                if (avail == null || avail.Length == 0)
                {
                    SlotMachineManager.SetCheatSkin(null, byte.MaxValue);
                    return;
                }
                Item pick = PickWithRarity(avail, want);
                if (pick == null)
                {
                    SlotMachineManager.SetCheatSkin(null, byte.MaxValue);
                    return;
                }
                byte idx = pick.SkinPreset.GetRandomSkinIndex(want);
                SlotMachineManager.SetCheatSkin(pick, idx);
            }
            catch
            {
                // never break the roll
            }
        }

        private static Item PickWithRarity(Item[] items, Rarity want)
        {
            int count = 0;
            Item chosen = null;
            for (int i = 0; i < items.Length; i++)
            {
                Item it = items[i];
                if (it == null || it.SkinPreset == null || !HasSkin(it.SkinPreset, want))
                {
                    continue;
                }
                count++;
                if (Random.Range(0, count) == 0)
                {
                    chosen = it;
                }
            }
            return chosen;
        }

        private static bool HasSkin(SkinPreset preset, Rarity want)
        {
            for (int i = 0; i < preset.Skins.Count; i++)
            {
                if (preset.Skins[i].Rarity == want)
                {
                    return true;
                }
            }
            return false;
        }
    }
}