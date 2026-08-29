namespace RPGFramework.Localisation.StreamingAssetLoader
{
    internal static class StreamingAssetLoaderProvider
    {
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
        private static readonly IStreamingAssetLoader m_Loader = new WebStreamingAssetLoader();
#else
        private static readonly IStreamingAssetLoader m_Loader = new FileStreamingAssetLoader();
#endif

        internal static IStreamingAssetLoader Get()
        {
            return m_Loader;
        }
    }
}