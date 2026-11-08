# ADR-001 — 采用 Stackshot 核心模块

日期：2026-11-04。决定：以 **Stackshot** 为唯一主底座，保留原生 WinForms/WPF/C#；精简宿主，复用捕获、选区、标注画布、形状渲染、贴图及滚动拼接。不是从零实现截图引擎。

## 实际检查的版本

| 候选 | 远端 | 分支、commit | 许可证 |
|---|---|---|---|
| Stackshot | https://github.com/rubenitx/stackshot | main / `0fa71af2346f09dc34dd8f48eb4f8df211f5ae7c` | MIT，Copyright 2026 Rubén Martínez |
| Snapboard | https://github.com/Flowdesktech/Snapboard | master / `379651086cd7ce6eb50733925ffe4c74f006fdab` | MIT，Copyright 2026 FlowDesk |

二者独立 clone 保存在 evaluation/。原始候选未改源码；Stackshot 的外部集成探针调用未修改的程序集，其结果与原始主程序手测明确分开。

## 构建、启动和功能分别记录

| 证据项 | Snapboard | Stackshot |
|---|---|---|
| Release 源码构建 | PASS，项目内安装 .NET SDK 10.0.403，0 warnings / 0 errors | PASS，Windows 自带 Framework64 csc，优化构建 |
| 启动 | PASS，真实主窗口和热键冲突提示 | PASS，真实主窗口 |
| 区域捕获、复制 | PASS，实际框选和箭头复制，剪贴板导出 PNG | PASS，实际区域捕获，剪贴板导出 PNG |
| 窗口识别 | 独立列表式选窗；悬停识别未证实 | 实际 hover 高亮，有截图 |
| 标注 | 实际箭头渲染；原始选区工具栏有定位缺陷 | 原始程序集 Editor 实际画矩形，有截图 |
| 两张独立贴图 | 实际连续创建，有两个浮窗捕获文件 | 原始程序集探针实际创建两个不同 HWND、TopMost=True，有日志与图像 |
| 全局键、退出 | 实际唤起；主界面 Exit 退出，托盘退出 NOT RUN | 主程序创建区域捕获；独立热键、托盘退出 NOT RUN |
| 发布包 | 自包含 publish 实际成功，约 191 MB EXE 加本机 DLL | 实际产出单 EXE，约 3.4 MB，依赖 Windows .NET Framework 4.8 |
| 原始 OCR、取色 | NOT RUN | NOT RUN |
| 真正标准账户、双屏混合 DPI | NOT RUN | NOT RUN |

基线日志、真实窗口证据：`docs/evidence/m0/`。环境：Windows 11 Pro 10.0.26300 / AMD Ryzen 9 5980HX / 约 32 GiB RAM。未进行可靠的 30 次延迟基准，不报告性能达标。

## 证据约束下的评分（0–5）

分数用于研发改造取舍，**不等于功能验收 PASS**。未测性能采用风险折扣，不凭 README 赋予性能通过分。

| 维度 | 权重 | Snapboard | Stackshot | 依据 |
|---|---:|---:|---:|---|
| 微信/QQ 主路径及可编辑标注改造 | 25% | 3 | 4 | 实际标注体验，加源码中对象选择/移动/缩放/撤销重做的实现；用户新增序号、云线、可编辑样式需求 |
| 真正独立多图贴屏 | 20% | 4 | 4 | 两者均有两个独立窗口的运行证据；均需增加管理与重新编辑 |
| 便携/最低权限 | 20% | 3 | 4 | 实际发布体积、manifest、Framework 部署路线；两者都需要重做可迁移配置 |
| 维护性 | 15% | 4 | 3 | Snapboard 约 35 个 C# 源文件；Stackshot 原库约 236 个，采用显式模块裁剪降低复杂度 |
| 性能/跨 DPI 风险 | 10% | 1 | 1 | 没有性能及混合 DPI 实测，不能据此宣称达标 |
| OCR/取色/滚动可复用性 | 10% | 2 | 4 | 审查本地 OCR、RegionPicker 工具和 Stitcher 算法，Stackshot 有现成可运行测试框架 |
| 加权 | 100% | **3.05** | **3.55** | 用户的完整标注需求使 Stackshot 更合适 |

## 复用边界与风险

- 复用文件在 `src/Upstream/Stackshot/`，版权头及根 LICENSE 保留；逐文件来源/哈希见 `docs/UPSTREAM_FILES.csv`。
- 原作者的宿主、联网更新、报告上传、Google OAuth、录屏输入钩子及角色素材未纳入编译与分发。没有 QQ 私有组件、Electron 或 WebView。
- `Canvas` 的对象编辑、撤销、裁剪及 `Painter` 的基础几何/马赛克继续使用原实现，再增加样式、画笔、云线说明和工程格式。
- 标注界面目前在选区确认后打开独立编辑器；与微信/QQ 在冻结遮罩内直接标注的交互仍有差距，不能将 M1 全部标为完成。
- 真实桌面 GUI 测试现依用户指示暂停。M0 尚有 OCR/取色、托盘退出、标准用户等基线未实测；记录为 NOT RUN，不隐去。
- 使用 OS 自带 .NET Framework 4.8，不将 .NET SDK 或 Python 作为用户运行依赖；干净 Windows 10/11 机器验证仍待执行。
