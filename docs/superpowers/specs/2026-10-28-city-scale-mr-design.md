# City-Scale MR Fighting — MVP 0.1 / 0.2 实施计划

## Context

目标:戴上 Meta Quest 3,从 39 楼窗前看到一个 30–200 米高的巨型角色站在真实城市里,头部移动时视差正确、窗框遮挡正确(Phase 1 / MVP 0.1);再用 Touch 控制器直接操控它走/跑/转向/跳/出拳/踢/咆哮,动作自然衔接(Phase 2 / MVP 0.2)。不做第二个怪兽、AI、血量、伤害、建筑破坏。

`C:\Users\xxian\godzilla` 目前是空目录,不是 git 仓库。本机有 Unity 6000.2.15f1(含 Android 模块、adb)和一个已在 Quest 3 上验证过的 MR 工程 `C:\Users\xxian\AI_Extended\questscanroom\myroom`(Meta XR Core SDK 85 + MRUK 85 + OpenXR 1.14.3 + Meta OpenXR 2.1 + AR Foundation 6.1.1 + URP 17.2 + Input System 1.16,OVRCameraRig Building Block + OVRPassthroughLayer 做透视)。新工程**复制它的 XR/Player/URP 配置文件,不复制任何 splat / 扫描内容**。

最优先要验证的问题(来自需求):从窗前左右移动头部时,远处 100 米角色是否"真的站在城市里"而不是贴在视野上。

## 已定决策

| 项目 | 决定 | 来源 |
|---|---|---|
| 角色资产 | Mixamo 通用人形占位(Y Bot)+ Mixamo 动画,Humanoid 导入,后续可换高达/哥斯拉重定向 | Xin |
| 测试 | Quest 3 真机验收 + 编辑器模拟模式(键鼠 + MRUK 预制房间) | Xin |
| 窗框 | MRUK 房间扫描自动读取 `WINDOW_FRAME` 锚点;Xin 的房间是"多扇窗 / 不确定",所以多窗选窗规则与 WallAsWindow / HeadFrame 两条退路都是必做项,真机测试矩阵三条路径都要覆盖 | Xin(退路设计是我的决定) |
| 工程位置 | `C:\Users\xxian\godzilla` 新建 Unity 工程 + `git init` | Xin |
| 比例策略 | **真实比例**:1 unit = 1 m,角色就放在 100–500 m 外、地面在楼下 ~120 m。视差与遮挡由真实几何自然产生,不做"缩小模型贴近眼前"的伪视差 | 我的决定 |
| 输入 | Unity Input System `.inputactions`,OpenXR Oculus Touch profile 绑定 + 键盘备用绑定 | 需求 + 我的决定 |
| 位移 | 程序位移(不用 Root Motion);动画只负责姿态,`Animator.speed` 统一慢放 | 需求允许二选一,我的决定 |
| 透视/遮挡 | 透视:OVRPassthroughLayer underlay(同 myroom)。遮挡主方案:MRUK `EffectMesh`(墙/天花/地板)+ `CutHoles = WINDOW_FRAME` 自动挖窗洞 + 仅写深度材质;Depth API(`EnvironmentDepthManager`)作可开关补充,默认关 | 需求 + 我的决定 |
| 位置持久化 | JSON 存 `Application.persistentDataPath`,位置以**窗锚点局部坐标**表达;MRUK 房间锚点由系统持久化,重启自动重定位,无需自建 Spatial Anchor | 我的决定 |

## 架构

### 坐标系(CitySpaceManager)
- **City Frame**:原点 = 窗锚点中心投影到室内地板高度;forward = 窗平面法线中"背离房间地板中心"的那一侧;up = 世界 up。
- **虚拟城市地面**:`groundY = floorY − groundDrop`(默认 120 m,可调)。角色脚底永远贴在该平面。
- 角色位姿参数(全部存档):`lateral`(米,沿窗平面)、`distance`(米,沿 forward)、`yaw`(度)、`heightMeters`(30–200)、`groundDrop`、`animTimeScale`。
- 多扇窗时:选"头部前向射线最先命中的 `WINDOW_FRAME`"(命不中则选面积最大的)为 City Frame 原点窗,存档记录其 UUID;所有窗都挖洞。
- 无 `WINDOW_FRAME` 但有房间时(含"整面落地玻璃被 Space Setup 标成 `WALL_FACE`"的情况):**WallAsWindow 退路** —— 头部前向射线命中的那面 `WALL_FACE` 整面视为窗:它从遮挡网格中排除,City Frame 原点 = 命中点投影到地板,forward = 该墙外法线。存档记录该墙 UUID。
- 既无房间时:**HeadFrame 退路** —— 头部位置投影到地板为原点、头部 yaw 为 forward,不生成任何遮挡。
- 三条路径都可在暂停菜单"重新校准"重做;开发面板有"强制退路"开关,让编辑器也能覆盖到 WallAsWindow / HeadFrame 两条路径(编辑器默认走 Bedroom00 的窗路径)。
- 相机 far clip 设为 3000 m(URP 默认 1000 不够)。

