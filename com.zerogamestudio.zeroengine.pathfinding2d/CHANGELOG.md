# Changelog

All notable changes to this package will be documented in this file.

## [1.6.9] - 2025-01-21

### Changed
- `FindTopEdges()` 改用法线方向判断顶部边缘，替代射线检测
  - 之前：射线检测可能被上方墙壁阻挡，导致侧面突出平台边缘无法识别
  - 现在：直接通过边的法线方向判断（法线 Y > 0.7 即为顶部边缘）

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
