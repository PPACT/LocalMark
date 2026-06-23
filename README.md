# LocalMark

轻量化单机数据标注工具，支持文本分类标注和图片矩形框标注。

## 技术栈

- **.NET 8 + WPF** — 桌面客户端
- **SQLite** — 本地数据库，零配置
- **CommunityToolkit.Mvvm** — MVVM 架构

## 功能（main 分支）

- 导入文本素材（.txt）和图片素材
- 文本分类标注：读取标签配置，选择分类标签
- 图片框标注：Canvas 拖拽绘制矩形框，坐标自动保存为 JSON
- 标注数据导出为 JSON（文本/图片分文件，含统计报告）
- 未标注/已标注筛选 + 全选
- 重复文件导入检测
- 现代化 UI（圆角卡片、行悬停、高亮提示）

## 运行

```bash
# 开发启动
launch.bat

# 或直接
dotnet run

# 打包为独立 exe（无需 .NET 运行时）
publish.bat
```

## 项目状态

本仓库有两个分支：

| 分支 | 说明 | 状态 |
|------|------|------|
| `main` | 纯手动标注基础版 | 稳定，编译 0 错误 |
| `feature/agent-annotation` | AI 增强版 | 开发中 |

```bash
# 体验 AI 增强版
git checkout feature/agent-annotation
dotnet run
```
## 目录结构

```
LocalMark/
├── Model/         实体层
├── View/          视图层 (XAML)
├── ViewModel/     视图模型层 (MVVM)
├── Repository/    数据仓储层 (SQLite CRUD)
├── Helper/        工具类 + AI Agent
├── Config/        配置文件 (XML 标签 + JSON 设置)
└── Converters/    值转换器
```
