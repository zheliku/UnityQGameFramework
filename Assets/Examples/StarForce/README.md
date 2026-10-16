# StarForce 示例

StarForce 是 Unity Q Game Framework 的可运行示例，演示流程、实体、UI、事件、资源加载、对象池和音效的组合使用。源码来源及原作者许可见 [第三方说明](THIRD-PARTY.md)。

示例已迁移至基础维护版的命名空间和 API，资源路径支持开发目录或 UPM 示例导入目录。保留原版鼠标操作，兼容新输入系统；21 个示例材质已转换为 URP 材质。Unity 6000.3.24f1 下已验证菜单进入战斗、鼠标移动和射击。QFramework 架构、Luban 改造及完整示例 Player 构建属于后续阶段。

## 查看与调试

1. 安装与当前 Unity 版本兼容的 Unity UI（`com.unity.ugui`）和 Universal RP（`com.unity.render-pipelines.universal`），并为 Graphics / 当前 Quality 指定 URP 管线资源。当前开发宿主使用 URP 17.3；框架核心不依赖 URP。其他版本的完整示例画面尚未验证。
2. 保存自己当前正在编辑的场景。
3. 打开本目录的 `StarForce Launcher.unity`。不要从 `GameMain/Scenes/Main.unity` 直接启动。
4. 将 Launcher、`GameMain/Scenes/Menu.unity`、`GameMain/Scenes/Main.unity` 加入当前 Build Profile 的场景列表，Launcher 放第一项。编辑器资源模式目前也需要这份列表。
5. 在 `Project Settings > Tags and Layers` 中保留一个名为 `Targetable Object` 的用户层。当前开发宿主已保存此配置；导入到其他工程时需要配置同名层。示例不自动修改宿主的项目设置。
6. 运行后在菜单点击开始，在 Game 视图按住鼠标左键移动并开火。这是原版示例的控制方式，没有 WASD / 方向键移动和空格开火功能。

输入代码只对原有鼠标读取做条件编译适配：启用 Input System 时使用 `Mouse.current`，否则使用旧 Input Manager。示例 UI 使用 `InputSystemUIInputModule`，导入时安装并启用 Input System；使用旧输入系统的工程则将 UI EventSystem 的输入模块替换为 `StandaloneInputModule`。

材质转换保留原有资源 GUID、贴图及 UV 缩放 / 偏移。飞船与陨石使用 URP Lit，背景使用 URP Unlit，粒子与子弹使用 URP Particles/Unlit 的透明加法混合。已在 Unity 6000.3.24f1 的 Play Mode 真实帧中验证鼠标移动和开火，并检查主场景画面与 Shader 编译。完整游戏回归与 Player 构建尚未验证。

源表、配置、实体、UI、场景及演示脚本保留在 `GameMain/`。没有复制上游 `Assets/GameFramework` 子模块、`ProjectSettings` 或 `.git`；框架来自本工程的 embedded package。

上游资源打包事件处理器会清理项目的 `Assets/StreamingAssets`，本轮未执行该工具。首次资源构建与正式游戏构建将在后续适配中验证。

## 开发与发布

本仓库的唯一可编辑示例源码是 `Assets/Examples/StarForce/`。框架核心不引用示例代码，也不依赖示例的 LitJson / protobuf-net。

运行下列命令生成发行暂存包：

```powershell
python Tools/Packaging/prepare_upm.py
```

工具在被 Git 忽略的 `Artifacts~/UPM/<版本>/` 中生成同路径的 UPM 包目录和 `.tgz`，把当前示例复制到发行包的 `Samples~/StarForce`，自动登记 `package.json.samples`，保留 `.meta` 并校验内容一致。不会修改开发源码；输出已存在时拒绝覆盖。

发行包消费者在 Package Manager 的 Samples 面板点击 Import 后，示例会复制到 `Assets/Samples/UnityQGameFramework/<版本>/StarForce`，再正常编辑与调试。资源定位使用 `GameMain.meta` 的 GUID，支持该导入路径。

本仓库开发工程已经含有相同示例，因此不要再导入发行包中的同一份示例，否则会产生相同脚本、类型和 GUID。开发分支的 Git URL 当前提供框架；未来包含示例的发行标签将指向生成的发布树，见框架包内的目录规划。

## 已做的接入调整

- 资源路径集中到 `AssetUtility.GameMainPath`，编辑器按 GameMain 目录 GUID 定位；Player 按首个启动场景定位并缓存。
- 原配置路径与旧读表工具改用同一根路径，不再写死 `Assets/GameMain`。
- 保留原版鼠标玩法，只对鼠标输入增加新输入系统兼容分支。
- 跳过已回收实体的延迟碰撞回调，避免同一颗子弹重复伤害、重复回收。
- 将 21 个示例材质转换为 URP 材质，不引入自定义 Shader 或新的运行时组件。
- 保留原有脚本 GUID 和资源引用。示例使用 `Assembly-CSharp` / `Assembly-CSharp-Editor`；框架也支持自定义业务 asmdef 的类型发现。示例 `link.xml` 保留运行时通过反射创建的 StarForce 类型，移入自定义程序集时需同步更新它。

上游 MIT 许可保留在 [LICENSE.md](LICENSE.md)。示例的第三方库见 [THIRD-PARTY.md](THIRD-PARTY.md)。
