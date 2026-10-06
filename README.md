# SimplePatch

《Alice in Cradle》的小补丁集合，统一入口、按补丁单独开关。

## 补丁列表

| 名称 | 作用 |
|---|---|
| `TxLoad` | 给事件脚本增加 `TX_LOAD` 命令，可在 `.cmd` 里直接写 `tx*.txt` 格式的文本（见下） |
| `DebugCursor` | 游戏内 debug 菜单（含事件行调试器）打开时强制显示鼠标，不用再先开菜单 UI |

### TxLoad

给事件脚本增加一条 `TX_LOAD` 命令，可以在 `.cmd` 里直接写 `tx*.txt` 格式的文本：

```
TX_LOAD <<<EOF [语言key，省略或 * 表示所有语言]
&&my_key 单行文本
/* ___ my_long ___ */
多行文本第一行
多行文本第二行
EOF;
TX_BOARD my_key
```

更多说明见 [SimplePatchMod/README.md](SimplePatchMod/README.md)。

## 两种用法

| | 免加载器版（推荐） | BepInEx 插件版 |
|---|---|---|
| 位置 | `SimplePatchBoot/dist/` | `SimplePatchMod/` |
| 安装 | 把 `version.dll` 和 `SimplePatch` 文件夹复制到游戏根目录（与 `AliceInCradle.exe` 同级） | 作为 BepInEx 6 插件放入 `BepInEx/plugins` |
| 卸载 | 删除这两项 | 删除插件 |

免加载器版不修改任何原版文件，可以和 BepInEx / Polaris 共存：检测到 BepInEx 时会等它启动完成再生效；已加载 BepInEx 插件版时自动闲置，不会重复打补丁。

`TX_LOAD` 的覆盖在整个游戏会话内有效；每次进入游戏（新游戏 / 读档）时会清空并恢复原文本。

日志在 `SimplePatch/log.txt`。环境变量 `SIMPLEPATCH_DISABLE=1` 可临时整体禁用。

## 开关单个补丁

- 免加载器版：在 `SimplePatch/` 下新建 `patches.txt`，每行 `补丁名=false` 即禁用该补丁（如 `DebugCursor=false`），缺省全部启用。
- BepInEx 版：`BepInEx/config/local.aic.simplepatch.cfg` 的 `[Patches]` 段。

## 从旧版（TxLoad）升级

项目原名 TxLoad。升级时删除游戏目录里旧的 `TxLoad` 文件夹，再按上面的方式复制新的 `version.dll` 和 `SimplePatch` 文件夹；BepInEx 版请删除旧的 `TxLoadMod.dll`。

## 添加新补丁

1. 在 `Shared/Patches/` 新建一个类实现 `IPatch`（`Name` / `Description` / `Install(Harmony)`），用 `h.CreateClassProcessor(typeof(X)).Patch()` 打自己的 `[HarmonyPatch]` 类（参考 `DebugCursorPatch.cs`）。
2. 在 `Shared/PatchHost.cs` 的 `Patches` 数组里登记。
两种发行形态自动包含，并自动获得开关。

## 平台

仅支持 Windows 版游戏。

## 构建

- 免加载器版：`SimplePatchBoot/native/build.bat` 编译 `version.dll`（需要 Visual Studio 的 C 编译器）；`SimplePatchBoot.csproj` 编译托管部分，再用 ILRepack 把 Harmony 等合并并内部化为 `SimplePatch/SimplePatchBoot.dll`。
- 项目引用游戏自带的 DLL，通过 `-p:GameDir=<游戏目录>` 指定。

## 许可证

[MIT](LICENSE)。`SimplePatchBoot/lib` 里的 Harmony、MonoMod、Mono.Cecil 同为 MIT 许可，版权归各自作者所有。
