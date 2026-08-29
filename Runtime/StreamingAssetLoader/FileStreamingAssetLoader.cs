using System;
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

            byte[] bytes = await File.ReadAllBytesAsync(path);

            return bytes;
        }

        async Task<byte[]> IStreamingAssetLoader.LoadRangeAsync(string path, long offset, int length)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            if (offset < 0 || length < 0)
            {
                throw new ArgumentOutOfRangeException($"{nameof(FileStreamingAssetLoader)}::{nameof(IStreamingAssetLoader.LoadRangeAsync)} Offset [{offset}] and length [{length}] must not be negative");
            }

            await using FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            if (offset >= stream.Length)
            {
                return Array.Empty<byte>();
            }

            int    toRead = (int)Math.Min(length, stream.Length - offset);
            byte[] buffer = new byte[toRead];

            stream.Seek(offset, SeekOrigin.Begin);

            int read = 0;

            while (read < toRead)
            {
                int count = await stream.ReadAsync(buffer, read, toRead - read);

                if (count == 0)
                {
                    break;
                }

                read += count;
            }

            if (read == toRead)
            {
                return buffer;
            }

            byte[] truncated = new byte[read];
            Array.Copy(buffer, truncated, read);

            return truncated;
        }
    }
}