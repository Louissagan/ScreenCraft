# 本轮失败闭环与回归

2026-11-04，所有本轮程序验证均在离屏位图或隐藏窗体上完成。

| 问题 | 可复现场景 | 原因与修复 | 当前验证 |
|---|---|---|---|
| 画笔节点在移动时丢失 | 克隆三点画笔 `(10,20),(40,60),(80,70)`，执行 `clone.Offset(clone,15,20)` | `from` 与接收对象相同时先替换 Points 列表导致遍历空列表。先保留原列表引用，再创建新列表；快照始终深拷贝 | `pen/clone and offset do not mutate undo source` PASS；画笔缩放/撤销与实际事件路径 PASS |
| 复制标注复用 ID（代码审查发现） | Ctrl+D 对象复制后导出可编辑工程 | Clone 用于撤销时应保留 ID，但用户复制是新对象。DuplicateSelected 单独生成新 ID | `canvas/duplicate generates fresh IDs export remains editable` PASS |
| 画笔把手缩放使用旧包围框（代码审查发现） | 缩放含多个节点的画笔 | Pen.Box 来自节点，不能在改变 A/B 后仍读取旧 Points 包围框。由新的对角坐标计算目标框后映射所有节点 | `pen/canvas resize and undo transform every node` PASS |
| 上一轮 EXE 文件占用 | 前一开发实例仍运行时构建 | 原构建直接覆盖锁定文件。改在独立目录成功编译，再保留旧二进制、替换输出 | Release 构建成功；未强制结束用户桌面窗口 |
| 隐藏 Form 的 DrawToBitmap 输出空白 | 不显示编辑器时尝试整窗渲染 | 隐藏子控件不能作为真实 GUI 渲染证据。已移除空白图，改为控件树/布局几何检查；纯 Painter 输出单独标为离屏夹具 | `editor/hidden ...` 测试 PASS，但 GUI 仍 NOT RUN |
| 20 图测试夹具超出颜色范围 | 扩展到 20 图后，第 14 个夹具使用红色值 260 | 测试输入问题，改为合法颜色 `%256`，未掩盖产品错误 | 20 隐藏 HWND 全部独立、TopMost=True、Visible=False；41 项回归 PASS |

当前实际输出：`docs/evidence/automated/results.csv`、`hidden-pin-evidence.json`；可复现场景就是仓库中对应的测试代码。早期构建/测试日志放在 `docs/evidence/m1/`，最终日志为 `tests-final.log`。

## 0.3.0 OCR / 取色 / 长截图

| 问题 | 处理 | 验证与限制 |
|---|---|---|
| 原 OCR 引擎优先西班牙语，中文空格/文本可信度评分偏拉丁文 | 按已安装语言自动简中/英文或显式语言选择，中文空格规范化，中文字符参与文本可信度判断 | 真实中英混排合成图 PASS；缺语言明确报错 |
| OCR 主线程同步结果、缺取消/超时/重试 | MTA 工作线程、自有图像、取消令牌、单调时钟、预览/重试控件 | 服务线程/取消/错误自动化 PASS；可见窗口 NOT RUN |
| 取色模式 Esc 仅关闭放大镜、点击时可能用上次鼠标位置 | 独立取色 Esc 直接取消，点击使用事件坐标；放大镜宽度按 HEX/RGB/HSL 文本测量 | 像素/格式/生产编辑器事件 PASS；冻结桌面窗口 NOT RUN |
| 取色等待状态在换工具或 Esc 后残留，剪贴板占用可能抛出异常 | 清除采样回调，状态栏说明重试 | Canvas 换工具/取消采样 PASS；真实剪贴板占用 NOT RUN |
| 滚动 GDI 创建失败在 try 外、BitBlt 失败未检查 | 独立 IDisposable 适配层，逐项检查及释放，事务保存部分结果 | 假采集器失败保留部分结果 PASS；实际屏幕失败路径 NOT RUN |
| 滚动达到尺寸上限可能静默停止追加；旧窗口未统一隐藏 | 显式停止提示，分段输出当前图像；会话期间隐藏/恢复自有窗口 | 上限/分段全像素 PASS；可见窗口恢复 NOT RUN |
| DIB 重复 Dispose 再操作旧 DC | 幂等释放、构造失败回收与屏幕读取错误反馈 | 200 次合成 DIB 句柄 79 → 79，无桌面读取 |
| OCR-001 原长图样例丢首行 | 单独裁出仍不能识别，属保留的引擎准确性限制，未谎称修复。长图分带验证改用已独立验证可识别的文字，并保留失败原句独立回归 | `native-ocr-probe.json` 有 expected/actual；KNOWN LIMITATION。去除未证明有效的稀疏裁剪试验代码 |
