# PinBall2D — Cursor C# / OmniSharp 配置说明

> **最后验证通过**：2026-06-21  
> 本文档记录当前可用的最终配置。C# 跳转、查找引用、CodeLens 均正常时，**不要改动** `.vscode/settings.json` 和 `omnisharp.json`，除非环境发生变化。

---

## 一、环境前提（缺一不可）

| 组件 | 要求 | 验证命令 |
|------|------|----------|
| .NET SDK | 8.x（当前 8.0.422） | `dotnet --version` |
| .NET Framework 引用程序集 | 4.8.1 Developer Pack | 目录存在：`C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8.1` |
| MSBuild | Visual Studio Build Tools 2022 | OmniSharp 日志出现 `MSBuild instance` |
| Unity 工程文件 | `PinBall2D.sln` + 5 个 `.csproj` | 项目根目录可见 |
| Unity 版本 | 2022.3.33f1 | — |

---

## 二、最终配置（焊死版）

### 2.1 `.vscode/settings.json`

```json
{
  "dotnet.defaultSolution": "PinBall2D.sln",
  "dotnet.server.useOmnisharp": true,
  "omnisharp.useModernNet": true,
  "omnisharp.dotnetPath": "C:\\Program Files\\dotnet",
  "omnisharp.dotNetCliPaths": [
    "C:\\Program Files\\dotnet"
  ],
  "dotnetAcquisitionExtension.sharedExistingDotnetPath": "C:\\Program Files\\dotnet\\dotnet.exe",
  "omnisharp.projectLoadTimeout": 180,
  "omnisharp.enableMsBuildLoadProjectsOnDemand": false,
  "omnisharp.loggingLevel": "information",
  "dotnet.codeLens.enableReferencesCodeLens": true,
  "dotnet.preferCSharpExtension": false,
  "keyboard.dispatch": "code"
}
```

### 2.2 `omnisharp.json`（项目根目录）

```json
{
  "MsBuild": {
    "LoadProjectsOnDemand": false
  },
  "RoslynExtensionsOptions": {
    "EnableAnalyzersSupport": false
  }
}
```

### 2.3 关键配置说明

| 配置项 | 必须值 | 常见错误 |
|--------|--------|----------|
| `omnisharp.useModernNet` | **`true`** | 设为 `false` 时 Legacy OmniSharp 无法加载 Unity 的 `.csproj` |
| `omnisharp.dotnetPath` | **`C:\Program Files\dotnet`（目录）** | 写成 `...\dotnet.exe` 会导致 `dotnet.exe --version` 失败 |
| `dotnet.server.useOmnisharp` | `true` | 与 C# Dev Kit 冲突，见下文扩展说明 |
| `dotnet.preferCSharpExtension` | `false` | 避免 Cursor 优先走 Dev Kit 路径 |

---

## 三、Cursor 扩展（启用 / 禁用）

### ✅ 保留

- `anysphere.csharp` — Cursor 自带 C# 扩展
- `ms-dotnettools.vscode-dotnet-runtime` — .NET 运行时 acquisition
- `visualstudiotoolsforunity.vstuc` — Unity 集成（可选）

### ❌ 必须禁用（重命名为 `.disabled` 或卸载）

- `ms-dotnettools.csharp` — 与 Cursor 自带 C# 冲突
- `ms-dotnettools.csdevkit` — 在 Cursor 中不稳定

---

## 四、正常工作的标志

1. **输出面板** → 选择 **「OmniSharp 日志」**（不是「C#」）
2. 日志中应出现：
   - `Starting OmniSharp server at ...`
   - `OmniSharp server started with .NET 8.0.xxx`
   - 5 个项目均有 `Successfully loaded project file '...\xxx.csproj'`
3. 在 `.cs` 文件中：
   - **F12** 可跳转定义
   - 右键 **查找所有引用** 有结果
   - CodeLens 显示引用数量（非 0）

---

## 五、快速故障排查（按顺序做）

