using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace StorageTabCycle
{
    public enum ShortcutModifier { None, Control, Shift, Alt }

    public sealed class ModEntry : IMod
    {
        internal static bool Enabled;
        internal static ConfigEntry<KeyCode> TabKey;
        internal static ConfigEntry<KeyCode> ContainerKey;
        internal static ConfigEntry<KeyCode> PreviousTabKey;
        internal static ConfigEntry<KeyCode> PreviousContainerKey;
        internal static ConfigEntry<ShortcutModifier> PreviousTabModifier;
        internal static ConfigEntry<ShortcutModifier> PreviousContainerModifier;
        internal static string SettingsWindowId;
        public void Init(ModManifest manifest)
        {
            var config = ModConfig.For(manifest.ID);
            SettingsWindowId = ModConfigWindow.WINDOW_ID_PREFIX + manifest.ID;
            TabKey = config.DefineEnum("cycle_tab_key", KeyCode.Tab, "快捷键 / Hotkeys",
                () => "切换标签 / Next tab",
                () => "循环切换当前容器或机器窗口的标签。None 表示禁用。\nCycle tabs in the current container or machine window. None disables this shortcut.");
            ContainerKey = config.DefineEnum("cycle_container_key", KeyCode.BackQuote, "快捷键 / Hotkeys",
                () => "切换容器 / Next container",
                () => "循环切换下方主物品栏的容器：关闭时打开，已打开时置前。BackQuote 是 Tab 上方的反引号键。None 表示禁用。\nCycle main-inventory containers: open or bring to front. BackQuote is the key above Tab. None disables this shortcut.");
            PreviousTabKey = config.DefineEnum("previous_tab_key", KeyCode.Tab, "快捷键 / Hotkeys",
                () => "上一个标签：按键 / Previous tab: key",
                () => "配合下方组合键使用，从第一页回到最后一页。None 表示禁用。\nUse with the modifier below. Wraps from the first tab to the last. None disables this shortcut.");
            PreviousTabModifier = config.DefineEnum("previous_tab_modifier", ShortcutModifier.Control, "快捷键 / Hotkeys",
                () => "上一个标签：组合键 / Previous tab: modifier",
                () => "Control = Ctrl；None = 无组合键。左右 Ctrl 均可。\nControl = Ctrl; None = no modifier. Either Ctrl key works.");
            PreviousContainerKey = config.DefineEnum("previous_container_key", KeyCode.BackQuote, "快捷键 / Hotkeys",
                () => "上一个容器：按键 / Previous container: key",
                () => "配合下方组合键使用，从第一个回到最后一个。BackQuote 是 Tab 上方的反引号键。None 表示禁用。\nUse with the modifier below. Wraps from the first container to the last. BackQuote is the key above Tab. None disables this shortcut.");
            PreviousContainerModifier = config.DefineEnum("previous_container_modifier", ShortcutModifier.Control, "快捷键 / Hotkeys",
                () => "上一个容器：组合键 / Previous container: modifier",
                () => "Control = Ctrl；None = 无组合键。左右 Ctrl 均可。\nControl = Ctrl; None = no modifier. Either Ctrl key works.");
            new ModLog(manifest).Log("Storage Tab Cycle " + manifest.ModVersion + " initialized.");
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
            if (!ModEntry.Enabled) return;
            var containerKey = ModEntry.ContainerKey?.Value ?? KeyCode.BackQuote;
            var tabKey = ModEntry.TabKey?.Value ?? KeyCode.Tab;
            bool previousContainer = Pressed(ModEntry.PreviousContainerKey?.Value ?? KeyCode.BackQuote,
                ModEntry.PreviousContainerModifier?.Value ?? ShortcutModifier.Control);
            bool previousTab = Pressed(ModEntry.PreviousTabKey?.Value ?? KeyCode.Tab,
                ModEntry.PreviousTabModifier?.Value ?? ShortcutModifier.Control);
            bool cycleContainer = Pressed(containerKey, ShortcutModifier.None);
            bool cycleTab = Pressed(tabKey, ShortcutModifier.None);
            if ((!cycleContainer && !cycleTab && !previousContainer && !previousTab) || StoreUIManager.IsAnyTyping()
                || KeyBindHelper.SuppressHotkeys)
                return;

            if ((__instance.gameOptionUIManager != null && __instance.gameOptionUIManager.IsOpen)
                || (__instance.escapeUIManager != null && __instance.escapeUIManager.menuPanel != null
                    && __instance.escapeUIManager.menuPanel.activeInHierarchy)) return;

            if ((ModUIManager.Instance != null && ModUIManager.Instance.panel != null
                    && ModUIManager.Instance.panel.activeInHierarchy)
                || (CustomUIManager.Instance != null && ModEntry.SettingsWindowId != null
                    && CustomUIManager.Instance.IsOpen(ModEntry.SettingsWindowId))) return;

            var windows = WindowsHandler.current?.visibleWindows;
            if (windows == null) return;

            if (cycleContainer || previousContainer)
            {
                CycleContainer(previousContainer ? -1 : 1);
                return;
            }

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

            // Keep the shortcut in the foreground item's content window, but do
            // not require an expansion-kit tag: native multi-page items lack it.
            if (focused == null || focused.isLocked) return;
            bool hasOwner = false;
            foreach (var item in focused.parentItems)
            {
                if (item != null && item.contentWindow == focused)
                {
                    hasOwner = true;
                    break;
                }
            }
            if (!hasOwner) return;
            var tabs = FindVisibleTabs(focused.childElement);
            if (tabs == null) return;

            tabs.Select((tabs.selected + (previousTab ? -1 : 1) + tabs.count) % tabs.count, notify: true);
        }

        private static bool Pressed(KeyCode key, ShortcutModifier modifier)
        {
            if (key == KeyCode.None || !Input.GetKeyDown(key)) return false;
            bool control = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            // Exact modifier matching keeps Ctrl+key from also firing the forward action
            // and leaves unrelated combinations such as Steam's Shift+Tab alone.
            return control == (modifier == ShortcutModifier.Control)
                && shift == (modifier == ShortcutModifier.Shift)
                && alt == (modifier == ShortcutModifier.Alt);
        }

        private static MachineryTabs FindVisibleTabs(PixelElement element)
        {
            if (element == null || element.handler == null
                || !element.handler.gameObject.activeInHierarchy) return null;
            if (element is MachineryTabs tabs) return tabs.count > 1 ? tabs : null;
            if (element is MachineryPanel panel) return FindVisibleTabs(panel.content);
            if (element is MachineryPanelRow row)
            {
                foreach (var child in row.Panels)
                {
                    var found = FindVisibleTabs(child);
                    if (found != null) return found;
                }
            }
            else if (element is MachineryStack stack)
            {
                foreach (var child in stack.elements)
                {
                    var found = FindVisibleTabs(child);
                    if (found != null) return found;
                }
            }
            // Traverse UI layout only, never inventory items or other windows.
            return null;
        }

        private static bool CanOpenContainer(GameItem item)
        {
            var window = item?.contentWindow;
            if (window == null || item.modifiedShape == null || !window.canMove || window.isLocked
                || window.handler == null || window.child == null
                || GeneralHelper.IsDisplayOnly(item)) return false;

            // Read the native lock state directly: IsCurrentlyLocked also tests
            // ownership, whereas a customer's sealed crate must be skipped too.
            if (item.IsTag(LockHelper.LOCKED_CONTAINER_TAG)
                && item.GetTagReadonly(LockHelper.LOCK_STATUS).GetBool()) return false;

            // The item must itself be accessible, even when its content is closed.
            return item is GameItemElement element && !element.IsDestroyed()
                && element.handler != null && element.handler.gameObject.activeInHierarchy;
        }

        private static void CycleContainer(int direction)
        {
            var shop = EmporiumEntry.Instance;
            var opener = ItemMouseDoubleClickHandler.current;
            if (shop == null || opener == null || shop.storeInteractables == null
                || !shop.storeInteractables.activeInHierarchy || ItemPinHelper.BlocksClicks) return;

            // Only the main grid beneath the counter. Other shop surfaces and
            // the contents of containers are outside the cycle's scope.
            var root = shop.invElement;
            if (root == null) return;
            var candidates = new List<GameItem>();
            var seen = new HashSet<PixelWindow>();
            foreach (var item in root.childItems)
                if (item != null && item.parentInventory == root
                    && CanOpenContainer(item) && seen.Add(item.contentWindow))
                    candidates.Add(item);
            if (candidates.Count == 0) return;

            // Native rendering places the top-left at (minX, -minY). Sort grid
            // rows top-to-bottom, then columns left-to-right, regardless of size,
            // item creation order, or the position of the opened content window.
            candidates.Sort((a, b) => {
                int row = a.modifiedShape.minY.CompareTo(b.modifiedShape.minY);
                if (row != 0) return row;
                int column = a.modifiedShape.minX.CompareTo(b.modifiedShape.minX);
                return column != 0 ? column : a.uniqueId.CompareTo(b.uniqueId);
            });
            int focusedIndex = -1;
            for (int i = 0; i < candidates.Count; i++)
            {
                var window = candidates[i].contentWindow;
                if (!window.handler.gameObject.activeInHierarchy) continue;
                if (focusedIndex < 0
                    || window.focusStamp > candidates[focusedIndex].contentWindow.focusStamp)
                    focusedIndex = i;
            }

            int nextIndex = focusedIndex < 0
                ? (direction < 0 ? candidates.Count - 1 : 0)
                : (focusedIndex + direction + candidates.Count) % candidates.Count;
            var next = candidates[nextIndex];
            // Native opening restores position/tabs, plays sound and records the
            // normal opened marker. It never unlocks a crate or activates a machine.
            if (next.contentWindow.IsVisible())
                next.contentWindow.ToFront();
            else
                opener.OpenContentAction(next, Input.mousePosition);
        }
    }
}
