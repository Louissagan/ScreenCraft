# 开发说明

需要 Windows 10/11 x64 和 .NET Framework 4.8。应用没有 NuGet 依赖，也不需要安装 .NET SDK。

## 构建与测试

```powershell
.\build.ps1
.\build.ps1 -Test
```

程序生成在 `bin/Release`。自动测试使用合成内容与测试窗口，不能代替不同电脑上的实际使用测试。

## 打包

```powershell
.\publish.ps1 -DistributionDirectory dist/0.9.4
.\tools\verify-package.ps1 -DistributionDirectory dist/0.9.4 -Rebuild
```

发布脚本生成便携 ZIP、每用户 MSI、源码 ZIP 和 SHA-256 校验文件。首次构建 MSI 时会下载并校验 WiX 3.14.1；运行应用不需要 WiX。

本机配置、截图历史、编译产物和运行日志不提交到 Git。测试证据会在本机生成，历史开发报告中的 `docs/evidence/` 链接指这些本地记录。

## 代码目录

- `src/App`：ScreenCraft 功能与界面。
- `src/Upstream/Stackshot`：复用的上游模块及原始许可证。
- `tests`：自动化测试。
- `tools`：打包、检查与界面测试工具。

源文件沿用各自的版权声明。更新上游模块后可运行 `tools/update-provenance.ps1` 更新来源记录。
