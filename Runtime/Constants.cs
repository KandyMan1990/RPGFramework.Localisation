namespace RPGFramework.Localisation
{
    public static class Constants
    {
        private const byte MAGIC_0 = (byte)'L';
        private const byte MAGIC_1 = (byte)'O';
        private const byte MAGIC_2 = (byte)'C';
        private const byte MAGIC_3 = (byte)'B';

        public static readonly byte[] MAGIC =
        {
                MAGIC_0,
                MAGIC_1,
                MAGIC_2,
                MAGIC_3
        };
    }
}