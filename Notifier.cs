using System;
using System.Reflection;

namespace StorageSorter
{
    internal static class Notifier
    {
        private static FieldInfo _cooldownField;
        private static bool _resolved;

        /// <summary>Shows a raw-text in-game notification (the leading backtick skips the game's localization lookup).</summary>
        public static void Show(string message)
        {
            Plugin.Log.LogInfo("[Notify] " + message);
            var canvas = GameCanvas.Instance;
            if (canvas == null) return;
            try
            {
                if (!_resolved)
                {
                    _resolved = true;
                    _cooldownField = typeof(GameCanvas).GetField("inCooldown", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                _cooldownField?.SetValue(canvas, false);
                canvas.CreateCanvasNotification("`" + message);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning("Notification failed: " + ex.Message);
            }
        }
    }
}
