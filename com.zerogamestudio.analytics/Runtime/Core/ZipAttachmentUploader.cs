using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace ZGS.Analytics
{
    /// <summary>
    /// 通用 ZIP 附件上传器 - SDK 内置实现
    /// 支持指数退避重试和离线队列
    /// </summary>
    public class ZipAttachmentUploader : IAttachmentUploader
    {
        private static bool _hasUploaded;

        public IEnumerator Upload(AttachmentUploadRequest request)
        {
            if (!AnalyticsConfig.IsConfigured)
            {
                AnalyticsLog.LogWarning("[ZipAttachmentUploader] 未配置服务器 URL 或 Secret，跳过上传");
                yield break;
            }

            if (_hasUploaded)
            {
                AnalyticsLog.LogWarning("[ZipAttachmentUploader] 已上传过反馈，本次忽略");
                yield break;
            }
#if !UNITY_EDITOR
            _hasUploaded = true;
#endif

            string safeUserName = SanitizeFileName(request.UserName ?? "Unknown");
            string version = $"{Application.productName}_v{Application.version}".Replace(" ", "");
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string prefix = $"{version}_{safeUserName}_{timestamp}";
            string prefixDir = prefix + "/";
            string zipName = prefix + ".zip";

            // 使用持久化目录存储 ZIP（不会被系统清理）
            string feedbackDir = FeedbackUploadQueue.FeedbackDirectory;
            string zipPath = Path.Combine(feedbackDir, zipName);
            if (File.Exists(zipPath)) File.Delete(zipPath);

            // 临时目录用于处理截图等中间文件
            string tmpDir = Application.temporaryCachePath;

            // 处理截图
            List<string> compressedShots = new();
            if (request.FilesToInclude != null)
            {
                for (int i = 0; i < request.FilesToInclude.Length; i++)
                {
                    string path = request.FilesToInclude[i];
                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;
                    string jpgPath = PrepareScreenshot(path, tmpDir, $"{prefix}_Screenshot{i + 1}");
                    compressedShots.Add(jpgPath);
                }
            }

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                // 打包目录
                if (request.DirectoriesToInclude != null)
                {
                    foreach (var dir in request.DirectoriesToInclude)
                    {
                        if (!Directory.Exists(dir)) continue;
                        string root = dir;
                        string logPath = Path.Combine(root, "Player.log");

                        // Player.log 复制后打包，避开文件锁
                        if (File.Exists(logPath))
                        {
                            string copy = Path.Combine(tmpDir, "Player_copy.log");
                            File.Copy(logPath, copy, true);
                            zip.CreateEntryFromFile(copy, prefixDir + "Player.log");
                        }

                        foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                        {
                            if (string.Equals(file, logPath, StringComparison.OrdinalIgnoreCase)) continue;
                            string rel = file.Substring(root.Length + 1).Replace('\\', '/');
                            // 使用 FileShare.ReadWrite 避免文件锁冲突
                            try
                            {
                                var entry = zip.CreateEntry(prefixDir + rel);
                                using var entryStream = entry.Open();
                                using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                                fileStream.CopyTo(entryStream);
                            }
                            catch (IOException ex)
                            {
                                AnalyticsLog.LogWarning($"[ZipAttachmentUploader] 跳过被占用的文件: {rel} - {ex.Message}");
                            }
                        }
                    }
                }

                // 反馈文本
                string fbPath = Path.Combine(tmpDir, "Feedback.txt");
                string feedbackContent = $"User: {request.UserName}\nMessage: {request.UserMessage}\n\n---TIMELINE---\n{request.TimelineJson}";
                File.WriteAllText(fbPath, feedbackContent);
                zip.CreateEntryFromFile(fbPath, prefixDir + "Feedback.txt");

                // 截图
                for (int i = 0; i < compressedShots.Count; i++)
                {
                    string jpg = compressedShots[i];
                    string nameInZip = $"Screenshot{i + 1}.jpg";
                    zip.CreateEntryFromFile(jpg, prefixDir + nameInZip);
                }
            }

            // 使用带重试的上传（失败后自动入队列）
            bool uploadSuccess = false;
            yield return FeedbackUploadQueue.UploadWithRetry(
                zipPath,
                version,
                safeUserName,
                success =>
                {
                    uploadSuccess = success;
                    if (success)
                    {
                        // 上传成功，删除 ZIP 文件
                        try
                        {
                            if (File.Exists(zipPath))
                                File.Delete(zipPath);
                        }
                        catch { }
                    }
                });

            AnalyticsLog.Log(uploadSuccess
                ? $"[ZipAttachmentUploader] 上传成功"
                : $"[ZipAttachmentUploader] 上传失败，已加入离线队列");
        }

        private static string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private const int MaxPixels = 1920 * 1080;
        private const int JpgQuality = 70;

        private static string PrepareScreenshot(string srcPath, string tmpDir, string prefix)
        {
            string outPath = Path.Combine(tmpDir, $"{prefix}.jpg");
            byte[] bytes = File.ReadAllBytes(srcPath);
            var texIn = new Texture2D(2, 2);
            texIn.LoadImage(bytes, false);

            var scaled = DownscaleIfNeeded(texIn);
            File.WriteAllBytes(outPath, scaled.EncodeToJPG(JpgQuality));
            Object.Destroy(texIn);
            if (scaled != texIn)
                Object.Destroy(scaled);

            return outPath;
        }

        private static Texture2D DownscaleIfNeeded(Texture2D src)
        {
            int pixels = src.width * src.height;
            if (pixels <= MaxPixels) return src;

            float ratio = Mathf.Sqrt(MaxPixels / (float)pixels);
            int w = Mathf.RoundToInt(src.width * ratio);
            int h = Mathf.RoundToInt(src.height * ratio);

            var dst = new Texture2D(w, h, TextureFormat.RGB24, false);
            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0);
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            dst.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return dst;
        }
    }
}
