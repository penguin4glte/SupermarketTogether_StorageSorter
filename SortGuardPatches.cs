using HarmonyLib;

namespace StorageSorter
{
    /// <summary>
    /// While a sort is in progress (~0.3s), stop anyone from putting boxes into / taking boxes
    /// out of storage, so the slots we cleared can't be grabbed before we refill them.
    /// Employees that hit this simply wait 1-1.5s and retry (vanilla behaviour for "no free row").
    /// </summary>
    [HarmonyPatch]
    internal static class SortGuardPatches
    {
        // Employee: "which row of this shelf can I drop my box into?" -> none.
        [HarmonyPatch(typeof(NPC_Manager), "GetFreeStorageRow")]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void FreeRowPostfix(ref int __result)
        {
            if (StorageSortRunner.IsSorting) __result = -1;
        }

        // Employee: "which row has a partial box I can merge into?" -> none.
        [HarmonyPatch(typeof(NPC_Manager), "GetStorageRowWithBoxToMerge")]
        [HarmonyPostfix, HarmonyPriority(Priority.Last)]
        private static void MergeRowPostfix(ref int __result)
        {
            if (StorageSortRunner.IsSorting) __result = -1;
        }

        // Host player interacting with a storage slot (take / put box).
        [HarmonyPatch(typeof(Data_Container), "GetStorageBox")]
        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static bool GetStorageBoxPrefix() => !StorageSortRunner.IsSorting;

        // Host player using the ordering tray on a storage slot.
        [HarmonyPatch(typeof(Data_Container), "BoxItemFromStorageTray")]
        [HarmonyPrefix, HarmonyPriority(Priority.First)]
        private static bool TrayPrefix() => !StorageSortRunner.IsSorting;
    }
}
