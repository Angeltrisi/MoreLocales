using Terraria.ID;

namespace MoreLocales.Core
{
    public static class MoreLocalesSets
    {
        internal static bool _contentReady = false;
        public static readonly InflectionData[] CachedInflectionData = ItemID.Sets.Factory.CreateCustomSet(InflectionData.Default);
        internal static void ReloadedLocalizations()
        {
            // AddComment actually causes files to be reloaded so i need to take that into account
            if (!_contentReady || LangUtils.FilesWillBeReloadedDueToCommentsChange)
                return;
            for (int i = 0; i < CachedInflectionData.Length; i++)
            {
                CachedInflectionData[i] = LangFeaturesPlus.GetItemInflection(i);
            }
        }
    }
}
