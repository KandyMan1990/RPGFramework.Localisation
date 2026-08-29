using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGFramework.Localisation.StreamingAssetLoader
{
    internal sealed class WebStreamingAssetLoader : IStreamingAssetLoader
    {
        private const long HTTP_NOT_FOUND = 404L;

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

        private static bool IsLocalUri(string path)
        {
            return path.StartsWith("file://") || path.StartsWith("jar:file://");
        }
    }
}