### 遮挡
- `EffectMesh` #1:Labels = `WALL_FACE | CEILING | FLOOR`,`CutHoles = WINDOW_FRAME | DOOR_FRAME`,材质 = URP 自写 `DepthOnlyOccluder.shader`(`ColorMask 0`,`ZWrite On`,Queue 1999),HideMesh=false。WallAsWindow 退路时,用 EffectMesh 的 per-anchor 生成/销毁接口把被选中的那面墙排除。
- 角色材质普通 URP Lit,渲染队列 2000,被墙深度自然遮挡,只透过窗洞可见。
- 开发面板可切换"显示遮挡网格(半透明青色)"用于对齐检查,以及"遮挡关闭"。
- MVP **不做**窗洞边缘内缩/外扩(MRUK 85 EffectMesh 无此参数,旧 `BorderSize` 已移除);若真机发现角色溢出窗框,再评估后处理生成网格,或用 Depth API 补偿。
- Depth API 开关:`EnvironmentDepthManager` + Meta `OcclusionLit` 风格材质(SDK 85 自带 URP 版)。玻璃反射可能让深度把角色整块遮掉,所以默认关。

### 应用状态机(AppFlow)
`Boot → LoadingRoom → (RoomReady | NoRoom→HeadFrameCalib) → Placement → Play ⇄ Paused`
- Placement:右扳机按住 = 射线拖动,打到虚拟地面平面决定 lateral/distance;左摇杆 X = lateral 微调(速度随 distance 放大),左摇杆 Y = 高度 ±;右摇杆 X = yaw,Y = distance;**A = 确认保存并进 Play**;B = 重置;X = 遮挡网格显示开关。(Play 状态下同一批按键才是跳/拳/踢/吼。)
- Play:Phase 2 控制。Menu 短按 = 暂停菜单(继续 / 重新放置 / 重新校准 / 退出);Menu 长按 1 s = 开发面板开关。
- 启动若存在存档且窗锚点 UUID 匹配 → 直接进 Play;不匹配 → 进 Placement 并提示。

### Phase 2 控制与动画
- `GiantCharacterController`:角色局部 XZ 位移(左摇杆),右摇杆 X 转向(`turnSpeedDeg/s`);
  `speed = baseSpeed(m/s@1.8m) × scaleFactor × animTimeScale`,`scaleFactor = heightMeters / modelHeight`;
  跳跃:`jumpHeight = jumpFactor × heightMeters`,`g = 9.81 × scaleFactor × animTimeScale²`(弹道时长与慢放动画一致);落地钳位 `groundY`。
  默认 `animTimeScale = 0.35`(怪兽电影惯例,体量感)。
- `GiantAnimationController`:Animator 参数 `Speed(0..1)`、`Grounded`、触发 `Jump/Punch/Kick/Roar`。Base layer:Idle/Walk/Run Blend Tree → Jump(Start/Air/Land)→ 单次动作状态(Has Exit Time,0.15 s crossfade)。单次动作期间锁定位移输入(`actionLocked`),动画 Exit 解锁。`Animator.speed = animTimeScale`。
- 动画逻辑抽成纯 C# 类 `GiantAnimLogic`(输入→参数/锁定判断),可无 Animator 做 EditMode 测试。
- 已知限制(MVP 接受):原地转向无转身动画会有脚底滑动;Walk/Run 必须用 Mixamo **In Place** 版避免双重位移。

