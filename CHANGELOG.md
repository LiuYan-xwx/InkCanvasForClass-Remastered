## 更改日志
本文档只是大概记录一下 ICC-Re 所做出的更改，也不全，仅供大致参考以及自己记录看看，无其他意思。  
其实没改什么东西的（）

### 请注意文档的更新时间
> 最后更新：2025-10-13

---

**2025-08-31 至 2025-10-13 的更新：**

功能增强：
- 引入可空引用类型以提升代码安全性
- 在 MainWindow 关闭时保存设置
- 在保存设置时记录日志
- 添加 NamesInputViewModel 和依赖注入支持
- 优化设置面板的显示/隐藏逻辑
- 部分图标换成 FontIcon
- crashtest 按钮的 Click 事件换成 Command
- 添加回页码点击功能
- 优化 PPT 按钮位置调整逻辑
- 重构浮动栏隐藏逻辑
- 重构 PPT 导航逻辑，优化样式与设置管理
- MainWindow 的 MessageBox 改为 iNKORE.UI.WPF.Modern 的
- 优化 PPT 按钮设置实现，改进数据绑定
- 重构 PPT 导航按钮，使用正确的 Button 控件和样式
- 实现新的统一 PPT 按钮架构，采用 MVVM 模式
- 重构抽奖逻辑并引入 RandViewModel
- 再次重写抽奖功能
- SettingsControl 支持不写 header
- 教学安全模式 (关闭 #4)
- 优化幻灯片墨迹管理与日志记录
- 优化 PowerPoint 服务交互及墨迹处理逻辑
- 移除手动触发放映事件前的等待

功能移除：
- 移除形状绘制功能及相关逻辑 (破坏性变更)
- 移除旧的 UI
- 移除很多用不到的自动收纳/查杀项 (关闭 #10)
- 移除无用的快捷键，仅保留 Ctrl+Z (撤销) 和 Ctrl+Y (重做)
- 移除 OperatingGuideWindow 和工具栏条目
- 移除底部 PPT 导航按钮及相关设置
- 移除主题代码

Bug 修复：
- 修复 PPT 按钮在白板模式下的显示问题
- 修复 Settings.cs 中 using 错误的 Json 序列化库
- 修复页码绑定错误
- 修复有时切换幻灯片会卡死的问题
- 修复各种绑定错误
- 修复放映问题
- 调整导航按钮 UI 并移除废弃逻辑
- 修复 XAML 编译错误 MC3024 - 移除重复的 Background 属性
- 移除无用的 UI 代码

重构与优化：
- 替换 currentMode 为 AppMode 枚举 (关闭 #23)
- 重构白板/黑板模式切换逻辑
- 手动控制 IsInSlideShow 属性
- 移除所有对 BorderFloatingBarExitPPTBtn.Visibility 的判断，改为 _powerPointService.IsInSlideShow
- 完全清理旧的 PPT 按钮实现
- PPT 按钮重构的最终优化 - 位置处理和可见性逻辑
- 主栏的 LayoutTransform 改为 RenderTransform (性能优化)

其他变更：
- 添加 Microsoft.Xaml.Behaviors.Wpf 包引用
- 更改 displayRandWindowNamesInputBtn 设置默认值为 true
- 注释掉 build.yml 中的 .NET Core 安装
- 添加 GitHub Copilot 开发指南
- 在 README 中添加 CHANGELOG.md 超链接
- 更新 README.md
- 移除无用代码（多次）
- 修正错别字

合并的 Pull Requests：
- #17: 添加 GitHub Copilot 开发指南
- #19: 在 README 中添加 CHANGELOG.md 链接
- #20: 优化 PPT 按钮相关功能
- #22: 移除 OperatingGuideWindow 和无用快捷键

---


**本项目的初始提交**相较 `icc-0610fix` 大概已有的变化：
> 具体的commit可以去看我的fork仓库 https://github.com/LiuYan-xwx/icc-0610fix/commits/master/
   - **移除功能：**
     - 墨迹识别（因为我用不到）
     - 白板鸡汤提示
     - 自动更新
   - **Bug修复**
     - 修复 PPT 翻页按钮不显示的问题
     - 修复鼠标指针会隐藏的问题  
   - **技术升级：**
     - 从 `.NET Framework 4.7.2` 升级至 `.NET 8`，以及必要的兼容性修改
     - 升级了 Nuget 各种库，以及必要的兼容性修改
   - **其他优化：**
     - 随机点名有些许修改

---

**本项目建立后**已经做出的：  

常规修改：
- 使用了新的 ICC-Re 图标，各种名字也改了
- 设置页开发者栏目修改
- 移除了侧边栏的快速面板
- 不默认开机自启
- 移除白板的`新页面`按钮，下一页不够会自动加
- 移除白板的icc水印
- 修复白板时间水印不显示的问题
- 使用单文件发布
- 日志重构，结构更清晰，会保存在`./Logs`文件夹下，一个实例一个日志，旧的自动压缩
- 移除`记忆并提示上次播放位置`功能
- 移除`提示隐藏幻灯片`功能
- 移除`提示自动播放`功能
- 移除`单次随机点名人数上限`设置项

技术性修改：
- 移除了所有 COM 引用
- 开机自启创建快捷方式由COM引用方式改用 [WindowsShortcutFactory](https://github.com/gdivis/WindowsShortcutFactory)
- 各种 `System.Timers.Timer` 改为 `DispatcherTimer`
- 自动收纳的代码逻辑重构
- 提取出 `SettingsServcie`，负责管理 `Settings` 的实例和加载、保存
- 提取出 `PowerPointService`，负责连接管理和控制 `PowerPoint` 应用
- 移除无用的白板ui xaml，使用视图模型和转换器自动处理翻页按钮的样式
- 引入 `Microsoft.Extensions.Hosting` 通用主机并使用依赖注入
- 重构 `Settings.cs`，大量使用 `CommunityToolkit.Mvvm` 源生成器
  > ！此更改后设置文件就与之前的不兼容了
- 新增 `FileFolderService`，`GZipHelper`
- 完全重构日志实现，基于 `Microsoft.Extensions.Logging` 框架，分等级记录，一个实例一个日志，旧的自动压缩，存储到`./Logs`文件夹下
- 新增控制台格式化器，有美观的上色，并且Debug编译会自动带一个控制台
- 移除 `DelAutoSavedFiles.cs` 而使用 `FileFolderService`
- 新增 `SettingsControl` 控件，并且设置页大部分改用此控件
- 
