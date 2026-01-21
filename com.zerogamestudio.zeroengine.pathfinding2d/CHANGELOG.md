# Changelog

All notable changes to this package will be documented in this file.

## [1.6.13] - 2025-01-21

### Added
- JumpLinkCalculator 添加详细边缘节点诊断日志
  - 输出每个边缘节点的坐标、NodeId、OneWay 属性
  - 便于调试跳跃链接生成问题

## [1.6.12] - 2025-01-21

### Fixed
- 修复边缘节点生成不足导致跳跃链接过少的问题
  - **窄平台修复**: 宽度 < MinPlatformWidth 的平台现在生成 Edge 节点而非 Surface 节点，允许作为跳跃起点/终点
  - **跨 Collider 转换节点**: 新增 `GenerateGlobalHeightTransitionNodes()` 方法，在跨 Collider 的高度交界处生成额外边缘节点
  - 预期效果：边缘节点从 17 个增加到 30+，跳跃链接从 1 个增加到 10+

### Added
- `_allEdgesCache`: 缓存所有边缘数据，用于全局转换节点后处理
- `GenerateGlobalHeightTransitionNodes()`: 全局高度转换节点生成，处理跨 Collider 的边缘

## [1.6.11] - 2025-01-21

### Fixed
- `FindTopEdges()` 改用混合检测策略，修复侧面突出平台边缘无法识别的问题
  - 问题：射线检测对于侧面突出平台会失败（射线被上方墙壁阻挡）
  - 解决：优先使用射线检测，失败时回退到法线方向判断
  - 新增 `IsClockwise()` 辅助方法，通过 Shoelace 公式判断多边形顶点顺序
  - 根据顶点顺序动态调整法线方向计算，确保法线始终指向多边形外部

## [1.6.10] - 2025-01-21

### Fixed
- 回滚 `FindTopEdges()` 到射线检测方案，修复 v1.6.9 引入的回归问题
  - 问题：法线方向判断假设了固定的顶点顺序，但 CompositeCollider2D 的顶点顺序不一定是顺时针
  - 导致：正常平台的边缘也无法识别
  - 解决：回滚到射线检测方案，并优化参数避免被墙壁阻挡
    - `standingHeight`: 1.0m → 0.5m（降低射线起点）
    - `rayLength`: 1.5m → 1.0m（缩短射线长度）
    - 命中误差容差: 0.2f → 0.3f（放宽判断）

## [1.6.9] - 2025-01-21

### Changed
- `FindTopEdges()` 改用法线方向判断顶部边缘，替代射线检测
  - 之前：射线检测可能被上方墙壁阻挡，导致侧面突出平台边缘无法识别
  - 现在：直接通过边的法线方向判断（法线 Y > 0.7 即为顶部边缘）
  - **注意：此版本存在回归问题，已在 1.6.10 修复**

## [1.6.8] - 2025-01-21

### Added
- `GenerateHeightTransitionNodes()`: 在高度变化处生成额外边缘节点
- `HasNodeNearPosition()`: 检查指定位置附近是否已存在节点

### Fixed
- 修复侧面墙壁突出平台无法生成 Jump 链接的问题
  - 问题根因：同一 CompositeCollider 的不同高度边缘，边缘节点只在最左/最右端生成
  - 导致上下层平台边缘节点水平距离过大，超过 MaxHorizontalDistance 限制
  - 解决方案：检测高度交界处，在下层平台的对应位置生成额外边缘节点

## [1.6.7] - 2025-01-20

### Added
- 添加同 Collider 轨迹阻挡诊断日志

## [1.6.6] - 2025-01-20

### Fixed
- JumpLinkCalculator 删除"只连最近边缘"限制，支持多边缘连接
