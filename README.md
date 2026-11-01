# Tab 切换储存标签 / Storage Tab Cycle

《深空当铺：可能是偷的》正式版原生 Mod：打开扩容储存区，按 **Tab** 切换到下一标签，最后一页循环回第一页。

Press **Tab** to cycle tabs in the focused expanded storage bay in **Probably Stolen**. The last tab wraps to the first. Uses the game's native mod loader; no dependencies.

**[Steam 创意工坊订阅 / Subscribe](https://steamcommunity.com/sharedfiles/filedetails/?id=3831400376)** · **[安装包 / Releases](https://github.com/w7lau/probably-stolen-tab-switch/releases)**

## 功能

- 每次按键切换一页，长按不连翻。
- 多个窗口同时打开时，先点击要操作的容器使其置前，Tab 只作用于该窗口。前台窗口不是扩容储存区时，不切换后台窗口。
- 文字输入（含重命名标签及控制台）、重绑快捷键、设置及暂停菜单中不触发。
- 调用游戏原生切页方法，保留音效和回调。不新增物品、储存格或自定义存档数据，不移动物品。
- 无前置 Mod，无额外能力权限。

## 安装

从 [Releases](https://github.com/w7lau/probably-stolen-tab-switch/releases) 下载 `StorageTabCycle-1.0.0.zip`，解压到游戏根目录的 `Mods` 文件夹。

```text
Probably Stolen/
  Mods/
    StorageTabCycle/
      manifest.xml
      StorageTabCycle.dll
```

在游戏 Mods 菜单启用 **Tab 切换储存标签 / Storage Tab Cycle** 并重启游戏。卸载时停用并重启，再删除 Mod 文件夹即可。

Extract the release ZIP into the game's `Mods` directory, enable the mod in the Mods menu, then restart. Click the intended expanded storage window before pressing Tab.

## 构建

需要合法安装的正式版游戏和 .NET SDK；源码及发布包均不分发游戏程序集或 Harmony。

```powershell
.\build.ps1 -GameDir 'D:\SteamLibrary\steamapps\common\Probably Stolen'
```

生成 `package/StorageTabCycle` 和 `dist/StorageTabCycle-1.0.0.zip`，不会自动安装或启用。项目以 `netstandard2.1` 为目标。

离线调用游戏自带扫描器（需要 .NET 9 SDK）：

```powershell
dotnet run --project tools/Scan -p:GameDir='D:\SteamLibrary\steamapps\common\Probably Stolen' -- package/StorageTabCycle 'D:\SteamLibrary\steamapps\common\Probably Stolen\Probably Stolen_Data\Managed'
```

## 验证与兼容

- 编译依据：Steam build **26337366**。游戏日志的 Mod 兼容版本字符串为 **0.46D**，manifest 使用该值，它与商店公告版本号不同。
- Release 编译：0 警告、0 错误。
- 游戏原版 ModScanner 离线检查：无拒绝项、无额外能力权限。该检查不包括线上信任列表或游戏内激活流程。
- **2026-11-01：用户安装启用后，确认游戏内核心切页功能可用。**
- 完整边界场景尚未逐项验收：3/5 页循环、手动选页后切换、多窗口焦点、普通容器置前、重命名/控制台/暂停/设置、关闭重开及读档。
- 其他 Mod 或玩家将 Tab 另绑为其他操作时，可能同时响应；游戏更新后可能需要适配。

## 创意工坊

创意工坊的内容文件夹使用构建后的 `package/StorageTabCycle`，不要上传源码项目、工具目录或 ZIP。
App ID 为 **4348910**。可按[官方指南](https://github.com/dragonpan2/probably-stolen-modding/blob/main/ModdingGuide.md#steam-workshop)使用 SteamCMD 发布；当前维护者使用独立工具调用 Steam 官方 UGC 接口上传。

## 许可

本项目原创代码使用 [MIT License](LICENSE)。游戏及其依赖的知识产权属于各自权利人。
