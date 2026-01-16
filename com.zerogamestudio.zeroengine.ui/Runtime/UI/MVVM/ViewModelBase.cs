using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if TEXTMESHPRO_ENABLED
using TMPro;
#endif

namespace ZeroEngine.UI.MVVM
{
    /// <summary>
    /// ViewModel基类
    /// </summary>
    public abstract class ViewModelBase : IDisposable
    {
        private List<IDisposable> _disposables = new();
        private bool _isDisposed;

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// 刷新数据
        /// </summary>
        public virtual void Refresh() { }

        /// <summary>
        /// 添加需要清理的资源
        /// </summary>
        protected void AddDisposable(IDisposable disposable)
        {
            _disposables.Add(disposable);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;

            OnDispose();

            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }

            _disposables.Clear();
            _isDisposed = true;
        }

        protected virtual void OnDispose() { }
    }

    /// <summary>
    /// 绑定上下文 - 管理View和ViewModel的绑定关系
    /// </summary>
    public class BindingContext : IDisposable
    {
        private List<Action> _unbindActions = new();

        /// <summary>
        /// 绑定属性到UI元素
        /// </summary>
        public void Bind<T>(BindableProperty<T> property, Action<T> setter)
        {
            property.RegisterAndInvoke(setter);
            _unbindActions.Add(() => property.Unregister(setter));
        }

        /// <summary>
        /// 绑定列表
        /// </summary>
        public void BindList<T>(BindableList<T> list, Action onChanged)
        {
            list.OnListChanged += onChanged;
            _unbindActions.Add(() => list.OnListChanged -= onChanged);
            onChanged?.Invoke(); // 立即触发一次
        }

        /// <summary>
        /// 绑定按钮点击
        /// </summary>
        public void BindClick(Button button, Action onClick)
        {
            if (button == null) return;
            UnityAction action = () => onClick?.Invoke();
            button.onClick.AddListener(action);
            _unbindActions.Add(() => button.onClick.RemoveListener(action));
        }

        /// <summary>
        /// 绑定命令到按钮
        /// </summary>
        public void BindCommand(Button button, ICommand command, object parameter = null)
        {
            if (button == null || command == null) return;

            UnityAction executeAction = () => command.Execute(parameter);

            EventHandler canExecuteChangedHandler = (s, e) =>
            {
                if (button != null)
                {
                    button.interactable = command.CanExecute(parameter);
                }
            };

            button.onClick.AddListener(executeAction);
            command.CanExecuteChanged += canExecuteChangedHandler;
            canExecuteChangedHandler(this, EventArgs.Empty);

            _unbindActions.Add(() =>
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(executeAction);
                }
                command.CanExecuteChanged -= canExecuteChangedHandler;
            });
        }

        /// <summary>
        /// 绑定Text组件
        /// </summary>
        public void BindText(Text text, BindableProperty<string> property)
        {
            if (text == null) return;
            Action<string> propChange = v => text.text = v;
            property.RegisterAndInvoke(propChange);
            _unbindActions.Add(() => property.Unregister(propChange));
        }

        /// <summary>
        /// 绑定Text组件（使用格式化值）
        /// </summary>
        public void BindTextFormatted<T>(Text text, BindableProperty<T> property)
        {
            if (text == null) return;
            Action<T> propChange = v => text.text = property.FormattedValue;
            property.RegisterAndInvoke(propChange);
            _unbindActions.Add(() => property.Unregister(propChange));
        }

        /// <summary>
        /// 绑定验证错误显示
        /// </summary>
        public void BindValidation<T>(Text errorText, BindableProperty<T> property)
        {
            if (errorText == null) return;

            Action<ValidationResult> handler = result =>
            {
                if (errorText != null)
                {
                    errorText.text = result.ErrorMessage ?? string.Empty;
                    errorText.gameObject.SetActive(!result.IsValid);
                }
            };

            property.OnValidationChanged += handler;
            handler(property.ValidationState);

            _unbindActions.Add(() => property.OnValidationChanged -= handler);
        }

        /// <summary>
        /// 绑定验证状态到 Graphic 颜色
        /// </summary>
        public void BindValidationColor<T>(Graphic graphic, BindableProperty<T> property,
            Color validColor, Color invalidColor)
        {
            if (graphic == null) return;

            Action<ValidationResult> handler = result =>
            {
                if (graphic != null)
                {
                    graphic.color = result.IsValid ? validColor : invalidColor;
                }
            };

            property.OnValidationChanged += handler;
            handler(property.ValidationState);

            _unbindActions.Add(() => property.OnValidationChanged -= handler);
        }

        /// <summary>
        /// 双向绑定两个属性
        /// </summary>
        public void BindTwoWay<T>(BindableProperty<T> source, BindableProperty<T> target)
        {
            bool updating = false;

            Action<T> sourceToTarget = v =>
            {
                if (updating) return;
                updating = true;
                target.Value = v;
                updating = false;
            };

            Action<T> targetToSource = v =>
            {
                if (updating) return;
                updating = true;
                source.Value = v;
                updating = false;
            };

            source.Register(sourceToTarget);
            target.Register(targetToSource);

            // 初始同步
            target.Value = source.Value;

            _unbindActions.Add(() =>
            {
                source.Unregister(sourceToTarget);
                target.Unregister(targetToSource);
            });
        }

        /// <summary>
        /// 绑定旧版输入框
        /// </summary>
        public void BindInput(InputField input, BindableProperty<string> property)
        {
            if (input == null) return;

            UnityAction<string> valueChangedAction = v => property.Value = v;
            Action<string> propertyChangedAction = v =>
            {
                if (input != null && input.text != v) input.text = v;
            };

            input.text = property.Value;
            input.onValueChanged.AddListener(valueChangedAction);
            property.Register(propertyChangedAction);

            _unbindActions.Add(() =>
            {
                if (input != null)
                {
                    input.onValueChanged.RemoveListener(valueChangedAction);
                }
                property.Unregister(propertyChangedAction);
            });
        }

