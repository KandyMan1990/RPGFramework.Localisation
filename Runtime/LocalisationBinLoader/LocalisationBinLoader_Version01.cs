using System.Collections.Generic;
using System.IO;
using System.Text;
using RPGFramework.Localisation.Data;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal sealed class LocalisationBinLoader_Version01 : ILocalisationBinLoader
    {
        LocalisationData ILocalisationBinLoader.LoadLocBin(BinaryReader binaryReader)
        {
            int count = binaryReader.ReadInt32();

            ulong[] hashes  = new ulong[count];
            int[]   offsets = new int[count];

            for (int i = 0; i < count; i++)
            {
                hashes[i]  = binaryReader.ReadUInt64();
                offsets[i] = binaryReader.ReadInt32();
            }

            int    remaining = (int)(binaryReader.BaseStream.Length - binaryReader.BaseStream.Position);
            byte[] table     = binaryReader.ReadBytes(remaining);

            return new LocalisationData(hashes, offsets, table);
        }

        string[] ILocalisationBinLoader.LoadLocMan(BinaryReader reader)
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

            return results.ToArray();
        }
    }
}