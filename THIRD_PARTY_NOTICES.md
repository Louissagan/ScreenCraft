# Third-party notices

Project-specific code is available under the repository's Apache License 2.0 (`LICENSE`), except where a file carries another license. Reused Stackshot modules retain their MIT license and original copyright.

ScreenCraft reuses selected C# sources from [rubenitx/stackshot](https://github.com/rubenitx/stackshot) at commit `0fa71af2346f09dc34dd8f48eb4f8df211f5ae7c`.

MIT License. Copyright (c) 2026 Rubén Martínez (@rubenitx). The complete upstream license is preserved in `src/Upstream/Stackshot/LICENSE`; source headers remain in `src/Upstream/Stackshot/`. Derived files include the editor Canvas/Shapes and the independent PinWindow. Exact provenance and original/derived hashes are in `docs/UPSTREAM_FILES.csv`.

The released executable compiles only `src/`. It includes no Tencent/QQ private DLLs, extracted QQ components, original Stackshot mascots/characters, FFmpeg, OAuth/update/report clients, or low-level recording hooks. It ships no third-party fonts or branded logos. Fonts use installed Windows families; the ScreenCraft icon is generated from this project's `BrandArtwork`.

Snapboard (MIT, Copyright 2026 FlowDesk, commit `379651086cd7ce6eb50733925ffe4c74f006fdab`) was independently cloned/built/run for evaluation. Its code is not part of the ScreenCraft executable or portable package.

Windows .NET Framework/WPF/WinForms and Windows OCR are OS/runtime components under their applicable Microsoft licenses; they are not redistributed in this package. This project adds no NuGet dependency.

Versions 0.8 and 0.9.0/0.9.1 included an optional Set2 comparison palette with color specifications developed by Cynthia Brewer, from [ColorBrewer](https://colorbrewer2.org/) / [axismaps/colorbrewer](https://github.com/axismaps/colorbrewer), under Apache License 2.0. The complete license is in `src/Assets/COLORBREWER_LICENSE.txt` (source) and `COLORBREWER_LICENSE.txt` (portable package). This product includes color specifications and designs developed by Cynthia Brewer. Version 0.9.2 removes those presets; the historical attribution and license are retained. The two current planning reference colors are from Appendix C of the Ministry of Natural Resources municipal spatial planning drawing guide (March 2021): residential living area RGB(254,221,120) and green recreation area RGB(13,123,62). Source: https://www.gov.cn/zhengce/zhengceku/2021-04/07/5598163/files/bd34c2bcbb6f467d8af880abc33f4c07.pdf. These are selectable reference colors, not a claim of planning certification.

The per-user MSI build derives its WiX structure from Stackshot tools/Stackshot.wxs (same MIT copyright above), with separate ScreenCraft product/component/upgrade identifiers and different privacy/lifecycle defaults. WiX Toolset 3.14.1, Copyright (c) .NET Foundation and contributors, is used as build tooling under Microsoft Reciprocal License (MS-RL). The fixed official release ZIP is verified with SHA-256 6ac824e1642d6f7277d0ed7ea09411a508f6116ba6fae0aa5f2c7daa2ff43d31. WiX itself is not required to run ScreenCraft. MSI UI resources/definitions are authored using WiX; the complete license is in src/Assets/WIX_LICENSE.txt and is included in portable and installed payloads. Official source: https://github.com/wixtoolset/wix3/tree/wix3141rtm.

## Stackshot MIT license (also included for binary distributions)

MIT License

Copyright (c) 2026 Rubén Martínez (@rubenitx)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
