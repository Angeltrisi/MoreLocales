using static Terraria.Localization.GameCulture;

namespace MoreLocales.Core
{
    /// <summary>
    /// The new added cultures. Enums can be freely cast into other enums without any errors. The enum underneath will keep the value.
    /// </summary>
    public enum CultureNamePlus
    {
        BritishEnglish = 10,
        Japanese,
        Korean,
        TraditionalChinese,
        Turkish,
        Thai,
        Ukrainian,
        MexicanSpanish,
        Czech,
        Hungarian,
        PortugalPortuguese,
        Swedish,
        Dutch,
        Danish,
        Vietnamese, // omg is this a mirrorman reference
        Finnish,
        Romanian,
        Indonesian,
        Unknown = 9999,
    }
    /// <summary>
    /// List of fonts that are needed to support different languages, especially Asian languages.
    /// </summary>
    public enum LocalizedFont
    {
        /// <summary>
        /// Does not change the font. Additionally, sets <see cref="FontHelper.forcedFont"/> to false.
        /// </summary>
        None,
        Default,
        Japanese,
        Korean,
    }
    /// <summary>
    /// Defines a 'pluralization style' for text formatting.
    /// </summary>
    public enum PluralizationStyle
    {
        /// <summary>
        /// Like zh-Hans.
        /// </summary>
        None = CultureName.Chinese,
        /// <summary>
        /// Like en-US, de-DE, it-IT, es-ES, pt-BR.
        /// </summary>
        Simple = CultureName.English,
        /// <summary>
        /// Like fr-FR.
        /// </summary>
        SimpleWithSingularZero = CultureName.French,
        /// <summary>
        /// Like ru-RU.
        /// </summary>
        RussianThreeway = CultureName.Russian,
        /// <summary>
        /// Like pl-PL.
        /// </summary>
        PolishThreeway = CultureName.Polish,
        /// <summary>
        /// Needs special pluralization rule. Defined in <see cref="MoreLocalesCulture.CustomPluralizationRule"/> in <see cref="CultureHelper.CustomPluralization(int, int, int, int)"/>.
        /// </summary>
        Custom = 10,
    }
    public enum AdjectiveOrderType
    {
        Before,
        After,
    }
}
