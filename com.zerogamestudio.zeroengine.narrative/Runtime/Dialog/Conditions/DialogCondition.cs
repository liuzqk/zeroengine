using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ZeroEngine.Dialog
{
    /// <summary>
    /// Comparison operators for conditions.
    /// </summary>
    public enum ComparisonOperator
    {
        Equal,          // ==
        NotEqual,       // !=
        LessThan,       // <
        LessOrEqual,    // <=
        GreaterThan,    // >
        GreaterOrEqual  // >=
    }

    /// <summary>
    /// Logical operators for combining conditions.
    /// </summary>
    public enum LogicalOperator
    {
        And,    // &&
        Or      // ||
    }

    /// <summary>
    /// A single condition expression.
    /// </summary>
    [Serializable]
    public class DialogConditionExpression
    {
        [Tooltip("Variable name to check")]
        public string Variable;

        [Tooltip("Comparison operator")]
        public ComparisonOperator Operator;

        [Tooltip("Value to compare against (auto-parsed based on variable type)")]
        public string Value;

        [Tooltip("Negate the result")]
        public bool Negate;

        /// <summary>
        /// Evaluate this condition against a variable provider.
        /// </summary>
        public bool Evaluate(DialogVariables variables)
        {
            var leftValue = variables.Get(Variable);
            var rightValue = ParseValue(Value, leftValue);

            bool result = Compare(leftValue, rightValue, Operator);
            return Negate ? !result : result;
        }

        private static object ParseValue(string valueStr, object referenceValue)
        {
            if (string.IsNullOrEmpty(valueStr)) return null;

            // Try to parse based on reference type
            if (referenceValue is bool)
            {
                if (bool.TryParse(valueStr, out bool b)) return b;
                if (valueStr == "1" || string.Equals(valueStr, "true", StringComparison.OrdinalIgnoreCase)) return true;
                if (valueStr == "0" || string.Equals(valueStr, "false", StringComparison.OrdinalIgnoreCase)) return false;
            }
            else if (referenceValue is int)
            {
                if (int.TryParse(valueStr, out int i)) return i;
            }
            else if (referenceValue is float)
            {
                if (float.TryParse(valueStr, out float f)) return f;
            }

            // Default parsing order: int -> float -> bool -> string
            if (int.TryParse(valueStr, out int intVal)) return intVal;
            if (float.TryParse(valueStr, out float floatVal)) return floatVal;
            if (bool.TryParse(valueStr, out bool boolVal)) return boolVal;

            return valueStr;
        }

        private static bool Compare(object left, object right, ComparisonOperator op)
        {
            // Handle null cases
            if (left == null && right == null)
            {
                return op == ComparisonOperator.Equal;
            }
            if (left == null || right == null)
            {
                return op == ComparisonOperator.NotEqual;
            }

            // For equality, use object.Equals
            if (op == ComparisonOperator.Equal)
            {
                return left.Equals(right) || left.ToString() == right.ToString();
            }
            if (op == ComparisonOperator.NotEqual)
            {
                return !left.Equals(right) && left.ToString() != right.ToString();
            }

            // For comparison operators, convert to double
            if (!TryConvertToDouble(left, out double leftNum) ||
                !TryConvertToDouble(right, out double rightNum))
            {
                // String comparison as fallback
                int cmp = string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal);
                return op switch
                {
                    ComparisonOperator.LessThan => cmp < 0,
                    ComparisonOperator.LessOrEqual => cmp <= 0,
                    ComparisonOperator.GreaterThan => cmp > 0,
                    ComparisonOperator.GreaterOrEqual => cmp >= 0,
                    _ => false
                };
            }

            return op switch
            {
                ComparisonOperator.LessThan => leftNum < rightNum,
                ComparisonOperator.LessOrEqual => leftNum <= rightNum,
                ComparisonOperator.GreaterThan => leftNum > rightNum,
                ComparisonOperator.GreaterOrEqual => leftNum >= rightNum,
                _ => false
            };
        }

        private static bool TryConvertToDouble(object value, out double result)
        {
            result = 0;
            if (value == null) return false;

            try
            {
                result = Convert.ToDouble(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// A group of conditions combined with a logical operator.
    /// </summary>
    [Serializable]
    public class DialogConditionGroup
    {
        [Tooltip("How to combine conditions in this group")]
        public LogicalOperator CombineWith = LogicalOperator.And;

        [Tooltip("Conditions in this group")]
        public List<DialogConditionExpression> Conditions = new();

        /// <summary>
        /// Evaluate the condition group.
        /// </summary>
        public bool Evaluate(DialogVariables variables)
        {
            if (Conditions == null || Conditions.Count == 0) return true;

            if (CombineWith == LogicalOperator.And)
            {
                for (int i = 0; i < Conditions.Count; i++)
                {
                    if (!Conditions[i].Evaluate(variables)) return false;
                }
                return true;
            }
            else // Or
            {
                for (int i = 0; i < Conditions.Count; i++)
                {
                    if (Conditions[i].Evaluate(variables)) return true;
                }
                return false;
            }
        }
    }

    /// <summary>
    /// Static utility for parsing and evaluating condition expressions.
    /// Supports string expression syntax: "health > 50 && hasKey == true"
    /// </summary>
    public static class DialogConditionParser
    {
        // Regex for parsing simple expressions
        private static readonly Regex ExpressionRegex = new(
            @"(\!)?(\w+)\s*(==|!=|<=|>=|<|>)\s*([""']?[\w.]+[""']?)",
            RegexOptions.Compiled);

        private static readonly Regex LogicalSplitRegex = new(
            @"\s*(&&|\|\|)\s*",
            RegexOptions.Compiled);

        /// <summary>
        /// Evaluate a string expression.
        /// Examples:
        /// - "hasKey" (checks truthiness)
        /// - "!hasKey" (negated truthiness)
        /// - "gold >= 100"
        /// - "name == 'Player'"
        /// - "health > 50 && mana > 20"
        /// - "isVIP || gold >= 1000"
        /// </summary>
        public static bool Evaluate(string expression, DialogVariables variables)
        {
            if (string.IsNullOrWhiteSpace(expression)) return true;

            expression = expression.Trim();

            // Check for logical operators
            var logicalParts = LogicalSplitRegex.Split(expression);

            if (logicalParts.Length == 1)
            {
                // Single expression
                return EvaluateSingle(expression, variables);
            }

            // Multiple expressions with logical operators
            bool result = EvaluateSingle(logicalParts[0], variables);

            for (int i = 1; i < logicalParts.Length; i += 2)
            {
                if (i + 1 >= logicalParts.Length) break;

                string op = logicalParts[i];
                bool nextResult = EvaluateSingle(logicalParts[i + 1], variables);

                if (op == "&&")
                {
                    result = result && nextResult;
                }
                else if (op == "||")
                {
                    result = result || nextResult;
                }
            }

            return result;
        }

        private static bool EvaluateSingle(string expression, DialogVariables variables)
        {
            expression = expression.Trim();
            if (string.IsNullOrEmpty(expression)) return true;

            // Match comparison expression
            var match = ExpressionRegex.Match(expression);
            if (match.Success)
            {
                bool negate = !string.IsNullOrEmpty(match.Groups[1].Value);
                string varName = match.Groups[2].Value;
                string opStr = match.Groups[3].Value;
                string valueStr = match.Groups[4].Value.Trim('"', '\'');

                var op = ParseOperator(opStr);

                // Direct evaluation without creating temporary object
                bool result = EvaluateComparison(varName, op, valueStr, variables);
                return negate ? !result : result;
            }

            // Simple variable truthiness check
            bool negated = expression.StartsWith("!");
            string variableName = negated ? expression.Substring(1).Trim() : expression;

            bool isTruthy = variables.IsTruthy(variableName);
            return negated ? !isTruthy : isTruthy;
        }

        /// <summary>
        /// Evaluate a comparison directly without allocating DialogConditionExpression.
        /// </summary>
        private static bool EvaluateComparison(string varName, ComparisonOperator op, string valueStr, DialogVariables variables)
        {
            var leftValue = variables.Get(varName);
            var rightValue = ParseValueDirect(valueStr, leftValue);
            return CompareDirect(leftValue, rightValue, op);
        }

        private static object ParseValueDirect(string valueStr, object referenceValue)
        {
            if (string.IsNullOrEmpty(valueStr)) return null;

            // Try to parse based on reference type
            if (referenceValue is bool)
            {
                if (bool.TryParse(valueStr, out bool b)) return b;
                if (valueStr == "1" || string.Equals(valueStr, "true", StringComparison.OrdinalIgnoreCase)) return true;
                if (valueStr == "0" || string.Equals(valueStr, "false", StringComparison.OrdinalIgnoreCase)) return false;
            }
            else if (referenceValue is int)
            {
                if (int.TryParse(valueStr, out int i)) return i;
            }
            else if (referenceValue is float)
            {
                if (float.TryParse(valueStr, out float f)) return f;
            }

            // Default parsing order: int -> float -> bool -> string
            if (int.TryParse(valueStr, out int intVal)) return intVal;
            if (float.TryParse(valueStr, out float floatVal)) return floatVal;
            if (bool.TryParse(valueStr, out bool boolVal)) return boolVal;

            return valueStr;
        }

        private static bool CompareDirect(object left, object right, ComparisonOperator op)
        {
            // Handle null cases
            if (left == null && right == null)
            {
                return op == ComparisonOperator.Equal;
            }
            if (left == null || right == null)
            {
                return op == ComparisonOperator.NotEqual;
            }

            // For equality, use object.Equals first
            if (op == ComparisonOperator.Equal)
            {
                if (left.Equals(right)) return true;
                // String comparison fallback
                return string.Equals(left.ToString(), right.ToString(), StringComparison.Ordinal);
            }
            if (op == ComparisonOperator.NotEqual)
            {
                if (!left.Equals(right))
                {
                    return !string.Equals(left.ToString(), right.ToString(), StringComparison.Ordinal);
                }
                return false;
            }

            // For comparison operators, convert to double
            if (!TryConvertToDoubleDirect(left, out double leftNum) ||
                !TryConvertToDoubleDirect(right, out double rightNum))
            {
                // String comparison as fallback
                int cmp = string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal);
                return op switch
                {
                    ComparisonOperator.LessThan => cmp < 0,
                    ComparisonOperator.LessOrEqual => cmp <= 0,
                    ComparisonOperator.GreaterThan => cmp > 0,
                    ComparisonOperator.GreaterOrEqual => cmp >= 0,
                    _ => false
                };
            }

            return op switch
            {
                ComparisonOperator.LessThan => leftNum < rightNum,
                ComparisonOperator.LessOrEqual => leftNum <= rightNum,
                ComparisonOperator.GreaterThan => leftNum > rightNum,
                ComparisonOperator.GreaterOrEqual => leftNum >= rightNum,
                _ => false
            };
        }

        private static bool TryConvertToDoubleDirect(object value, out double result)
        {
            result = 0;
            if (value == null) return false;

            if (value is int i) { result = i; return true; }
            if (value is float f) { result = f; return true; }
            if (value is double d) { result = d; return true; }
            if (value is bool b) { result = b ? 1 : 0; return true; }

            return double.TryParse(value.ToString(), out result);
        }

        private static ComparisonOperator ParseOperator(string op)
        {
            return op switch
            {
                "==" => ComparisonOperator.Equal,
                "!=" => ComparisonOperator.NotEqual,
                "<" => ComparisonOperator.LessThan,
                "<=" => ComparisonOperator.LessOrEqual,
                ">" => ComparisonOperator.GreaterThan,
                ">=" => ComparisonOperator.GreaterOrEqual,
                _ => ComparisonOperator.Equal
            };
        }
    }
}
