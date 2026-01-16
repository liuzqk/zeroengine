using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZeroEngine.Dialog
{
    /// <summary>
    /// Supported variable types in the dialog system.
    /// </summary>
    public enum DialogVariableType
    {
        Bool,
        Int,
        Float,
        String
    }

    /// <summary>
    /// A single dialog variable with typed value.
    /// </summary>
    [Serializable]
    public struct DialogVariable
    {
        public string Name;
        public DialogVariableType Type;

        // Stored values
        public bool BoolValue;
        public int IntValue;
        public float FloatValue;
        public string StringValue;

        public object GetValue()
        {
            return Type switch
            {
                DialogVariableType.Bool => BoolValue,
                DialogVariableType.Int => IntValue,
                DialogVariableType.Float => FloatValue,
                DialogVariableType.String => StringValue,
                _ => null
            };
        }

        // Type-specific getters to avoid boxing
        public bool GetBool() => BoolValue;
        public int GetInt() => IntValue;
        public float GetFloat() => FloatValue;
        public string GetString() => StringValue ?? string.Empty;

        /// <summary>
        /// Get value as double without boxing for numeric comparisons.
        /// </summary>
        public double GetAsDouble()
        {
            return Type switch
            {
                DialogVariableType.Int => IntValue,
                DialogVariableType.Float => FloatValue,
                DialogVariableType.Bool => BoolValue ? 1.0 : 0.0,
                _ => 0.0
            };
        }

        /// <summary>
        /// Check truthiness without boxing.
        /// </summary>
        public bool IsTruthy()
        {
            return Type switch
            {
                DialogVariableType.Bool => BoolValue,
                DialogVariableType.Int => IntValue != 0,
                DialogVariableType.Float => FloatValue != 0f,
                DialogVariableType.String => !string.IsNullOrEmpty(StringValue),
                _ => false
            };
        }

        public void SetValue(object value)
        {
            switch (Type)
            {
                case DialogVariableType.Bool:
                    BoolValue = Convert.ToBoolean(value);
                    break;
                case DialogVariableType.Int:
                    IntValue = Convert.ToInt32(value);
                    break;
                case DialogVariableType.Float:
                    FloatValue = Convert.ToSingle(value);
                    break;
                case DialogVariableType.String:
                    StringValue = value?.ToString() ?? string.Empty;
                    break;
            }
        }

        public static DialogVariable Create(string name, bool value)
        {
            return new DialogVariable { Name = name, Type = DialogVariableType.Bool, BoolValue = value };
        }

        public static DialogVariable Create(string name, int value)
        {
            return new DialogVariable { Name = name, Type = DialogVariableType.Int, IntValue = value };
        }

        public static DialogVariable Create(string name, float value)
        {
            return new DialogVariable { Name = name, Type = DialogVariableType.Float, FloatValue = value };
        }

        public static DialogVariable Create(string name, string value)
        {
            return new DialogVariable { Name = name, Type = DialogVariableType.String, StringValue = value ?? string.Empty };
        }
    }

    /// <summary>
    /// Container for dialog variables with scope support.
    /// Manages both global (persistent) and local (per-session) variables.
    /// </summary>
    public class DialogVariables
    {
        private readonly Dictionary<string, DialogVariable> _localVariables = new(16);
        private static readonly Dictionary<string, DialogVariable> _globalVariables = new(16);

        /// <summary>
        /// Event fired when any variable changes.
        /// </summary>
        public event Action<string, object, object> OnVariableChanged;

        #region Local Variables

        /// <summary>
        /// Set a local variable (cleared when dialog ends).
        /// </summary>
        public void SetLocal(string name, object value)
        {
            object oldValue = null;
            if (_localVariables.TryGetValue(name, out var existing))
            {
                oldValue = existing.GetValue();
            }

            var variable = CreateVariable(name, value);
            _localVariables[name] = variable;
            OnVariableChanged?.Invoke(name, oldValue, value);
        }

        /// <summary>
        /// Set a typed local variable.
        /// </summary>
        public void SetLocal<T>(string name, T value)
        {
            SetLocal(name, (object)value);
        }

        /// <summary>
        /// Get a local variable value.
        /// </summary>
        public object GetLocal(string name)
        {
            return _localVariables.TryGetValue(name, out var variable) ? variable.GetValue() : null;
        }

        /// <summary>
        /// Get a typed local variable value.
        /// </summary>
        public T GetLocal<T>(string name, T defaultValue = default)
        {
            if (_localVariables.TryGetValue(name, out var variable))
            {
                try
                {
                    return (T)Convert.ChangeType(variable.GetValue(), typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Check if a local variable exists.
        /// </summary>
        public bool HasLocal(string name) => _localVariables.ContainsKey(name);

        /// <summary>
        /// Clear all local variables.
        /// </summary>
        public void ClearLocal() => _localVariables.Clear();

        #endregion

        #region Global Variables

        /// <summary>
        /// Set a global variable (persists across dialogs).
        /// </summary>
        public static void SetGlobal(string name, object value)
        {
            var variable = CreateVariable(name, value);
            _globalVariables[name] = variable;
        }

        /// <summary>
        /// Get a global variable value.
        /// </summary>
        public static object GetGlobal(string name)
        {
            return _globalVariables.TryGetValue(name, out var variable) ? variable.GetValue() : null;
        }

        /// <summary>
        /// Get a typed global variable value.
        /// </summary>
        public static T GetGlobal<T>(string name, T defaultValue = default)
        {
            if (_globalVariables.TryGetValue(name, out var variable))
            {
                try
                {
                    return (T)Convert.ChangeType(variable.GetValue(), typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Check if a global variable exists.
        /// </summary>
        public static bool HasGlobal(string name) => _globalVariables.ContainsKey(name);

        /// <summary>
        /// Clear all global variables.
        /// </summary>
        public static void ClearGlobal() => _globalVariables.Clear();

        #endregion

        #region Unified Access

        /// <summary>
        /// Get a variable value (checks local first, then global).
        /// Variable names starting with "$" are treated as global.
        /// </summary>
        public object Get(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            // Global prefix convention
            if (name.StartsWith("$"))
            {
                return GetGlobal(name.Substring(1));
            }

            // Local first, then global
            if (_localVariables.TryGetValue(name, out var localVar))
            {
                return localVar.GetValue();
            }

            return GetGlobal(name);
        }

        /// <summary>
        /// Get a typed variable value.
        /// </summary>
        public T Get<T>(string name, T defaultValue = default)
        {
            var value = Get(name);
            if (value == null) return defaultValue;

            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Set a variable (respects global prefix).
        /// </summary>
        public void Set(string name, object value)
        {
            if (string.IsNullOrEmpty(name)) return;

            if (name.StartsWith("$"))
            {
                SetGlobal(name.Substring(1), value);
            }
            else
            {
                SetLocal(name, value);
            }
        }

        /// <summary>
        /// Check if a variable exists (checks local first, then global).
        /// </summary>
        public bool Has(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            if (name.StartsWith("$"))
            {
                return HasGlobal(name.Substring(1));
            }

            return HasLocal(name) || HasGlobal(name);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Evaluate a variable as boolean (for conditions).
        /// Optimized to avoid boxing.
        /// </summary>
        public bool IsTruthy(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            // Handle global prefix
            if (name.StartsWith("$"))
            {
                return IsTruthyGlobal(name.Substring(1));
            }

            // Check local first
            if (_localVariables.TryGetValue(name, out var localVar))
            {
                return localVar.IsTruthy();
            }

            // Then global
            return IsTruthyGlobal(name);
        }

        /// <summary>
        /// Check truthiness of a global variable without boxing.
        /// </summary>
        private static bool IsTruthyGlobal(string name)
        {
            return _globalVariables.TryGetValue(name, out var globalVar) && globalVar.IsTruthy();
        }

        public static bool IsTruthyValue(object value)
        {
            if (value == null) return false;
            if (value is bool b) return b;
            if (value is int i) return i != 0;
            if (value is float f) return f != 0f;
            if (value is string s) return !string.IsNullOrEmpty(s);
            return true;
        }

        private static DialogVariable CreateVariable(string name, object value)
        {
            return value switch
            {
                bool b => DialogVariable.Create(name, b),
                int i => DialogVariable.Create(name, i),
                float f => DialogVariable.Create(name, f),
                string s => DialogVariable.Create(name, s),
                double d => DialogVariable.Create(name, (float)d),
                long l => DialogVariable.Create(name, (int)l),
                _ => DialogVariable.Create(name, value?.ToString() ?? string.Empty)
            };
        }

        /// <summary>
        /// Export all local variables for saving.
        /// </summary>
        public Dictionary<string, object> ExportLocal()
        {
            var result = new Dictionary<string, object>(_localVariables.Count);
            foreach (var kvp in _localVariables)
            {
                result[kvp.Key] = kvp.Value.GetValue();
            }
            return result;
        }

        /// <summary>
        /// Import local variables from saved data.
        /// </summary>
        public void ImportLocal(Dictionary<string, object> data)
        {
            _localVariables.Clear();
            if (data == null) return;

            foreach (var kvp in data)
            {
                SetLocal(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Export all global variables for saving.
        /// </summary>
        public static Dictionary<string, object> ExportGlobal()
        {
            var result = new Dictionary<string, object>(_globalVariables.Count);
            foreach (var kvp in _globalVariables)
            {
                result[kvp.Key] = kvp.Value.GetValue();
            }
            return result;
        }

        /// <summary>
        /// Import global variables from saved data.
        /// </summary>
        public static void ImportGlobal(Dictionary<string, object> data)
        {
            _globalVariables.Clear();
            if (data == null) return;

            foreach (var kvp in data)
            {
                SetGlobal(kvp.Key, kvp.Value);
            }
        }

        #endregion
    }
}
