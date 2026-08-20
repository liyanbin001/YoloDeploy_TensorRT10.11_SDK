# YoloDeploy 仓库文件维护说明

本文描述当前主线中 BAT、PowerShell 和主要 Markdown 文件的职责。

## 1. 正式发布入口

### publish_release.bat / publish_release.ps1

用途：WPF 客户 Runtime 发布。

负责：
- 构建 `YoloDeploy.Native`
- 发布 `.NET 8 win-x64 self-contained` WPF
- 收集 TensorRT/CUDA DLL
- 复制 ONNX/类别资源
- 生成运行脚本、验证脚本、manifest
- 生成 ZIP

应保留。

### publish_sdk_runtime.bat / publish_sdk_runtime.ps1

用途：当前唯一正式 `.NET 8 SDK Runtime` 发布入口。

支持：
- Detect
- OBB
- Seg
- Auto Task
- Camera memory frame

当前根目录只保留 `publish_sdk_runtime.*` 作为 .NET 8 SDK 正式发布入口；`publish_multitask_sdk_runtime.*` 已退出根目录，仅作为历史版本概念追溯。

应保留。

### publish_net48_runtime.bat / publish_net48_runtime.ps1

用途：`.NET Framework 4.8` SDK 客户 Runtime。

应保留。

### release_env.bat

用途：当前开发机本地构建/发布环境变量。

应保留，但其中路径属于开发机配置，不应当作所有电脑的固定安装路径。

### release_env.example.bat

用途：环境变量模板。

建议公开仓库主要参考该模板。

## 2. Runtime Assets

### sdk_runtime_assets

.NET 8 SDK 客户发布资源：
- README
- Examples
- verify_runtime.bat
- Models 等

### sdk_runtime_assets_net48

Net48 客户发布资源：
- README
- verify_runtime_net48.bat
- Examples 等

这些目录应保留。

## 3. 已归档的旧脚本

以下文件属于开发/迁移历史，不再作为当前正式入口：

- `add_sdk_to_solution.bat`
- `add_multitask_sdk_to_solution.bat`
- `add_net48_sdk_to_solution.bat`
- `publish_sdk.ps1`
- 旧版 `publish_sdk_runtime.bat`
- 旧版 `publish_sdk_runtime.ps1`
- `setup_env_example.bat`
- `setup_env_example.txt`
- 根目录旧 `verify_runtime.bat`

统一放在：

```text
tools\legacy\
```

原因：
- Solution 已包含当前 SDK/Net48 工程，不再需要 add-to-solution
- 当前 `.NET 8 SDK` 已经是 Detect/OBB/Seg 正式主线，不需要两套 Runtime Publisher
- Runtime 自己已经有专用验证脚本

## 4. 当前主要 Markdown

### README.md

描述“当前软件是什么”，不再按 Phase 1～6 叙述开发历史。

### SDK_INTEGRATION_CN.md

当前 `.NET 8 SDK` 集成说明。

### NET48_INTEGRATION_CN.md

当前 `.NET Framework 4.8` 集成说明。

### docs\BUILD_STEPS_CN.md

开发构建专题，可继续保留。

### docs\PHASE6_YOLO26_SEG_MINRECT_CN.md

当前 Seg/最小面积旋转矩形技术专题，可保留。

## 5. 历史 Markdown

旧版本迁移说明和 Phase 1～5 演进资料统一放入：

```text
docs\history\
```

这些文件只用于追溯，不代表当前程序行为。

## 6. Engine Cache 唯一规则

当前 WPF、.NET 8 SDK、Net48 SDK 应保持同一规则：

```text
Engine + .engine.json 与 ONNX 位于同一目录
```

不再使用全局：

```text
%LOCALAPPDATA%\YoloDeploy\EngineCache
```

如果未来再次修改缓存策略，必须同时检查：
- `YoloDeploy.App\EngineCacheManager.cs`
- `YoloDeploy.SDK\EngineCacheManager.cs`
- `YoloDeploy.SDK.Net48\EngineCacheManager.cs`
- `publish_release.ps1`
- `publish_sdk_runtime.ps1`
- `publish_net48_runtime.ps1`
- Runtime README
- 根 README
- SDK/Net48 集成文档

## 7. 中文和长路径

Native 导出使用 `wchar_t*`，C# P/Invoke 使用 Unicode 路径。

当前工程可使用中文路径。

但 TensorRT / ONNX Parser / Windows 文件访问仍建议使用较短完整路径；现场模型推荐类似：

```text
D:\YoloModels\ProjectA\
```

不要使用非常深的目录层级。
## 8. 当前根目录发布入口基线

当前根目录只应保留三组正式发布入口：

```text
publish_release.bat
publish_release.ps1

publish_sdk_runtime.bat
publish_sdk_runtime.ps1

publish_net48_runtime.bat
publish_net48_runtime.ps1
```

根目录不应再出现：

```text
publish_multitask_sdk_runtime.bat
publish_multitask_sdk_runtime.ps1
```

如本地旧分支仍存在这些文件，应归档到 `tools\legacy\`，不要重新作为正式入口。

建议在提交/发布前运行：

```text
tools\verify_current_repository.ps1
```

用于检查 Engine Cache、发布入口和文档口径是否发生回退。