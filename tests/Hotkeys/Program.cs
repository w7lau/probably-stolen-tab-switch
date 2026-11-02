using System;
using System.Collections.Generic;
using System.Reflection;
using StorageTabCycle;
using UnityEngine;

// Run production code against game/UI doubles, not a Unity integration test.
static class Program
{
    static readonly MethodInfo Tick = typeof(ModEntry).Assembly.GetType("StorageTabCycle.StorageTabHotkey")!
        .GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic)!;
    static readonly StoreUIManager UI = new();
    static int checks;
    static void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; }
    static GameItemElement Container(int id, GameInventory root)
    {
        var item = new GameItemElement { uniqueId = id, contentWindow = new(), parentInventory = root };
        item.modifiedShape.minX = id;
        item.contentWindow.parentItems.Add(item);
        item.contentWindow.Hide(); root.childItems.Add(item); return item;
    }
    static void Press(params KeyCode[] keys)
    { Input.Keys = new(keys); Tick.Invoke(null, new object[] { UI }); }
    static void Expect(GameItem item, string label)
    {
        int before = ItemMouseDoubleClickHandler.Calls;
        int expectedCalls = item.contentWindow.IsVisible() ? before : before + 1;
        Press(ModEntry.ContainerKey.Value);
        Check(ItemMouseDoubleClickHandler.Calls == expectedCalls && PixelWindow.Front == item.contentWindow
            && item.contentWindow.handler.gameObject.activeInHierarchy, label);
    }
    static void Blocked(string label)
    {
        int before = ItemMouseDoubleClickHandler.Calls;
        long stamp = PixelWindow.Sequence;
        Press(ModEntry.ContainerKey.Value);
        Check(ItemMouseDoubleClickHandler.Calls == before && PixelWindow.Sequence == stamp, label);
    }
    static void Main()
    {
        var mod = new ModEntry(); mod.Init(new ModManifest()); mod.OnEnable();
        Check(ModConfig.For("storage_tab_cycle").Count == 6, "Forward keys plus reverse keys and modifiers registered under manifest ID");
        Check(ModEntry.TabKey.Value == KeyCode.Tab && ModEntry.ContainerKey.Value == KeyCode.BackQuote,
            "Default shortcuts retained");
        var shop = EmporiumEntry.Instance; var root = shop.invElement;
        var c = Container(30, root); var a = Container(10, root); var b = Container(20, root);
        // Deliberately make IDs and insertion order disagree with visual order.
        a.uniqueId = 900; b.uniqueId = 100; c.uniqueId = 1;
        c.modifiedShape.minX = 0; c.modifiedShape.minY = 3;
        Check(WindowsHandler.current.visibleWindows.Count == 0, "All containers initially closed");
        foreach (var item in new[] { a, b, c, a, b, c }) Expect(item, "Open/cycle/wrap by row then column, independent of IDs");
        b.contentWindow.ToFront(); Expect(c, "Continue from manually focused container");
        b.contentWindow.Hide(); a.contentWindow.ToFront(); Expect(b, "Closed container reopens");
        Expect(c, "Reopening retains order");
        root.childItems.Remove(b); shop.showcaseElement.childItems.Add(b); b.parentInventory = shop.showcaseElement;
        a.contentWindow.ToFront(); Expect(c, "Moving to showcase removes container from cycle");
        shop.showcaseElement.childItems.Remove(b); root.childItems.Add(b); b.parentInventory = root;
        a.contentWindow.ToFront(); Expect(b, "Moving back into main grid restores container");
        b.modifiedShape.minY = 4; a.contentWindow.ToFront(); Expect(c, "Rearranging grid changes cycle immediately");
        Expect(b, "Container moved to lower row follows upper rows");
        b.modifiedShape.minY = 0;
        foreach (var other in new[] { shop.frontInvinvElement, shop.backInvinvElement, shop.showcaseElement,
            shop.hiddenElement, shop.docInvElement, shop.trashInvElement, shop.drainInvElement,
            shop.faucetElement, shop.cassettePlayerElement, shop.vendingMachineElement, shop.vendingFountainElement })
        {
            var outside = Container(15, other); outside.contentWindow.Show();
            a.contentWindow.ToFront(); Expect(b, "Visible container on another shop surface is excluded");
        }
        root.childItems.Remove(c); c.parentInventory = null;
        Expect(a, "Removed container excluded despite open window");

        var nested = Container(11, new GameInventory()); nested.contentWindow.Show();
        var hidden = Container(12, root); hidden.handler.gameObject.activeInHierarchy = false;
        var locked = Container(13, root); locked.contentWindow.isLocked = true;
        var crate = Container(14, root); crate.sealedType = true; crate.lockState.Value = true;
        var display = Container(15, root); display.displayOnly = true;
        var destroyed = Container(16, root); destroyed.destroyed = true;
        var empty = Container(17, root); empty.contentWindow.child = null;
        var fixedWindow = Container(18, root); fixedWindow.contentWindow.canMove = false;
        var stale = Container(19, root); stale.parentInventory = new GameInventory();
        a.contentWindow.ToFront(); Expect(b, "Skip nested/hidden/locked/sealed/display/destroyed/stale items");
        Check(crate.lockState.Value && !crate.contentWindow.handler.gameObject.activeInHierarchy, "Sealed crate stays locked and closed");
        crate.lockState.Value = false; a.contentWindow.ToFront(); Expect(crate, "Unlocked crate immediately joins cycle");
        crate.lockState.Value = true; crate.contentWindow.Hide(); a.contentWindow.ToFront();
        Expect(b, "Relocked crate excluded");

        var previousShop = shop; EmporiumEntry.Instance = new(); shop = EmporiumEntry.Instance;
        shop.invElement.childItems.Add(crate); crate.parentInventory = shop.invElement;
        Blocked("Only sealed crate: no native open call");
        crate.lockState.Value = false; crate.contentWindow.isLocked = true;
        Blocked("Window locked with cleared tag: no native open call");
        crate.contentWindow.isLocked = false; Expect(crate, "Single closed unlocked container opens");
        shop.invElement.childItems.Clear(); Blocked("Empty shop: no native open call");
        EmporiumEntry.Instance = previousShop; shop = previousShop; crate.parentInventory = null;

        StoreUIManager.Typing = true; Blocked("Typing"); StoreUIManager.Typing = false;
        KeyBindHelper.SuppressHotkeys = true; Blocked("Key capture"); KeyBindHelper.SuppressHotkeys = false;
        UI.gameOptionUIManager.IsOpen = true; Blocked("Options"); UI.gameOptionUIManager.IsOpen = false;
        UI.escapeUIManager.menuPanel.activeInHierarchy = true; Blocked("Pause"); UI.escapeUIManager.menuPanel.activeInHierarchy = false;
        ItemPinHelper.BlocksClicks = true; Blocked("Pin mode"); ItemPinHelper.BlocksClicks = false;
        ModUIManager.Instance.panel.activeInHierarchy = true; Blocked("Mod management panel"); ModUIManager.Instance.panel.activeInHierarchy = false;
        CustomUIManager.Instance.OpenId = ModEntry.SettingsWindowId; Blocked("Native mod settings window"); CustomUIManager.Instance.OpenId = null;
        shop.storeInteractables.activeInHierarchy = false; Blocked("Outside/unavailable shop"); shop.storeInteractables.activeInHierarchy = true;
        new ModEntry().OnDisable(); Blocked("Mod disabled"); new ModEntry().OnEnable();
        int calls = ItemMouseDoubleClickHandler.Calls; Press(); Check(calls == ItemMouseDoubleClickHandler.Calls, "No key-down means no repeat");
        var opener = ItemMouseDoubleClickHandler.current; ItemMouseDoubleClickHandler.current = null;
        Blocked("Native opener unavailable"); ItemMouseDoubleClickHandler.current = opener;

        a.contentWindow.ToFront(); a.extraTabs = 2;
        var tabs = new MachineryTabs { count = 3, selected = 2 };
        a.contentWindow.childElement = new MachineryPanelRow { panel = new Panel { content = tabs } };
        Press(KeyCode.Tab); Check(tabs.selected == 0 && tabs.notify, "Tab wraps with native notification");
        Press(KeyCode.Tab); Check(tabs.selected == 1, "Tab advances");
        Expect(b, "Container cycle with expanded storage"); Check(tabs.selected == 1, "Selected tab preserved");
        Press(KeyCode.Tab); Check(tabs.selected == 1, "Background storage unaffected");
        Press(KeyCode.BackQuote, KeyCode.Tab);
        Check(PixelWindow.Front == a.contentWindow && tabs.selected == 1, "Simultaneous keys only switch container");

        ModEntry.ContainerKey.Set(KeyCode.F6); ModEntry.TabKey.Set(KeyCode.F7);
        a.contentWindow.ToFront(); long stamp = a.contentWindow.focusStamp;
        Press(KeyCode.BackQuote, KeyCode.Tab);
        Check(a.contentWindow.focusStamp == stamp && tabs.selected == 1, "Old keys stop working after rebind");
        calls = ItemMouseDoubleClickHandler.Calls;
        Press(KeyCode.F6);
        Check(PixelWindow.Front == b.contentWindow && ItemMouseDoubleClickHandler.Calls == calls,
            "Custom container key brings open container forward without reopening");
        b.contentWindow.Hide(); a.contentWindow.ToFront(); Expect(b, "Custom container key opens closed container");
        a.contentWindow.ToFront(); Press(KeyCode.F7);
        Check(tabs.selected == 2, "Custom tab key works immediately");
        mod.Init(new ModManifest());
        Check(ModEntry.ContainerKey.Value == KeyCode.F6 && ModEntry.TabKey.Value == KeyCode.F7,
            "Previously saved native config values used at initialization (config double)");
        ModEntry.ContainerKey.Set(KeyCode.None); ModEntry.TabKey.Set(KeyCode.None);
        stamp = PixelWindow.Sequence; Press(KeyCode.F6, KeyCode.F7, KeyCode.None);
        Check(PixelWindow.Sequence == stamp && tabs.selected == 2, "None disables each shortcut");
        ModEntry.ContainerKey.Set(KeyCode.F6); ModEntry.TabKey.Set(KeyCode.F6);
        a.contentWindow.ToFront(); Press(KeyCode.F6);
        Check(PixelWindow.Front == b.contentWindow && tabs.selected == 2, "Same-key conflict performs one container action");
        CheckNativeContainerTabs();
        CheckReverseShortcuts();
        Console.WriteLine($"PASS: {checks} hotkey behavior checks (game/UI doubles).");
    }

    static void CheckReverseShortcuts()
    {
        ModEntry.TabKey.Set(KeyCode.Tab); ModEntry.ContainerKey.Set(KeyCode.BackQuote);
        Check(ModEntry.PreviousTabKey.Value == KeyCode.Tab && ModEntry.PreviousContainerKey.Value == KeyCode.BackQuote
            && ModEntry.PreviousTabModifier.Value == ShortcutModifier.Control
            && ModEntry.PreviousContainerModifier.Value == ShortcutModifier.Control, "Default reverse bindings use Ctrl");
        WindowsHandler.current.visibleWindows.Clear();
        EmporiumEntry.Instance = new(); var root = EmporiumEntry.Instance.invElement;
        var c = Container(30, root); var a = Container(10, root); var b = Container(20, root);
        var locked = Container(25, root); locked.sealedType = true; locked.lockState.Value = true;
        Press(KeyCode.LeftControl, KeyCode.BackQuote);
        Check(PixelWindow.Front == c.contentWindow, "Reverse with none open starts at final container");
        Press(KeyCode.RightControl, KeyCode.BackQuote);
        Check(PixelWindow.Front == b.contentWindow && !locked.contentWindow.IsVisible(), "Right Ctrl reverses and skips locked crate");
        Press(KeyCode.LeftControl, KeyCode.BackQuote); Check(PixelWindow.Front == a.contentWindow, "Reverse continues to first");
        int calls = ItemMouseDoubleClickHandler.Calls;
        Press(KeyCode.LeftControl, KeyCode.BackQuote);
        Check(PixelWindow.Front == c.contentWindow && ItemMouseDoubleClickHandler.Calls == calls, "Reverse wrap brings open last container forward without reopening");
        b.contentWindow.ToFront(); Press(KeyCode.LeftControl, KeyCode.BackQuote);
        Check(PixelWindow.Front == a.contentWindow, "Reverse follows manual focus");
        b.modifiedShape.minY = 1; a.contentWindow.ToFront(); Press(KeyCode.LeftControl, KeyCode.BackQuote);
        Check(PixelWindow.Front == b.contentWindow, "Reverse respects rearranged visual positions");
        b.modifiedShape.minY = 0;
        var tabs = new MachineryTabs { count = 3, selected = 0 }; a.contentWindow.childElement = tabs; a.contentWindow.ToFront();
        Press(KeyCode.LeftControl, KeyCode.Tab); Check(tabs.selected == 2 && tabs.notify, "Previous tab wraps first to last once");
        Press(KeyCode.RightControl, KeyCode.Tab); Check(tabs.selected == 1, "Right Ctrl selects previous tab once");
        Press(KeyCode.Tab); Check(tabs.selected == 2, "Unmodified Tab still advances");
        Press(KeyCode.LeftShift, KeyCode.Tab); Check(tabs.selected == 2, "Steam Shift+Tab does not cycle tabs");
        Press(KeyCode.LeftControl, KeyCode.LeftShift, KeyCode.Tab); Check(tabs.selected == 2, "Extra modifiers do not accidentally match");
        Input.Held.Add(KeyCode.LeftControl); Press(KeyCode.Tab); Check(tabs.selected == 1, "Ctrl held before Tab works");
        Press(); Check(tabs.selected == 1, "Holding modifier alone does not repeat"); Input.Held.Clear();
        Press(KeyCode.LeftControl, KeyCode.BackQuote, KeyCode.Tab);
        Check(PixelWindow.Front == c.contentWindow && tabs.selected == 1, "Simultaneous reverse bindings perform only container action");
        a.contentWindow.ToFront();
        foreach (var mode in new[]{"typing", "capture", "options", "pause", "mod-panel", "mod-settings", "disabled"})
        {
            StoreUIManager.Typing = mode == "typing"; KeyBindHelper.SuppressHotkeys = mode == "capture";
            UI.gameOptionUIManager.IsOpen = mode == "options"; UI.escapeUIManager.menuPanel.activeInHierarchy = mode == "pause";
            ModUIManager.Instance.panel.activeInHierarchy = mode == "mod-panel";
            CustomUIManager.Instance.OpenId = mode == "mod-settings" ? ModEntry.SettingsWindowId : null;
            ModEntry.Enabled = mode != "disabled"; long stamp = PixelWindow.Sequence;
            Press(KeyCode.LeftControl, KeyCode.BackQuote); Press(KeyCode.LeftControl, KeyCode.Tab);
            Check(stamp == PixelWindow.Sequence && tabs.selected == 1, "Reverse guard: " + mode);
        }
        StoreUIManager.Typing = false; KeyBindHelper.SuppressHotkeys = false; UI.gameOptionUIManager.IsOpen = false;
        UI.escapeUIManager.menuPanel.activeInHierarchy = false; ModUIManager.Instance.panel.activeInHierarchy = false;
        CustomUIManager.Instance.OpenId = null; ModEntry.Enabled = true;
        ModEntry.PreviousTabKey.Set(KeyCode.F7); ModEntry.PreviousTabModifier.Set(ShortcutModifier.Alt);
        Press(KeyCode.LeftControl, KeyCode.Tab); Check(tabs.selected == 1, "Old reverse binding stops after rebind");
        Press(KeyCode.RightAlt, KeyCode.F7); Check(tabs.selected == 0, "Custom reverse key and right Alt modifier work");
        ModEntry.PreviousContainerKey.Set(KeyCode.F6); ModEntry.PreviousContainerModifier.Set(ShortcutModifier.Shift);
        Press(KeyCode.RightShift, KeyCode.F6); Check(PixelWindow.Front == c.contentWindow, "Custom reverse container binding works");
        new ModEntry().Init(new ModManifest());
        Check(ModEntry.PreviousTabKey.Value == KeyCode.F7 && ModEntry.PreviousTabModifier.Value == ShortcutModifier.Alt
            && ModEntry.PreviousContainerKey.Value == KeyCode.F6 && ModEntry.PreviousContainerModifier.Value == ShortcutModifier.Shift,
            "Reverse key and modifier config values survive initialization");
        ModEntry.PreviousContainerKey.Set(KeyCode.None); ModEntry.PreviousTabKey.Set(KeyCode.None);
        long disabledStamp = PixelWindow.Sequence; Press(KeyCode.RightShift, KeyCode.F6); Press(KeyCode.RightAlt, KeyCode.F7);
        Check(PixelWindow.Sequence == disabledStamp && tabs.selected == 0, "None disables reverse shortcuts");
        ModEntry.PreviousTabKey.Set(KeyCode.Tab); ModEntry.PreviousTabModifier.Set(ShortcutModifier.None); a.contentWindow.ToFront();
        Press(KeyCode.Tab); Check(tabs.selected == 2, "Identical forward/reverse key performs one reverse action");
        ModEntry.PreviousContainerKey.Set(KeyCode.BackQuote); ModEntry.PreviousContainerModifier.Set(ShortcutModifier.Control);
        root.childItems.Clear(); root.childItems.Add(a); Press(KeyCode.LeftControl, KeyCode.BackQuote);
        Check(PixelWindow.Front == a.contentWindow, "Single-container reverse stays on same window");
        root.childItems.Clear(); calls = ItemMouseDoubleClickHandler.Calls; Press(KeyCode.LeftControl, KeyCode.BackQuote);
        Check(ItemMouseDoubleClickHandler.Calls == calls, "Empty reverse cycle does nothing");
    }

    static void CheckNativeContainerTabs()
    {
        ModEntry.TabKey.Set(KeyCode.Tab); ModEntry.ContainerKey.Set(KeyCode.BackQuote);
        var container = Container(99, new GameInventory());
        var window = container.contentWindow; window.Show();
        var tabs = new MachineryTabs { count = 2, selected = 0 };
        var row = new MachineryPanelRow { panel = new Panel { content = tabs } };
        window.childElement = row;
        Check(container.extraTabs == 0, "Native tabs have no expansion marker");
        Press(KeyCode.Tab);
        Check(tabs.selected == 1 && tabs.notify, "Hypertech storage native tabs advance without expansion kit");
        Press(KeyCode.Tab); Check(tabs.selected == 0, "Hypertech native tabs wrap");

        row.panel.role = "settings";
        Press(KeyCode.Tab); Check(tabs.selected == 1, "Auto butcher settings tabs are not restricted to storage role");
        Press(KeyCode.Tab); Check(tabs.selected == 0, "Auto butcher pages wrap");

        window.childElement = tabs;
        Press(KeyCode.Tab); Check(tabs.selected == 1, "Tabs attached directly to content window");
        window.childElement = new MachineryStack { elements = { new Panel { content = row } } };
        Press(KeyCode.Tab); Check(tabs.selected == 0, "Tabs inside native panel/stack layouts");

        var hiddenTabs = new MachineryTabs { count = 2 };
        hiddenTabs.handler.gameObject.activeInHierarchy = false;
        row.Panels.Insert(0, new Panel { content = hiddenTabs });
        Press(KeyCode.Tab);
        Check(tabs.selected == 1 && hiddenTabs.selected == 0, "Hidden tab group skipped");
        tabs.handler.gameObject.activeInHierarchy = false;
        Press(KeyCode.Tab); Check(tabs.selected == 1, "No hidden tab group is changed");
        tabs.handler.gameObject.activeInHierarchy = true;
        window.childElement.handler.gameObject.activeInHierarchy = false;
        Press(KeyCode.Tab); Check(tabs.selected == 1, "Hidden ancestor blocks traversal");
        window.childElement = row;
        row.Panels.RemoveAt(0);
        tabs.count = 1; tabs.selected = 0; tabs.notify = false;
        Press(KeyCode.Tab); Check(tabs.selected == 0 && !tabs.notify, "Single page does nothing");
        tabs.count = 2;
        window.isLocked = true; Press(KeyCode.Tab);
        Check(tabs.selected == 0, "Locked content window is skipped"); window.isLocked = false;
        var dialog = new PixelWindow { childElement = new MachineryTabs { count = 2 } }; dialog.Show();
        Press(KeyCode.Tab);
        Check(tabs.selected == 0 && ((MachineryTabs)dialog.childElement).selected == 0,
            "Non-container foreground dialog blocks access to tabs behind it");
        dialog.Hide();
        var plain = Container(100, new GameInventory()); plain.contentWindow.Show();
        Press(KeyCode.Tab); Check(tabs.selected == 0, "Container without tabs blocks background switching");
        plain.contentWindow.Hide(); window.ToFront();
        var nestedItem = Container(101, new GameInventory());
        nestedItem.contentWindow.childElement = new MachineryTabs { count = 2 };
        window.childElement = nestedItem.parentInventory;
        Press(KeyCode.Tab);
        Check(((MachineryTabs)nestedItem.contentWindow.childElement).selected == 0,
            "Traversal never enters inventory items or their separate content windows");
        window.childElement = row;
        StoreUIManager.Typing = true; Press(KeyCode.Tab); StoreUIManager.Typing = false;
        Check(tabs.selected == 0, "Native tab shortcuts suppressed during typing");
        ModEntry.TabKey.Set(KeyCode.F7); Press(KeyCode.Tab);
        Check(tabs.selected == 0, "Old key does not affect native tabs after rebind");
        Press(KeyCode.F7); Check(tabs.selected == 1, "Custom key affects native tabs");
        window.Hide();
    }
}

