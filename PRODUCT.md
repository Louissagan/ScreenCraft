# ScreenCraft

<!-- impeccable:product-schema 1 -->

## Platform

Windows native desktop, Windows 11/10 x64. The skill's web/mobile enum does not describe this platform.

## Users

用户日常在建筑图纸、网页、PDF 和设计软件之间工作，用标注截图作为桌面参照。

## Product Purpose

热键截图、全面且可再次编辑的标记，然后将多张结果作为独立置顶窗口同时保留。

## Stack

用户明确授权技术选型。两个候选已实际拉取与构建，开发采用 Stackshot 的原生 C#/WinForms/WPF 核心模块，保留 MIT 通知。

## Capabilities and Constraints

以 Windows_Screenshot_Tool_Codex_Spec.md 和 docs/PRODUCT_REQUIREMENTS_ADDENDUM.md 为产品依据。微信/QQ 截图习惯优先。标注、贴图、OCR、取色、保存、区域滚动截图为主要能力。录屏可选。普通权限、离线、无遥测、用户级部署。不要复制腾讯私有组件或第三方角色素材。

## Product Principles

- 默认完成即复制，不强制保存。
- 标注属性是可编辑状态，导出的像素必须对应预览。
- 每张贴图独立，关闭一张不影响其余。
- 未实测的项目不得声称通过。

## Evidence on Hand

docs/evidence/m0 中有两个候选的真实构建日志、UI 截图、剪贴板 PNG；Stackshot 原始程序集多图集成探针有真实 HWND 和 TopMost 记录。尚无已通过全部验收的版本。
