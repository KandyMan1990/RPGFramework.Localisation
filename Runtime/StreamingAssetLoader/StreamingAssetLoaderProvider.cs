namespace RPGFramework.Localisation.StreamingAssetLoader
{
    internal static class StreamingAssetLoaderProvider
    {
        internal static IStreamingAssetLoader Get()
        {
#if (UNITY_ANDROID || UNITY_WEBGL) && !UNITY_EDITOR
            return new WebStreamingAssetLoader();
#else
            return new FileStreamingAssetLoader();
#endif
        }
    }
}