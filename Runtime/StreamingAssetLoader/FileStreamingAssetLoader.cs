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
                throw new FileNotFoundException($"{nameof(FileStreamingAssetLoader)}::{nameof(IStreamingAssetLoader.LoadAsync)} File not found at path [{path}]");
            }

            return await File.ReadAllBytesAsync(path);
        }
    }

}