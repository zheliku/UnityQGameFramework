# 目录与发布约定

仓库根是 Unity 6.3 开发宿主。框架唯一源码放在 embedded package `Packages/com.zheliku.unityqgameframework`；Assets 用于示例与集成验证，不维护第二份框架源码。

## 程序集边界

```text
UnityQGameFramework.Core
    ↑
UnityQGameFramework.Runtime
    ↑
UnityQGameFramework.Editor
```

Editor 同时引用 Core；Core 开启 `noEngineReferences`，仅 Core 因二进制转换工具开启 unsafe。Unity 版本相关 API 位于 Runtime/Unity 和 Editor。运行时程序集不引用示例、渲染管线或开发工具。

包声明所需 Unity 内置模块。宿主的 URP、Input System、Alchemy、Rainbow Folders 和 Unity CLI Pipeline 留在宿主清单。消费者可独立选择自己的渲染管线和工具。

## StarForce

唯一可编辑源码为 `Assets/Examples/StarForce/`。开发时可以直接进入常规脚本、场景和资源工作流。发行时运行：

```powershell
python Tools/Packaging/prepare_upm.py
```

工具在被忽略的 `Artifacts~/UPM/<版本>/` 下创建同路径的包目录，将示例复制到生成包的 `Samples~/StarForce`，登记 Package Manager 的 Samples 入口，核对文件字节和 GUID，并生成 `.tgz`。输出已存在时拒绝覆盖；可用 `--output` 指定新的 Artifacts 子目录。

开发分支 Git URL 提供框架。含示例的发行标签应指向生成的发布树，保留原包路径，消费者在 Git URL 末尾追加标签。Samples~ 被 Unity 正常导入流程忽略，用户点击 Import 后才进入 Assets。本仓库已含示例，不应再导入同一份示例，避免类型与 GUID 重复。

## 发布产物

- UPM：从包子目录安装，发行归档根为 `package/`。
- unitypackage：将同一份包源码暂存到独立导出工程的 Assets，保留 GUID 后导出；不携带宿主设置、缓存和工具插件。
- GitHub：来源和许可证随源码保留，生成产物放 Releases；本次维护不自动提交、推送或发布。

正式发布前应在干净消费者工程验证 Git URL、归档安装和示例导入。

## 后续阶段

Architecture、Model、System、Utility、BindableProperty 与 Luban 将各自建立明确的契约和生命周期，再接入现有资源、实体、UI、流程、事件与对象池。游戏源表、生成代码和业务数据属于消费者工程，不写入只读的 UPM 缓存。本基础维护版继续提供现有 Config/DataTable。
