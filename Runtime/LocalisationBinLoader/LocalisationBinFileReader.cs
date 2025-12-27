using System.IO;
using System.Linq;
using System.Text;
using RPGFramework.Localisation.Data;

namespace RPGFramework.Localisation.LocalisationBinLoader
{
    internal static class LocalisationBinFileReader
    {
        internal static LocalisationData ReadLocBin(byte[] bytes, string language, string neutralLanguage)
        {
            using MemoryStream stream = new MemoryStream(bytes);
            using BinaryReader reader = new BinaryReader(stream);

            byte[] magic = reader.ReadBytes(Constants.LocBinMagic.Length);
            if (!magic.SequenceEqual(Constants.LocBinMagic))
            {
                throw new InvalidDataException("Invalid locbin magic");
            }

            byte version       = reader.ReadByte();
            byte cultureLength = reader.ReadByte();

            string fileCulture = Encoding.UTF8.GetString(reader.ReadBytes(cultureLength));

            if (fileCulture != language && fileCulture != neutralLanguage)
            {
                throw new InvalidDataException($"{nameof(LocalisationBinFileReader)}::{nameof(ReadLocBin)} Invalid locbin language, expected [{language}] or [{neutralLanguage}] but file is [{fileCulture}]");
            }

            ILocalisationBinLoader localisationBinLoader = LocalisationBinLoaderProvider.GetLocalisationBinLoader(version);

            return localisationBinLoader.LoadLocBin(reader);
        }

        internal static string[] ReadLocMan(byte[] bytes)
        {
            using MemoryStream stream = new MemoryStream(bytes);
            using BinaryReader reader = new BinaryReader(stream);

            byte[] magic = reader.ReadBytes(Constants.LocManMagic.Length);
            if (!magic.SequenceEqual(Constants.LocManMagic))
            {
                throw new InvalidDataException("Invalid locman magic");
            }

            byte version = reader.ReadByte();

            ILocalisationBinLoader localisationBinLoader = LocalisationBinLoaderProvider.GetLocalisationBinLoader(version);

            return localisationBinLoader.LoadLocMan(reader);
        }
    }
}