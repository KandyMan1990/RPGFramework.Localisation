using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using RPGFramework.Localisation.Data;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal static class LocalisationBinReader
    {
        internal static void ValidateHeader(BinaryReader reader, string language, string neutralLanguage, byte fileVersion)
        {
            byte[] magic = reader.ReadBytes(Constants.LocBinMagic.Length);
            if (!magic.SequenceEqual(Constants.LocBinMagic))
            {
                throw new InvalidDataException("Invalid locbin magic");
            }

            byte version = reader.ReadByte();
            if (version != fileVersion)
            {
                throw new VersionNotFoundException($"{nameof(LocalisationBinReader)}::{nameof(ValidateHeader)} Expected version [{fileVersion}] but locbin version is [{version}]");
            }

            byte   cultureLength = reader.ReadByte();
            string fileCulture   = Encoding.UTF8.GetString(reader.ReadBytes(cultureLength));

            if (fileCulture != language && fileCulture != neutralLanguage)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinReader)}::{nameof(ValidateHeader)} Invalid locbin language, expected [{language}] or [{neutralLanguage}] but file is [{fileCulture}]");
            }
        }

        internal static LocalisationData ReadLocalisationData(BinaryReader binaryReader, int startPosition, int length)
        {
            binaryReader.BaseStream.Position = startPosition;

            uint count = binaryReader.ReadUInt32();

            ulong[] hashes  = new ulong[count];
            int[]   offsets = new int[count];

            for (int i = 0; i < count; i++)
            {
                hashes[i]  = binaryReader.ReadUInt64();
                offsets[i] = binaryReader.ReadInt32();
            }

            int    remaining = (int)(length - (binaryReader.BaseStream.Position - startPosition));
            byte[] table     = binaryReader.ReadBytes(remaining);

            return new LocalisationData(hashes, offsets, table);
        }
    }
}