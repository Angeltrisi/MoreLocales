using System;
using Terraria.Localization;
using static MoreLocales.Core.CultureNamePlus;
using Terraria;
using System.Runtime.CompilerServices;

namespace MoreLocales.Utilities
{
    public static class CultureHelper
    {
        public static bool CustomCultureActive(CultureNamePlus customCulture) => LanguageManager.Instance.ActiveCulture.LegacyId == (int)customCulture;
        public static string FullName(this GameCulture culture) => ExtraLocalesSupport.extraCulturesV2[culture.LegacyId].Name; // culture.IsCustom() ? ((CultureNamePlus)culture.LegacyId).ToString() : ((CultureName)culture.LegacyId).ToString();
        public static bool IsCustom(this GameCulture culture) => !ExtraLocalesSupport.extraCulturesV2[culture.LegacyId].Vanilla;
        public static bool IsValid(int culture) => culture > 0 && culture < ExtraLocalesSupport.extraCulturesV2.Length;
        /// <summary>
        /// Maps a custom culture's ID to a vanilla culture with the same pluralization rule. Returns 10 for <see cref="PluralizationStyle.Custom"/>.
        /// </summary>
        /// <param name="realID"></param>
        /// <returns></returns>
        public static int MapLegacyIDToPluralizationID(int realID)
        {
            if (realID < (int)BritishEnglish)
                return realID;
            return (int)ExtraLocalesSupport.extraCulturesV2[realID].PluralizationRule;
        }
        public static int CustomPluralization(int c, int mod10, int mod100, int count)
        {
            return ExtraLocalesSupport.extraCulturesV2[c].CustomPluralizationRule(count, mod10, mod100);
        }
        #region Pluralization Rules
        public static int czechPlural(int count, int mod10, int mod100)
        {
            if (count == 1)
                return 0;
            else if (count >= 2 && count <= 4)
                return 1;
            return 2;
        }
        public static int turkishPlural(int count, int mod10, int mod100)
        {
            if (count > 1)
                return 1;
            return 0;
        }
        public static int romanianPlural(int count, int mod10, int mod100)
        {
            if (count == 1)
                return 0;
            else if (count == 0 || (mod100 > 0 && mod100 < 20))
                return 1;
            return 2;
        }
        #endregion
        /// <summary>
        /// Replicates the behavior of <see cref="Item.Name"/> before the effects of <see cref="FeaturesPlus.RemovePrefixLiteralFromName(ILContext)"/>.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string GetRealName(this Item i) => i._nameOverride ?? Lang.GetItemNameValue(i.type);
        public static ref MoreLocalesCulture RegisterCulture(this Mod mod,
            string internalName,
            string languageCode,
            int fallbackCulture = 1,
            bool hasSubtitle = true,
            bool hasDescription = false,
            PluralizationStyle pluralizationStyle = PluralizationStyle.Simple,
            Func<int, int, int, int> customPluralizationRule = null,
            AdjectiveOrder adjectiveOrder = default,
            Func<int, int, bool> contextChangesAdjective = null)
        =>
            ref ExtraLocalesSupport.RegisterCulture( internalName, languageCode, fallbackCulture, hasSubtitle, hasDescription,
                pluralizationStyle, customPluralizationRule, adjectiveOrder, contextChangesAdjective, mod);
    }
    /// <summary>
    /// A light text formatting structure for adjective-noun order.
    /// </summary>
    /// <param name="Type">Whether or not the adjective should go before or after the noun.</param>
    /// <param name="Connector">The string to insert between the adjective and the noun, if any.</param>
    public readonly record struct AdjectiveOrder(AdjectiveOrderType Type = AdjectiveOrderType.Before, string Connector = "")
    {
        private static readonly AdjectiveOrder _before = new();
        private static readonly AdjectiveOrder _after = new(AdjectiveOrderType.After);
        private static readonly AdjectiveOrder _beforeWithSpace = new(AdjectiveOrderType.Before, " ");
        private static readonly AdjectiveOrder _afterWithSpace = new(AdjectiveOrderType.After, " ");

        public static AdjectiveOrder Before => _before;
        public static AdjectiveOrder After => _after;
        public static AdjectiveOrder BeforeWithSpace => _beforeWithSpace;
        public static AdjectiveOrder AfterWithSpace => _afterWithSpace;

        public string Apply(string noun, string adjective)
        {
            if (Type == AdjectiveOrderType.Before)
                return $"{adjective}{Connector}{noun}";
            return $"{noun}{Connector}{adjective}";
        }
    }
}
