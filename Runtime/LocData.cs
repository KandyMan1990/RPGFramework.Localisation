using System;
using System.IO;
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

        public static LocData FromBytes(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader br = new BinaryReader(ms))
            {
                byte[] magic = br.ReadBytes(4);

                if (Constants.MAGIC != Encoding.UTF8.GetString(magic))
                {
                    throw new InvalidDataException("Invalid locbin magic");
                }

                int version = br.ReadInt32();

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