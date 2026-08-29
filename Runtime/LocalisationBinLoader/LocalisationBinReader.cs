using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using RPGFramework.Localisation.Data;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal static class LocalisationBinReader
    {
        private const int ENTRY_SIZE = sizeof(ulong) + sizeof(int);

        internal static void ValidateHeader(BinaryReader reader, string language, string neutralLanguage, byte fileVersion)
        {
            byte[] magic = reader.ReadBytes(Constants.LocBinMagic.Length);

            if (!magic.SequenceEqual(Constants.LocBinMagic))
            {
                throw new InvalidDataException($"{nameof(LocalisationBinReader)}::{nameof(ValidateHeader)} Invalid locbin magic");
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
            Stream stream = binaryReader.BaseStream;

            if (startPosition < 0 || length < 0 || startPosition + (long)length > stream.Length)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinReader)}::{nameof(ReadLocalisationData)} Sheet range [{startPosition}..{startPosition + (long)length}] lies outside the {stream.Length} byte file");
            }

            stream.Position = startPosition;

            uint count       = binaryReader.ReadUInt32();
            long headerBytes = stream.Position - startPosition;
            long maxEntries  = (length - headerBytes) / ENTRY_SIZE;

            if (count > maxEntries)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinReader)}::{nameof(ReadLocalisationData)} Entry count [{count}] exceeds the [{maxEntries}] the sheet's {length} bytes can hold");
            }

            ulong[] hashes  = new ulong[count];
            int[]   offsets = new int[count];

            for (int i = 0; i < count; i++)
            {
                hashes[i]  = binaryReader.ReadUInt64();
                offsets[i] = binaryReader.ReadInt32();
            }

            int remaining = (int)(length - (stream.Position - startPosition));

            if (remaining < 0)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinReader)}::{nameof(ReadLocalisationData)} Sheet index overran its declared {length} byte extent");
            }

            byte[] table = binaryReader.ReadBytes(remaining);

            if (table.Length != remaining)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinReader)}::{nameof(ReadLocalisationData)} String table truncated, expected [{remaining}] bytes but read [{table.Length}]");
            }

            return new LocalisationData(hashes, offsets, table);
        }
    }
}