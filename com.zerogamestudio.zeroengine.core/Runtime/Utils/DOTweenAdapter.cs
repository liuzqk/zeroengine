using System;
using System.Reflection;
using UnityEngine;

namespace ZeroEngine.Utils
{
    /// <summary>
    /// Internal Adapter to call DOTween methods via Reflection.
    /// allows ZeroEngine to support DOTween optionally without hard Assembly References.
    /// </summary>
    internal static class DOTweenAdapter
    {
        private static bool _initialized;
        private static bool _available;

        private static MethodInfo _miDOFade; // ShortcutExtensions.DOFade(CanvasGroup, float, float)
        private static MethodInfo _miDOKill; // ShortcutExtensions.DOKill(Component, bool)
        
        private static MethodInfo _miSetEase; // TweenSettingsExtensions.SetEase(Tween, Ease)
        private static MethodInfo _miSetUpdate; // TweenSettingsExtensions.SetUpdate(Tween, bool)
        
        // Cache Types
        private static Type _tEase;

        public static bool IsAvailable
        {
            get
            {
                if (!_initialized) Initialize();
                return _available;
            }
        }

        private static void Initialize()
        {
            _initialized = true;
            try
            {
                // 1. Find Assemblies
                // We scan for specific types to locate the assembly
                Type tShortcut = Type.GetType("DG.Tweening.ShortcutExtensions, DOTween"); 
                if (tShortcut == null) tShortcut = Type.GetType("DG.Tweening.ShortcutExtensions, DOTween.Modules"); // UPM Modules split
                
                // Fallback: Scan all loaded
                if (tShortcut == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        tShortcut = asm.GetType("DG.Tweening.ShortcutExtensions");
                        if (tShortcut != null) break;
                    }
                }

                if (tShortcut == null) return;
                
                // 2. Find Types
                Type tTweenSettings = null;
                // Assembly that contains ShortcutExtensions usually contains TweenSettingsExtensions or referenced it
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type t = asm.GetType("DG.Tweening.TweenSettingsExtensions");
                    if (t != null)
                    {
                        tTweenSettings = t;
                        break;
                    }
                }
                
                // Find Ease Enum
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                     Type t = asm.GetType("DG.Tweening.Ease");
                     if (t != null) { _tEase = t; break; }
                }

                if (tShortcut != null && tTweenSettings != null && _tEase != null)
                {
                     // 3. Get Methods
                     // CanvasGroup DOFade
                     _miDOFade = tShortcut.GetMethod("DOFade", new Type[] { typeof(CanvasGroup), typeof(float), typeof(float) });
                     _miDOKill = tShortcut.GetMethod("DOKill", new Type[] { typeof(Component), typeof(bool) });

                     // Tween Settings
                     // SetEase(Tween, Ease)
                     // Since Tween is generic/base, we usually search by name or logic
                     // SetEase returns T (Tween)
                     var methods = tTweenSettings.GetMethods();
                     foreach (var m in methods)
                     {
                         if (m.Name == "SetEase" && m.GetParameters().Length == 2)
                         {
                             var p2 = m.GetParameters()[1];
                             if (p2.ParameterType == _tEase) { _miSetEase = m; }
                         }
                         if (m.Name == "SetUpdate" && m.GetParameters().Length == 2)
                         {
                             var p2 = m.GetParameters()[1];
                             if (p2.ParameterType == typeof(bool)) { _miSetUpdate = m; }
                         }
                     }
                     
                     if (_miDOFade != null) _available = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ZeroEngine] functionality check warning: {e.Message}");
                _available = false;
            }
        }

        public static void DOKill(Component target)
        {
            if (!IsAvailable || target == null) return;
            try
            {
                 // DOKill is extension: DOKill(target, complete=false)
                 _miDOKill.Invoke(null, new object[] { target, false }); 
            }
            catch { }
        }

        public static void FadeCanvasGroup(CanvasGroup cg, float endValue, float duration, ZeroEase ease, bool outputDebug = false)
        {
            if (!IsAvailable || cg == null) return;
            try
            {
                // var tween = cg.DOFade(endValue, duration)
                object tween = _miDOFade.Invoke(null, new object[] { cg, endValue, duration });
                
                if (tween != null)
                {
                    // .SetUpdate(true)
                    if (_miSetUpdate != null)
                    {
                        _miSetUpdate.Invoke(null, new object[] { tween, true });
                    }

                    // .SetEase(ease)
                    if (_miSetEase != null && _tEase != null)
                    {
                        // Convert ZeroEase to DG.Tweening.Ease
                        // We assume Enum names match or use index.
                        // Standard DOTween Ease enum: Unset=0, Linear=1, InSine=2...
                        // ZeroEase: Linear=0, InSine=1...
                        // Need parsing to be safe.
                        try
                        {
                            object easeVal = Enum.Parse(_tEase, ease.ToString());
                            _miSetEase.Invoke(null, new object[] { tween, easeVal });
                        }
                        catch
                        {
                             // Fallback or ignore
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if(outputDebug) Debug.LogError($"[DOTweenAdapter] Error: {e}");
            }
        }
    }
}
