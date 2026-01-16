// ============================================================================
// ZeroEngine v2.7.0 - Skill Visual System
// 伤害数字弹出事件
// ============================================================================

using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

#if ZEROENGINE_DOTWEEN
using DG.Tweening;
#endif

namespace ZeroEngine.RPG.SkillVisual
{
    /// <summary>
    /// 弹出类型
    /// </summary>
    public enum PopupType
    {
        /// <summary>伤害</summary>
        Damage,
        /// <summary>治疗</summary>
        Heal,
        /// <summary>暴击</summary>
        Critical,
        /// <summary>Miss</summary>
        Miss,
        /// <summary>自定义文本</summary>
        Custom
    }

    /// <summary>
    /// 伤害/数字弹出事件 - 在目标位置显示弹出文字
    /// </summary>
    [Serializable]
#if ODIN_INSPECTOR
    [LabelText("Damage Popup")]
#endif
    public class DamagePopupEvent : VisualEvent
    {
#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("弹出类型")]
        public PopupType PopupType = PopupType.Damage;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), ShowIf("PopupType", PopupType.Custom)]
#endif
        [Tooltip("自定义文本")]
        public string CustomText;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings"), HideIf("PopupType", PopupType.Custom), HideIf("PopupType", PopupType.Miss)]
#endif
        [Tooltip("数值 (从 Context 获取时忽略)")]
        public int Value;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("弹出位置")]
        public SpawnTarget PopupAt = SpawnTarget.Target;

#if ODIN_INSPECTOR
        [FoldoutGroup("Settings")]
#endif
        [Tooltip("位置偏移")]
        public Vector3 Offset = new Vector3(0, 1f, 0);

#if ODIN_INSPECTOR
        [FoldoutGroup("Animation")]
#endif
        [Tooltip("弹出动画时长")]
        public float Duration = 1f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Animation")]
#endif
        [Tooltip("向上飘动距离")]
        public float FloatDistance = 1f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Animation")]
#endif
        [Tooltip("缩放动画")]
        public bool ScaleAnimation = true;

#if ODIN_INSPECTOR
        [FoldoutGroup("Animation"), ShowIf("ScaleAnimation")]
#endif
        [Tooltip("起始缩放")]
        public float StartScale = 0.5f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Animation"), ShowIf("ScaleAnimation")]
#endif
        [Tooltip("最大缩放")]
        public float MaxScale = 1.2f;

#if ODIN_INSPECTOR
        [FoldoutGroup("Style")]
#endif
        [Tooltip("伤害颜色")]
        public Color DamageColor = Color.red;

#if ODIN_INSPECTOR
        [FoldoutGroup("Style")]
#endif
        [Tooltip("治疗颜色")]
        public Color HealColor = Color.green;

#if ODIN_INSPECTOR
        [FoldoutGroup("Style")]
#endif
        [Tooltip("暴击颜色")]
        public Color CriticalColor = Color.yellow;

#if ODIN_INSPECTOR
        [FoldoutGroup("Style")]
#endif
        [Tooltip("弹出文字 Prefab (需要有 TextMeshPro 或 Text 组件)")]
        public GameObject PopupPrefab;

#if ZEROENGINE_DOTWEEN
        public override void AddToSequence(Sequence sequence, VisualContext context)
        {
            if (!Enabled) return;

            sequence.InsertCallback(Delay, () => ShowPopup(context));
        }
#endif

        public override void Execute(VisualContext context)
        {
            if (!Enabled) return;
            ShowPopup(context);
        }

        private void ShowPopup(VisualContext context)
        {
            if (PopupPrefab == null)
            {
                Debug.LogWarning("[DamagePopupEvent] PopupPrefab is not set");
                return;
            }

            Vector3 position = GetSpawnPosition(context, PopupAt, Offset);

            // 实例化弹出文字
            var popupGO = UnityEngine.Object.Instantiate(PopupPrefab, position, Quaternion.identity);

            // 设置文本和颜色
            string text = GetText();
            Color color = GetColor();
            SetPopupText(popupGO, text, color);

            // 播放动画
#if ZEROENGINE_DOTWEEN
            AnimatePopup(popupGO, position);
#else
            // 没有 DOTween 时直接延迟销毁
            UnityEngine.Object.Destroy(popupGO, Duration);
#endif
        }

        private string GetText()
        {
            return PopupType switch
            {
                PopupType.Damage => Value.ToString(),
                PopupType.Heal => "+" + Value.ToString(),
                PopupType.Critical => Value.ToString() + "!",
                PopupType.Miss => "MISS",
                PopupType.Custom => CustomText,
                _ => Value.ToString()
            };
        }

        private Color GetColor()
        {
            return PopupType switch
            {
                PopupType.Damage => DamageColor,
                PopupType.Heal => HealColor,
                PopupType.Critical => CriticalColor,
                PopupType.Miss => Color.gray,
                PopupType.Custom => Color.white,
                _ => Color.white
            };
        }

        private void SetPopupText(GameObject popupGO, string text, Color color)
        {
            // 尝试 TextMeshPro
#if TEXTMESHPRO
            var tmp = popupGO.GetComponentInChildren<TMPro.TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = text;
                tmp.color = color;
                return;
            }

            var tmpUI = popupGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (tmpUI != null)
            {
                tmpUI.text = text;
                tmpUI.color = color;
                return;
            }
#endif

            // 回退到 Unity UI Text
            var uiText = popupGO.GetComponentInChildren<UnityEngine.UI.Text>();
            if (uiText != null)
            {
                uiText.text = text;
                uiText.color = color;
                return;
            }

            // 回退到 TextMesh
            var textMesh = popupGO.GetComponentInChildren<TextMesh>();
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
            }
        }

#if ZEROENGINE_DOTWEEN
        private void AnimatePopup(GameObject popupGO, Vector3 startPosition)
        {
            var transform = popupGO.transform;

            // 初始缩放
            if (ScaleAnimation)
            {
                transform.localScale = Vector3.one * StartScale;
            }

            var sequence = DOTween.Sequence();

            // 向上飘动
            sequence.Append(transform.DOMoveY(startPosition.y + FloatDistance, Duration)
                .SetEase(Ease.OutQuad));

            // 缩放动画
            if (ScaleAnimation)
            {
                sequence.Insert(0, transform.DOScale(MaxScale, Duration * 0.3f)
                    .SetEase(Ease.OutBack));
                sequence.Insert(Duration * 0.3f, transform.DOScale(1f, Duration * 0.2f)
                    .SetEase(Ease.InOutQuad));
            }

            // 淡出 (如果有 CanvasGroup)
            var canvasGroup = popupGO.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                sequence.Insert(Duration * 0.6f, canvasGroup.DOFade(0f, Duration * 0.4f));
            }

            // 完成后销毁
            sequence.OnComplete(() =>
            {
                if (popupGO != null)
                {
                    UnityEngine.Object.Destroy(popupGO);
                }
            });

            sequence.Play();
        }
#endif
    }
}
