# Third Party Notices

UnityQGameFramework 的框架源码沿用 MIT 许可；下列组件保留各自版权与许可。`package.json` 中的 MIT 不替代第三方组件许可。

## UnityGameFramework / GameFramework

- Copyright (c) 2013-2021 Jiang Yin.
- License: MIT.
- Sources: https://github.com/EllanJiang/UnityGameFramework and https://github.com/EllanJiang/GameFramework.
- Exact commits and mappings: [upstream.md](Documentation~/upstream.md).
- Full license texts: [UnityGameFramework](Documentation~/Licenses/UnityGameFramework-MIT.md), [GameFramework](Documentation~/Licenses/Framework-MIT.md), and [LICENSE.md](LICENSE.md).

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

## Design references only

QFramework, GodotGameFramework and the local GameZMXY project are reference material for later work. This import does not include their source code, gameplay content or generated data. Record their actual licenses and commits when code is incorporated.
