# PrintScreenApp v2.0 - 微信/QQ风格截图编辑工具

![Language](https://img.shields.io/badge/Language-C%23-green)
![Framework](https://img.shields.io/badge/Framework-.NET%208.0-blue)
![License](https://img.shields.io/badge/License-MIT-yellow)

## 📱 概述

PrintScreenApp是一个Windows截图工具，现已升级为具有**微信/QQ风格截图编辑功能**的应用。截图完成后，应用会自动弹出编辑器，支持画笔、箭头、马赛克、矩形、圆形等5种标注工具，以及完整的撤销/重做功能。

## ✨ 主要特性

### 🎨 5种标注工具
- **画笔 (Pen)** - 自由绘制线条
- **箭头 (Arrow)** - 绘制指向箭头
- **马赛克 (Mosaic)** - 区域模糊处理，保护隐私
- **矩形 (Rectangle)** - 矩形框标注
- **圆形 (Circle)** - 圆形框标注

### 🎮 完整的编辑功能
- 实时颜色选择
- 可调笔画粗细 (1-15)
- 无限撤销/重做
- 快捷键支持
- 一键保存

### ⚡ 快捷键
- `Alt+Q` - 启动截图
- `1-5` - 切换工具
- `Ctrl+Z` - 撤销
- `Ctrl+Y` - 重做
- `Esc` - 取消编辑

## 🚀 快速开始

### 安装要求
- Windows 10/11
- .NET 8.0 Runtime

### 基本用法

```
1. 按 Alt+Q 进入截图模式
   或点击主窗口中的"区域截图"按钮

2. 拖拽选择要截图的区域

3. 编辑器自动打开

4. 使用工具进行标注
   - 点击工具按钮或按数字键 (1-5) 选择工具
   - 点击 Color 选择颜色
   - 拖动 Size 滑块调整粗细
   - 在图像上绘制

5. 点击 "✓ Save" 保存或 "✕ Cancel" 放弃
```

## 📖 详细文档

- **[ANNOTATION_GUIDE.md](ANNOTATION_GUIDE.md)** - 完整使用指南和故障排除
- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - 快速参考和快捷键
- **[CHANGELOG.md](CHANGELOG.md)** - 版本更新日志
- **[IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md)** - 技术实现细节
- **[VERIFICATION_CHECKLIST.md](VERIFICATION_CHECKLIST.md)** - 功能验证清单

## 🏗️ 项目结构

```
PrintScreenApp/
├── 核心文件
│   ├── Form1.cs                      # 主窗体
│   ├── RegionSelectorForm.cs         # 区域选择窗体
│   ├── ScreenshotHelper.cs           # 截图辅助类
│   ├── HotKeyManager.cs              # 快捷键管理
│   └── Program.cs                    # 程序入口
│
├── 编辑器和管理
│   ├── AnnotationEditorForm.cs       # ✨ 编辑器主窗体
│   ├── AnnotationEditorForm.Designer.cs
│   └── DrawingManager.cs             # 绘制操作管理
│
├── 标注工具
│   ├── IAnnotationTool.cs            # 工具接口
│   ├── PenTool.cs                    # 画笔工具
│   ├── ArrowTool.cs                  # 箭头工具
│   ├── MosaicTool.cs                 # 马赛克工具
│   ├── RectangleTool.cs              # 矩形工具
│   └── CircleTool.cs                 # 圆形工具
│
├── 文档
│   ├── ANNOTATION_GUIDE.md           # 使用指南
│   ├── QUICK_REFERENCE.md            # 快速参考
│   ├── CHANGELOG.md                  # 版本日志
│   ├── IMPLEMENTATION_SUMMARY.md     # 实现总结
│   └── VERIFICATION_CHECKLIST.md     # 验证清单
│
└── 项目文件
    ├── PrintScreenApp.csproj
    └── README.md                      # 本文件
```

## 🎯 工作流程

```
┌─────────────────────┐
│ 启动应用 Alt+Q      │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ 全屏选择器          │
│ (拖拽选择区域)      │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ 编辑器自动打开 ✨  │
│ (显示截图内容)      │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────┐
│ 选择工具进行标注    │
│ 1-5: 工具选择       │
│ Color: 颜色选择     │
│ Size: 粗细调节      │
└──────────┬──────────┘
           │
           ▼
    ┌──────┴──────┐
    │             │
    ▼             ▼
┌────────┐   ┌────────┐
│ 保存   │   │ 取消   │
│✓Save   │   │✕Cancel │
└────────┘   └────────┘
```

## 💻 技术栈

- **语言**: C# 
- **框架**: .NET 8.0 with Windows Forms
- **API**: System.Drawing (GDI+)
- **设计模式**: 
  - Strategy (工具系统)
  - Command (撤销/重做)
  - Composite (UI组件)

## 🔧 架构设计

### 可插拔工具系统
所有标注工具实现`IAnnotationTool`接口，支持轻松扩展。

```csharp
public interface IAnnotationTool
{
    string Name { get; }
    Color ToolColor { get; set; }
    int ToolSize { get; set; }
    
    void OnMouseDown(MouseEventArgs e, Graphics g, Bitmap bitmap);
    void OnMouseMove(MouseEventArgs e, Graphics g, Bitmap bitmap);
    void OnMouseUp(MouseEventArgs e, Graphics g, Bitmap bitmap);
    void DrawPreview(Graphics g);
    void Commit(Graphics g, Bitmap bitmap);
    void Reset();
}
```

### 完整的编辑历史
`DrawingManager`使用栈结构实现无限撤销/重做。

```csharp
public class DrawingManager
{
    private Stack<Bitmap> _undoStack;   // 撤销栈
    private Stack<Bitmap> _redoStack;   // 重做栈
    
    public bool Undo() { ... }
    public bool Redo() { ... }
    public void SaveState() { ... }
}
```

## 📊 代码统计

| 项目 | 数值 |
|------|------|
| 新增文件 | 10个 |
| 修改文件 | 1个 |
| 总代码行数 | ~1800行 |
| 注释行数 | ~200行 |
| 文档文件 | 5个 |

## 🎓 使用场景

### 场景1: 工作汇报
```
截图 → 用箭头指出重点 → 用矩形框出关键数据 → 保存
```

### 场景2: 隐私保护
```
截图 → 用马赛克隐藏个人信息 → 分享
```

### 场景3: 技术文档
```
截图 → 用画笔标注代码 → 用圆形标注错误位置 → 保存
```

## 🐛 故障排除

### 编辑器不打开
- ✓ 确认截图区域有效（宽高都 > 0）
- ✓ 检查系统资源是否充足
- ✓ 重启应用

### 标注效果不理想
- ✓ 调整笔画大小
- ✓ 检查颜色选择
- ✓ 尝试撤销并重新操作

### 性能问题
- ✓ 减小截图区域大小
- ✓ 简化标注操作
- ✓ 关闭其他应用

更多问题见 [ANNOTATION_GUIDE.md](ANNOTATION_GUIDE.md#故障排除)

## 🔜 未来计划

- [ ] **文字工具** - 添加文本标注
- [ ] **荧光笔** - 高亮效果
- [ ] **橡皮擦** - 清除标注
- [ ] **图层支持** - 多层编辑
- [ ] **历史面板** - 可视化编辑历史
- [ ] **性能优化** - 支持超大分辨率
- [ ] **主题系统** - 暗色模式
- [ ] **快捷菜单** - 右键快捷菜单

## 📝 许可证

MIT License - 详见 [LICENSE](LICENSE)

## 🤝 贡献

欢迎提交Issue和Pull Request！

## 📮 反馈

遇到问题或有建议？
1. 查看 [ANNOTATION_GUIDE.md](ANNOTATION_GUIDE.md)
2. 检查 [VERIFICATION_CHECKLIST.md](VERIFICATION_CHECKLIST.md)
3. 提交Issue或PR

## 🎉 致谢

感谢所有使用和支持此项目的用户！

---

**PrintScreenApp v2.0**  
微信/QQ风格的截图编辑工具  
让截图编辑变得简单快速！

**最后更新**: 2026-05-26  
**版本**: 2.0.0  
**状态**: ✅ 生产就绪
