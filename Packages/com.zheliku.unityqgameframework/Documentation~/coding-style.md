# C# 代码约定

保持 Unity 2022.3 支持的 C# 9 语法和现有公共 API。基础版只整理实现，不引入新的架构层。

- 只有存储职责的非序列化属性使用自动属性；只在构造时赋值的只读字段使用 `{ get; }`，类内可修改的只读 API 使用 `{ get; private set; }`。
- 简单计算属性使用单行 `=>`，过长表达式或包含多个步骤的 getter 保留代码块。包含校验、通知、缓存和其他副作用的 setter 保留原有逻辑。
- Unity 序列化字段保留 `[SerializeField]` / `[SerializeReference]` 和原字段名；对外访问可使用简短 getter/accessor。不要直接改成 `[field: SerializeField]` 自动属性，否则 backing field 名称变化会影响已有资产和 Inspector。
- `[Serializable]` 类型、固定二进制布局、字段特性、`ref/out` 和可变值类型的操作需要明确字段，不能只为缩短代码替换。
- 保留 XML 文档、版权和必要的平台分支；不为了统一外观重写第三方源码、生成的工程文件或资产 YAML。
- 集合查询使用对应的默认比较器，允许合法的 null 元素；值类型的默认实例应能执行相等比较和哈希计算。修复边界行为时保留合法输入的原有结果，并补充回归验证。

根目录 `.editorconfig` 提供 IDE 建议。验证使用 `Tools/Validation/prepare_project.py` 创建独立消费者工程，另核对公共成员和 Unity 序列化字段，以防格式整理改变可见 API 或资产绑定。
