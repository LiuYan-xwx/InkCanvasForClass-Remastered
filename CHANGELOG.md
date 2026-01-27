## 更改日志
本文档只是大概记录一下 ICC-Re 所做出的更改，也不全，仅供大致参考以及自己记录看看，无其他意思。  
其实没改什么东西的（）

### 请注意文档的更新时间

---


**本项目的初始提交**相较 `icc-0610fix` 大概已有的变化：（即项目建立之前做出的）
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
- 移除白板的“新页面”按钮，下一页不够会自动加
- 移除白板的icc水印
- 修复白板时间水印不显示的问题
- 使用单文件发布
- 日志重构，结构更清晰，会保存在`./Logs`文件夹下，一个实例一个日志，旧的自动压缩
- 移除“记忆并提示上次播放位置”功能
- 移除“提示隐藏幻灯片”功能
- 移除“提示自动播放”功能
- 移除“单次随机点名人数上限”设置项
- 新增教学安全模式
- 删了很多自动收纳的软件
- 又优化了抽奖逻辑，支持不重复抽取
- 移除了大部分快捷键
- ppt翻页按钮的底部按钮没了
- 移除图形绘制功能
- 优化了设置面板的显隐动画
- 移除了墨迹重播功能
- 支持调节翻页按钮面板宽度
- 新增无焦点模式
- 修复“启动后收纳”功能失效
- 移除 FullScreenHelper
- 改动抽人面板布局
- 新增窗口模式设置(正常全屏和假全屏，这个能解决任务栏问题)
- 优化设置面板动画
- 默认保存笔迹、截图等文件到 ./Saves 下
- 移除 截图分日期文件夹保存 功能
- 调试功能: 强制显示 PPT 翻页按钮

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
- 优化了ppt放映的墨迹管理
- 新增，或提取出来 `PPTNavigationButton`, `PPTNavigationPanel` 自定义控件，翻页按钮替换为这个，但没做底部的按钮
- SidePanel侧边栏的显隐使用数据驱动，包括storyboard
- 删了很多根本没用的xaml和c#代码
- 设置界面的显隐也改为数据驱动
- 重构了这该死的 `currentMode` 字段，使用 `AppMode` enum 类型
- 工具面板的元素改成button控件
- 部分白板的ui也使用数据驱动(根据AppMode)
- 新增 `FloatingBarButton` 自定义控件
- mw.cs里连接powerpoint的定时timer的逻辑移动到 PowerPointService 里
- 新增 CommonDirectories.cs，管理所有路径使用
- 新增 INotificationService，用于显示弹出通知
- 抽离 ScreenshotHelper
- 部分 InkCanvas 属性改为 MVVM 绑定
- 
