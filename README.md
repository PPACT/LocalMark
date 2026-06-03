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

项目正在持续完善中，欢迎关注。

## Agent 自动标注（规划中）

计划引入 AI Agent 实现半自动标注，人从操作者转变为审核者。

### 模型方案

| 标注类型 | 模型 | 部署方式 | 资源占用 |
| ---- | ---- | ---- | ---- |
| 文本分类 | DeepSeek API | 云端调用 | 无本地开销 |
| 图片检测 | Qwen2-VL 7B | Ollama 本地 | ~5 GB 显存 |

### 硬件要求（Agent 版）

| 硬件 | 最低要求 | 推荐配置 |
| ---- | ---- | ---- |
| GPU 显存 | 8 GB | 16 GB+ |
| 系统内存 | 16 GB | 32 GB |
| 磁盘 | 5 GB（模型文件） | 10 GB+ |

> Qwen2-VL 7B 通过 Ollama 量化部署，仅占用约 5 GB 显存。模型按需加载，闲置自动释放，不会常驻显存。运行时：WPF 界面零 GPU 开销，仅点击 AI 标注瞬间 GPU 工作几秒。

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
