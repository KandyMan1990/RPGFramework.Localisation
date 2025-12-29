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

            return ReadManifest(bytes);
        }

        private static ManifestData ReadManifest(byte[] bytes)
        {
            using MemoryStream stream = new MemoryStream(bytes);
            using BinaryReader reader = new BinaryReader(stream);

            byte[] magic = reader.ReadBytes(Constants.LocManMagic.Length);
            if (!magic.SequenceEqual(Constants.LocManMagic))
            {
                throw new InvalidDataException($"{nameof(ManifestProvider)}::{nameof(ReadManifest)} Invalid locman magic");
            }

            byte version = reader.ReadByte();

            return version switch
                   {
                           1 => ReadV1(reader),
                           _ => throw new VersionNotFoundException($"{nameof(ManifestProvider)}::{nameof(ReadManifest)} version [{version}] is not registered")
                   };
        }

        private static ManifestData ReadV1(BinaryReader reader)
        {
            const byte version = 1;

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

            return new ManifestData(version, languages);
        }
    }
}