# Changelog

All notable changes to this package will be documented in this file.

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
