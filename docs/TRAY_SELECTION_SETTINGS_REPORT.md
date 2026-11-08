# 0.5.0 托盘、选区与设置修订报告

日期：2026-11-04。按用户提供的两张照片修复 UI-01~04，不操作用户桌面。

默认双击和重复启动保持托盘驻留。后台宿主使用没有 MainForm 的 ApplicationContext；宿主还显式拒绝显示，删除旧启动面板控件。托盘左键截图、右键完整功能菜单；仅显式 `--settings` 或菜单打开设置。设置最小化后再次请求会恢复已有窗口。退出路径清理全局键、托盘与贴图。未实际运行完整托盘应用，以上由启动策略、隐藏上下文及代码构建验证，不是 GUI 已验收声明。

窗口识别与编辑统一使用 SelectionAppearance：默认外部压暗 70%，设置可调 45–85%；蓝色外边、白色内边和黑色外衬保证对比。八个白底蓝边控制点更明确。主工具条优先放在选区下方；下方只够一条时，属性条独立放到选区上方。底图、导出和可编辑工程不包含遮罩和界面。

设置参照 [1Capture 官方高级技巧](https://1capture.com/docs/advanced-tips) 的 macOS Settings 图片（left_double_click.jpg），不是工具条营销图。六个顶部图标分区：常规、截图、快捷键、工具栏、OCR 与取色、关于。采用浅灰标题区、圆角白色分组、蓝色输入值和开关。原有热键录入、冲突回滚与用户级启动项保存流程保留；工具条编辑嵌入同一设置窗口。公开参考图片保存在 gitignored 本地参考目录，不作为运行资源打包。

自动化记录为 **110 PASS / 0 FAIL**，包括：

- 默认/后台/重复启动与显式设置分别路由；隐藏控制窗体不显示，ApplicationContext 无 MainForm，退出释放。
- 自绘托盘图标从自有原生句柄复制后仍有效；不创建 NotifyIcon 或改系统托盘进行测试。
- 45/70/85% 遮罩外部像素、内部原始像素、前后选区 alpha 对齐，边框与控制点像素。
- 旧 JSON 缺少遮罩设置迁移到 70%；新值保存读回，原配置不被待保存设置修改。
- 主工具条优先下方、属性条不与主条重叠；原有负原点、窄显示器换行、裁剪/马赛克一致性回归。
- 六个设置分区、录入、清除、恢复、保存、工具配置和开关值；正常/紧凑尺寸、隐藏生产控件绘制。

测试命令：`build.ps1 -Test`。明细：`evidence/automated/results.csv`。初轮发现设置构造期间顶部标签尚未全部初始化就触发布局（105 PASS / 5 FAIL），已修复并重跑。离屏渲染还修复了隐藏子控件不绘制及 GDI TextRenderer 忽略 Graphics 位移的问题；预览用实际控件状态和生产绘制，原生隐藏文本使用控件真实值合成。预览明确标注 OFFSCREEN SYNTHETIC，不能视作屏幕实拍或 GUI 运行证据。

离屏证据：

- `evidence/automated/in-place-overlay-synthetic.png`
- `evidence/automated/settings-0-offscreen.png` 至 `settings-5-offscreen.png`
- `evidence/automated/settings-compact-offscreen.png`
- `evidence/automated/tray-icon-offscreen.png`

发布：`dist/0.5.0` 内的 EXE、便携 ZIP、源码 ZIP、配置及 SHA-256。便携内容和校验和由 verify-package.ps1 检查；源码 ZIP 解压后独立构建并执行完整自动化，日志保存为 `evidence/automated/package-verification-0.5.0.txt`。

未运行：实际托盘与多进程交互、可见窗口层级、真实窗口识别/框选、鼠标拖动、焦点恢复、真实键盘热键和混合 DPI。AC-01~30 全项仍为 NOT RUN，Gate A/B 未通过。文件编辑器、旧贴图再编辑和贴图管理仍有旧 UI；用户级安装包与完整 1Capture 功能尚未全部交付。
