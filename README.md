# LocalMark

轻量化单机数据标注工具，支持文本分类标注和图片矩形框标注。

## 技术栈

- **.NET 8 + WPF** — 桌面客户端
- **SQLite** — 本地数据库，零配置
- **CommunityToolkit.Mvvm** — MVVM 架构
- **MVVM 分层** — Model / View / ViewModel / Repository / Helper

## 功能

- 导入文本素材（.txt）和图片素材
- 文本分类标注：读取标签配置，选择分类标签
- 图片框标注：Canvas 拖拽绘制矩形框，坐标自动保存为 JSON
- 标注数据导出为 JSON 格式
- 支持未标注/已标注筛选

## 运行

```bash
dotnet run
```

## 项目状态

v1.0 基础版已完成，编译 0 错误 0 警告，可直接运行。

## 目录结构

```
LocalMark/
├── Model/         实体层
├── View/          视图层 (XAML)
├── ViewModel/     视图模型层 (MVVM)
├── Repository/    数据仓储层 (SQLite CRUD)
├── Helper/        工具类
├── Config/        配置文件 (XML 标签)
└── Converters/    值转换器
```
