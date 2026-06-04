# LocalMark

轻量化单机数据标注工具，支持手动标注 + AI 半自动标注。

## 技术栈

- **.NET 8 + WPF** — 桌面客户端
- **SQLite** — 本地数据库，零配置
- **CommunityToolkit.Mvvm** — MVVM 架构
- **Ollama + Qwen2.5-VL 7B** — 图片 AI 自动标注
- **DeepSeek API** — 文本 AI 自动分类（规划中）

## 功能

### 手动标注（已完成）
- 导入文本素材（.txt）和图片素材
- 文本分类标注：读取标签配置，选择分类标签
- 图片框标注：Canvas 拖拽绘制矩形框，坐标自动保存为 JSON
- 标注数据导出为 JSON 格式
- 支持未标注/已标注筛选

### AI 自动标注（开发中）
- 图片一键 AI 标注：调用本地 Qwen2.5-VL 7B 自动检测目标并画框
- 人机协作：AI 初标 → 人工审核修正 → 保存
- 文本素材：对接 DeepSeek API 自动分类（规划中）

## 运行

```bash
# 启动 WPF 应用
dotnet run

# AI 标注功能需先安装 Ollama 并拉取模型
ollama pull qwen2.5vl:7b
```

## AI 标注实测数据

| 指标 | 数值 |
|------|------|
| 模型 | Qwen2.5-VL 7B (Ollama, 6.0 GB) |
| 显存占用 | ~5 GB |
| 单图推理 | 22-27s |
| bbox 输出 | JSON [x1,y1,x2,y2] |

## 硬件要求

| 硬件 | 手动标注 | AI 标注 |
|------|---------|---------|
| GPU 显存 | 无要求 | 8 GB+ |
| 系统内存 | 4 GB | 16 GB+ |
| 磁盘 | 50 MB | 5 GB+（模型文件） |

> Qwen2.5-VL 7B 通过 Ollama 量化部署，模型按需加载，闲置自动释放。WPF 界面零 GPU 开销，仅点击 AI 标注瞬间 GPU 工作。

## 目录结构

```
LocalMark/
├── Model/         实体层
├── View/          视图层 (XAML)
├── ViewModel/     视图模型层 (MVVM)
├── Repository/    数据仓储层 (SQLite CRUD)
├── Helper/        工具类 + OllamaAgent
├── Config/        配置文件 (XML 标签)
└── Converters/    值转换器
```
