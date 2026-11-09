# 二楼、三楼基础结构

场景：`Assets/Scenes/JEI_Floor01_Structure.unity`。在现有场景中增加二、三楼，场景文件名保持原样。

## 布局

- 延续一楼 36 × 26 m 轮廓、灰色墙体、分色地面、0.30 m 外墙、0.20 m 隔墙及 2.40 m 环形走廊。
- 二楼地面标高 +3.60 m：北侧三个较大的教学空间（约 73–82 m²），南侧五个研讨、学习或办公空间（约 39–55 m²）。
- 三楼地面标高 +7.20 m：北侧五个较小的研讨、研究空间（约 39–59 m²），南侧三个较大的办公、学习空间（约 74–82 m²）。
- 中央庭院、两侧楼梯和服务核心上下对齐。上层楼板为真实分块几何，保留庭院、楼梯和电梯井洞口；电梯井入口封闭预留。外墙原一楼出口在上层改为窗洞。
- 二楼设通向三楼的双跑楼梯，采用薄踏步与斜向承托板，避免实心台阶阻挡下层净空。三楼仅保留到达平台、下行洞口和基础护挡，不继续向四楼延伸。
- 没有添加家具、灯具装饰或其他陈设。顶层保持开顶，方便查看基础结构。

## 查看

展开 Hierarchy 的 `JEI_Structure_Study`，可看到 `Floor_01...`、`Floor_02...`、`Floor_03...` 三个楼层组。

Play 模式：鼠标拖动环绕，滚轮缩放；`1 / 2 / 3` 单独显示对应楼层，`0` 显示全部。`Tab` 切换第一人称并恢复整栋显示，`WASD` 行走，鼠标转向，`Esc` 释放鼠标。两侧楼梯都能步行上下三层。

## 资源和检查

- 户型数据：`Assets/JEI_Blockout/Floor02_Plan.json`、`Floor03_Plan.json`。
- 生成代码：`Assets/Editor/JEIUpperFloors.cs`；复用一楼的墙体、门窗和材质生成方法。
- `All_Floors.png`：整栋预览；`Floor02/03_Overview.png`、`Floor02/03_Top.png`：各层透视和平面预览。
- `Unity_Verification.txt`：门洞、走廊、楼板留洞、房间入口及两侧楼梯往返的 CharacterController 实测。
- `Unity_Runtime_Verification.txt`：实际 Play 模式的分层显示和相机切换检查。
- `FirstFloor_Preservation.txt`：一楼所有对象与组件的序列化记录比对。

`Tools > JEI > Structure > Verify Three Floor Structure` 可重新检查几何和通行。新增楼层菜单遇到已有二、三楼时会停止，避免覆盖手工编辑；原一楼重建菜单也会保护已经扩展的场景。
