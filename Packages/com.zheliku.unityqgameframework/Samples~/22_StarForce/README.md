# StarForce

在 URP 工程中打开 `StarForce Launcher.unity` 并运行。Game 视图选 3:4；点击“开始”后按住鼠标左键移动并开火。

代码阅读顺序见包内 `Documentation~/05-starforce.md`。

修改表格时，在 `QGF > Settings` 选择 `UnityQGameFramework/Config/Editor/LubanProfile.asset`，编辑 `UnityQGameFramework/Config/Source/Datas` 下的 Excel，再执行 `QGF > Generate Tables`。启动参数和语言字典使用 TOML。

构建 Windows Player 时执行 `QGF > Examples > 22 StarForce > Build Windows Player`。

资源来源见 [THIRD-PARTY.md](THIRD-PARTY.md)，许可见 [LICENSE.md](LICENSE.md)。
