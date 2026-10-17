# 外部素材索引

素材按用途分组，分组内保留各供应商自己的原始文件结构。移动素材时保留了包目录和包内资源的 `.meta` 文件，现有 Unity GUID 引用因此保持不变。

曾按 Unity 序列化引用关系清理未使用的模型、Prefab、贴图、材质、动画和示例场景。TerrainData 使用二进制序列化，其中 TerrainLayer、地表贴图和装饰原型的引用无法由普通文本 GUID 扫描完整发现；因此从初始工程恢复了含 TerrainLayer/地形装饰资源的素材包及项目自有 Art/Prefab 资源。今后清理这类资源应使用 Unity `AssetDatabase.GetDependencies` 核对，不能只依赖文本引用扫描。未引用的源码、Shader 源文件和供应商文档仍保留；插件/框架目录不做批量裁剪。

| 用途 | 目录 | 包含内容 |
| --- | --- | --- |
| 环境与地面 | `Environment/` | LowPolyFantasyBundle、KayKit、Halloween Pack-Cemetery Snap、Handpainted Grass and Ground Textures、IL3DN Grass、polyperfect、Pure Poly、kenney platformer kit |
| 角色 | `Characters/` | GhostCharacter Free |
| 天空与特效 | `SkyVFX/` | AllSkyFree、Fantasy Skybox FREE、Lana Studio、PS1 Water Shader |
| 框架与 UI | `Frameworks/` | Pixel Crushers、TextMesh Pro |

## 维护约定

- 每个供应商包都作为独立目录保留，不把不同来源的文件混放。
- 移动或重命名包时，`.meta` 文件与资源目录必须一起移动；不要重新生成已有包的 GUID。
- 删除或升级包前，先检查场景、预制体、材质、脚本和 `Resources` 目录中的引用。
- `Library/`、`Temp/`、`Logs/` 属于 Unity 生成目录，不放入此处，也不手动整理。
