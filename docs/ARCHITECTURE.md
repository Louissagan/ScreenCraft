# 架构

- `src/Upstream/Stackshot/Capture/RegionPicker*`：冻结虚拟桌面、窗口识别、框选及选区调整。
- `src/Upstream/Stackshot/Editor/Canvas.cs`：沿用对象选择、拖动、控制点缩放、裁剪、撤销重做。
- `Shapes.cs` + `src/App/AnnotationStyles.cs`：统一生产渲染器；新增圆角/填充/线型、画笔、云线及序号说明。
- `AnnotationCanvas.cs`：标注命令、默认样式、验证、对象和可编辑工程的桥接。
- `AnnotationDocument.cs`：`.sccraft` ZIP（`source.png` + `document.json`），显式保留源图及标注；像素/节点/文字上限、版本验证、原子保存。PNG 导出不包含源图和对象元数据。
- `CraftEditor.cs` + `AnnotationInspector.cs`：原生中文工具栏和属性控件。文本输入态禁用工具单字母键。
- `PinWindow.cs`：保留原来的每像素透明浮窗和光标锚定缩放；一图一 HWND，独立文档与窗口状态。重新编辑后更新原窗口。
- `PinState.cs`：不覆盖已有 ID 的注册表、每窗状态、错位摆放、屏幕迁移几何、5% 透明度步进。`Dispose` 和 `Close` 都清理注册及位图资源。
- `TransactionalHotkeys.cs`：官方 RegisterHotKey；保留旧注册直到新组合注册成功。测试使用假适配层，不占用用户实际快捷键。
- `Preferences.cs` / `SettingsDialog.cs`：便携 Data 或每用户配置、损坏恢复、显式 HKCU 自启。测试不写真实启动项。
- `Ocr.cs` + `LocalOcrService.cs`：Windows 本地 OCR，线程局部语言/取消/单调时钟期限；MTA 任务持有源图副本，重叠分带识别、去重和中文空格规范化。`OcrResultWindow.cs` 提供语言选择、结果编辑、重试和取消，关闭不会让迟到结果访问已释放控件。
- `ColorTools.cs`：HEX/RGB/HSL、冻结像素采样与独立取色模式；放大镜读取 DIB 物理像素，编辑器取源图。负坐标/明确显示器局部 DPI 转换仅有纯算法验证，未宣称混合 DPI 桌面通过。
- `ScrollCapture.cs`：沿用重叠匹配、固定头尾、浮动元素及亚像素匹配。`ScrollFrames.cs` 将屏幕读取隔离为 `IScrollFrameSource`，生产使用 GDI，测试使用合成帧，同一处理器处理缓冲区、取消、失败/上限提示。达到上限不自动继续未捕获页面；分段 PNG 是对当前已捕获图片的导出。

新宿主不加载原上游的联网更新、报告、OAuth、录屏或角色模块。异常日志不保存原图、OCR 或剪贴板文字。

编译与测试均可离线。自动化不启动主程序、不显示 Form、不截取桌面、不注入输入、不写用户剪贴板。隐藏 HWND 的样式检查只是集成证据，不能证明真实桌面的层级与拖动效果。

0.9 新增：
- CaptureWorkflows 只负责物理范围解析，显式截图仍经过同一 CaptureOverlay；LastCapture 只在成功完成后更新。
- DailyPreferences/DailySettings 负责 10 动作、默认工具、贴图/导出/历史配置及传输验证；自启运行状态从本机只读校对，不随配置迁移授权。
- PinDaily/PinViews 保留非破坏方向、WPF 贴图材质、WM_MOVING 内本窗吸附及按标记隐藏恢复。PinManagerWindow 不承担截图引擎。
- CaptureCompletion 与 CaptureHistory 将默认不持久化、显式自动保存、自有历史副本分开；HistoryWindow 通过同一渲染/贴图出口处理选择项。
- BoardDocument 以共享不可变源图 + 独立几何/层级快照实现画板，撤销不复制全部位图；自包含 ZIP、GUID 资源与像素/数量边界，不解压任意路径。BoardView 的输入与世界/视图转换独立可测。
- BackgroundDialog 调用原 Backdrop，Canvas 快照现在包含美化状态；原位工程只保留选区底图和美化参数。
- ImageTransfer 仅在显式 Ctrl 拖出时准备自有 PNG 和 FileDrop，真实 OLE 仍待验。
- InstallerCleanup 先于 UI/热键初始化分派卸载命令；路径哈希事件仅请求当前安装目录的实例正常退出，不使用强杀或全局钩子。
- tools/ScreenCraft.wxs/build-installer/test-installer/inspect-installer 构建并核对 perUser/limited MSI。隔离安装测试不同产品码/注册键/目录，运行时清理动作另用适配测试，生产 GUI 未验。

应用构建/测试可离线；首次 MSI 编译下载校验固定 WiX ZIP，目标运行时不下载。MSI 静默隔离测试只启动安装引擎，不启动 ScreenCraft，不修改真实启动项。
