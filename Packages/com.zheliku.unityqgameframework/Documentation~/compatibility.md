# Unity 兼容性与验证

最低安装版本为 **Unity 2022.3**。框架只声明所需内置模块，使用该基线存在的 API。开发宿主使用 Unity 6000.3.24f1，独立消费者验证不携带宿主的 URP、Input System、CLI Pipeline 或其他工具插件。

## 当前验证记录

| Unity 版本 | 验证状态与范围 |
| --- | --- |
| 2022.3.62f3 | 通过独立消费者编译、19 模块创建与单例检查、事件/对象池回归、类型发现、预制体引用、AssetBundle 构建读取和 Windows Mono Player 无图形启动 |
| 6000.0.70f1 | 同上，全部通过 |
| 6000.3.24f1 | 同上，全部通过；另通过 StarForce 菜单进入战斗、原版鼠标移动/射击、21 个 URP 材质的实际帧验证；Console 无新增错误/警告 |

更新日期：2026-10-16。Android JNI 的字节转换已验证原始二进制值不变，但 Android/iOS/WebGL、IL2CPP 和远程热更新尚未做完整平台回归。表中 Windows Player 为基础框架启动，不等同于完整 StarForce Player 资源发行。

属性整理后，上述三个版本均重新通过独立工程的 9 项验证及 Player 启动，新增覆盖 null 元素查询和默认值类型键的哈希计算。开发宿主中 9795 条公共成员与 Unity 序列化字段记录与整理前一致；StarForce 鼠标移动、射击及 URP 检查已重新通过。

## 版本策略

- 不保留 Unity 2021 及更早版本的条件分支；平台、编辑器和输入系统的必要分支继续保留。
- 以 2022.3 公共 API 为共同基线。新 API 只有解决已确认的问题时才引入，并在实际版本边界加条件编译；不按未知未来版本编写猜测分支。
- 继续使用 EditorBuildSettings.scenes 访问活动场景列表，适配旧 Build Settings 与 Unity 6 Build Profiles 的共有入口。
- 核心与渲染管线、输入实现隔离。URP、Input System 和 CLI 的版本由消费者工程选择，升级这些包不要求替换核心。
- 本轮维护和回归范围为 Unity 2022.3、6.0 和 6.3；其他版本后续按需要验证。

这些策略为后续 Unity 6 更新提供可重复维护的基础，不将未运行过的版本描述为已验证。

## 可重复验证

仓库提供 `Tools/Validation/prepare_project.py`，在忽略的 Artifacts 目录创建独立消费者工程，复制当前框架唯一源码，使用基础内置模块，不导入开发宿主设置。

```powershell
python Tools/Validation/prepare_project.py --unity-version 2022.3.62f3 --output Artifacts~/Validation/Foundation/2022.3.62f3-new
```

使用对应 Editor 执行：

```text
Unity.exe -batchmode -nographics -projectPath <生成的工程> -executeMethod Foundation.Validation.FoundationValidation.Run -logFile <日志>
```

验证方法会自行退出 Editor，无需添加 -quit。Windows 验证要求该 Editor 包含 Windows Mono 构建支持，并能使用本机 Unity 许可证。结果为工程中的 Results/editor-result.json、Results/player-result.json 和日志；只有 success=true 且进程成功退出才算通过。

验证覆盖模块单例、拒绝具体类型调用、事件的同步/排队语义、对象池借用/归还、关闭后的重新创建、含 null 的链表范围查询、默认及有效值类型键的哈希计算、后加载程序集、自定义 runtime asmdef 流程发现、21 个序列化辅助器、AssetBundle 构建/加载/卸载，以及实际 Windows Player 的流程启动和组件访问。编译日志应另外检查 CS0618、编译错误和 Unity 迁移警告。

## 官方 API 依据

- [Unity 2022.3 对象查找](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Object.FindObjectsByType.html)
- [Unity 2022.3 刷新率](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Resolution-refreshRateRatio.html)
- [Unity 类型缓存](https://docs.unity3d.com/2022.3/Documentation/ScriptReference/TypeCache.GetTypesDerivedFrom.html)
