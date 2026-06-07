# PrintScreenApp

PrintScreenApp 是一个 Windows 截图和标注工具，使用 C#、.NET 8 和 Windows Forms 实现。程序启动后常驻后台，可以通过全局快捷键或托盘菜单开始区域截图，截图确认后进入标注窗口，保存时自动复制到剪贴板并保存为 PNG 文件。

## 主要功能

- 后台常驻，支持系统托盘菜单
- 默认写入当前用户登录启动项，登录 Windows 后自动在后台监听快捷键
- 单实例运行，重复启动会唤起已有实例
- 多组可配置全局截图快捷键
- 鼠标框选截图区域
- 选区可移动、可通过 8 个控制点调整大小
- Enter 或双击确认截图，Esc 或右键取消
- 截图后可进行标注编辑
- 支持画笔、箭头、马赛克、矩形、圆形
- 支持标注颜色和粗细调整
- 保存后自动复制到剪贴板
- 保存后自动写入本地图片目录

## 运行环境

- Windows
- .NET 8 SDK 或 .NET 8 Desktop Runtime

项目目标框架：

```text
net8.0-windows
```

## 编译运行

在项目根目录执行：

```powershell
dotnet build
dotnet run
```

也可以直接运行编译后的程序：

```text
bin\Debug\net8.0-windows\PrintScreenApp.exe
```

程序启动后默认最小化运行，可以从任务栏或系统托盘恢复窗口。

首次运行后，程序会自动启用“Run at startup”。之后每次登录 Windows，它都会在后台启动并注册截图快捷键，不需要再手动打开 exe。可以在托盘菜单中取消勾选 `Run at startup` 来关闭自启动。

## 使用方法

1. 启动程序。
2. 按截图快捷键，或右键托盘图标选择“立即截图”。
3. 拖动鼠标选择截图区域。
4. 选区生成后，可拖动选区内部移动位置，也可拖动边角控制点调整大小。
5. 按 Enter 或双击选区确认截图。
6. 在标注窗口中添加标注。
7. 点击保存按钮完成截图。

取消方式：

- 选区阶段：按 Esc 或右键。
- 标注阶段：按 Esc 或点击取消按钮。

## 默认截图快捷键

默认配置了 4 组截图快捷键，任意一组都可以触发截图：

| 快捷键 | 作用 |
| --- | --- |
| `Ctrl + Alt + Z` | 开始截图 |
| `Ctrl + Alt + B` | 开始截图 |
| `Ctrl + Win + Z` | 开始截图 |
| `Ctrl + Win + B` | 开始截图 |

如果快捷键被其他软件占用，可以从托盘菜单打开“设置快捷键...”进行修改。

快捷键配置文件保存位置：

```text
%APPDATA%\PrintScreenApp\hotkeys.json
```

## 标注工具

| 工具 | 快捷键 | 说明 |
| --- | --- | --- |
| 画笔 | `1` | 自由绘制线条 |
| 箭头 | `2` | 绘制指向箭头 |
| 马赛克 | `3` | 模糊矩形区域 |
| 矩形 | `4` | 绘制矩形框 |
| 圆形 | `5` | 绘制圆形或椭圆框 |

其他编辑操作：

| 操作 | 方式 |
| --- | --- |
| 选择颜色 | 工具栏颜色按钮 |
| 调整粗细 | 工具栏滑块 |
| 撤销 | `Ctrl + Z` 或工具栏按钮 |
| 重做 | `Ctrl + Y` 或工具栏按钮 |
| 保存 | 工具栏保存按钮 |
| 取消 | `Esc` 或工具栏取消按钮 |

## 保存位置

保存后会同时执行：

- 复制最终截图到剪贴板
- 保存 PNG 到用户图片目录

默认目录：

```text
%USERPROFILE%\Pictures\PrintScreenApp
```

文件名格式：

```text
Screenshot_yyyyMMdd_HHmmss.png
```

## 托盘菜单

托盘菜单包含：

- 显示
- 隐藏
- 立即截图
- 设置快捷键...
- Run at startup
- 退出

## 项目结构

| 文件 | 说明 |
| --- | --- |
| `Program.cs` | 程序入口、单实例控制 |
| `Form1.cs` | 主窗体、托盘菜单、快捷键、截图流程、自动保存 |
| `RegionSelectorForm.cs` | 全屏区域选择窗口 |
| `AnnotationEditorForm.cs` | 标注编辑窗口 |
| `ToolbarForm.cs` | 浮动标注工具栏 |
| `IAnnotationTool.cs` | 标注工具接口 |
| `PenTool.cs` | 画笔工具 |
| `ArrowTool.cs` | 箭头工具 |
| `MosaicTool.cs` | 马赛克工具 |
| `RectangleTool.cs` | 矩形工具 |
| `CircleTool.cs` | 圆形工具 |
| `DrawingManager.cs` | 编辑历史状态管理 |
| `HotKeyConfig.cs` | 快捷键配置读写 |
| `HotKeySettingsForm.cs` | 快捷键设置界面 |
| `GlobalKeyboardHook.cs` | 低级键盘钩子 |
| `ScreenshotHelper.cs` | 截图保存和剪贴板辅助 |

## 日志

程序会在运行目录写入日志：

```text
PrintScreenApp.log
```

日志会记录快捷键注册、截图触发、保存和异常信息，方便排查问题。

## 注意事项

- 当前项目只面向 Windows 桌面环境。
- 区域选择基于鼠标所在屏幕。
- 自动保存格式固定为 PNG。
- 如果所有快捷键注册失败，请在“设置快捷键...”里换一组未被占用的组合。
- 当前编辑器已经接入撤销/重做入口和历史管理类；如果需要严格逐笔撤销，建议继续完善每次绘制提交后的历史快照同步。
