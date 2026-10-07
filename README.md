# taptap 古堡调查原型

当前原型以 `Assets/Docs/程序说明文档.md` 为功能依据，Unity 入口是 `Assets/Castle/Scenes/CastlePrototype.unity`。

- [原型运行与开发说明](Assets/Castle/README.md)
- [六表导入说明](Assets/Castle/Content/README.md)
- [项目交接](Assets/Docs/项目交接.md)
- [协作与同步](Assets/Docs/协作与同步.md)
- [第一版验收记录](Assets/Docs/第一版验收记录.md)
- [程序说明文档（策划原文）](Assets/Docs/程序说明文档.md)
- [游戏系统与技术设计](Assets/Docs/游戏系统与技术设计.md)

正式案件内容尚未交付，当前 CSV 是可替换的独立示例。

## 目录

| 路径 | 用途 |
|---|---|
| Assets/Castle/Scripts/V2/Content | 六表模型、解析与校验 |
| Assets/Castle/Scripts/V2/Configuration | Inspector 配置类型 |
| Assets/Castle/Scripts/V2/Session | 会话、存档模型与磁盘读写 |
| Assets/Castle/Scripts/V2/Gameplay | 事件、道具、录音与判定规则 |
| Assets/Castle/Scripts/V2/UI | 页面、控件和拖放 |
| Assets/Castle/Scripts/V2/Diagnostics | 开发播放器自动检查 |
| Assets/Castle/Editor、Tests | Unity 导入、构建和回归测试 |
| Assets/Castle/Content、Resources、Scenes | 内容表、运行时资源和场景 |
| Assets/Docs | 策划、参考原型、技术与验收文档 |
| Tools | 辅助脚本，使用前阅读 Tools/README.md |
| Assets/Plugins、Assets/TextMesh Pro | 第三方内容，保留原目录及许可 |

Library、Temp、obj、Logs、Builds、.vs 和根目录 csproj/sln 是本地缓存、输出或生成文件，已由 Git 忽略；不需要为了目录整洁逐个删除。
