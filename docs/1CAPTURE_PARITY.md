# 1Capture 公开功能对照

依据用户 2026-11-04 的要求，将 1Capture 官网和工具文档作为功能/交互对照。路线采用公开行为复刻并复用已验证的 Windows 底座；本轮未逆向、未运行 macOS 二进制，也没有宣称全功能一比一完成。

AUTO PASS 只指表内所列自动化子项；全部真实 GUI 仍 NOT RUN。PARTIAL / TODO 为尚未达成的功能差距，SOURCE UPCOMING 是官网自己标注即将推出。公开资料不等于对原程序所有内部功能的完整审计。

| ID | 功能 | 状态 | 当前 Windows 实现与缺口 |
|---|---|---|---|
| 1C-01 | [冻结选区、窗口识别](https://1capture.com/docs/index.md) | AUTO PASS / GUI NOT RUN | 复用 Stackshot 捕获；截图主路径改为原位覆盖层 |
| 1C-02 | [选区八点调整、移动、微调](https://1capture.com/docs/index.md) | AUTO PASS / GUI NOT RUN | 原位八点、十字线、拖动尺寸标签、方向键/组合调边；范围变更可撤销 |
| 1C-03 | [截图自身圆角与区域滚轮调整](https://1capture.com/docs/advanced-tips.md) | TODO | 标注矩形支持圆角；截图外形圆角不是同一项 |
| 1C-04 | [图标主工具条与二级属性条](https://1capture.com/) | AUTO PASS / GUI NOT RUN | 细线图标、浅色浮条、蓝色选中、当前工具属性；未宣称逐像素相同 |
| 1C-05 | [矩形、圆角、填充、线型](https://1capture.com/docs/index.md) | AUTO PASS / GUI NOT RUN | 生产渲染及属性联动已测 |
| 1C-06 | [椭圆、直线、画笔](https://1capture.com/docs/index.md) | AUTO PASS / GUI NOT RUN | 生产渲染、拖动、控制点、撤销已测 |
| 1C-07 | [箭头直线、曲线、折线、粗箭头](https://1capture.com/docs/tools/arrow.md) | PARTIAL | 细线/渐宽/白边实心及平滑曲线箭头已有；复杂折线和全部参考样式未齐全 |
| 1C-08 | [荧光笔](https://1capture.com/) | AUTO PASS / GUI NOT RUN | 沿用生产 Highlight 透明渲染；可从更多或自定义工具栏打开 |
| 1C-09 | [文字与样式](https://1capture.com/docs/index.md) | PARTIAL | 文字/字号/颜色/底色及编辑已有；完整字体与参考样式未对齐 |
| 1C-10 | [计数点输入说明、侧栏输出](https://1capture.com/docs/tools/counter.md) | PARTIAL | 有可改号码及内联说明；不是参考的逐次对话框和导出侧栏 |
| 1C-11 | [两次点击创建备注](https://1capture.com/docs/tools/remark.md) | PARTIAL | 目标点/引线/文字牌、独立移动、输入增长/换行/宽度已有；两次点击全部手势未齐全 |
| 1C-12 | [马赛克与实色遮盖](https://1capture.com/docs/index.md) | AUTO PASS / GUI NOT RUN | 可调块大小、裁剪边界及贴图像素一致性已测 |
| 1C-13 | [彩色模糊与橡皮擦](https://1capture.com/) | PARTIAL | 柔化模糊/彩点完全遮盖已自动化；像素橡皮擦尚未实现 |
| 1C-14 | [撤销、重做与重新编辑](https://1capture.com/docs/index.md) | AUTO PASS / GUI NOT RUN | 对象模型、选区变更、工程保存重开及贴图再编辑 |
| 1C-15 | [工具条排序、隐藏](https://1capture.com/docs/advanced-tips.md) | AUTO PASS / GUI NOT RUN | 配置、列表拖放/上下移事件、隐藏后更多入口，保存重开已测；真实拖放未测 |
| 1C-16 | [自定义工具单键](https://1capture.com/docs/advanced-tips.md) | AUTO PASS / GUI NOT RUN | 单键录入、重复保护、P/C 保留、输入态隔离及持久化 |
| 1C-17 | [工具滑块滚轮调整](https://1capture.com/docs/advanced-tips.md) | PARTIAL | 第一滑块支持滚轮；多修饰键对应第二/第三滑块未完成 |
| 1C-18 | [截图、文件、剪贴板贴图](https://1capture.com/docs/tools/pin-image.md) | AUTO PASS / GUI NOT RUN | 三种来源及独立窗口/文档，新的原位截图贴图交接已测 |
| 1C-19 | [多张独立置顶](https://1capture.com/docs/tools/pin-image.md) | AUTO PASS / GUI NOT RUN | 20 个隐藏独立 HWND 与关闭隔离；真实可见层级未测 |
| 1C-20 | [贴图等比缩放、透明度](https://1capture.com/docs/tools/pin-image.md) | AUTO PASS / GUI NOT RUN | 等比滚轮/角拖动、透明度、滚轮交换/反向及初始缩放偏好已测 |
| 1C-21 | [S 缩略与 Z 原始大小切换](https://1capture.com/docs/tools/pin-image.md) | AUTO PASS / GUI NOT RUN | 保留前一比例及缩略恢复已测 |
| 1C-22 | [贴图旋转与翻转](https://1capture.com/docs/tools/pin-image.md) | AUTO PASS / GUI NOT RUN | 90° 旋转/双向翻转/重置、工程视图方向、保留原图与标注、复制输出已测 |
| 1C-23 | [本地 OCR](https://1capture.com/docs/pro-features.md) | AUTO PASS / GUI NOT RUN | 真实 Windows 中英/长图合成识别；OCR-001 短行漏识别仍保留 |
| 1C-24 | [二维码识别](https://1capture.com/docs/pro-features.md) | TODO | 没有用 OCR 冒充扫码 |
| 1C-25 | [取色与格式复制](https://1capture.com/docs/tools/color-picker.md) | AUTO PASS / GUI NOT RUN | 冻结原图、HEX/RGB/HSL、C 复制与样式取色 |
| 1C-26 | [滚动长截图](https://1capture.com/docs/tools/scrolling-capture.md) | AUTO PASS / GUI NOT RUN | 真实拼接算法、上限/部分结果与分段；Chrome/Edge 场景未测 |
| 1C-27 | [背景美化、圆角、阴影](https://1capture.com/docs/tools/background.md) | AUTO PASS / GUI NOT RUN | 纯色/12 渐变、边距/圆角/采样边框/阴影/比例，预览/撤销/导出/工程往返已测 |
| 1C-28 | [桌面/自定义背景、比例、边框](https://1capture.com/docs/tools/background.md) | PARTIAL | 比例和采样边框已有；桌面/自选壁纸库及完整参数未齐全 |
| 1C-29 | [文字/图片水印](https://1capture.com/docs/tools/watermark.md) | PARTIAL | 文字水印角度/间距/颜色/字号/透明度及工程往返已测；图片/Logo 水印缺失 |
| 1C-30 | [局部放大](https://1capture.com/) | TODO | 贴图缩放不能算截图局部放大工具 |
| 1C-31 | [聚光灯](https://1capture.com/) | AUTO PASS / GUI NOT RUN | 更多入口已接通，可拖范围；明暗与遮盖组合已测 |
| 1C-32 | [AI 消除](https://1capture.com/) | TODO | 需要实际离线引擎与效果验证；没有放置无效按钮 |
| 1C-33 | [一键去背景](https://1capture.com/) | TODO | 需要实际分割引擎与效果验证 |
| 1C-34 | [录屏与 GIF](https://1capture.com/) | TODO | 现有发布程序不包含录屏/GIF；原可选项按新要求纳入对照 |
| 1C-35 | [录屏标注、点击动效](https://1capture.com/) | TODO | 未交付 |
| 1C-36 | [放大光标、光标标签](https://1capture.com/) | SOURCE UPCOMING | 官网标记即将推出，不能称已验证原版具备完整实现 |
| 1C-37 | [图片拖出分享](https://1capture.com/docs/index.md) | AUTO DATA / OLE NOT RUN | Ctrl 拖出实际调用 OLE；PNG/FileDrop 和过期缓存边界通过，外部应用接收未实测 |
| 1C-38 | [本地图片编辑界面](https://1capture.com/docs/local-images.md) | PARTIAL | 图片再编辑已换共享图标/二级模式/样式控件；精确属性在右侧，未逐像素同构 |
| 1C-39 | [设置及菜单深度个性化](https://1capture.com/docs/advanced-tips.md) | PARTIAL | 七分区、10 全局动作、默认工具、导入导出、贴图/保存/历史偏好已接通；完整工作流编辑器缺失 |

此外继续保留原任务书中的 Windows 要求：普通用户/便携/用户级安装、自启、热键冲突、多图、画板、历史、混合 DPI 与 30 项验收。它们不能因为参考程序是 Mac 应用而被删除。

视觉基准及代码规则见根目录 DESIGN.md；原位截图与缩放证据见 IN_PLACE_CAPTURE_REPORT.md。
