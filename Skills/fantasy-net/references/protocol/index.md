# Protocol 入口

**本文件只做分流。** 涉及 `.proto`、`Outer/Inner`、协议导出、导出工具安装时，先在这里判断要读哪个子文档。

## Workflow

```text
定义新协议 -> define.md
已明确是外网协议 -> define-outer.md
已明确是内网协议 -> define-inner.md
字段 / 集合 / Map / 枚举 / 序列化细节 -> define-common.md
运行导出生成 C# -> export.md
安装或配置导出工具 -> export-install.md
```

## 必记规则

1. 协议目录按 `Outer/` 和 `Inner/` 分开
2. Request / Response 注释中的消息名必须与实际名称完全一致
3. 不要手动修改导出生成的 `.cs` 文件
4. 可视化编辑器需要人工操作，AI 优先走命令行导出工具
