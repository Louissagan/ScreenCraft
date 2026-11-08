# 0.8.0 交付核对

日期：2026-11-05。

- Release 构建完成，版本 0.8.0.0。
- 工作目录完整自动化：171 PASS / 0 FAIL。
- 从交付 Source ZIP 解压到新目录独立构建并运行：171 PASS / 0 FAIL。
- Portable 中 EXE 与独立 EXE 的 SHA-256 相同；源码文件与工作目录匹配。
- ColorBrewer 完整许可及归属随包。
- 静态已编译源码审计 PASS；这不是网络抓包或系统权限运行验收。
- 测试使用合成图、隐藏 HWND、生产回调与假剪贴板/启动项/热键适配，没有操作用户桌面或关闭现有程序。
- 构建保留 CS1690（WinForms 继承封送对象的值类型字段访问）及 CS0649（错误适配测试钩子）警告，没有编译错误。
- 全部 30 项真实人工验收 NOT RUN；用户级安装包未生成，Gate A/B 未通过。这是开发预览。

日志：evidence/automated/build-test-0.8.0.txt、package-verification-0.8.0.txt、package-integrity-0.8.0.txt。完整功能边界见 1CAPTURE_CHANGELOG_AUDIT.md。