namespace HarmonyLib
{
    [AttributeUsage(AttributeTargets.Class)] public sealed class HarmonyPatch : Attribute
    { public HarmonyPatch(Type type, string method) { } }
}
namespace UnityEngine
{
    public enum KeyCode { None, Tab, BackQuote, F6, F7, LeftControl, RightControl, LeftShift, RightShift, LeftAlt, RightAlt }
    public struct Vector2 { }
    public static class Input
    {
        public static HashSet<KeyCode> Keys = new();
        public static HashSet<KeyCode> Held = new();
        public static Vector2 mousePosition;
        public static bool GetKeyDown(KeyCode key) => Keys.Contains(key);
        public static bool GetKey(KeyCode key) => Keys.Contains(key) || Held.Contains(key);
    }
    public class GameObject { public bool activeInHierarchy = true; }
}
public interface IMod { void Init(ModManifest manifest); void OnEnable(); void OnDisable(); }
public class ModManifest { public string ID = "storage_tab_cycle", ModVersion = "test"; }
public class ConfigEntry<T>
{
    public T Value { get; private set; }
    public ConfigEntry(T value) { Value = value; }
    public void Set(T value) { Value = value; }
}
public class ModConfig
{
    static readonly Dictionary<string, ModConfig> Configs = new();
    readonly Dictionary<string, object> entries = new();
    public int Count => entries.Count;
    public static ModConfig For(string id)
    { if (!Configs.ContainsKey(id)) Configs[id] = new(); return Configs[id]; }
    public ConfigEntry<T> DefineEnum<T>(string key, T value, string section, Func<string> label,
        Func<string> tooltip = null, bool requiresRestart = false) where T : struct, Enum
    {
        if (!entries.ContainsKey(key)) entries[key] = new ConfigEntry<T>(value);
        return (ConfigEntry<T>)entries[key];
    }
}
public static class ModConfigWindow { public const string WINDOW_ID_PREFIX = "mod_config:"; }
public class ModUIManager
{ public static ModUIManager Instance = new(); public GameObject panel = new() { activeInHierarchy = false }; }
public class CustomUIManager
{
    public static CustomUIManager Instance = new(); public string OpenId;
    public bool IsOpen(string id) => id == OpenId;
}
public class ModLog { public ModLog(ModManifest manifest) { } public void Log(string value) { } }
public class StoreUIManager
{
    public static bool Typing;
    public static bool IsAnyTyping() => Typing;
    public Options gameOptionUIManager = new();
    public Escape escapeUIManager = new();
}
public class Options { public bool IsOpen; }
public class Escape { public GameObject menuPanel = new() { activeInHierarchy = false }; }
public static class KeyBindHelper { public static bool SuppressHotkeys; }
public static class ItemPinHelper { public static bool BlocksClicks; }
public class WindowsHandler { public static WindowsHandler current = new(); public List<PixelWindow> visibleWindows = new(); }
public class Handler { public GameObject gameObject = new(); }
public class PixelWindow
{
    static long sequence;
    public static long Sequence => sequence;
    public static PixelWindow Front;
    public bool canMove = true, isLocked;
    public Handler handler = new();
    public object child = new();
    public PixelElement childElement;
    public List<GameItem> parentItems = new();
    public long focusStamp;
    public bool IsVisible() => handler.gameObject.activeInHierarchy;
    public void ToFront() { focusStamp = ++sequence; Front = this; }
    public void Hide() { handler.gameObject.activeInHierarchy = false; WindowsHandler.current.visibleWindows.Remove(this); }
    public void Show()
    {
        if (isLocked) return;
        handler.gameObject.activeInHierarchy = true;
        if (!WindowsHandler.current.visibleWindows.Contains(this)) WindowsHandler.current.visibleWindows.Add(this);
        ToFront();
    }
}
public class TagState { public bool Value; public bool GetBool() => Value; }
public class GridShape { public int minX, minY; }
public class GameItem
{
    public PixelWindow contentWindow;
    public GridShape modifiedShape = new();
    public int extraTabs, uniqueId;
    public GameInventory parentInventory;
    public bool sealedType, displayOnly;
    public TagState lockState = new();
    public bool IsTag(string tag) => tag == LockHelper.LOCKED_CONTAINER_TAG && sealedType;
    public TagState GetTagReadonly(string tag) => tag == LockHelper.LOCK_STATUS ? lockState : throw new Exception(tag);
}
public class GameItemElement : GameItem
{
    public Handler handler = new(); public bool destroyed;
    public bool IsDestroyed() => destroyed;
}
public class PixelElement { public Handler handler = new(); }
public class GameInventory : PixelElement { public List<GameItem> childItems = new(); }
public class EmporiumEntry
{
    public static EmporiumEntry Instance = new();
    public GameObject storeInteractables = new();
    public GameInventory invElement = new(), backInvinvElement = new(), frontInvinvElement = new(),
        showcaseElement = new(), hiddenElement = new(), docInvElement = new(), trashInvElement = new(),
        drainInvElement = new(), faucetElement = new(), cassettePlayerElement = new(),
        vendingMachineElement = new(), vendingFountainElement = new();
}
public static class GeneralHelper { public static bool IsDisplayOnly(GameItem item) => item.displayOnly; }
public static class LockHelper { public const string LOCKED_CONTAINER_TAG = "LOCKED_CONTAINER_TAG", LOCK_STATUS = "LOCK_STATUS"; }
public class ItemMouseDoubleClickHandler
{
    public static ItemMouseDoubleClickHandler current = new(); public static int Calls;
    public void OpenContentAction(GameItem item, Vector2 mouse) { Calls++; item.contentWindow.Show(); }
}
public static class StorageExpansionHelper { public static int GetExtraTabs(GameItem item) => item.extraTabs; }
public class MachineryPanelRow : PixelElement
{
    public List<MachineryPanel> Panels = new();
    public MachineryPanel panel { get => Panels.Count == 0 ? null : Panels[0]; set { Panels.Clear(); Panels.Add(value); } }
    public MachineryPanel GetPanel(string name) => Panels.Find(p => p.role == name);
}
public class MachineryPanel : PixelElement { public PixelElement content; public string role = "storage"; }
public class Panel : MachineryPanel { }
public class MachineryStack : PixelElement { public List<PixelElement> elements = new(); }
public class MachineryTabs : PixelElement
{
    public int count, selected; public bool notify;
    public void Select(int index, bool notify) { selected = index; this.notify = notify; }
}
