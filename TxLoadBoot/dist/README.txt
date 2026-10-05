TxLoadBoot - 免加载器版 TX_LOAD
用法：把本目录里的 version.dll 和 TxLoad 文件夹复制到游戏根目录（与 AliceInCradle.exe 同级）即可。
- 不修改任何原版文件；删除这两项即还原。
- 可与 BepInEx 共存：不占用 winhttp.dll / doorstop_config.ini，Harmony 已合并并内部化；
  若检测到 BepInEx，会等 BepInEx 启动完成后再注入（避免与其预加载器冲突，先装本补丁再装 BepInEx/Polaris 也能正常启动）；
  若检测到 BIE 版 TxLoadMod 已加载则自动闲置，不会重复打补丁。
- 日志：TxLoad\log.txt、TxLoad\native.log。在 TxLoad 下新建空文件 selftest 可启用启动自检。
- 环境变量 TXLOAD_DISABLE=1 可临时禁用。
命令：TX_LOAD <<<EOF [语言key] ... EOF;（详见 TxLoadMod\README.md）
