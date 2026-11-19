# 0.10 内核重写

2026-11-09。该版本替换了 0.9.4 中参与编译的 27 个 Stackshot C# 文件。现有界面、配置和可编辑工程格式继续使用；旧实现没有作为备用内核留在程序中。

| 模块 | 文件 | 当前实现 |
|---|---|---|
| 采集 | `src/Core/WindowsCapture.cs` | 自有 DIB/DC 生命周期、GDI 采集、窗口枚举和光标绘制 |
| 选区 | `RegionSelectionWindow.cs` | 冻结底图、缓存遮罩、输入状态机；松手也是有效输入 |
| 几何 | `AnnotationGeometry.cs` | 曲线精确极值、坐标变换、独立克隆和缓存所有权 |
| 渲染 | `AnnotationRenderer.cs` | 新箭头路径、按块求均值的马赛克、遮盖和聚光灯遮罩 |
| 画布 | `AnnotationCanvas.cs` | 选择、拖动、缩放、文字输入和历史事务；取消保留原重做分支 |
| 贴图 | `PinnedImageWindow.cs` | 自有 GDI+ 透明窗口、等比缩放、独立文档和像素提交 |
| 拼接 | `ScrollStitcher.cs` | 双向相关匹配、固定头尾处理、分条存储；含糊匹配不追加 |
| 滚动采集 | `ScrollCaptureController.cs` | 采集线程、帧稳定检查、手动或显式自动滚动、完成及取消 |
| OCR 调用 | `WindowsOcr.cs` | 自有图像准备、重叠分带和行去重，调用本机 Windows OCR |
| 美化 | `BackdropCompositor.cs` | 自有边距、比例、采样边框、渐变、圆角和阴影合成 |
| 平台边界 | `WindowsApi.cs`、`LayeredFrame.cs` | Windows ABI 声明及浮窗像素提交 |
| 基础工具 | `ShortcutCodec.cs`、`DrawingPrimitives.cs` | 快捷键编解码、圆角路径、字体缓存 |

## 来源与范围

这次根据现有接口、行为和测试重新实现。开发时检查过旧接口与旧实现，因此不使用“隔离式 clean-room 开发”的说法。历史版本确实使用过 Stackshot，Git 历史与原始许可继续保留。

当前运行时 C# 只来自 `src/App` 与 `src/Core`，命名空间为 `ScreenCraft`。构建遇到其他目录中的 C# 会停止；每次构建输出 `.sources.txt` 编译输入清单，可运行 `tools/audit-core.ps1` 检查。

Windows、.NET Framework、GDI+、UI Automation 和 Windows OCR 仍是平台能力。重写的是应用实现，并非操作系统、字体引擎或 OCR 识别模型。安装模板和构建工具来源见 `THIRD_PARTY_NOTICES.md`。旧源码来源清单保存在 `UPSTREAM_FILES_0.9.4.csv`，不描述当前输入。

## 验证

```powershell
.\build.ps1 -Test
.\build.ps1
.\tools\audit-core.ps1 -Output .build/core-audit.json
.\bin\Release\ScreenCraft.Tests.exe --ocr-probe
```

保留原有 237 项回归，另加快速松手、反向拖动、负坐标、丢失捕获、取消保留重做、100 次隐藏窗口重绘、透明贴图、曲线极值、马赛克均值、聚光灯边缘与重复纹理误匹配测试。

自动检查只用合成输入与隐藏窗口，不读取桌面、不写用户剪贴板。Windows 本地 OCR 另测中英文和长图。真实多显示器、混合 DPI、自动滚动及长时间拖动仍需桌面验收，见 `KNOWN_LIMITATIONS.md`。

Windows OCR 调用依据 [Microsoft API 文档](https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr.ocrengine.recognizeasync)。

本轮结果见 [0.10.0 测试记录](RELEASE_0.10.0.md)。
