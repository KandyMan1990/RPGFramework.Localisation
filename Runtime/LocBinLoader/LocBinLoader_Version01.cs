using System.IO;

namespace RPGFramework.Localisation.LocBinLoader
{
    internal class LocBinLoader_Version01 : ILocBinLoader
    {
        LocData ILocBinLoader.Load(BinaryReader binaryReader)
        {
            int     count   = binaryReader.ReadInt32();
            ulong[] hashes  = new ulong[count];
            int[]   offsets = new int[count];

            for (int i = 0; i < count; i++)
            {
                hashes[i]  = binaryReader.ReadUInt64();
                offsets[i] = binaryReader.ReadInt32();
            }

            int    remaining = (int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position);
            byte[] table     = binaryReader.ReadBytes(remaining);

            return new LocData(hashes, offsets, table);
        }
    }
}