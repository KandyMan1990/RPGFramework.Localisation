using System.IO;
using UnityEngine;

namespace RPGFramework.Localisation.Editor
{
    internal static class Constants
    {
        private const byte MAGIC_0 = (byte)'L';
        private const byte MAGIC_1 = (byte)'O';
        private const byte MAGIC_2 = (byte)'C';
        private const byte MAGIC_3 = (byte)'B';
        private const byte MAGIC_4 = (byte)'M';

        internal static readonly byte[] LocBinMagic =
        {
                MAGIC_0,
                MAGIC_1,
                MAGIC_2,
                MAGIC_3
        };
        
        internal static readonly byte[] LocManMagic =
        {
                MAGIC_0,
                MAGIC_1,
                MAGIC_2,
                MAGIC_4
        };
        
        internal static string BasePath => Path.Combine(Application.streamingAssetsPath, "Localisation");
    }
}