### 输入映射(`Assets/Input/GiantControls.inputactions`)
| 动作 | Touch(OpenXR Oculus Touch) | 键盘(编辑器) |
|---|---|---|
| Move | `<XRController>{LeftHand}/primary2DAxis` | WASD |
| Turn | `{RightHand}/primary2DAxis` | Q/E(X),上下箭头(Y,放置时为 distance) |
| Jump | `{RightHand}/primaryButton`(A) | Space |
| Punch | `{RightHand}/secondaryButton`(B) | J |
| Kick | `{LeftHand}/primaryButton`(X) | K |
| Roar | `{LeftHand}/secondaryButton`(Y) | L |
| Run | `{LeftHand}/trigger` | LeftShift |
| Menu | `{LeftHand}/menu` | Esc |
| Place-drag(仅 Placement) | `{RightHand}/trigger` 按住 | 鼠标左键按住 |
| Confirm(仅 Placement,复用 A) | `{RightHand}/primaryButton` | Space / Enter |
| 编辑器模拟头部 | — | 右键拖动 = 视角,IJKL = 走动(测视差) |

### 编辑器模拟(EditorSimRig)
- Play 时若 `XRSettings.isDeviceActive == false`:在 CenterEye 相机上启用鼠标视角 + IJKL 平移;背景黑色代替透视。
- MRUK `SceneSettings.DataSource = DeviceWithPrefabFallback`(已确认 MRUK 85 有此枚举),`RoomPrefabs = [Packages/com.meta.xr.mrutilitykit/Core/Rooms/Prefabs/Bedroom/Bedroom00.prefab]`(已确认 Bedroom00/01/04/05/07 都含 `WINDOW_FRAME` 锚点),编辑器里窗洞逻辑与真机走同一条代码路径。只有当 Bedroom 窗位置不便测试时才自建 `Assets/Sim/SimRoom.prefab`。

## 工程搭建(Phase 0)

1. `git init`(Unity 标准 `.gitignore`,排除 Library/Temp/Logs/Builds/obj/UserSettings),`Unity.exe -batchmode -quit -createProject C:\Users\xxian\godzilla`。远端与推送(Xin 指定):
   ```
   git remote add origin https://github.com/Xin-Ray/godzilla.git
   git branch -M main
   git push -u origin main
   ```
   首次提交 = 工程骨架 + 本计划的设计文档(`docs/superpowers/specs/2026-10-28-city-scale-mr-design.md`);之后每完成一个任务编号提交一次并推送 main。大文件(FBX 动画、APK)不入库:APK 进 `.gitignore`,FBX 若超过 50 MB 用 Git LFS。
2. `Packages/manifest.json`:仅保留 `com.meta.xr.mrutilitykit 85.0.0`、`com.unity.xr.openxr 1.14.3`、`com.unity.xr.meta-openxr 2.1.0`、`com.unity.xr.arfoundation 6.1.1`(OpenXR 设置资产引用其 feature 脚本,保留以免 missing script)、`com.unity.inputsystem 1.16.0`、`com.unity.render-pipelines.universal 17.2.0`、`com.unity.test-framework`、`com.unity.ugui`、内置 modules。不要 splat / roomscan / unity-mcp / ai 包。
3. 从 myroom 复制:`ProjectSettings/*`、`Assets/XR/*`(OpenXR 设置 + Loader)、`Assets/Settings/URP-*.asset` 与 `Build Profiles/Meta Quest.asset`、`Assets/Plugins/Android/AndroidManifest.xml`。随后修正:
   - ProjectSettings:productName `Godzilla`、applicationIdentifier `com.xin.godzilla`、company;保留 ARM64/IL2CPP/Vulkan/Linear/Single Pass Instanced/Input System。
   - URP Renderer:删除 `GaussianSplatURPFeature` 引用;`m_RenderingMode` 改 **Forward(0)**(myroom 是 Deferred,Quest 不宜);阴影关闭;MSAA 4 保留。
   - AndroidManifest:去掉 `HAND_TRACKING`、`HEADSET_CAMERA`、NetworkSecurityConfig;Activity 的 `android:name` 实际仍是 `com.unity3d.player.appui.AppUIGameActivity`(注释写的是 UnityPlayerGameActivity,别被注释骗),必须真改成 `com.unity3d.player.UnityPlayerGameActivity`,并保留 `BaseUnityGameActivityTheme` + `tools:replace`(ProjectSettings `androidApplicationEntry: 2` = GameActivity);保留 `USE_SCENE`、`USE_ANCHOR_API`、`com.oculus.feature.PASSTHROUGH`、`BOUNDARYLESS_APP`。
   - Meta 项目设置自动修复:`OVRProjectSetup.FixAllAsync(BuildTargetGroup.Android)`(已确认存在于 Core SDK 85 `Editor/OVRProjectSetup/OVRProjectSetup.cs`)。
   - OpenXR Android features 保持 myroom 清单:Meta Quest Support、Meta XR Feature、Oculus Touch profile、ARFoundation Meta Session/Camera/Occlusion、LifeCycle;Foveation/SubsampledLayout 继续关闭(构建脚本强制)。
