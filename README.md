# bbjhmod

《背包江湖（BackpackJianghu）》BepInEx Mod 项目。

当前版本包含以下改动：

- 商店刷新金额固定为 `1` 金
- 商店商品数量固定为 `8` 个
- 商店商品布局调整为更适合 8 个物品显示的紧凑排布

## 环境要求

- Windows
- .NET Framework `4.7.2`
- BepInEx 5
- 游戏本体已安装到：
  `C:\Programs\Steam\steamapps\common\BackpackJianghu`

## 项目结构

- `BbjhMod.cs`
  Mod 主入口与 Harmony 补丁
- `bbjhmod.csproj`
  项目配置与游戏程序集引用
- `Properties/AssemblyInfo.cs`
  程序集元数据
- `GameRoot`
  指向游戏根目录的本地联接点，仅用于本地开发，不应提交

## 当前补丁说明

### 1. 商店刷新金额

通过 Harmony 补丁拦截 `Game.Window.Battle.BattleInfoFrag.get_RefreshShopCost`，
将刷新价格固定为 `1`。

这会同时影响：

- 商店界面显示的刷新价格
- 刷新前的金币校验
- 实际刷新扣费

### 2. 商店商品数量

通过 Harmony 补丁拦截 `TableDR.ShopConfig.get_ShopNum`，
将商店商品数量固定为 `8`。

只修改商店商品数量，不修改掉落界面物品数量。

### 3. 商店布局

由于原版商店显示数量较少，直接增加商品数后会发生越界或拥挤。
当前版本在商店打开时重排商品位置，使 8 个商品能够在商店区域内更稳定显示。

## 构建

项目默认输出目录配置为：

`GameRoot\BepInEx\plugins\bbjhmod\`

也就是构建后会直接输出到游戏插件目录。

常用命令：

```powershell
dotnet build .\bbjhmod.csproj -c Debug /p:OutputPath='bin\Debug\'
```

如果使用项目默认输出路径，也可以直接构建：

```powershell
dotnet build .\bbjhmod.csproj -c Debug
```

## 游戏内插件位置

构建完成后插件 DLL 位于：

`C:\Programs\Steam\steamapps\common\BackpackJianghu\BepInEx\plugins\bbjhmod\bbjhmod.dll`

## 开发说明

- 项目通过引用游戏反编译源码和游戏程序集进行开发
- 实际运行依赖游戏目录下的 `BepInEx`、`Game.dll`、`Table.dll`、`UnityEngine` 相关程序集
- 当前仓库是功能型 Mod 工程，不包含通用框架封装

## 注意事项

- 修改商店布局时，优先保持商店区域内可读性，不要影响掉落界面
- `GameRoot` 为本地开发联接点，应通过 `.gitignore` 忽略
- 如果游戏更新导致类名、属性名或 UI 结构变化，现有 Harmony 补丁可能需要同步调整
