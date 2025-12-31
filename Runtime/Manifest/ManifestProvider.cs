using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RPGFramework.Localisation.Helpers;
using RPGFramework.Localisation.StreamingAssetLoader;

namespace RPGFramework.Localisation.Manifest
{
    internal static class ManifestProvider
    {
        private const string MANIFEST_FILE = "manifest.locman";

        internal static async Task<ManifestData> GetManifestAsync()
        {
            IStreamingAssetLoader assetLoader = StreamingAssetLoaderProvider.Get();

            string path = HelperFunctions.CombinePath(Constants.BasePath, MANIFEST_FILE);

            byte[] bytes = await assetLoader.LoadAsync(path);

            if (bytes == null || bytes.Length == 0)
            {
                throw new FileNotFoundException($"{nameof(ManifestProvider)}::{nameof(GetManifestAsync)} Manifest not found");
            }

            using MemoryStream stream = new MemoryStream(bytes);
            using BinaryReader reader = new BinaryReader(stream);

            byte localisationVersion = ReadManifestHeader(reader);

            return ReadManifestBody(reader, localisationVersion);
        }

        private static byte ReadManifestHeader(BinaryReader reader)
        {
            byte[] magic = reader.ReadBytes(Constants.LocManMagic.Length);
            if (!magic.SequenceEqual(Constants.LocManMagic))
            {
                throw new InvalidDataException($"{nameof(ManifestProvider)}::{nameof(ReadManifestHeader)} Invalid locman magic");
            }

            byte version = reader.ReadByte();

            return version;
        }

        private static ManifestData ReadManifestBody(BinaryReader reader, byte localisationVersion)
        {
            int    remaining = (int)(reader.BaseStream.Length - reader.BaseStream.Position);
            byte[] data      = reader.ReadBytes(remaining);

            List<string> results = new List<string>();
            int          start   = 0;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0)
                {
                    int length = i - start;
                    if (length > 0)
                    {
                        string str = Encoding.UTF8.GetString(data, start, length);
                        results.Add(str);
                    }

                    start = i + 1;
                }
            }

            string[] languages = results.ToArray();

            return new ManifestData(localisationVersion, languages);
        }
    }
}