4. `Assets/Editor/ProjectBootstrap.cs`(`-executeMethod ProjectBootstrap.Run`,幂等):用代码组装并保存 `Assets/Scenes/City.unity`——实例化 `Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab` 并**手动 AddComponent OVRManager**(已确认该预制不含 OVRManager;设 passthrough on、scene permission on、tracking origin Floor、**Dynamic Resolution off**),加 `OVRPassthroughLayer`(underlay)、CenterEye 相机 clear 黑色 alpha 0 / far 3000、MRUK + EffectMesh、`EnvironmentDepthManager`(disabled)、`AppFlow` 根物体与全部脚本、Directional Light、EventSystem(OVRInputModule)、两套 UI Canvas。场景由代码生成,可审阅、可重建。尝试调用 Meta `OVRProjectSetup` 自动修复 Android 项;修不了的列出给 Xin 手动点。
5. `Assets/Editor/QuestBatchBuild.cs`(沿用 myroom `QABatchBuild` 模式)+ `tools/build_quest.ps1`、`tools/deploy_quest.ps1`(adb install -r、am start、logcat 过滤 `[City]|[Window]|[Place]|[Giant]|Unity`)、`tools/wifi_adb.ps1`(按 splat-validation 技能的无线 adb 流程)。adb 路径:`C:\Program Files\Unity\Hub\Editor\6000.2.15f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe`。
6. 验收文档:运行 splat-validation 技能的 `init_validation.py` 生成 `docs/validation/{GOALS,TEST_MATRIX,FAILURE_LOG,HANDOFF}.md`,测试 ID 用 `T-MVP-0xx`(Stop hook 只认 `T-` 前缀与 Required/Status 列)。

## Phase 1 任务(MVP 0.1)

| # | 任务 | 文件 |
|---|---|---|
| 1.1 | 坐标系 + 地面平面 + 相机 far clip;EditMode 测试 city↔world 转换 | `Assets/Scripts/Core/CitySpaceManager.cs`,`Assets/Tests/EditMode/CitySpaceTests.cs` |
| 1.2 | MRUK 加载、找窗、外法线判定、EffectMesh 挖洞、HeadFrame 退路、Recalibrate | `Assets/Scripts/Core/WindowCalibration.cs`,`Assets/Shaders/DepthOnlyOccluder.shader` |
| 1.3 | 放置交互(射线/摇杆/高度)、角色根物体缩放、JSON 存读、UUID 匹配 | `Assets/Scripts/Placement/CharacterPlacement.cs`,`PlacementStore.cs`,`PlacementData.cs`,`Assets/Tests/EditMode/PlacementStoreTests.cs` |
| 1.4 | 输入资产 + `PlayerInput.cs`(输出纯结构体 `InputFrame`) | `Assets/Input/GiantControls.inputactions`,`Assets/Scripts/Input/PlayerInput.cs` |
| 1.5 | 状态机 + 玩家 HUD(提示/暂停菜单)+ 开发面板(高度/距离/yaw/groundDrop/animTimeScale 滑杆,遮挡网格显示,Depth API 开关,FPS,状态文本) | `Assets/Scripts/App/AppFlow.cs`,`Assets/Scripts/UI/PlayerHud.cs`,`Assets/Scripts/UI/DevPanel.cs` |
| 1.6 | 编辑器模拟 rig(MRUK Bedroom00 预制作房间;SimRoom.prefab 仅在需要时自建) | `Assets/Scripts/Sim/EditorSimRig.cs` |
| 1.7 | 占位角色导入(静态 Idle 即可)放到场景,100 m 高、320 m 远 | `Assets/Characters/Placeholder/` |
| 1.8 | Depth API 可选遮挡 | `WindowCalibration.cs` 内开关 |

## Phase 2 任务(MVP 0.2)

