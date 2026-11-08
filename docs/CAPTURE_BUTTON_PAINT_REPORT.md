# 0.5.2 截图工具按钮多余方框修复

日期：2026-11-04。用户提供的照片显示截图工具条按钮周围存在多余方框。未操作用户桌面或关闭正在使用的程序。

UI-PAINT-002：基线对隐藏按钮发送 WM_PRINTCLIENT，将实际原生绘制与生产 PaintButton 的预期绘制比较，得到 **9 PASS / 1 FAIL**。矩形按钮默认状态有 67 个上/左边界像素不一致；原生绘制叠加的边线导致每个图标看起来额外套了框。失败不是通过代码存在推断，见 `evidence/automated/capture-button-baseline-0.5.1.txt`。

修复截图按钮的背景处理：统一使用完整背景绘制基类，实心底色覆盖时关闭抗锯齿，随后再抗锯齿绘制图标；选中与悬停圆角提示内缩 2px，外缘保持工具条底色。键盘焦点改成内部蓝色圆角线。保留正常矩形/无填充等工具图标，不把功能图标中的正方形误删。

修复后原生绘制匹配生产绘制，11 种图标的默认和选中状态通过，四个边缘中点与角点无多余框线；额外验证悬停与禁用状态。新增 3 项回归，完整自动化 **121 PASS / 0 FAIL**。快速绘制回归日志为 `capture-button-fixed-0.5.2.txt`，完整结果为 `build-test-0.5.2.txt` 与 `results.csv`；后者包含新加入的四状态渲染检查。

离屏证据：

- `evidence/automated/capture-button-native-mismatch.png` 与 `capture-button-expected.png`：修复前原生输出和预期输出。
- `evidence/automated/capture-button-baseline-diff.csv`：67 个边界像素差异。
- `evidence/automated/capture-buttons-native-states.png`：修复后默认、选中、悬停、禁用状态，生产隐藏 HWND 绘制。
- `evidence/automated/in-place-overlay-synthetic.png`：合成底图上的实际工具条和选区布局。

构建命令：`build.ps1 -Test`。发布到 `dist/0.5.2`，提供 EXE、配置、便携 ZIP、源码 ZIP 和 SHA-256。源码 ZIP 解压后独立构建并运行 121 项自动化，记录在 `evidence/automated/package-verification-0.5.2.txt`。便携包和全部校验和另行核对。

边界：图片是离屏测试证据，不是桌面实拍；真实截图态鼠标、焦点和显示器 DPI 交互仍 NOT RUN。AC-01~30 人工验收没有因此改为通过，Gate A/B 仍未通过。
