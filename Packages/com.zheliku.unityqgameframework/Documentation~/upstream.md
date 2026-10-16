# 源码来源记录

本文件仅用于来源追溯。产品文档、帮助入口、示例介绍和运行时版本均使用 Unity Q Game Framework；不随包保留旧项目的宣传 README 或旧 package.json。

| 来源 | 基线提交 | 当前用途 |
| --- | --- | --- |
| UnityGameFramework | e7eb3ef11393df0ac504204c480cdb839752b05f | Unity 组件及编辑器工具的初始来源 |
| GameFramework | d0c010b05167c58e92350449d04864a91ca13fd2 | 纯 C# 模块的初始来源 |
| StarForce | d4ce803353f89852c4d58df65b2482a3b08eb11f | 可选演示的初始来源 |

最初导入于 2026-10-16。基础维护版已迁移命名空间、程序集和文件布局，并更新 Unity API；当前源码不再宣称与基线逐字节相同。保留已有资产和脚本 GUID，删除的 WWW 辅助器在最低支持版本中本来不参与编译。

版权、原 MIT 许可及第三方压缩库许可完整保留，见 [第三方声明](../Third%20Party%20Notices.md) 和 Licenses 目录。公共 API 迁移见 [migration.md](migration.md)，维护内容见包 CHANGELOG。
