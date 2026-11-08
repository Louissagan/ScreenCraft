# 0.3.1：快捷键直接录入修复

2026-11-04。用户实际截图反馈：设置字段只是普通 TextBox，点击后按组合键不能直接设置快捷键。已按该反馈修复；保留原生 Windows 界面。

五个快捷键字段使用 `HotkeyInput`：点击后直接按 Ctrl/Alt/Shift 加一个按键，替换并规范化当前值。单独按修饰键或裸字母不覆盖旧值，有就地提示；Delete/Backspace/“清除”禁用，Esc 恢复进入该字段时的值，Tab/Shift+Tab 保留切栏。OCR 语言仍是普通文字输入。按“保存并应用”才注册并持久化，原有冲突回滚保持有效。

录入自有已注册快捷键时，通过现有 RegisterHotKey 消息窗口的 WM_HOTKEY 路由进录入控件，避免触发截图/OCR，也不需临时释放旧注册。设置关闭后恢复正常动作。没有加入键盘钩子或输入注入。其他程序或系统已截获的组合键可能不能送达此控件，实际系统冲突仍需真实桌面验证。

## 实际验证

- Release 0.3.1 构建通过。
- 回归 **73 PASS / 0 FAIL**，其中新增 9 项：生产 ProcessCmdKey 录入/重复按键/数字，修饰键与裸字母保护，清除/Esc，Tab/特殊键解析，五个隐藏录入控件/OCR 普通输入，真实按钮事件保存/恢复默认，已注册消息路由/不触发动作、不重复注册和冲突保留旧键。
- 这些测试直接调用生产控件的消息处理及按钮事件，并用假注册适配层验证；没有 SendInput、没有显示窗口、没有改剪贴板/真实全局键/启动项，也没有操作用户桌面。真实物理键盘点击录入的端到端验收 **NOT RUN**。
- 证据：`docs/evidence/hotkey-input/tests.log`、`docs/evidence/automated/results.csv`。包完整性、SHA-256 与源码独立构建在 `package-verify.log` 记录。

复现命令：

```powershell
.\build.ps1 -Test
.\publish.ps1 -SkipTests -DistributionDirectory dist/0.3.1
.\tools\verify-package.ps1 -DistributionDirectory dist/0.3.1 -Rebuild
```

取用 `dist/0.3.1/ScreenCraft-Portable-win-x64.zip`；先手动退出托盘旧版，再运行新包。设置标题应显示 `0.3.1.0`。本轮不自动退出或替换用户正在使用的实例。
