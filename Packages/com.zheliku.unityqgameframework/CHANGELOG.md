# Changelog

## Unreleased — Luban DataTable

- 生成输出增加文件写入失败回滚，保留生成物／Profile／Catalog 的 GUID 与原状态；恢复失败时保留可定位的备份清单。拒绝无效 Catalog 和未保存的 Profile。
- 下载增加超时；外部进程默认 300 秒超时，可在项目设置中调整本机超时和编译器路径。数据浏览移除每次绘制的反射和整表数组分配。
- 新增包检查与真实 Git 消费者验证工具；覆盖首次安装、自动工作区、下载编译器、菜单生成和 Windows Mono Player。
- 默认工作区及 StarForce 使用官方 Excel schema；Profile Inspector 显示 namespace／manager／groups，并定位其 luban.conf 配置源。
- 游戏 Config 改为 Tomlyn 解析 TOML；提供 TextAsset 导入器，保留键值查询与资源加载流程，移除默认辅助器中的旧 TXT／二进制解析。
- 规范化 XLSX 命名空间以兼容表格差异查看器，保持单元格与生成二进制内容。
- DataTableComponent 保留场景节点与查询接口，使用官方 Luban 生成的数据；整批解析引用、版本检查、原子注册、失败事件、资源模式加载、取消和整组卸载。
- 新增 Luban 一键生成菜单、可移动目录 Profile、固定版本下载／校验、生成前校验、只同步工具拥有的输出与运行时表浏览。
- 新增可共享的全局 Profile 项目设置、首次安装工作区初始化与 DataTable Inspector 的方案选择／设为全局／Catalog 绑定；文件夹菜单区分 Unity 路径和 Luban 输出路径。
- 修正 StarForce 构建场景与资源编辑器的旧路径，使用相对配置文件的路径适配 Samples 导入；场景配置无效时保留宿主现有场景列表。
- 删除旧 DataTable TXT／逐行二进制解析、辅助器及 StarForce 旧表生成器；11 张示例表迁移为 Excel + schema，业务查询保持原方式。
- 内置隔离命名空间的最小 Luban runtime；包不引用游戏类型，不要求额外 Git 依赖。

## 0.1.0-exp.2 — 基础维护版

- 整理重复的字段/属性及简单 getter，保留 Unity 序列化字段和访问权限；增加 C# 代码约定与 IDE 建议。
- 修复链表范围包含 null 元素时的查询，以及默认值类型键的哈希计算；保留合法键的原有哈希结果。
- 统一 Unity Q Game Framework 名称、菜单、默认预制体、程序集与公共命名空间；同步示例、反射类型字符串、资源工具 XML 和 linker 配置。
- 清理旧项目帮助、宣传与演示服务链接；来源记录和许可证集中保留。运行时版本改为当前包版本。
- 最低 Unity 版本提升为 2022.3，移除仅用于更早版本的条件分支及未生效的 WWW 辅助器。
- 更新对象查找、刷新率、脚本宏定义、JNI 和资源打包 API，保留原有功能及数据格式。
- 编辑器类型发现支持业务 asmdef，使用 TypeCache；核心类型查找支持后加载程序集并容忍部分类型加载失败。
- 缓存模块及组件查找，保留模块更新/关闭顺序；修复未初始化配置模块关闭异常、组件注册残留和低内存订阅释放。
- 流程初始化改为等待其他组件 Start 完成后的下一帧，支持无图形 Player；初始化前读取流程状态安全返回空状态。
- 提供独立消费者验证工具，覆盖核心语义、资源包构建读取与 Windows Player 启动。具体版本结果见 Documentation~/compatibility.md。

## 0.1.0-exp.1 — 初始接入

- 建立单一 embedded UPM package 与可调试 StarForce 示例，提供 Samples~ 发行暂存工具。
- 修复废弃 AssetBundle 选项、Debugger 剪贴板初始化和空音频资源时间设置。
- 保留原版鼠标控制，兼容新输入系统；转换 21 个示例 URP 材质，并跳过已回收实体的延迟碰撞。
