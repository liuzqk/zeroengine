using System.Collections;

namespace ZGS.Analytics
{
    /// <summary>
    /// 附件上传器接口 - 业务项目实现具体的上传逻辑
    /// </summary>
    public interface IAttachmentUploader
    {
        /// <summary>
        /// 上传附件（截图、存档等）
        /// </summary>
        /// <param name="request">上传请求，包含附件路径和元数据</param>
        /// <returns>协程</returns>
        IEnumerator Upload(AttachmentUploadRequest request);
    }

    /// <summary>
    /// 附件上传请求
    /// </summary>
    public class AttachmentUploadRequest
    {
        /// <summary>用户反馈内容</summary>
        public string UserMessage;
        
        /// <summary>用户名</summary>
        public string UserName;
        
        /// <summary>要打包上传的目录（如 persistentDataPath）</summary>
        public string[] DirectoriesToInclude;
        
        /// <summary>要上传的文件路径（如截图）</summary>
        public string[] FilesToInclude;
        
        /// <summary>Timeline JSON</summary>
        public string TimelineJson;
        
        /// <summary>额外元数据</summary>
        public System.Collections.Generic.Dictionary<string, object> ExtraData;
    }
}
