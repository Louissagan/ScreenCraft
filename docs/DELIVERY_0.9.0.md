# ScreenCraft 0.9.0 交付记录

日期：2026-11-05。阶段交付：日常截图、标注、贴图和用户级安装开发预览；不是最终 M5 发布。

## 文件

发行目录：dist/0.9.0。

- ScreenCraft.exe 与同目录 ScreenCraft.exe.config：免安装程序。
- ScreenCraft-Portable-win-x64.zip：带 portable.flag 的便携包，配置保存在自身 Data 中。
- ScreenCraft-Setup-PerUser-win-x64.msi：当前用户安装包，默认 LocalAppData/Programs/ScreenCraft。
- ScreenCraft-Source.zip：源代码、测试、打包脚本、许可证和测试报告。
- SHA256SUMS.txt：发行文件校验和。

试用时先从旧版托盘菜单退出旧版，再运行新版，避免单实例机制仍由旧进程处理操作。本轮没有替用户退出、安装或启动实际应用。

## 本轮结果

- 工作目录完整自动化：203 PASS / 0 FAIL，新增 32 项。
- 最终源码 ZIP 独立解压：完整 203 项再次通过，Release 和 MSI 独立重建、MSI 数据库检查通过。
- 隔离 MSI 引擎安装、升级、卸载：6 PASS。使用独立测试产品、GUID、HKCU 测试键和工作区目录，没有启动应用或修改真实 ScreenCraft 自启项。
- 静态源码边界检查、源码 ZIP 的 src/tests/tools 内容哈希、便携包 EXE 一致性和发行 SHA-256 核对通过。
- 原 30 项完整验收：30 NOT RUN；未操作桌面、截图桌面或注入输入。隐藏窗口和合成渲染不替代真实 GUI 验收。

## 本轮范围与证据

已接通全屏/窗口/重复区域、贴图旋转与翻转、隐藏恢复与吸附、外观设置、文件拖出准备、自动保存与可选历史、拼图画板、美化背景、配置导入导出和用户级 MSI。细节、失败与修复、边界见 DAILY_WORKFLOW_0.9.0.md。

主要证据在 docs/evidence/automated：build-test-0.9.0.txt、package-verification-0.9.0.txt、installer-0.9.0/results.csv、installer-artifact.json、daily-results.csv、daily-gallery.html。图库仅含合成图或隐藏控件绘制结果。

真实标准用户令牌、生产安装向导/活动应用卸载、真实多图置顶与拖放、混合 DPI、截图闪烁仍未运行。QR、横向长图、图片水印、任意对象角度旋转等缺口见 KNOWN_LIMITATIONS.md 和 1CAPTURE_PARITY.md。录屏/GIF 后置。Gate A/B 未通过。
