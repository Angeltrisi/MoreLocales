using Terraria.ID;

namespace MoreLocales.Core
{
    public static class MoreLocalesSets
    {
        public static readonly GenderPluralization[] CachedGenderPluralization = ItemID.Sets.Factory.CreateCustomSet(GenderPluralization.Default);
        internal static void ReloadedLocalizations()
        {
            for (int i = 0; i < CachedGenderPluralization.Length; i++)
            {
                CachedGenderPluralization[i] = FeaturesPlus.GetItemGenderPluralization(i);
            }
        }
    }
}
