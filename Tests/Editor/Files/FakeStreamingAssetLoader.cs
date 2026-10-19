using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RPGFramework.Localisation.Tests.Files
{
    /// <summary>StreamingAssets held in memory, read as the file loader reads them: a missing file is null.</summary>
    internal sealed class FakeStreamingAssetLoader : IStreamingAssetLoader
    {
        private readonly Dictionary<string, byte[]> m_Files;

        internal FakeStreamingAssetLoader()
        {
            m_Files = new Dictionary<string, byte[]>();
        }

        internal void Put(string path, byte[] bytes)
        {
            m_Files[path] = bytes;
        }

        internal void Remove(string path)
        {
            m_Files.Remove(path);
        }

        internal byte[] Get(string path)
        {
            byte[] bytes = m_Files[path];

            return bytes;
        }

        Task<byte[]> IStreamingAssetLoader.LoadAsync(string path)
        {
            m_Files.TryGetValue(path, out byte[] bytes);

            return Task.FromResult(bytes);
        }

        Task<byte[]> IStreamingAssetLoader.LoadRangeAsync(string path, long offset, int length)
        {
            if (!m_Files.TryGetValue(path, out byte[] bytes))
            {
                return Task.FromResult<byte[]>(null);
            }

            if (offset >= bytes.Length)
            {
                return Task.FromResult(Array.Empty<byte>());
            }

            int    count = (int)Math.Min(length, bytes.Length - offset);
            byte[] range = new byte[count];

            Array.Copy(bytes, offset, range, 0, count);

            return Task.FromResult(range);
        }
    }
}
