# PrintScreenApp v1.0.0

这是 PrintScreenApp 的首个 GitHub Release。PrintScreenApp 是一个基于 C#、.NET 8 和 Windows Forms 的 Windows 截图标注工具，支持全局快捷键截图、区域选择、浮动工具栏标注、自动复制到剪贴板和本地保存。

## Highlights

- 后台常驻运行，支持系统托盘菜单。
- 支持多组可配置全局截图快捷键。
- 支持区域截图，选区可移动、可通过 8 个控制点调整大小。
- 截图后自动进入标注编辑器。
- 支持 7 种标注工具：矩形、圆形、箭头、画笔、荧光笔、橡皮擦、马赛克。
- 支持颜色选择、笔触粗细调整、撤销和重做入口。
- 保存后自动复制到剪贴板。
- 保存后自动写入用户图片目录下的 `PrintScreenApp` 文件夹。
- 支持单实例运行，重复启动会唤起已有实例。

## Default Hotkeys

任意一组默认快捷键都可以开始截图：

| Hotkey | Action |
| --- | --- |
| `Ctrl + Alt + Z` | Start screenshot |
| `Ctrl + Alt + B` | Start screenshot |
| `Ctrl + Win + Z` | Start screenshot |
| `Ctrl + Win + B` | Start screenshot |

如果快捷键被其他软件占用，可以通过托盘菜单中的“设置快捷键...”修改。

## Output

保存截图后会同时执行：

- 复制最终图片到剪贴板。
- 保存 PNG 文件到 `%USERPROFILE%\Pictures\PrintScreenApp`。

## Requirements

- Windows
- .NET 8 Desktop Runtime

## Notes

- 当前区域选择基于鼠标所在屏幕。
- 自动保存格式固定为 PNG。
- 本次本地打包环境无法访问 `C:\Users\yoyo\AppData\Local\Microsoft SDKs`，因此未能在当前环境重新执行 `dotnet publish -c Release`。上传的构建包如使用现有 `bin\Debug\net8.0-windows` 输出，请视为预发布测试包；正式分发建议在 SDK 权限正常的环境重新生成 Release 包。

