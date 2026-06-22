# LocalMark

轻量化单机数据标注工具，支持文本分类标注和图片矩形框标注。

## 技术栈

- **.NET 8 + WPF** — 桌面客户端
- **SQLite** — 本地数据库，零配置
- **CommunityToolkit.Mvvm** — MVVM 架构
- **MVVM 分层** — Model / View / ViewModel / Repository / Helper

## 功能（main 分支）

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

本仓库有两个分支：

| 分支 | 说明 | 状态 |
|------|------|------|
| `main` | 纯手动标注基础版 | 稳定，编译 0 错误 |
| `feature/agent-annotation` | AI 增强版（推进中） | 开发中，见下方进度 |

### feature/agent-annotation 进度

| 功能 | 状态 |
|------|------|
| 图片 AI 标注（本地 Ollama + Qwen2.5-VL） | ✅ 已完成 |
| 文本 AI 分类（DeepSeek / OpenAI 兼容 API） | ✅ 已完成 |
| 批量 AI 标注（多选一键标注 + 进度窗口） | ✅ 已完成 |
| 系统设置中心（Ollama / API / 输出目录配置） | ✅ 已完成 |
| 分文件导出（文本/图片分别输出 + 统计报告） | ✅ 已完成 |
| UI 美化（现代风格 + 全选 + 清除功能） | ✅ 已完成 |
| 一键启动打包（独立 exe，无需安装运行时） | ✅ 已完成 |
| 多目标导航（AI 检出多目标时切换查看） | ⬜ 待开发 |
| 标注结果人工修正审核面板 | ⬜ 待开发 |

```bash
# 体验 AI 增强版
git checkout feature/agent-annotation
dotnet run

# 图片 AI 需先安装 Ollama 并拉取视觉模型
ollama pull qwen2.5vl:7b
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
