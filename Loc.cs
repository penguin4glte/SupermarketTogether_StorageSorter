using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StorageSorter
{
    /// <summary>
    /// Message localization.
    /// Built-in languages: en, ja. Extra languages (or overrides) can be added as
    /// "&lt;plugin folder&gt;/lang/&lt;code&gt;.txt" files with "key = value" lines.
    /// </summary>
    internal static class Loc
    {
        public const string Auto = "Auto";
        public const string Fallback = "en";

        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["host_only"] = "Only the host can sort the storage.",
                    ["no_shelves"] = "No storage shelves found.",
                    ["aborted"] = "Storage sort aborted (check the BepInEx log).",
                    ["already_sorted"] = "Storage is already sorted.",
                    ["error"] = "An error occurred while sorting the storage (check the BepInEx log).",
                    ["sorted"] = "Storage sorted{1}: {0} slot(s) updated{2}",
                    ["sorted.category"] = " (category shelves first)",
                    ["sorted.merged"] = ", boxes {0} → {1}",
                    ["lost"] = "Warning: {0} box(es) could not be placed. Check the BepInEx log.",
                },
                ["ja"] = new Dictionary<string, string>
                {
                    ["host_only"] = "倉庫の並び替えはホストのみ実行できます。",
                    ["no_shelves"] = "倉庫の棚が見つかりません。",
                    ["aborted"] = "倉庫の並び替えを中止しました（BepInExのログを確認してください）。",
                    ["already_sorted"] = "倉庫はすでに整理されています。",
                    ["error"] = "倉庫の並び替え中にエラーが発生しました（BepInExのログを確認してください）。",
                    ["sorted"] = "倉庫を並び替えました{1}: {0} スロット更新{2}",
                    ["sorted.category"] = "（カテゴリ棚優先）",
                    ["sorted.merged"] = "、箱 {0}→{1}",
                    ["lost"] = "注意: {0} 箱を配置できませんでした。BepInExのログを確認してください。",
                },
            };

        /// <summary>Language codes that have a table (built-in or from lang/*.txt).</summary>
        public static IEnumerable<string> Available => Tables.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

        /// <summary>Loads lang/*.txt next to the plugin DLL. Files starting with "_" are ignored (templates).</summary>
        public static void LoadExternal(string pluginDir)
        {
            var dir = Path.Combine(pluginDir, "lang");
            if (!Directory.Exists(dir)) return;
            foreach (var file in Directory.GetFiles(dir, "*.txt"))
            {
                var code = Path.GetFileNameWithoutExtension(file);
                if (code.StartsWith("_") || code.Length == 0) continue;
                try
                {
                    if (!Tables.TryGetValue(code, out var table))
                        Tables[code] = table = new Dictionary<string, string>();
                    int n = 0;
                    foreach (var raw in File.ReadAllLines(file, Encoding.UTF8))
                    {
                        var line = raw.Trim();
                        if (line.Length == 0 || line.StartsWith("#")) continue;
                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;
                        var key = line.Substring(0, eq).Trim();
                        // Keep leading spaces of the value only if quoted: key = " (text)"
                        var value = line.Substring(eq + 1).Trim();
                        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                            value = value.Substring(1, value.Length - 2);
                        table[key] = value.Replace("\\n", "\n");
                        n++;
                    }
                    Plugin.Log.LogInfo($"Loaded language file '{code}' ({n} entries).");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"Could not read language file {file}: {ex.Message}");
                }
            }
        }

        /// <summary>The language actually used right now.</summary>
        public static string Current
        {
            get
            {
                var setting = Plugin.Language?.Value ?? Auto;
                if (!string.Equals(setting, Auto, StringComparison.OrdinalIgnoreCase) && Tables.ContainsKey(setting))
                    return setting;
                return GameIsJapanese() ? "ja" : Fallback;
            }
        }

        /// <summary>Returns the message for <paramref name="key"/> in the current language (falls back to English, then the key).</summary>
        public static string T(string key, params object[] args)
        {
            string text = null;
            if (Tables.TryGetValue(Current, out var table)) table.TryGetValue(key, out text);
            if (text == null) Tables[Fallback].TryGetValue(key, out text);
            if (text == null) return key;
            if (args == null || args.Length == 0) return text;
            try { return string.Format(text, args); }
            catch (FormatException) { return text; }
        }

        /// <summary>
        /// The game keeps its own language only inside LocalizationManager, so look at the loaded strings:
        /// any hiragana/katakana means the game is running in Japanese (Chinese text has no kana).
        /// </summary>
        private static bool GameIsJapanese()
        {
            var lm = LocalizationManager.instance;
            if (lm == null || lm.LocalizationDictionary == null || lm.LocalizationDictionary.Count == 0)
                return UnityEngine.Application.systemLanguage == UnityEngine.SystemLanguage.Japanese;
            int checkedCount = 0;
            foreach (var v in lm.LocalizationDictionary.Values)
            {
                if (v != null && v.Any(c => c >= '぀' && c <= 'ヿ')) return true;
                if (++checkedCount >= 300) break;
            }
            return false;
        }
    }
}
