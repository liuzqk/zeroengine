using System;
using System.Text.RegularExpressions;

namespace ZeroEngine.UI.MVVM
{
    /// <summary>
    /// 常用验证器集合
    /// </summary>
    public static class Validators
    {
        /// <summary>
        /// 非空验证
        /// </summary>
        public static Func<string, ValidationResult> NotEmpty(string errorMessage = "不能为空")
        {
            return value => string.IsNullOrWhiteSpace(value)
                ? ValidationResult.Invalid(errorMessage)
                : ValidationResult.Valid;
        }

        /// <summary>
        /// 最小长度验证
        /// </summary>
        public static Func<string, ValidationResult> MinLength(int minLength, string errorMessage = null)
        {
            return value =>
            {
                if (string.IsNullOrEmpty(value) || value.Length < minLength)
                {
                    return ValidationResult.Invalid(errorMessage ?? $"长度至少 {minLength} 个字符");
                }
                return ValidationResult.Valid;
            };
        }

        /// <summary>
        /// 最大长度验证
        /// </summary>
        public static Func<string, ValidationResult> MaxLength(int maxLength, string errorMessage = null)
        {
            return value =>
            {
                if (value != null && value.Length > maxLength)
                {
                    return ValidationResult.Invalid(errorMessage ?? $"长度不能超过 {maxLength} 个字符");
                }
                return ValidationResult.Valid;
            };
        }

        /// <summary>
        /// 长度范围验证
        /// </summary>
        public static Func<string, ValidationResult> LengthRange(int min, int max, string errorMessage = null)
        {
            return value =>
            {
                int len = value?.Length ?? 0;
                if (len < min || len > max)
                {
                    return ValidationResult.Invalid(errorMessage ?? $"长度必须在 {min}-{max} 个字符之间");
                }
                return ValidationResult.Valid;
            };
        }

        /// <summary>
        /// 正则表达式验证
        /// </summary>
        public static Func<string, ValidationResult> Regex(string pattern, string errorMessage = "格式不正确")
        {
            var regex = new Regex(pattern);
            return value =>
            {
                if (string.IsNullOrEmpty(value) || !regex.IsMatch(value))
                {
                    return ValidationResult.Invalid(errorMessage);
                }
                return ValidationResult.Valid;
            };
        }

        /// <summary>
        /// 邮箱验证
        /// </summary>
        public static Func<string, ValidationResult> Email(string errorMessage = "邮箱格式不正确")
        {
            return Regex(@"^[\w\.-]+@[\w\.-]+\.\w+$", errorMessage);
        }

        /// <summary>
        /// 数值范围验证
        /// </summary>
        public static Func<int, ValidationResult> IntRange(int min, int max, string errorMessage = null)
        {
            return value =>
            {
                if (value < min || value > max)
                {
                    return ValidationResult.Invalid(errorMessage ?? $"必须在 {min}-{max} 之间");
                }
                return ValidationResult.Valid;
            };
        }

        /// <summary>
        /// 浮点数范围验证
        /// </summary>
        public static Func<float, ValidationResult> FloatRange(float min, float max, string errorMessage = null)
        {
            return value =>
            {
                if (value < min || value > max)
                {
                    return ValidationResult.Invalid(errorMessage ?? $"必须在 {min:F2}-{max:F2} 之间");
                }
                return ValidationResult.Valid;
            };
        }

        /// <summary>
        /// 正数验证
        /// </summary>
        public static Func<int, ValidationResult> Positive(string errorMessage = "必须为正数")
        {
            return value => value > 0 ? ValidationResult.Valid : ValidationResult.Invalid(errorMessage);
        }

        /// <summary>
        /// 非负数验证
        /// </summary>
        public static Func<int, ValidationResult> NonNegative(string errorMessage = "不能为负数")
        {
            return value => value >= 0 ? ValidationResult.Valid : ValidationResult.Invalid(errorMessage);
        }

        /// <summary>
        /// 组合多个验证器（全部通过才通过）
        /// </summary>
        public static Func<T, ValidationResult> All<T>(params Func<T, ValidationResult>[] validators)
        {
            return value =>
            {
                foreach (var validator in validators)
                {
                    var result = validator(value);
                    if (!result.IsValid)
                    {
                        return result;
                    }
                }
                return ValidationResult.Valid;
            };
        }

        /// <summary>
        /// 组合多个验证器（任一通过即通过）
        /// </summary>
        public static Func<T, ValidationResult> Any<T>(params Func<T, ValidationResult>[] validators)
        {
            return value =>
            {
                ValidationResult lastResult = ValidationResult.Valid;
                foreach (var validator in validators)
                {
                    var result = validator(value);
                    if (result.IsValid)
                    {
                        return ValidationResult.Valid;
                    }
                    lastResult = result;
                }
                return lastResult;
            };
        }
    }
}
