# StarForce

宿主需启用 Input System：在 Player Settings 的 Active Input Handling 选择 Input System Package (New) 或 Both。示例不会替项目修改该设置。

在 URP 工程中打开 `StarForce Launcher.unity` 并运行。Game 视图选 3:4；点击“开始”后按住鼠标左键移动并开火。

导入 Sample 后等待编译完成即可运行，默认使用 YooAsset Editor Simulation，无需先点击 `Fix` 或 `Save`。构建应用时，将资源配置的`Play Mode`设为 `OfflinePlayMode`；在`Open Builder`打开 YooAsset Builder，选择对应包并使用完整首包复制选项，将内置文件输出到项目 `Assets/StreamingAssets/yoo`。再用 Unity 的 Build Profiles（2022.3 为 Build Settings）选择启动场景并构建。

代码阅读顺序见包内 `Documentation~/05-starforce.md`。

文本统一使用 TMP。首次使用 TMP 的宿主工程按官方提示导入 TMP Essential Resources。语言字典直接放在 `GameMain/Localization/en.toml`、`zh-Hans.toml`、`zh-Hant.toml` 和 `ko.toml`；字体资产在 `GameMain/Fonts`。设置窗口提交语言后立即异步应用，菜单和池化界面由 LocalizeText 自动刷新，不需要重启。

修改表格时，在 DataTable 组件 Inspector 选择 `UnityQGameFramework/Config/LubanProfile.asset` 并点击 `Set Global`，编辑 `UnityQGameFramework/Config/Source` 下的 Excel，再点击 `Generate`。命名空间和源表路径在 `UnityQGameFramework/Config/luban.conf` 设置；组件中展开 `Generation Settings` 可直接编辑 Profile。启动参数和语言字典使用 TOML。

资源来源见 [THIRD-PARTY.md](THIRD-PARTY.md)，许可见 [LICENSE.md](LICENSE.md)。
