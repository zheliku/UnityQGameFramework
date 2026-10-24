# Third Party Notices

UnityQGameFramework 的框架源码沿用 MIT 许可；下列组件保留各自版权与许可。`package.json` 中的 MIT 不替代第三方组件许可。

## UnityGameFramework / GameFramework

- Copyright (c) 2013-2021 Jiang Yin.
- License: MIT.
- Sources: https://github.com/EllanJiang/UnityGameFramework and https://github.com/EllanJiang/GameFramework.
- UnityGameFramework baseline: `e7eb3ef11393df0ac504204c480cdb839752b05f`; GameFramework baseline: `d0c010b05167c58e92350449d04864a91ca13fd2`. Initially imported on 2026-10-16; namespaces, assemblies, Unity APIs and data-table implementation have since changed.
- Full license texts: [UnityGameFramework](Documentation~/Licenses/UnityGameFramework-MIT.md), [GameFramework](Documentation~/Licenses/Framework-MIT.md), and [LICENSE.md](LICENSE.md).

## StarForce UI artwork

- The tutorial button outline and checkmark are copied unchanged from StarForce `Assets/GameMain/UI/UISprites/Common/button-outline.png` and `checked.png`, commit `d4ce803353f89852c4d58df65b2482a3b08eb11f`.
- Copyright (c) 2013-2021 Jiang Yin; upstream MIT notice retained in [LICENSE.md](LICENSE.md) and [StarForce's license](Samples~/02_StarForce/LICENSE.md).
- Each numbered Sample carries its own small UI images, so importing a module does not require importing StarForce.

## SharpZipLib 0.86.0.518

- Imported unchanged from UnityGameFramework's `Libraries/ICSharpCode.SharpZipLib.dll`.
- Binary copyright: Copyright 2001-2010 Mike Krueger, John Reilly.
- This legacy version uses GPL v2 or later with the upstream linking exception. It must not be described as the MIT-licensed modern SharpZipLib release.
- [GPL text](Documentation~/Licenses/SharpZipLib-0.86-COPYING.txt).
- [License header and linking exception](Documentation~/Licenses/SharpZipLib-0.86-exception.txt), copied verbatim from the corresponding source tag's `src/GZip/GZipInputStream.cs` header.
- Corresponding source: https://github.com/icsharpcode/SharpZipLib/tree/0.86.0.518.
- Source archive: https://github.com/icsharpcode/SharpZipLib/archive/refs/tags/0.86.0.518.tar.gz.
- Legacy release and license information: https://icsharpcode.github.io/SharpZipLib/legacy.html.

The binary is retained to preserve the upstream compression baseline. Dependency replacement is a later modernization task.

## Luban

- Official generator: [Luban v5.1.0](https://github.com/focus-creative-games/luban/tree/v5.1.0), commit `df34215d2e22035073d1af83d44e3ac0744dc3b3`.
- Templates copied from `Templates/cs-bin/bean.sbn`, `table.sbn`, `tables.sbn`; changes adapt the private runtime namespace and make generated tables implement the QGF table contract. License retained at `Editor/Luban/Templates~/LICENSE.txt`.
- Minimal binary runtime: [luban_unity](https://github.com/focus-creative-games/luban_unity/tree/061314200e2f03307c4d90b448e1de7b85431905), commit `061314200e2f03307c4d90b448e1de7b85431905`, package version 1.2.0. Only `ByteBuf.cs`, `BeanBase.cs`, `StringUtil.cs` are bundled. Copyright 2025 Code Philosophy, MIT.
- Runtime changes: namespace `Luban` → `UnityQGameFramework.Luban`, separate assembly with unsafe code enabled. Full runtime license: `Runtime/Luban/LICENSE.txt`.
- The compiler is downloaded on demand into Library, with release SHA-256 `bac9a1b8d69cfeaa7ef1d8c67b34a007324d1cd158e0b14294521b3879ba5213`. No compiler binaries are bundled in the UPM package.
- Upstream source hashes and version lock: `Editor/Luban/luban-lock.json`.

## Tomlyn 0.19.0

- Official package: [Tomlyn 0.19.0](https://www.nuget.org/packages/Tomlyn/0.19.0), `lib/netstandard2.0/Tomlyn.dll`, bundled unchanged in `Runtime/Plugins`.
- Source: [xoofx/Tomlyn, tag 0.19.0](https://github.com/xoofx/Tomlyn/tree/0.19.0). Copyright (c) 2019-2022, Alexandre Mutel. BSD-2-Clause; [full license](Documentation~/Licenses/Tomlyn-BSD-2-Clause.txt).
- DLL SHA-256: `a7d2ea40533a5a912bc6a64ab8f1347e60fe92ea0ab456d423b64db2214c904b`.
- The package archive SHA-512 was checked against the official NuGet catalog. QGF uses the untyped TOML model; its adapter flattens scalar settings into the Config module's key/value API. QGF does not implement TOML grammar.

## UniTask 2.5.11

- Source: [Cysharp/UniTask, tag 2.5.11](https://github.com/Cysharp/UniTask/tree/2.5.11), bundled unchanged from `src/UniTask/Assets/Plugins/UniTask` into `Runtime/Plugins/UniTask`.
- Copyright (c) 2019 Cysharp, Inc. MIT; [full license](Runtime/Plugins/UniTask/LICENSE.txt).
- Source archive SHA-256: `8a938cba442e4f0289d9c55c34a2b53afaedbd2684041df1d7adc2f0dcb7dbe5`.
- QGF bundles the official runtime and editor source. Do not install a second UniTask copy in the same Unity project. Unity-facing sample waits use UniTask directly; no tutorial-specific asynchronous wrapper is added.

## Optional editor tools (not bundled)

`QGF > Extensions` offers installation links for the following independent packages. Their source and artwork are not included in the QGF UPM package; installing QGF alone does not install them. Their upstream licenses remain applicable to separate installations.

- [Alchemy](https://github.com/annulusgames/Alchemy): [MIT](https://github.com/annulusgames/Alchemy/blob/main/LICENSE).
- [Rainbow Folders 2](https://github.com/Borod4r/Rainbow-Folders-2): the [upstream license notice](https://github.com/Borod4r/Rainbow-Folders-2#license) specifies Apache-2.0 for code and CC BY-NC 4.0 for icons, images and other artwork. Commercial use of the artwork requires checking the applicable upstream authorization; QGF's MIT license does not grant those rights.

## Design references only

QFramework, GodotGameFramework and the local GameZMXY project are design references. This import does not include their source code, gameplay content or generated data. Record their actual licenses and commits when code is incorporated.
