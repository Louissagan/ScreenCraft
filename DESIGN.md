---
name: ScreenCraft
description: 以 1Capture 公开界面为视觉基准的 Windows 原位截图标注
colors:
  primary: "#0069EF"
  selected: "#DAEAFD"
  surface: "#F7F9FC"
  ink: "#20242A"
  line: "#D9DFE7"
  danger: "#F53F65"
typography:
  body:
    fontFamily: "Microsoft YaHei UI"
    fontSize: "9pt"
    fontWeight: 400
rounded:
  floating-bar: "13px"
  tool-state: "8px"
spacing:
  bar-padding: "6px"
  bar-gap: "6px"
  selection-gap: "8px"
components:
  tool-button:
    backgroundColor: "{colors.surface}"
    textColor: "{colors.ink}"
    rounded: "{rounded.tool-state}"
    width: "34px"
    height: "34px"
  tool-button-selected:
    backgroundColor: "{colors.selected}"
    textColor: "{colors.primary}"
---
# Design System: ScreenCraft

## Overview

**Creative North Star: "原位截图，紧凑标注"**

用户指定 1Capture 为界面和图标参考，替换原来的大窗口文字工具条。视觉依据为 https://1capture.com/ 的 `toolbar-list.webp`、`built-for-feedback.webp` 与公开工具文档；操作依据同时保留用户提供的选区原位编辑照片。设置以官方 advanced-tips 的 `left_double_click.jpg` 中 Settings 窗口为依据：顶部图标分区、浅灰标题区、白色圆角分组、蓝色录入值和开关。公开图片仅供本地分析，不作为程序资产分发。

Windows 截图主路径由冻结桌面、原位选区、浅色浮动工具条及二级属性条组成。选区内保持原始物理像素；文字说明用中文悬停提示和详细属性面板承载。代码依据：`CaptureVisuals.cs`、`CaptureOverlay.cs`。

## Colors

蓝色只标识当前工具、滑块和选区；浅灰蓝浮条与深色细线图标保持克制。取消使用 danger 色。源图本身不调色；遮罩只画在选区外，不进入导出。

## Typography

中文操作提示和控件使用 Microsoft YaHei UI 9pt。工具优先以几何图标呈现，通过 AccessibleName、悬停提示和键盘焦点保持可识别性。

## Layout

主工具条和属性条按总高度一起定位，属性始终位于主条下方 6px。默认整体优先选区下方，不够则整体上方；均不够时整体限制在工作区，可覆盖部分选区。设置支持优先上方。主条最大宽度 728px，小屏换行；二级条根据当前工具显示线型、圆角、填充、箭头样式、字号或马赛克块大小。工具顺序和隐藏状态由用户配置，隐藏工具进入“更多”。

选区支持八个白色蓝边控制点；黑色外衬、蓝色外边与白色内边共同区分明暗背景。窗口识别和原位编辑使用同一边框绘制函数。尺寸标签可拖动移动范围，方向键微调。图像坐标固定 1:1，工具控件与图像缩放分离。这里记录的是 96 DPI 基线；混合 DPI 的真实交互仍需验证。

设置客户区默认 860×680，支持紧凑尺寸与正文滚动；顶部六个 96px 宽的图标分区、固定底部恢复/保存。内容顶栏 100px，底部 64px；布局按设备 DPI 换算。根据用户最新要求，标题栏及右上角窗口按钮交给 Windows 原生窗口管理，移除 Mac 红黄绿按钮。正文说明按内容测量，常规页不因空白块产生滚动条。全局热键和工具单键录入分别配置，长错误可悬停查看。后台宿主没有可见主窗口；只由托盘入口或显式 --settings 打开设置。

## Elevation & Depth

0.8 工具及属性浮条采样冻结画面，降采样、两遍模糊、浅色叠加，按位置和范围缓存真实材质。按钮继承同一材质，导出排除；不是 macOS 系统玻璃。选区外的冻结画面默认以约 179/255 的黑色遮罩压暗（70%），可调 45–85%。设置使用 #F2F3F5 底色、白色卡片与输入、#E0E3E7 细边和轻阴影。

## Shapes

浮动条 13px 圆角；工具状态底板 8px 圆角。图标采用统一 1.7px 圆端线条，24×24 坐标绘制到 22×22 图标区。图形、线条及按钮状态由生产 GDI+ 绘制，不从官网截取按钮图片。

## Components

