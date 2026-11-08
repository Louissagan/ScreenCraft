# 安装与迁移

## 当前用户 MSI

运行发行目录的 ScreenCraft-Setup-PerUser-win-x64.msi。默认安装到当前用户 LocalAppData/Programs/ScreenCraft，在当前用户开始菜单创建 ScreenCraft 和设置入口。不申请管理员权限，不写系统级安装目录，不启用自启，也不自动运行应用。

安装完成后从开始菜单打开 ScreenCraft；短暂提示后驻留托盘。登录后启动由常规设置主动开启。更新前先从旧版托盘退出，保存未完成画板。重复启动不能替换已经运行的旧进程。

~~~powershell
msiexec /i ScreenCraft-Setup-PerUser-win-x64.msi
# 静默安装；应用和启动项仍默认关闭
msiexec /i ScreenCraft-Setup-PerUser-win-x64.msi /qn /norestart
~~~

## 卸载

使用 Windows 已安装的应用，或对应 MSI 的卸载入口。卸载会请求当前安装路径对应实例正常退出；遇到未保存画板，请先保存或确认放弃，再重试。不会强制终止其他便携实例。

默认保留当前用户配置、历史和所有手动保存图片。可在程序历史面板主动清理自有副本。部署时明确选择 REMOVEUSERDATA=1 才请求清理生成的历史缓存；不递归删除配置或用户保存目录。

~~~powershell
msiexec /x ScreenCraft-Setup-PerUser-win-x64.msi
~~~

## 便携包

解压 ScreenCraft-Portable-win-x64.zip 后运行其中 EXE；portable.flag 使偏好写入同目录 Data。迁移时复制整个目录。相对自动保存目录随数据目录移动，绝对目录保留原设置，必要时自行调整。

自启是每台电脑/每个用户的授权，迁移配置不会自动启用。程序只读检查当前启动项，路径不一致时提示，在新位置主动开启即可更新。只读便携目录会明确回退每用户数据目录。

## 构建与验证边界

应用使用目标 Windows 已有的 .NET Framework 4.8；OCR 需要本地已安装语言包，不自动下载。MSI 构建工具 WiX 仅为开发依赖，目标机器不需要它。

本次实际运行了隔离产品的静默安装/升级/卸载，完整生产向导、标准账户令牌、登录自启、实际卸载窗口交互仍 NOT RUN。安装包未代码签名，SmartScreen/组织策略放行未验收。发行文件附 SHA-256，不绕过系统策略。
