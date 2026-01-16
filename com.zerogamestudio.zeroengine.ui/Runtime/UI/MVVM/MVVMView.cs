using UnityEngine;

namespace ZeroEngine.UI.MVVM
{
    public abstract class MVVMView<TViewModel> : MonoBehaviour where TViewModel : ViewModelBase, new()
    {
        protected TViewModel ViewModel;
        protected BindingContext BindingContext;

        protected virtual void Awake()
        {
            ViewModel = new TViewModel();
            BindingContext = new BindingContext();
            OnInitialize();
        }

        protected virtual void Start()
        {
            OnBindViewModels();
            ViewModel.Initialize();
        }

        protected virtual void OnDestroy()
        {
            BindingContext.Dispose();
            ViewModel.Dispose();
        }

        protected abstract void OnBindViewModels();
        
        protected virtual void OnInitialize() { }
    }
}
