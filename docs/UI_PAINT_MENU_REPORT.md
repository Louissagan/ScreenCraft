# 0.5.1 设置重绘与右键菜单修复

日期：2026-11-04。问题来自用户本轮提供的真实 0.5.0 设置截图与托盘菜单照片。未访问或操作用户桌面，未关闭现有进程或贴图。

UI-PAINT-001：顶部设置标签出现图标和文字残留，开关/按钮周边有未清除的画面。新增回归在旧实现中实际得到 **0 PASS / 2 FAIL**：一是在已绘制标签上更换图标和选中状态后与干净绘制不一致；二是对隐藏控件发送 WM_PRINTCLIENT，在不同初始背景上的结果不一致。失败日志保存在 `evidence/automated/ui-paint-baseline-0.5.0.txt`。

修复将自绘按钮和开关设为完整背景、UserPaint、AllPaintingInWmPaint、OptimizedDoubleBuffer 与 ResizeRedraw；每次绘制覆盖整个控件区域。自绘文字统一使用 GDI+，减少 GDI TextRenderer 与 GDI+ 的混绘。两个原失败用例在修复后通过；100 次图标/选中/开关状态切换的隐藏原生绘制也通过。没有以离屏示意图替代真实窗口验收。

设置改用 Windows 原生 Sizable 窗口，标准右上角最小化、最大化与关闭按钮替换 Mac 红黄绿点；去掉自绘标题栏拖动和窗口 Region。说明文本按内容计算高度，常规页不再出现由空白块撑出的滚动条；六个标签加宽，完整显示 OCR 与取色。

托盘菜单采用统一圆角白底、细线图标、蓝色悬停、功能分组、右对齐热键和红色退出项，顶部显示版本号。菜单打开时刷新当前配置热键，显示热键不额外注册键。贴图、截图“更多”及颜色菜单复用同一 Renderer。保留 ToolStrip 原生键盘导航、点击、勾选和子菜单，不重写系统输入。

完整自动化：**118 PASS / 0 FAIL**。本次新增 8 项，覆盖：

- 两个修复前失败的像素/隐藏 HWND 重绘用例。
- 100 次状态切换、100/125/150/200% 尺寸原生控件绘制；这是测试尺寸，不是切换用户显示器 DPI。
- Windows 原生标题栏配置、常规页正文尺寸、设置保持隐藏。
- 托盘 10 个动作全部由生产菜单项回调到测试替身，热键标签刷新且没有实际注册。
- 生产 ContextMenuStrip 的隐藏绘制，验证文本/图标及选中行存在；保留勾选和子菜单 Renderer。
- 既有标注、贴图、OCR、取色、长截图、热键与导出回归。

证据：`evidence/automated/build-test-0.5.1.txt`、`results.csv`、`ui-paint-fixed-0.5.1.txt`、`tab-native-print.png`、`tabs-native-dpi-sizes.png`、`tray-menu-native-offscreen.png`、`settings-windows-client-offscreen.png`。设置预览只包括客户区，明确排除 Windows 原生标题栏，不能冒充窗口全屏实拍。

构建：`build.ps1 -Test`。仅运行绘制回归可在此命令前设置环境变量 `SCREENCRAFT_UI_ONLY=1`，该模式写 `ui-paint-results.csv`，不覆盖完整 `results.csv`；完成后清除此变量。发布到 `dist/0.5.1`，便携/源码/EXE/配置与校验和齐备；verify-package.ps1 解压源码包独立构建和执行全部测试，日志为 `evidence/automated/package-verification-0.5.1.txt`。

GUI 边界：实际可见设置页、系统托盘菜单开合、真实窗口按钮交互、键盘导航及混合 DPI 全流程均 NOT RUN。AC-01~30 的完整人工验收仍为 NOT RUN，Gate A/B 未通过；这次修复不表示完整 1Capture 功能已交付。