| # | 任务 | 文件 |
|---|---|---|
| 2.1 | 动画资产:复用 avatar 工程 `Anim_idle2/Anim_jump/Anim_kick.fbx`;**Xin 已在 Chrome 登录 mixamo.com,由我用 claude-in-chrome 代下**:角色 Y Bot(T-pose,FBX for Unity);动画 Walking(勾 In Place)、Running(勾 In Place)、Punching(或 Cross Punch)、Roar(Mixamo "Roaring"/"Zombie Scream"),格式 FBX for Unity、Without Skin、30 fps。下载落到 `~/Downloads` 后复制进 `Assets/Characters/Placeholder/`;Humanoid 导入、Loop 设置;导入后检查 Walk/Run 根位移 ≈ 0 | `Assets/Characters/Placeholder/Anim/` |
| 2.2 | `GiantAnimLogic`(纯逻辑)+ EditMode 测试:速度归一化、动作锁定、跳跃状态 | `Assets/Scripts/Character/GiantAnimLogic.cs`,`Assets/Tests/EditMode/GiantAnimLogicTests.cs` |
| 2.3 | Animator Controller(Blend Tree + Jump + 单次动作)由 Editor 脚本生成,保证可重建 | `Assets/Editor/GiantAnimatorBuilder.cs` → `Assets/Characters/Placeholder/Giant.controller` |
| 2.4 | `GiantCharacterController`(缩放感知位移/转向/跳跃/重力/钳位)+ EditMode 测试 | `Assets/Scripts/Character/GiantCharacterController.cs`,`GiantMover.cs`(纯逻辑),`Assets/Tests/EditMode/GiantMoverTests.cs` |
| 2.5 | `GiantAnimationController` 接 Animator;Play 状态接入输入 | `Assets/Scripts/Character/GiantAnimationController.cs` |
| 2.6 | 开发面板动画测试按钮(Idle/Walk/Run/Jump/Punch/Kick/Roar/Reset)+ Current State | `DevPanel.cs` |

## 验证

### 自动化(每次改动后跑)
- EditMode:`Unity.exe -batchmode -projectPath … -runTests -testPlatform EditMode -testResults results.xml`。
- PlayMode(编辑器内、SimRoom):加载 City 场景 → 断言找到窗锚点、EffectMesh 含洞、角色按存档落位;**视差探针**:从头位 x = −0.5/0/+0.5 m 渲染三帧 PNG,断言角色相对窗洞边缘的屏幕位移方向与解析值一致(远物几乎不动,窗洞边缘大幅移动),PNG 存 `docs/validation/evidence/T-MVP-0xx/`,我逐帧看图。
- Phase 2 行为(按 visual-behavior-testing 技能):固定全景探针相机连拍 walk/run ≥5 帧、jump/punch/kick/roar 前中后 ≥3 帧,逐帧写"预期 vs 实际"判读表;遥测(位置、Animator 状态)只作辅证。
- 构建:`QuestBatchBuild` 出 APK 为硬门槛。

### 真机(Quest 3,Xin 佩戴)
1. `tools/wifi_adb.ps1` 建无线 adb → `deploy_quest.ps1` 安装启动 → logcat 看到 `[Window] window=<uuid> hole=…`、`[Place] restored …`。
2. 截图:`adb exec-out screencap -p`;若合成器挡住透视,用应用内 `ScreenCapture` 落盘再 `adb pull`。
3. 人工判定清单(写入 HANDOFF.md):角色可见于窗外、墙体遮挡无穿透、左右移动头部视差自然、重启后位置恢复、A/B/X/Y/摇杆各动作对应、动作切换无明显穿模滑步、FPS ≥ 72(OVR Metrics Tool 或 logcat FPS 行)。
4. 未在真机实际跑过的测试一律 `READY_FOR_DEVICE`,不写 PASS。

## 待 Xin 手动完成
- Quest 3 系统 Space Setup 把窗户标记为 **Window**(否则走 HeadFrame 退路)。
- ~~Mixamo 登录~~ 已登录,由我用 Chrome 代下(见任务 2.1)。
- 首次 USB 连接 + 无线 adb 授权弹窗点"始终允许"。
- 每轮真机验收并回填结果。

## 风险
| 风险 | 对策 |
|---|---|
| 扫描窗洞边缘与实物差几厘米,角色"溢出"窗框 | MVP 先接受;真机确认后再做网格后处理或 Depth API 补偿 |
| 39 楼落地玻璃被 Space Setup 标成整面 `WALL_FACE`,没有 Window 锚点 | WallAsWindow 退路(整面墙视为窗)是一等公民,不是临时补丁 |
| 深度缓冲 0.1–3000 m 精度 | Vulkan 反向 Z,URP 默认,单一大物体无 Z-fighting |
| OpenXR 设置资产跨工程复制引用失效 | 同版本包 GUID 稳定;Bootstrap 校验 feature 启用状态并打印 |
| Mixamo In Place 未勾选导致滑步/双重位移 | 导入检查根骨骼位移 ≈ 0,写进测试 |
