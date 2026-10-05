# SimplePatch

给《Alice in Cradle》事件脚本增加一条 `TX_LOAD` 命令，可以在 `.cmd` 里直接写 `tx*.txt` 格式的文本：

```
TX_LOAD <<<EOF [语言key，省略或 * 表示所有语言]
&&my_key 单行文本
/* ___ my_long ___ */
多行文本第一行
多行文本第二行
EOF;
TX_BOARD my_key
```

更多说明见 [TxLoadMod/README.md](TxLoadMod/README.md)。

## 两种用法

| | 免加载器版（推荐） | BepInEx 插件版 |
|---|---|---|
| 位置 | `TxLoadBoot/dist/` | `TxLoadMod/` |
| 安装 | 把 `version.dll` 和 `TxLoad` 文件夹复制到游戏根目录（与 `AliceInCradle.exe` 同级） | 作为 BepInEx 6 插件放入 `BepInEx/plugins` |
| 卸载 | 删除这两项 | 删除插件 |

免加载器版不修改任何原版文件，可以和 BepInEx / Polaris 共存：检测到 BepInEx 时会等它启动完成再生效；已加载 BepInEx 插件版时自动闲置，不会重复打补丁。

`TX_LOAD` 的覆盖在整个游戏会话内有效；每次进入游戏（新游戏 / 读档）时会清空并恢复原文本。

日志在 `TxLoad/log.txt`。环境变量 `TXLOAD_DISABLE=1` 可临时禁用。

## 平台

仅支持 Windows 版游戏。

## 构建

- 免加载器版：`TxLoadBoot/native/build.bat` 编译 `version.dll`（需要 Visual Studio 的 C 编译器）；`TxLoadBoot.csproj` 编译托管部分，再用 ILRepack 把 Harmony 合并并内部化为 `TxLoad/TxLoadBoot.dll`。
- 项目引用游戏自带的 DLL，通过 `-p:GameDir=<游戏目录>` 指定。

## 许可证

[MIT](LICENSE)。`TxLoadBoot/lib` 里的 Harmony、MonoMod、Mono.Cecil 同为 MIT 许可，版权归各自作者所有。
