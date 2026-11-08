$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$date='2026-11-05'
$os=(Get-CimInstance Win32_OperatingSystem).Caption
$cpu=(Get-CimInstance Win32_Processor | Select-Object -First 1).Name.Trim()
$tests=Import-Csv (Join-Path $root 'docs\evidence\automated\results.csv')
$pass=@($tests | Where-Object status -eq PASS).Count
$fail=@($tests | Where-Object status -eq FAIL).Count
$revision='working-tree-0.9.0'
if(Test-Path (Join-Path $root '.git')){$revision=(& git -C $root rev-parse --short HEAD).Trim()+' + working tree'}
$matrix=@()
foreach($line in Get-Content (Join-Path $root 'Windows_Screenshot_Tool_Codex_Spec.md')){
 if($line -match '^\| (AC-\d\d) \| (P\d) \| (.+) \|$'){
  $id=$Matches[1];$priority=$Matches[2];$expectation=$Matches[3]
  $note='用户指示暂不操作真实桌面；完整验收未执行。'
  switch($id){
   'AC-02'{$note='固定全屏画布、缓存遮罩和绘制型控制点，冻结层连续交接；隐藏原生重绘与坐标自动化 PASS。真实进入闪烁/拖影及 Esc GUI NOT RUN。'}
   'AC-04'{$note='手动十字线、方向键组合调边、原位八把手、范围移动/撤销、负原点、主条优先下方及工具条边界有自动化 PASS；真实窗口识别与混合 DPI GUI NOT RUN。'}
   'AC-05'{$note='点/云线/序号二级切换、引线牌、自动换行/宽度、平滑与白边箭头、文字水印/模糊/聚光灯、任意圆角、输入中字号/颜色/底色、透明或色底白字、独立说明拖动、撤销及工程往返自动化 PASS；真实鼠标标注 NOT RUN。'}
   'AC-07'{$note='中文路径、PNG Alpha、原子保存和占用路径恢复自动化 PASS；Ctrl+S GUI NOT RUN。'}
   'AC-08'{$note='独立窗口结构及文档隔离已实现；真实 A/B/C 连续截取贴屏 NOT RUN。'}
   'AC-09'{$note='20 个隐藏独立 HWND、置顶样式、关闭隔离、等比滚轮/角拖动缩放、S/Z、方向键、文字备忘渲染、透明度有自动化 PASS；真实可见层级/拖动 NOT RUN。'}
   'AC-12'{$note='隐藏 HWND 鼠标穿透样式开关有 PASS；托盘恢复可见浮窗 NOT RUN。'}
   'AC-13'{$note='自启适配层写入/删除与命令引号有 PASS；本轮未写真实注册表，注销重登 NOT RUN。'}
   'AC-14'{$note='直接组合键录入/清除/恢复/设置事件/自有快捷键路由及冲突回滚、交换、禁用有自动化 PASS；未占用用户实际快捷键，真实物理键盘和微信/QQ 冲突 NOT RUN。'}
   'AC-15'{$note='启动提示的无激活样式/位置/时序、默认/后台/重复启动不请求设置，显式设置独立唤醒路由、无 MainForm 的隐藏应用上下文与退出/释放有自动化 PASS；未启动真实托盘应用、系统单实例/热键接管 GUI NOT RUN。'}
   'AC-16'{$note='HEX/RGB/HSL 精确采样、默认 RGB/显式偏好保留/自绘格式选择器有自动化 PASS；真实 Windows OCR 合成中英/长图原实测及 OCR-001 限制保留。真实取色/下拉交互/剪贴板和断网 GUI NOT RUN。'}
   'AC-18'{$note='每用户 MSI 成品与隔离产品安装/升级/卸载 6 项 PASS，载荷/路径/保留用户图、HKCU 和有限权限表核对已测；标准账户令牌、生产向导/退出/自启仍 NOT RUN。'}
   'AC-20'{$note='200 次离屏编辑/渲染循环有 PASS；200 次真实截图/取消/贴图 NOT RUN。'}
   'AC-21'{$note='合成重叠/固定头尾/侧栏/色差/亚像素/部分失败/尺寸上限/分段输出 PASS；Chrome/Edge 真实页面及 125% GUI NOT RUN。'}
   'AC-22'{$note='独立画板 10 图/吸附/等比缩放/90°/层级/撤销/工程重开自动化 PASS；实际鼠标及未保存提醒 GUI NOT RUN。'}
   'AC-23'{$note='画板完整内容边界导出、负坐标/视口外、单图贴屏适配有自动化 PASS；真实可见画板/贴图 GUI NOT RUN。'}
   'AC-24'{$note='工具栏排序/隐藏、单键配置与输入态隔离、配置保存读回、生产隐藏控件事件有自动化 PASS；真实拖放及重启 GUI NOT RUN。'}
   'AC-25'{$note='默认关闭历史；显式记录、限制/删除/清理及假剪贴板/贴图 UI 事件 PASS，手动保存哨兵保留；真实 GUI NOT RUN。'}
   'AC-26'{$note='美化面板/预览、纯色/渐变/圆角/边框/阴影/比例、撤销和工程像素往返 PASS；真实 GUI NOT RUN。'}
   'AC-27'{$note='编译源码未纳入网络客户端/上传/遥测；外网断开和抓包 NOT RUN。'}
   'AC-28'{$note='设置损坏、冲突、锁定导出目标恢复，以及 StyledMenu 提前释放回归、UI-PAINT-001/002 设置重绘残留与截图按钮边线的复现/修复回归有自动化 PASS；完整 GUI 错误提示及用户显示器确认 NOT RUN。'}
   'AC-29'{$note='Release、Portable、Source ZIP、per-user MSI 和校验和由脚本生成；源码独立重建与安装包检查有证据，完整发布/GUI 验收仍未通过。'}
   'AC-30'{$note='M0 候选程序有真实截图；新宿主的标注/三图/设置/冲突连续演示 NOT RUN。'}
  }
  $matrix+=[pscustomobject]@{ID=$id;Priority=$priority;Date=$date;OS=$os;Machine=$cpu;Method='manual (not executed)';Status='NOT RUN';Expected=$expectation;Evidence='docs/evidence/automated/results.csv';FailureOrNext=$note;IssueOrCommit=$revision}
 }
}
if($matrix.Count -ne 30){throw 'Expected 30 acceptance cases'}
$matrix | Export-Csv (Join-Path $root 'docs\ACCEPTANCE_REPORT.csv') -NoTypeInformation -Encoding utf8
$lines=@('# 验收报告','',("日期："+$date+'。系统：'+$os+'。CPU：'+$cpu+'。'),"自动化子测试：**$pass PASS / $fail FAIL**。完整 AC-01~30：**30 NOT RUN**。",'','本轮遵守用户指示：不使用 Computer Use、不显示测试窗口、不截图桌面、不注入鼠标/键盘、不改实际热键或真实启动项。隐藏 HWND 测试不能替代可见浮窗验收。','离屏 PNG 是生产渲染器在合成测试图上的输出；不是桌面运行证据。','', '| ID | 优先级 | 状态 | 证据与缺口 |','|---|---|---|---|')
foreach($row in $matrix){$lines+='| '+$row.ID+' | '+$row.Priority+' | '+$row.Status+' | '+$row.FailureOrNext+' |'}
$lines+=@('','逐项日期、操作系统、机器、执行方式、证据和复现/后续步骤见同名 CSV；完整操作期望来自原任务书。','', '自动化明细：`docs/evidence/automated/results.csv`。测试命令：`.\build.ps1 -Test`。','真实 OCR 合成图：`docs/evidence/automated/native-ocr-probe.json`；5 PASS + 1 KNOWN LIMITATION（OCR-001）。构建/功能/验收边界见 `docs/M3_M4_UTILITY_REPORT.md`。','', 'Gate A / Gate B：**未通过，不能作为最终发布宣称完成**。')
Set-Content -LiteralPath (Join-Path $root 'docs\ACCEPTANCE_REPORT.md') -Value $lines -Encoding utf8
