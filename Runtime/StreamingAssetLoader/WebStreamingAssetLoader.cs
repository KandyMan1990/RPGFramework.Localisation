using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGFramework.Localisation.StreamingAssetLoader
{
    internal sealed class WebStreamingAssetLoader : IStreamingAssetLoader
    {
        private const long HTTP_NOT_FOUND = 404L;

        private string m_CachedPath;
        private byte[] m_CachedBytes;

        async Task<byte[]> IStreamingAssetLoader.LoadAsync(string path)
        {
            using UnityWebRequest req = UnityWebRequest.Get(path);

            await req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                return req.downloadHandler.data;
            }

            bool notFound = req.result == UnityWebRequest.Result.ProtocolError   && req.responseCode == HTTP_NOT_FOUND ||
                            req.result == UnityWebRequest.Result.ConnectionError && req.responseCode == 0L && IsLocalUri(path);

            if (notFound)
            {
                return null;
            }

            throw new IOException($"{nameof(WebStreamingAssetLoader)}::{nameof(IStreamingAssetLoader.LoadAsync)} Status [{req.result}] code [{req.responseCode}] error [{req.error}] when requesting file at path [{path}]");
        }

        /// <summary>
        /// Range requests are not usable here: StreamingAssets on Android lives inside the APK behind a
        /// <c>jar:file://</c> URI, which does not honour a Range header. The file is fetched once and cached
        /// instead, so repeated range reads still cost a single fetch rather than one per call.
        /// </summary>
        async Task<byte[]> IStreamingAssetLoader.LoadRangeAsync(string path, long offset, int length)
        {
            if (offset < 0 || length < 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(WebStreamingAssetLoader)}::{nameof(IStreamingAssetLoader.LoadRangeAsync)} Offset [{offset}] and length [{length}] must not be negative");
            }

            if (!string.Equals(m_CachedPath, path, StringComparison.Ordinal))
            {
                byte[] fetched = await ((IStreamingAssetLoader)this).LoadAsync(path);

                m_CachedPath  = path;
                m_CachedBytes = fetched;
            }

            if (m_CachedBytes == null)
            {
                return null;
            }

            if (offset >= m_CachedBytes.Length)
            {
                return Array.Empty<byte>();
            }

            int    toRead = (int)Math.Min(length, m_CachedBytes.Length - offset);
            byte[] buffer = new byte[toRead];

            Array.Copy(m_CachedBytes, offset, buffer, 0, toRead);

            return buffer;
        }

        private static bool IsLocalUri(string path)
        {
            return path.StartsWith("file://") || path.StartsWith("jar:file://");
        }
    }
}