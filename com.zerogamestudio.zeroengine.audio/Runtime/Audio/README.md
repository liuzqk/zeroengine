# ZeroEngine.Audio API 文档

> **用途**: 本文档面向AI助手，提供Audio（音频系统）模块的快速参考。
> **版本**: v1.0.0+
> **最后更新**: 2025-12-31

---

An advanced, pooled audio system designed for AAA-like flexibility without middleware overhead.

## Key Features
- **AudioCue**: Rich SFX definitions with random pitch/volume and global cooldowns (spam protection).
- **Interactive Music**: Intro + Loop support with cross-fading.
- **Smart Pooling**: Zero-allocation playback via `AudioEmitter`.
- **3D Spatial**: Simple API for 3D sound positioning.

## Structure
- **Config**: `AudioCueSO` (SFX), `AudioMusicSO` (Music)
- **Runtime**: `AudioManager`, `AudioEmitter`

## Usage

### 1. Sound Effects (SFX)
Create an `AudioCueSO` asset:
- Right-click -> **ZeroEngine** -> **Audio** -> **Audio Cue**.
- Assign multiple clips (randomly picked).
- Set Pitch/Volume ranges and Cooldown (e.g., 0.1s to prevent machine-gun sounds).

Play it:
```csharp
// 2D Stereo (UI)
AudioManager.Instance.PlaySFX(myCue);

// 3D Spatial
AudioManager.Instance.PlaySFX(explosionCue, transform.position);
```

### 2. Music (BGM)
Create an `AudioMusicSO` asset:
- Right-click -> **ZeroEngine** -> **Audio** -> **Audio Music**.
- **IntroClip**: Played once (optional).
- **LoopClip**: Looped indefinitely.

Play it:
```csharp
// Cross-fade to new music over 1.0 second
AudioManager.Instance.PlayMusic(battleMusic, 1.0f);

// Stop music
AudioManager.Instance.StopMusic(2.0f);
```

### 3. Legacy Support
You can still play raw clips, though `AudioCue` is recommended.
```csharp
AudioManager.Instance.PlaySFX(simpleClip);
```
