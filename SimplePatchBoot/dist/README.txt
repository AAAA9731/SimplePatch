SimplePatchBoot - 免加载器版 SimplePatch（补丁：TxLoad、DebugCursor）
用法：把本目录里的 version.dll 和 SimplePatch 文件夹复制到游戏根目录（与 AliceInCradle.exe 同级）即可。
- 不修改任何原版文件；删除这两项即还原。
- 可与 BepInEx 共存：不占用 winhttp.dll / doorstop_config.ini，Harmony 已合并并内部化；
  若检测到 BepInEx，会等 BepInEx 启动完成后再注入（避免与其预加载器冲突，先装本补丁再装 BepInEx/Polaris 也能正常启动）；
  若检测到 BIE 版 SimplePatchMod 已加载则自动闲置，不会重复打补丁。
- 从旧版 TxLoad 升级：先删除旧的 TxLoad 文件夹。
- 日志：SimplePatch\log.txt、SimplePatch\native.log。在 SimplePatch 下新建空文件 selftest 可启用 TX_LOAD 启动自检。
- 开关单个补丁：在 SimplePatch 下新建 patches.txt，每行写 DebugCursor=false 或 TxLoad=false 即禁用该补丁。
- 环境变量 SIMPLEPATCH_DISABLE=1 可临时禁用。
DebugCursor：debug 菜单打开时强制显示鼠标。
TxLoad 命令：TX_LOAD <<<EOF [语言key] ... EOF;（详见 SimplePatchMod\README.md）
