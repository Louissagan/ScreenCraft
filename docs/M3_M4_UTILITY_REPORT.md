# OCR、取色与滚动长截图验证记录

2026-11-04，ScreenCraft 0.3.0 开发预览。依据原任务书推进 M3 的 OCR/取色和 M4 的长截图，未宣称 M3/M4 或 Gate A/B 整体完成。

## 已实现的生产路径

- OCR：框选、编辑器、贴图入口调用 Windows 本地引擎。后台 MTA 任务使用自有图像副本；已安装语言选择、简中优先 auto、取消、30 秒单调时钟期限、进度、可编辑结果、复制和重试。所选语言缺失会明确报错，不静默换云服务或其他脚本语言。CJK 字间空格合并保留英文空格与换行。
- 取色：主界面/托盘/独立全局键；原有冻结 DIB 放大镜读取物理像素，单击/Enter 返回格式化颜色并复制。截图态 C、编辑器 C 取原图。HEX 大小写、RGB/HSL 及热键持久化；Esc 取消采样，剪贴板占用显示可理解提示。
- 滚动：框选后手动采集及用户主动开启的 Auto、原有预览/高度/完成/取消。沿用 Stackshot 的重叠/固定头尾/浮动遮挡/亚像素算法。`IScrollFrameSource` 隔离 GDI 与合成帧；同一生产处理器管理缓冲区及完成事务。采集器检查原生资源分配与 BitBlt 失败并释放资源；失败保留已拼接部分。到达 30000 行或 4000 万像素明确停止并提示，分段导出当前结果为 PNG。不会假称已捕获上限之外的内容。
- 主界面滚动期间隐藏本软件已显示窗口，结束/取消/启动失败恢复；截图/取色不会与活动滚动会话重叠。相关窗口生命周期的真实 GUI 仍待验收。

## 实际执行与证据

| 检查 | 结果 | 方法和证据 |
|---|---|---|
| Release 构建 | PASS | `build.ps1`，`docs/evidence/m3/build-utilities.log` |
| 自动化回归 | 64 PASS / 0 FAIL | `build.ps1 -Test`，`docs/evidence/automated/results.csv`；纯算法、生产事件方法、离屏和隐藏 HWND |
| 本机 OCR 语言发现 | PASS | 实际 API 返回 en-US、zh-Hans-CN；`native-ocr-probe.json` |
| 实际英文及简中/英文混排 OCR | PASS | 真实引擎识别测试生成的 `ocr-synthetic-input.png`；结果逐字断言关键短语，不是模拟后端 |
| 实际 2700px 长图 OCR | PASS | `ocr-tall-synthetic-input.png`，顺序包含 12345 / 22222 / 33333，重叠边界 22222 仅出现一次 |
| 缺失 OCR 语言 | PASS | 指定 zz-MISSING，真实 API 路径返回已安装语言及操作提示，不静默回退 |
| OCR-001 孤立短行 | KNOWN LIMITATION | `ocr-known-limitation-input.png` 的 `First line 11111` 在英文引擎输出空白，单独裁剪/原始及放大比例亦复现。完整保留 expected/actual/error |
| 原生 DIB 资源 | PASS | 合成 DIB 200 次创建/读取/重复 Dispose，GDI 句柄 79 → 79；`color-gdi-resource-probe.txt`，未读取屏幕 |
| 取色准确性 | PASS（自动化子项） | BGR 通道/边缘、HEX/RGB/HSL、冻结拷贝/负原点、96/120/144/192 DPI 纯转换、生产编辑器点击采样/取消 |
| 滚动拼接 | PASS（自动化子项） | 全像素检查顺序/接缝、重复帧、固定头尾、固定侧栏、色差、亚像素累计、向上/无重叠拒绝、失败部分保留、上限及分段逐像素重组 |
| 静态源码检查 | PASS | `tools/audit-source.ps1`，编译源码无网络客户端、低级键盘钩子、HKLM 或提权 manifest；不是抓包 |
| 便携/源码包 | PASS | `tools/verify-package.ps1 -Rebuild` 校验便携 EXE、所有 SHA-256、每个 src 文件；解压源码包在独立目录再次构建并跑回归 |

真实 OCR 汇总是 **5 PASS + 1 KNOWN LIMITATION**（包含语言发现），单独记录在 `docs/evidence/automated/native-ocr-probe.json`。它不与 64 项回归混为模拟识别准确率。

## GUI 验收边界

遵守用户要求：没有 Computer Use、没有打开可见窗口、没有截图用户桌面、没有注入键鼠、没有改真实快捷键/启动项/剪贴板、没有安装语言包或断开用户网络。

AC-16 的真实放大镜/剪贴板/OCR 结果窗口/断网流程、AC-19 混合 DPI 桌面、AC-21 Chrome/Edge/PDF/Win32 与 125%/动态页面均 **NOT RUN**。30 项完整复合验收仍为 NOT RUN；子项自动化不能替代整项 GUI。

恢复真实测试时，使用 `tests/fixtures/scroll-page.html`：分别在 Chrome/Edge 打开，手动调 125%，包含固定顶部、sticky 侧栏和动态浮层；逐段核对 001~048 与末尾 END 标记。另从纯正文选区开始，再加入导航/侧栏记录差异。原图、结果与接缝近照须由实际运行产生。当前没有打开此文件。

还需工具栏定制/跨屏实测、独立画板、历史、美化、安装包、标准账户与全部 Gate 验收。当前交付是开发预览。
