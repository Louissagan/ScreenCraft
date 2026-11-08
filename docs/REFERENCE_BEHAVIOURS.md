# 交互参考与取舍

日期：2026-11-04。用户明确指定微信/QQ 主路径以及 HapiGo 式完整标注。本机未对微信/QQ 客户端操作，不宣称当前客户端细节已亲测。

| 依据 | 已查阅内容或用途 | 证据边界 |
|---|---|---|
| https://isux.tencent.com/articles/qq-screenshot.html | QQ 官方设计文章，提到框选、尺寸、色值、窗口识别、重复编辑、打码 | 文章主要进一步讨论移动 QQ 长图；不能据此断言所有 Windows 按键细节 |
| https://www.ithome.com/0/976/379.htm | 2026-07-14 转述微信派的截图、文字提取、色值和打码说明 | 二手介绍，不等同 Windows 客户端实测 |
| https://docs-cn.hapigo.com/extention/screenshot | 官方教程：选区、工具和内部快捷键、贴图缩放等 | macOS 快捷键不原样移植到 Windows |
| https://updates-1c.hapigo.com/history/v1.0.0 | 官方发布记录列出标注、备注、长截图、背景、贴图、取色、OCR | 不证明本项目实现或本机可用 |
| 用户本聊天直接说明 | 可编辑圆角/填充/线型、序号气泡、云线文字、多个独立置顶、OCR、保存、长截图；录屏可选 | 产品优先级，以需求补充为准 |

默认 Ctrl+Alt+A 进入选区，选区完成进入标注；Enter/Ctrl+C 复制完成，Ctrl+S 保存，P 钉图，Esc 取消。文字框或属性输入控件获取焦点时单字母工具命令不处理。

标注采用对象模型：预览与导出共用同一渲染器；保留原图用于重新编辑。分享 PNG 是扁平像素；显式保存 `.sccraft` 工程会保留原图。

真实程序证据只来自 `docs/evidence/m0/`；`docs/evidence/automated/` 中的 PNG 是测试夹具经生产渲染器离屏输出，**不是桌面运行截图**。
