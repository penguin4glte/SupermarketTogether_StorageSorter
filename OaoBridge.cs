using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace StorageSorter
{
    /// <summary>
    /// Optional, reflection-only link to OrderAndOrganize's category shelves.
    /// If that mod is missing or its internals change, every shelf is treated as untagged.
    /// </summary>
    internal static class OaoBridge
    {
        public const string OaoGuid = "com.ddqminik.supermarkettogether.orderandorganize";

        private static bool _resolved;
        private static object _manager;
        private static MethodInfo _getCategory;

        public static bool Available
        {
            get { Resolve(); return _manager != null && _getCategory != null; }
        }

        public static int? GetCategory(Data_Container container)
        {
            if (!Available) return null;
            try
            {
                return (int?)_getCategory.Invoke(_manager, new object[] { container });
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("OrderAndOrganize category lookup failed: " + ex.Message);
                return null;
            }
        }

        private static void Resolve()
        {
            // The manager is created during OAO's Awake, so retry until we get it.
            if (_resolved && _manager != null) return;
            _resolved = true;
            try
            {
                if (!Chainloader.PluginInfos.TryGetValue(OaoGuid, out var info) || info.Instance == null)
                    return;
                var field = AccessTools.Field(info.Instance.GetType(), "_categoryShelfManager");
                _manager = field?.GetValue(info.Instance);
                if (_manager == null) return;
                _getCategory = AccessTools.Method(_manager.GetType(), "GetCategory", new[] { typeof(Data_Container) });
                if (_getCategory != null)
                    Plugin.Log.LogInfo("OrderAndOrganize category shelves detected.");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Could not link to OrderAndOrganize: " + ex.Message);
                _manager = null;
            }
        }
    }
}
