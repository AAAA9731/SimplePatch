# SimplePatch

《Alice in Cradle》的小补丁集合：免加载器或 BepInEx 二选一，每个补丁可单独开关。仅支持 Windows 版游戏。

| 补丁 | 作用 |
|---|---|
| `TxLoad` | 事件脚本新增 `TX_LOAD`，在 `.cmd` 里直接写 `tx*.txt` 格式的文本 |
| `DebugCursor` | debug 菜单（含事件行调试器）打开时强制显示鼠标 |
| `PicLoad` | 事件脚本新增 `PIC_LOAD`，把 `SimplePatch_pic` 里的 PNG 注册成可用 `PIC` 显示的图片 |

## 安装

| | 免加载器版（推荐） | BepInEx 插件版 |
|---|---|---|
| 文件 | `SimplePatchBoot/dist/` 里的 `version.dll` 和 `SimplePatch` 文件夹 | `SimplePatchMod.dll` |
| 安装 | 复制到游戏根目录（与 `AliceInCradle.exe` 同级） | 放入 `BepInEx/plugins` |
| 卸载 | 删除这两项 | 删除插件 |

免加载器版不改任何原版文件，可与 BepInEx / Polaris 共存；已加载 BepInEx 插件版时自动闲置。日志在 `SimplePatch/log.txt`，环境变量 `SIMPLEPATCH_DISABLE=1` 可整体禁用。

**开关补丁：** 免加载器版在 `SimplePatch/patches.txt` 里写 `DebugCursor=false`（每行一个）；BepInEx 版改 `BepInEx/config/local.aic.simplepatch.cfg` 的 `[Patches]`。

**从旧版 TxLoad 升级：** 先删除旧的 `TxLoad` 文件夹（BepInEx 版删除旧的 `TxLoadMod.dll`），再按上表安装。

## TxLoad

```
TX_LOAD <<<EOF [语言key，省略或 * 表示所有语言]
&&my_key 单行文本
/* ___ my_long ___ */
多行文本第一行
多行文本第二行
EOF;
TX_BOARD my_key
```

覆盖在整个游戏会话内有效，进入游戏（新游戏 / 读档）时恢复原文本。详见 [SimplePatchMod/README.md](SimplePatchMod/README.md)。

## PicLoad

```
PIC_LOAD <id> <文件名[.png]>
PIC &1 <id>
```

- 图片放在 `AliceInCradle_Data/StreamingAssets/SimplePatch_pic/`；`<id>` 不能与游戏自带图片重名；文件改动后再次执行会重新加载。
- 之后照常用 `PIC` 系列命令。游戏的 `PIC` 只支持固定几档缩放：`h` 标志（0.5 倍）、`PIC_MVA <层> ZOOM2/3/4`（2/3/4 倍）；位置用 `PIC_MV <层> x y 0`（画面中心为原点，y 向上）。
- **可视化生成：** 用 [VPicker](https://github.com/AAAA9731/VPicker) 0.3 的「自定义图片」标签页，拖动摆放、选缩放档位，直接生成以上指令；也可把 PNG 拖进去导入并重命名。

## 添加新补丁

在 `Shared/Patches/` 新建实现 `IPatch` 的类（参考 `DebugCursorPatch.cs`），并在 `Shared/PatchHost.cs` 的 `Patches` 里登记；两种发行形态自动包含并自动获得开关。

## 构建

`SimplePatchBoot/native/build.bat` 编译 `version.dll`（需 Visual Studio C 编译器）；`SimplePatchBoot.csproj` 编译托管部分，再用 ILRepack 合并并内部化 Harmony 等为 `SimplePatch/SimplePatchBoot.dll`。项目引用游戏自带的 DLL，用 `-p:GameDir=<游戏目录>` 指定。

## 许可证

[MIT](LICENSE)。`SimplePatchBoot/lib` 里的 Harmony、MonoMod、Mono.Cecil 同为 MIT 许可，版权归各自作者所有。
