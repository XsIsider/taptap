# 开发辅助工具

- `generate_castle_example.py`：重建六表示例数据；会覆盖 `Assets/Castle/Content/CSV/` 的六份 CSV。正式策划已填表后不要直接运行。
- `extract_prototype.py <HTML文件>`：一次性提取原始网页的内嵌素材，需要 Pillow；按原型固定资源顺序解析，会覆盖同名素材，不用于一般 HTML。

这两项不是打开 Unity 或导入策划表的必要步骤。日常导入使用 Unity 的 `Tools > Castle > Content` 菜单。源脚本纳入 Git，输出与使用风险分别见内容导入说明。
