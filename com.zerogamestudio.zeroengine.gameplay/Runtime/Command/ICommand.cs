#if UNITASK
using Cysharp.Threading.Tasks;
#else
using System.Threading.Tasks;
#endif

namespace ZeroEngine.Command
{
    public interface ICommand
    {
#if UNITASK
        UniTask Execute();
        UniTask Undo();
#else
        Task Execute();
        Task Undo();
#endif
    }
}
