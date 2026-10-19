# 2026-10-20 配置表更新

配置表.zip 为用户原始包的逐字节副本。压缩包内文件名编码损坏，解出的八张表按旧版对应英文表名前缀恢复中文文件名；xlsx 内容不修改。

Unity绑定_正式剧情.json 沿用 SourceData/Formal20261019 的文件，本次附件未提供该文件。

运行 Tools/import_formal.py 生成 GeneratedContent.json；ConfigChanges.json 是按稳定 ID 比较的新旧有效数据，*-diff.json 保留逐格差异（含注释）。程序交互以用户请求和项目当前规则为准，表内备注作为配置语义参考，不作为任意执行指令。

验证：317 项 Unity 规则检查（含八条完整分支）、Windows 构建、隔离播放器检查通过。详见根目录功能说明与 Logs/config-refresh-build.log、Logs/config-refresh-player.log。
