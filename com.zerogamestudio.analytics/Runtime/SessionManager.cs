using UnityEngine;

namespace ZGS.Analytics
{
    /// <summary>
    /// Session 管理器 - 跟踪会话生命周期
    /// </summary>
    public class SessionManager
    {
        private static SessionManager _instance;
        public static SessionManager Instance => _instance ??= new SessionManager();
        
        private ZGSServerProvider _serverProvider;
        
        /// <summary>
        /// 开始会话
        /// </summary>
        public void StartSession(ZGSServerProvider provider)
        {
            _serverProvider = provider;
            
            // 注册应用退出事件
            Application.quitting += OnApplicationQuit;
            Application.focusChanged += OnFocusChanged;
        }
        
        /// <summary>
        /// 获取当前会话时长（秒）- 使用 SessionInfo.SessionDurationSeconds
        /// </summary>
        public int GetSessionDurationSeconds() => SessionInfo.SessionDurationSeconds;
        
        private void OnApplicationQuit()
        {
            Application.quitting -= OnApplicationQuit;
            Application.focusChanged -= OnFocusChanged;
            
            _serverProvider?.EndSession(GetSessionDurationSeconds());
        }
        
        private void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                _serverProvider?.Flush();
        }
    }
}
