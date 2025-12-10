using System;
using System.IO;
using System.Linq;
using System.Text;
using RPGFramework.Localisation.LocBinLoader;

namespace RPGFramework.Localisation
{
    internal class LocData : IDisposable
    {
        public ulong[] Hashes      { get; private set; }
        public int[]   Offsets     { get; private set; }
        public byte[]  StringTable { get; private set; }

        internal LocData(ulong[] hashes, int[] offsets, byte[] stringTable)
        {
            Hashes      = hashes;
            Offsets     = offsets;
            StringTable = stringTable;
        }

        public static LocData FromBytes(byte[] bytes, string language, string neutralLanguage)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader br = new BinaryReader(ms))
            {
                byte[] magic = br.ReadBytes(4);

                if (!magic.SequenceEqual(Constants.MAGIC))
                {
                    throw new InvalidDataException("Invalid locbin magic");
                }

                byte version       = br.ReadByte();
                byte cultureLength = br.ReadByte();

                string fileCulture = Encoding.UTF8.GetString(br.ReadBytes(cultureLength));

                if (fileCulture != language && fileCulture != neutralLanguage)
                {
                    throw new InvalidDataException($"Invalid locbin language, expected [{language}] or [{neutralLanguage}] but file is [{fileCulture}]");
                }

                ILocBinLoader locBinLoader = LocBinLoaderProvider.GetLocBinLoader(version);

                return locBinLoader.Load(br);
            }
        }

        public void Dispose()
        {
            Hashes      = null;
            Offsets     = null;
            StringTable = null;
        }
    }
}