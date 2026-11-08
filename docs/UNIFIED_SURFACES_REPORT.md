# 0.6.0 设置、弹出菜单、图标与 RGB 默认值

日期：2026-11-04。依据用户本轮提供的 4 张图片统一处理，不操作桌面、不关闭现有进程、不改变真实偏好或热键。

设置取色格式原先仍是原生 ComboBox。现在使用 PreferencesChoice，闭合为自绘圆角胶囊，展开使用与托盘相同的 StyledMenu。支持方向键、Home/End、Alt+Down/F4 和正常 Tab 导航；原生 ToolStrip 继续负责弹出项点击、勾选和子菜单。测试直接调用控件键盘处理和实际选项回调，不发送桌面键鼠输入。

菜单改为共享 painter 绘制整个面板；图标、文本、快捷键与分组在同一坐标系排布，避免自绘背景与原生图像/文字栏拼接。移除菜单的 WS_BORDER 与扩展边缘样式。Windows 11 的额外边框抑制和小圆角请求依据 [DWMWINDOWATTRIBUTE](https://learn.microsoft.com/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute) 与 [DWM_WINDOW_CORNER_PREFERENCE](https://learn.microsoft.com/windows/win32/api/dwmapi/ne-dwmapi-dwm_window_corner_preference)，失败时保留回退；未把隐藏调用成功等同于可见效果验收。

设置分组不再使用矩形白行叠加窗口 Region 的方式裁切边角。圆角背景由父面板统一绘制，行和文字继承父背景，分组本身不设置窗口 Region；实际隐藏 HWND 行合成验证四角不会被白行盖住。离屏预览也会调用透明子控件的背景绘制，不以错误底色模拟。

以前 EXE 没有 Win32 应用图标资源，设置等窗口使用了系统默认图标。新增 BrandArtwork 单一图稿，构建辅助程序输出 16/20/24/32/48/64/128/256px ICO，同时作为 /win32icon 和托管资源编译进 EXE。托盘、设置、OCR、编辑器、贴图和贴图管理复用；菜单品牌标识同源。设置图标更换为同一绘制系统中的齿轮。

新 Preferences、旧配置缺失格式字段、恢复默认、无偏好与无效格式的取色输出均默认 RGB。明确保存的 HEX/HSL 不被覆盖。RGB/HSL 下 HEX 大写开关禁用；实际用户配置未由测试改写。

完整自动化：**129 PASS / 0 FAIL**，新增 8 项：

- RGB 新配置/缺省迁移/无效回退与显式 HEX 保留。
- 自绘选择器键盘选择、无原生矩形框、实际选项回调、保存和恢复默认。
- 隐藏原生分组及子行合成四角检查，组 Region 为空。
- 原生弹出层样式无附加框，实际菜单控件全表面绘制。
- 八尺寸 ICO 资源、从编译后的 PE 提取实际应用图标、各窗口同源标识。
- 设置色彩页、关于页和打开选项的生产绘制证据。

测试期间修正了隐藏控件测试的尺寸获取顺序：先创建自己的 HWND，再读取完成布局后的尺寸，避免旧尺寸裁切；始终没有 Show、真实屏幕捕获或系统键鼠输入。圆角抗锯齿边线属于合法过渡色，检查点采用真正的最外角，不把边线误判为白行凸出。

`surfaces-0.6.0.txt` 保留早期诊断的 18 PASS / 1 FAIL（检查点落在合法抗锯齿边线上）；最终专门回归另存为 `surfaces-final-0.6.0.txt`，结果 19 PASS / 0 FAIL，完整套件仍以 `build-test-0.6.0.txt` 的 129 PASS 为准。另从实际发布 EXE 提取图标检查通过，保存为 `release-exe-icon-0.6.0.png`，无需运行应用。

证据位于 `evidence/automated/`：`build-test-0.6.0.txt`、`results.csv`、`settings-color-unified-offscreen.png`、`settings-about-unified-offscreen.png`、`choice-capsule-native.png`、`choice-options-native.png`、`rounded-group-native-composite.png`、`tray-menu-unified-native.png`、`exe-icon-native.png`、`app-icon-artwork.png`。设置图只包括客户区，标题栏未画入，所有证据均为离屏/隐藏控件产物。

构建 `build.ps1 -Test`；发布 `dist/0.6.0`，包含 EXE、配置、便携 ZIP、源码 ZIP、SHA-256。图标构建辅助工具源码随源码包提供，不依赖外部绘图程序。verify-package.ps1 解压源码独立构建并执行全部测试，记录在 `evidence/automated/package-verification-0.6.0.txt`。

真实菜单开合/点击、系统焦点、任务栏缓存、可见圆角与混合 DPI 仍 NOT RUN。AC-01~30 完整人工验收维持 NOT RUN，Gate A/B 未通过；本地图片编辑和 OCR 结果窗口的其他旧式控件未宣称整体重做。
