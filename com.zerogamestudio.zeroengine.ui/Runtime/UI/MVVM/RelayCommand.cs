using System;

namespace ZeroEngine.UI.MVVM
{
    /// <summary>
    /// 命令接口 - MVVM命令绑定核心
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 当命令可执行状态改变时触发
        /// </summary>
        event EventHandler CanExecuteChanged;

        /// <summary>
        /// 判断命令当前是否可执行
        /// </summary>
        bool CanExecute(object parameter);

        /// <summary>
        /// 执行命令
        /// </summary>
        void Execute(object parameter);
    }

    /// <summary>
    /// 通用命令实现
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        /// <summary>
        /// 触发CanExecuteChanged事件，通知UI更新状态
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 带参数的通用命令实现
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public event EventHandler CanExecuteChanged;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null) return true;

            if (parameter == null && typeof(T).IsValueType)
                return _canExecute(default(T));

            if (parameter is T t)
                return _canExecute(t);

            return false;
        }

        public void Execute(object parameter)
        {
            if (parameter is T t)
                _execute(t);
            else if (parameter == null && !typeof(T).IsValueType)
                _execute(default(T));
        }

        /// <summary>
        /// 触发CanExecuteChanged事件，通知UI更新状态
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
