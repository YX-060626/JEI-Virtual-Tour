# 角色、跑步和跳跃

场景：`Assets/Scenes/JEI_Floor_Structure.unity`。

- 新角色采用简洁卡通人物造型：短发、五官、夹克、长裤和运动鞋，肩肘与髋膝分段活动。
- 行走、跑步按实际移动速度驱动交替迈腿和反向摆臂，包含膝盖与脚踝弯曲、身体起伏、跑步前倾、待机呼吸及起跳/落地过渡。撞墙停止移动时，动作也会逐渐停下。
- **Shift 按一下切换跑步，再按一下恢复步行**，左右 Shift 均可。步行 3.2 m/s、跑步 5.8 m/s，屏幕提示显示 WALK/RUN。
- **Space 空格跳跃**，设计高度约 0.9 m，只能着地起跳，碰到天花板会停止上升。第一、第三人称均可使用。
- **V** 切换第一/第三人称，**Tab** 切换建筑总览；Esc 释放鼠标，点击 Game 画面恢复控制。

人物为项目内生成的关节模型和程序动画，不依赖外部下载或动画包。控制参数可在 `JEITourController` 组件中调整。

旧人偶保留为玩家下的 `Previous mannequin__inactive`，默认停用；新角色为 `Tour Avatar__articulated_student`。

`Verification.txt` 为 Unity 运行测试结果，PNG 为模型及姿态预览。
