using System.IO;
using System.Threading.Tasks;

namespace RPGFramework.Localisation.StreamingAssetLoader
{
    internal sealed class FileStreamingAssetLoader : IStreamingAssetLoader
    {
        async Task<byte[]> IStreamingAssetLoader.LoadAsync(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            return await File.ReadAllBytesAsync(path);
        }
    }

}