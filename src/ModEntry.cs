using HarmonyLib;
using UnityEngine;

namespace StorageTabCycle
{
    public sealed class ModEntry : IMod
    {
        internal static bool Enabled;
        public void Init(ModManifest manifest)
        {
            new ModLog(manifest).Log("Storage Tab Cycle 1.0.0 initialized.");
        }
        public void OnEnable() => Enabled = true;
        public void OnDisable() => Enabled = false;
    }

    // The native loader applies this patch; do not create a second Harmony instance.
    [HarmonyPatch(typeof(StoreUIManager), "Update")]
    internal static class StorageTabHotkey
    {
        private static void Postfix(StoreUIManager __instance)
        {
            if (!ModEntry.Enabled || !Input.GetKeyDown(KeyCode.Tab)
                || StoreUIManager.IsAnyTyping() || KeyBindHelper.SuppressHotkeys)
                return;

            if ((__instance.gameOptionUIManager != null && __instance.gameOptionUIManager.IsOpen)
                || (__instance.escapeUIManager != null && __instance.escapeUIManager.menuPanel != null
                    && __instance.escapeUIManager.menuPanel.activeInHierarchy)) return;

            var windows = WindowsHandler.current?.visibleWindows;
            if (windows == null) return;

            // visibleWindows is insertion-ordered, not focus-ordered. The game's
            // ToFront updates focusStamp even when it does not reorder that list.
            PixelWindow focused = null;
            foreach (var window in windows)
            {
                if (window == null || !window.canMove || window.handler == null
                    || !window.handler.gameObject.activeInHierarchy) continue;
                if (focused == null || window.focusStamp > focused.focusStamp)
                    focused = window;
            }

            // Do not reach through a different foreground container to a storage
            // window behind it. Use the exact native expansion panel structure.
            if (focused == null || focused.isLocked) return;
            bool expandedStorage = false;
            foreach (var item in focused.parentItems)
            {
                if (item != null && item.contentWindow == focused
                    && StorageExpansionHelper.GetExtraTabs(item) > 0)
                {
                    expandedStorage = true;
                    break;
                }
            }
            if (!expandedStorage) return;
            var tabs = (focused.childElement as MachineryPanelRow)
                ?.GetPanel("storage")?.content as MachineryTabs;
            if (tabs == null || tabs.count < 2 || tabs.handler == null
                || !tabs.handler.gameObject.activeInHierarchy) return;

            tabs.Select((tabs.selected + 1) % tabs.count, notify: true);
        }
    }
}
