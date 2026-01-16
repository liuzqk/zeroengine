using System.Collections.Generic;
using UnityEngine;
using ZeroEngine.Core;

#if UNITASK
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace ZeroEngine.Command
{
    public class CommandManager : Singleton<CommandManager>
    {
        private Stack<ICommand> _history = new Stack<ICommand>();
        private Stack<ICommand> _redoHistory = new Stack<ICommand>();
        
        public bool IsExecuting { get; private set; }

#if UNITASK
        public async UniTask ExecuteCommand(ICommand command)
#else
        public async Task ExecuteCommand(ICommand command)
#endif
        {
            if (command == null) return;
            
            // Clear redo history on new command
            _redoHistory.Clear();
            
            IsExecuting = true;
            try
            {
                await command.Execute();
                _history.Push(command);
            }
            finally
            {
                IsExecuting = false;
            }
        }

#if UNITASK
        public async UniTask Undo()
#else
        public async Task Undo()
#endif
        {
            if (_history.Count == 0) return;
            if (IsExecuting) return;

            var cmd = _history.Pop();
            IsExecuting = true;
            try
            {
                await cmd.Undo();
                _redoHistory.Push(cmd);
            }
            finally
            {
                IsExecuting = false;
            }
        }
        
#if UNITASK
        public async UniTask Redo()
#else
        public async Task Redo()
#endif
        {
            if (_redoHistory.Count == 0) return;
            if (IsExecuting) return;

            var cmd = _redoHistory.Pop();
            IsExecuting = true;
            try
            {
                await cmd.Execute();
                _history.Push(cmd);
            }
            finally
            {
                IsExecuting = false;
            }
        }
        
        public void ClearHistory()
        {
            _history.Clear();
            _redoHistory.Clear();
        }
    }
}
