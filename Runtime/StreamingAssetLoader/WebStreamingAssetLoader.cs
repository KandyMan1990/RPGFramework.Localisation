using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace RPGFramework.Localisation.StreamingAssetLoader
{
    internal sealed class WebStreamingAssetLoader : IStreamingAssetLoader
    {
        async Task<byte[]> IStreamingAssetLoader.LoadAsync(string path)
        {
            using UnityWebRequest req = UnityWebRequest.Get(path);

            await req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new IOException($"{nameof(WebStreamingAssetLoader)}::{nameof(IStreamingAssetLoader.LoadAsync)} Status [{req.result}] when requesting file at path [{path}]");
            }

            return req.downloadHandler.data;
        }
    }
}