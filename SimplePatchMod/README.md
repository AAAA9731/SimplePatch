# SimplePatchMod (BepInEx 6, Mono)

补丁集合的 BepInEx 版，包含 `TxLoad`、`DebugCursor`（debug 菜单打开时强制显示鼠标）与 `PicLoad`（`PIC_LOAD <id> <文件.png>`，读取 StreamingAssets/SimplePatch_pic 下的 PNG 供 `PIC` 显示）；各补丁可在 `local.aic.simplepatch.cfg` 的 `[Patches]` 里开关。以下是 `TxLoad` 补丁的说明：


在 evt 的 .cmd 里用 `<<<EOF` 直接定义 tx*.txt 风格的文本。

    TX_LOAD <<<EOF [语言key，省略或*=所有语言]
    &&my_key 单行文本
    /* ___ my_long ___ */
    多行文本第一行
    多行文本第二行
    EOF;
    TX_BOARD my_key        // 或 MSG / 其它任何读取 &&my_key 的地方

- heredoc 内容原样交给游戏自己的 TX.readTexts 解析，语法与 tx*.txt 完全一致。
- TX_LOAD 在事件预读(cache)阶段和执行阶段都会处理；重复执行是幂等的(先清空同名 key 再写入)。
- 切换语言/reloadTx 后会自动重新注入。
- 在 TX_LOAD 之前已经被缓存的文本不会更新。
- 日志里打开 BepInEx/config/local.aic.simplepatch.cfg 的 SelfTest=true 可做启动自检。
