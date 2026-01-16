using System;
using UnityEngine;

namespace ZeroEngine.AI.UtilityAI
{
    /// <summary>
    /// 响应曲线 - 将输入值映射到 0-1 的评分
    /// 基于 Dave Mark 的 Utility AI 理论
    /// </summary>
    [Serializable]
    public class ResponseCurve
    {
        #region Serialized Fields

        [SerializeField] private CurveType _curveType = CurveType.Linear;
        [SerializeField] private float _slope = 1f;
        [SerializeField] private float _exponent = 2f;
        [SerializeField] private float _xShift = 0f;
        [SerializeField] private float _yShift = 0f;
        [SerializeField] private bool _invert = false;
        [SerializeField] private AnimationCurve _customCurve;

        #endregion

        #region Properties

        public CurveType Type
        {
            get => _curveType;
            set => _curveType = value;
        }

        public float Slope
        {
            get => _slope;
            set => _slope = value;
        }

        public float Exponent
        {
            get => _exponent;
            set => _exponent = Mathf.Max(0.01f, value);
        }

        public bool Invert
        {
            get => _invert;
            set => _invert = value;
        }

        #endregion

        #region Constructors

        public ResponseCurve()
        {
            _customCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        public ResponseCurve(CurveType type, float slope = 1f, float exponent = 2f)
        {
            _curveType = type;
            _slope = slope;
            _exponent = exponent;
            _customCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        }

        #endregion

        #region Evaluation

        /// <summary>
        /// 评估曲线值
        /// </summary>
        /// <param name="input">输入值 (通常 0-1)</param>
        /// <returns>输出值 (0-1)</returns>
        public float Evaluate(float input)
        {
            // 应用 X 偏移
            float x = input + _xShift;

            // 计算基础值
            float result = _curveType switch
            {
                CurveType.Linear => EvaluateLinear(x),
                CurveType.Quadratic => EvaluatePolynomial(x, 2f),
                CurveType.Polynomial => EvaluatePolynomial(x, _exponent),
                CurveType.Logistic => EvaluateLogistic(x),
                CurveType.Logit => EvaluateLogit(x),
                CurveType.Sine => EvaluateSine(x),
                CurveType.Cosine => EvaluateCosine(x),
                CurveType.Exponential => EvaluateExponential(x),
                CurveType.Logarithmic => EvaluateLogarithmic(x),
                CurveType.Threshold => EvaluateThreshold(x),
                CurveType.Custom => EvaluateCustom(x),
                _ => x
            };

            // 应用 Y 偏移
            result += _yShift;

            // 反转
            if (_invert)
            {
                result = 1f - result;
            }

            // 钳制到 0-1
            return Mathf.Clamp01(result);
        }

        #endregion

        #region Curve Functions

        private float EvaluateLinear(float x)
        {
            return _slope * x;
        }

        private float EvaluatePolynomial(float x, float exp)
        {
            return _slope * Mathf.Pow(x, exp);
        }

        private float EvaluateLogistic(float x)
        {
            // S 形曲线: 1 / (1 + e^(-slope * (x - 0.5) * 10))
            float k = _slope * 10f;
            return 1f / (1f + Mathf.Exp(-k * (x - 0.5f)));
        }

        private float EvaluateLogit(float x)
        {
            // Logit 曲线 (反 S 形)
            x = Mathf.Clamp(x, 0.001f, 0.999f);
            return _slope * Mathf.Log(x / (1f - x)) / 10f + 0.5f;
        }

        private float EvaluateSine(float x)
        {
            return _slope * Mathf.Sin(x * Mathf.PI * 0.5f);
        }

        private float EvaluateCosine(float x)
        {
            return _slope * (1f - Mathf.Cos(x * Mathf.PI * 0.5f));
        }

        private float EvaluateExponential(float x)
        {
            // e^(slope * x) - 1, 归一化
            float max = Mathf.Exp(_slope) - 1f;
            if (Mathf.Abs(max) < 0.001f) return x;
            return (Mathf.Exp(_slope * x) - 1f) / max;
        }

        private float EvaluateLogarithmic(float x)
        {
            // log(1 + slope * x), 归一化
            if (x <= 0f) return 0f;
            float max = Mathf.Log(1f + _slope);
            if (Mathf.Abs(max) < 0.001f) return x;
            return Mathf.Log(1f + _slope * x) / max;
        }

        private float EvaluateThreshold(float x)
        {
            return x >= _slope ? 1f : 0f;
        }

        private float EvaluateCustom(float x)
        {
            return _customCurve?.Evaluate(x) ?? x;
        }

        #endregion

        #region Factory Methods

        /// <summary>线性曲线</summary>
        public static ResponseCurve Linear(float slope = 1f)
        {
            return new ResponseCurve(CurveType.Linear, slope);
        }

        /// <summary>二次曲线 (快速增长)</summary>
        public static ResponseCurve Quadratic(float slope = 1f)
        {
            return new ResponseCurve(CurveType.Quadratic, slope);
        }

        /// <summary>S 形曲线 (平滑过渡)</summary>
        public static ResponseCurve Logistic(float slope = 1f)
        {
            return new ResponseCurve(CurveType.Logistic, slope);
        }

        /// <summary>阈值曲线 (开关式)</summary>
        public static ResponseCurve Threshold(float threshold = 0.5f)
        {
            return new ResponseCurve(CurveType.Threshold, threshold);
        }

        /// <summary>反向线性曲线</summary>
        public static ResponseCurve InverseLinear(float slope = 1f)
        {
            return new ResponseCurve(CurveType.Linear, slope) { _invert = true };
        }

        /// <summary>对数曲线 (快速饱和)</summary>
        public static ResponseCurve Logarithmic(float slope = 10f)
        {
            return new ResponseCurve(CurveType.Logarithmic, slope);
        }

        /// <summary>指数曲线 (慢启动快增长)</summary>
        public static ResponseCurve Exponential(float slope = 3f)
        {
            return new ResponseCurve(CurveType.Exponential, slope);
        }

        /// <summary>自定义 AnimationCurve</summary>
        public static ResponseCurve Custom(AnimationCurve curve)
        {
            return new ResponseCurve
            {
                _curveType = CurveType.Custom,
                _customCurve = curve
            };
        }

        #endregion
    }

    /// <summary>
    /// 曲线类型
    /// </summary>
    public enum CurveType
    {
        /// <summary>线性 y = mx</summary>
        Linear,
        /// <summary>二次 y = mx²</summary>
        Quadratic,
        /// <summary>多项式 y = mx^n</summary>
        Polynomial,
        /// <summary>S 形曲线 (Sigmoid)</summary>
        Logistic,
        /// <summary>反 S 形曲线</summary>
        Logit,
        /// <summary>正弦曲线</summary>
        Sine,
        /// <summary>余弦曲线</summary>
        Cosine,
        /// <summary>指数曲线</summary>
        Exponential,
        /// <summary>对数曲线</summary>
        Logarithmic,
        /// <summary>阈值曲线 (阶跃函数)</summary>
        Threshold,
        /// <summary>自定义 AnimationCurve</summary>
        Custom
    }
}