- `CaptureIconButton`：默认、蓝色选中、悬停、禁用和键盘焦点；保留原生 Button 键盘与点击行为。
- 截图按钮的背景必须完全覆盖默认按钮绘制，实心背景禁用抗锯齿；图标/圆角提示再启用抗锯齿。默认按钮无外框，状态提示内缩 2px；键盘焦点使用内缩蓝色圆角线，避免黑色矩形边线。
- `CaptureSlider`：灰轨、蓝色已选部分、白色圆形滑块；支持拖动、滚轮和方向键。
- `CaptureBar`：包裹工具行或当前工具属性行；位于选区附近，导出不包含工具条。
- `AnnotationInspector`：可折叠的精确属性面板，保留复杂线型、独立填充色、透明度、序号与文字编辑。
- `PinWindow`：独立置顶图像；滚轮/右下角拖拽/键盘保持图像宽高比，透明度作为独立状态。
- `PreferencesPaintButton`：完整背景、UserPaint、AllPaintingInWmPaint 和 OptimizedDoubleBuffer；切换状态后不得保留旧像素。设置自绘文字统一使用 Graphics.DrawString，避免与 GDI TextRenderer 混绘。
- `StyledMenu` / `CraftMenuRenderer`：托盘、贴图及截图弹出菜单共用；白底、圆角蓝色悬停、细线图标、快捷键右对齐、功能分组、退出红色。原生 ToolStrip 负责点击、键盘导航、勾选与子菜单。
- 0.6 的弹出层统一使用 #F9FAFC 面板与单条 #DADADF 边界，整个菜单由共享 painter 绘制，不再拼接原生文字/图像栏与自绘背景；图标与快捷键位置在同一坐标系计算。Windows 11 采用 DWM 小圆角与无系统附加边框，保留不支持时的回退。
- `PreferencesChoice`：自绘输入胶囊和共享菜单选项；方向键/Home/End 选择，Alt+Down/F4 展开，Tab 继续导航；颜色默认 RGB。设置不再使用原生 ComboBox 外观。
- `PreferencesGroup` / `PreferencesRow`：一个圆角背景，行与标签继承父背景，不把矩形白色子面板堆在圆角上；不使用组窗口 Region 裁切。实际原生行合成的四角必须保留外部底色。
- `BrandArtwork`：EXE、托盘、窗口标题及菜单标识的唯一图稿源。构建时生成 16/20/24/32/48/64/128/256px ICO，作为 Win32 图标与托管资源嵌入程序。设置齿轮与功能图标共用 CaptureLook 的细线几何绘制。

## Do's and Don'ts

0.7 截图层使用固定全屏 Canvas，冻结源图和压暗缓存作为底层，选区、八个控制点和尺寸在画布内绘制；工具条是其子控件。移动范围仅改变裁剪坐标与覆盖区域，不移动底图 HWND。冻结窗口在编辑结束后关闭，详细属性延迟创建。

属性面板采用与设置相同的圆角分组、自绘选择器和开关；颜色按钮用色点展示当前颜色，分别控制描边、文字和底色。文字及说明默认透明，提供色底白字；输入期间属性变化即时反馈。圆角滑块可调整任意半径，说明框与标记主体可独立拖动。

- 以 1Capture 的公开控件结构、图标语法和选中状态作为视觉基准；Windows 使用 Ctrl 映射。
- 保留选区原位；导出、OCR 与贴图均来自选区像素及标注，不含工具条、遮罩或选区外桌面。
- 合成离屏图只能作为渲染与布局证据，不能标成真实桌面截图。
- 设置已迁移到顶部图标分区；本地图片编辑、贴图再编辑与贴图管理仍有旧界面，不把这些旧界面记录成新的设计标准。

0.8 标注一级入口下用二级模式切换点/云线/序号。点备注为目标点、引线、圆角文字牌；数字牌有引线/圆形样式。输入自然增长至宽度上限后换行，拖角点或滑块设固定宽度；字体、前景和背景编辑不结束输入。通用色、建筑分析建议色和 Set2 均使用圆形色点，保留可访问名称及独立 HEX 录入。没有复制官网图标资产。

## 0.9 日用窗口

保留原位浮条的冻结磨砂；文件编辑、画板、历史和贴图管理采用白色操作条、共享几何图标及清晰内容区。管理器用左列表/右预览，空状态给出下一步；列表选中浅蓝，操作在下方。文件编辑主条为图标，标注子模式在下一行，精确属性保留右侧。

设置增加贴图页，七个分区仍在顶部，Windows 原生标题栏不变。配置/导出/缓存权限解释随具体操作出现，默认不开启历史/自动保存。画板保存与导出分别命名，关闭未保存内容提醒。

隐藏 Form 的 WM_PRINT 只绘制外壳。客户端合成证据逐个执行生产 OnPaint/OwnerDraw；隐藏子项按自身可见状态过滤。报告不将它们称为真实 GUI 截图。
