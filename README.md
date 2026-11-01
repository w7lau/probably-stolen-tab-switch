# Tab 切换储存标签 / Storage Tab Cycle

在扩容后的储存区中，按 **Tab** 切换到下一个标签页，最后一页自动循环回第一页。

同时打开多个容器时，先点击要操作的容器窗口，再按 Tab。

无需前置 Mod。在游戏 Mods 菜单中启用后，重启游戏即可使用。

Press **Tab** to switch to the next tab in an expanded storage bay. The last tab automatically wraps back to the first.

When multiple containers are open, click the container you want to use first, then press Tab.

No dependencies. Enable the mod in the game's Mods menu and restart the game.

**[Steam 创意工坊订阅 / Subscribe](https://steamcommunity.com/sharedfiles/filedetails/?id=3831400376)** · **[安装包 / Releases](https://github.com/w7lau/probably-stolen-tab-switch/releases)**

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

## 兼容性

- 编译依据：Steam build **26337366**。游戏日志的 Mod 兼容版本字符串为 **0.46D**，manifest 使用该值，它与商店公告版本号不同。
- 其他 Mod 或玩家将 Tab 另绑为其他操作时，可能同时响应；游戏更新后可能需要适配。

## 创意工坊

创意工坊的内容文件夹使用构建后的 `package/StorageTabCycle`，不要上传源码项目、工具目录或 ZIP。
App ID 为 **4348910**。可按[官方指南](https://github.com/dragonpan2/probably-stolen-modding/blob/main/ModdingGuide.md#steam-workshop)使用 SteamCMD 发布。

## 许可

本项目原创代码使用 [MIT License](LICENSE)。游戏及其依赖的知识产权属于各自权利人。
