using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace StorageSorter
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency(OaoBridge.OaoGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "penguin4glte.supermarkettogether.storagesorter";
        public const string PluginName = "StorageSorter";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        internal static ConfigEntry<KeyboardShortcut> SortHotkey;
        internal static ConfigEntry<bool> MergeBoxes;
        internal static ConfigEntry<bool> UseCategoryShelves;
        internal static ConfigEntry<string> Language;

        private Harmony _harmony;
        private float _lastSortTime = -999f;

        private void Awake()
        {
            Log = Logger;

            Loc.LoadExternal(Path.GetDirectoryName(Info.Location));

            // Config descriptions are written once to the .cfg file, so they are bilingual.
            SortHotkey = Config.Bind("General", "SortHotkey", new KeyboardShortcut(KeyCode.F9),
                "Key that sorts the storage (host only).\n倉庫の並び替えを実行するキー（ホストのみ）。");
            MergeBoxes = Config.Bind("General", "MergeBoxes", true,
                "Combine partially filled boxes of the same product up to the per-box limit. The total item count never changes.\n" +
                "同じ商品で中身が中途半端な箱を、1箱の上限まで寄せてまとめます。商品の総数は変わりません。");
            UseCategoryShelves = Config.Bind("General", "UseCategoryShelves", true,
                "Fill shelves that have an OrderAndOrganize category with that category first.\n" +
                "OrderAndOrganize でカテゴリを設定した棚に、そのカテゴリの商品を優先して入れます。");
            Language = Config.Bind("General", "Language", Loc.Auto,
                new ConfigDescription(
                    "Message language. Auto = Japanese if the game is in Japanese, otherwise English. " +
                    "Add more languages as lang/<code>.txt in the plugin folder.\n" +
                    "メッセージの言語。Auto = ゲームが日本語なら日本語、それ以外は英語。",
                    new AcceptableValueList<string>(new[] { Loc.Auto }.Concat(Loc.Available).ToArray())));

            _harmony = new Harmony(PluginGuid);
            _harmony.PatchAll(typeof(SortGuardPatches));

            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Hotkey: {SortHotkey.Value}");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        private void Update()
        {
            if (!SortHotkey.Value.IsDown())
                return;

            // Only react during normal gameplay (mouse captured), not in menus or text fields.
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            if (StorageSortRunner.IsSorting || Time.unscaledTime - _lastSortTime < 2f)
                return;

            if (NPC_Manager.Instance == null || ProductListing.Instance == null || NPC_Manager.Instance.storageOBJ == null)
                return;

            if (!NetworkServer.active)
            {
                Notifier.Show(Loc.T("host_only"));
                return;
            }

            _lastSortTime = Time.unscaledTime;
            StartCoroutine(StorageSortRunner.Run());
        }
    }
}