```
C# 功能失效
    │
    ├─ 1. 看 OmniSharp 日志（输出 → OmniSharp 日志）
    │
    ├─ 2. 报错含 "dotnet.exe --version" / "不是内部或外部命令"
    │      → 检查 omnisharp.dotnetPath 是否为【目录】而非 .exe
    │      → 应为：C:\\Program Files\\dotnet
    │      → F1 → 「OmniSharp: Restart OmniSharp」
    │      → 仍失败则完全退出 Cursor 再开
    │
    ├─ 3. 报错含 "Failed to load project file"
    │      → Unity 可能重新生成了 .csproj（Rider 格式）
    │      → 在项目根运行：.\fix-csproj.ps1
    │      → 重启 OmniSharp
    │
    ├─ 4. 日志为空 / 无任何 OmniSharp 输出
    │      → 确认扩展冲突项已禁用（第三节）
    │      → 确认 dotnet.server.useOmnisharp = true
    │
    └─ 5. 项目加载成功但引用仍为 0
           → 等待 30–60 秒（大项目首次索引慢）
           → F1 → 「OmniSharp: Restart OmniSharp」
```

---

## 六、Unity 重新生成 .csproj 后

Unity 若 External Script Editor 仍为 **Rider**，生成的 `.csproj` 可能含无效占位符，导致 OmniSharp 加载失败。

**处理步骤：**

1. 在项目根目录 PowerShell 执行：

   ```powershell
   cd f:\Project\PinBall2D
   .\fix-csproj.ps1
   ```

2. 脚本会：
   - 替换 Rider 占位路径 `non_empty_path_generated_by_unity.rider.package`
   - 修复被破坏的 `<?xml` 头
   - 以 **UTF-8 无 BOM** 保存（**BOM 会导致 OmniSharp 静默失败**）

3. F1 → **OmniSharp: Restart OmniSharp**

**长期建议（可选）：** 在 Unity → Preferences → External Tools 中将 External Script Editor 改为 **Visual Studio 2022**，再 Regenerate project files，可生成更兼容 VS/OmniSharp 的 `.csproj`。

---

## 七、手动验证 OmniSharp（高级）

在 PowerShell 中运行（确认 SDK 与项目本身无问题）：

```powershell
$omni = "$env:USERPROFILE\.cursor\extensions\anysphere.csharp-1.0.1-win32-x64\.omnisharp\1.39.12-net6.0"
& "C:\Program Files\dotnet\dotnet.exe" "$omni\OmniSharp.dll" `
  -s "f:\Project\PinBall2D\PinBall2D.sln" -l information
```

若此处 5 个项目均 `Successfully loaded`，问题在 Cursor 配置/扩展；若此处也失败，问题在 `.csproj` 或 SDK/MSBuild。

---

## 八、历史踩坑记录（勿再犯）

| 现象 | 根因 | 解决 |
|------|------|------|
| `dotnet.exe --version` 失败 | `omnisharp.dotnetPath` 写成了 `.exe` 路径 | 改为目录 `C:\Program Files\dotnet` |
| 重启电脑仍报 dotnet 找不到 | 同上，与 PATH 无关 | 修正 `omnisharp.dotnetPath` |
| 5 个 csproj 全部 Failed to load | Rider 占位符 + UTF-8 BOM | 运行 `fix-csproj.ps1` |
| `useModernNet: false` 有 UI 但引用为 0 | Legacy OmniSharp 不支持当前 Unity 工程格式 | **必须** `useModernNet: true` |
| C# Dev Kit 安装后一切失效 | 与 Cursor 自带 C# 冲突 | 禁用 csdevkit 和 ms-dotnettools.csharp |
| Ctrl+Shift+P 无反应 | 中文输入法占用快捷键 | 用 **F1** 或 **Ctrl+Alt+P** |

---

## 九、涉及文件清单

```
PinBall2D/
├── PinBall2D.sln
├── omnisharp.json
├── fix-csproj.ps1
├── .vscode/
│   ├── settings.json          ← 核心配置，勿乱改
│   └── Cursor-CSharp-配置说明.md  ← 本文档
├── Assembly-CSharp.csproj
├── Assembly-CSharp-firstpass.csproj
├── Assembly-CSharp-Editor.csproj
├── Assembly-CSharp-Editor-firstpass.csproj
└── EasySave3.csproj
```

---

## 十、恢复流程（30 秒版）

1. 确认 `.vscode/settings.json` 与第二节一致  
2. 确认冲突扩展已禁用  
3. `.\fix-csproj.ps1`（若 Unity 刚重新生成过工程文件）  
4. 完全退出 Cursor → 重新打开项目  
5. F1 → **OmniSharp: Restart OmniSharp**  
6. 查看 **OmniSharp 日志** → 确认 5 个项目 Successfully loaded  