#if TEXTMESHPRO_ENABLED
        /// <summary>
        /// 绑定TMP输入框
        /// </summary>
        public void BindInput(TMP_InputField input, BindableProperty<string> property)
        {
            if (input == null) return;

            UnityAction<string> valueChangedAction = v => property.Value = v;
            Action<string> propertyChangedAction = v =>
            {
                if (input != null && input.text != v) input.text = v;
            };

            input.text = property.Value;
            input.onValueChanged.AddListener(valueChangedAction);
            property.Register(propertyChangedAction);

            _unbindActions.Add(() =>
            {
                if (input != null)
                {
                    input.onValueChanged.RemoveListener(valueChangedAction);
                }
                property.Unregister(propertyChangedAction);
            });
        }

        /// <summary>
        /// 绑定TMP下拉框
        /// </summary>
        public void BindDropdown(TMP_Dropdown dropdown, BindableProperty<int> property)
        {
            if (dropdown == null) return;

            UnityAction<int> valueChangedAction = v => property.Value = v;
            Action<int> propertyChangedAction = v =>
            {
                if (dropdown != null && dropdown.value != v) dropdown.value = v;
            };

            dropdown.value = property.Value;
            dropdown.onValueChanged.AddListener(valueChangedAction);
            property.Register(propertyChangedAction);

            _unbindActions.Add(() =>
            {
                if (dropdown != null)
                {
                    dropdown.onValueChanged.RemoveListener(valueChangedAction);
                }
                property.Unregister(propertyChangedAction);
            });
        }
#endif

        /// <summary>
        /// 绑定滑动条
        /// </summary>
        public void BindSlider(Slider slider, BindableProperty<float> property)
        {
            if (slider == null) return;

            UnityAction<float> valueChangedAction = v => property.Value = v;
            Action<float> propertyChangedAction = v =>
            {
                if (slider != null && !Mathf.Approximately(slider.value, v)) slider.value = v;
            };

            slider.value = property.Value;
            slider.onValueChanged.AddListener(valueChangedAction);
            property.Register(propertyChangedAction);

            _unbindActions.Add(() =>
            {
                if (slider != null)
                {
                    slider.onValueChanged.RemoveListener(valueChangedAction);
                }
                property.Unregister(propertyChangedAction);
            });
        }

        /// <summary>
        /// 绑定开关
        /// </summary>
        public void BindToggle(Toggle toggle, BindableProperty<bool> property)
        {
            if (toggle == null) return;

            UnityAction<bool> valueChangedAction = v => property.Value = v;
            Action<bool> propertyChangedAction = v =>
            {
                if (toggle != null && toggle.isOn != v) toggle.isOn = v;
            };

            toggle.isOn = property.Value;
            toggle.onValueChanged.AddListener(valueChangedAction);
            property.Register(propertyChangedAction);

            _unbindActions.Add(() =>
            {
                if (toggle != null)
                {
                    toggle.onValueChanged.RemoveListener(valueChangedAction);
                }
                property.Unregister(propertyChangedAction);
            });
        }

        /// <summary>
        /// 绑定旧版下拉框
        /// </summary>
        public void BindDropdown(Dropdown dropdown, BindableProperty<int> property)
        {
            if (dropdown == null) return;

            UnityAction<int> valueChangedAction = v => property.Value = v;
            Action<int> propertyChangedAction = v =>
            {
                if (dropdown != null && dropdown.value != v) dropdown.value = v;
            };

            dropdown.value = property.Value;
            dropdown.onValueChanged.AddListener(valueChangedAction);
            property.Register(propertyChangedAction);

            _unbindActions.Add(() =>
            {
                if (dropdown != null)
                {
                    dropdown.onValueChanged.RemoveListener(valueChangedAction);
                }
                property.Unregister(propertyChangedAction);
            });
        }

        /// <summary>
        /// 解除所有绑定
        /// </summary>
        public void Dispose()
        {
            foreach (var unbind in _unbindActions)
            {
                unbind?.Invoke();
            }
            _unbindActions.Clear();
        }
    }
}
