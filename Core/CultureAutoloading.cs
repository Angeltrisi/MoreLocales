using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;

namespace MoreLocales.Core
{
    /// <summary>
    /// Represents an autoloaded culture.<para/>
    /// Works similarly to tModLoader's ModX classes, except this class' instance is not relevant to the user. It is simply used as a template to generate a <see cref="MoreLocalesCulture"/>.<para/>
    /// </summary>
    public abstract class ModCulture : ILoadable
    {
        private static readonly MethodInfo BaseContextCullerMethod = typeof(ModCulture).GetMethod(nameof(ContextChangesAdjective));
        private static readonly MethodInfo BaseAvailabilityMethod = typeof(ModCulture).GetMethod(nameof(IsAvailable));
        private static readonly MethodInfo BaseButtonPanelHijackMethod = typeof(ModCulture).GetMethod(nameof(PreDrawButtonPanel));
        private Mod _mod;
        public Mod Mod => _mod;
        /// <summary>
        /// The internal name of this. Used for localization generation.
        /// </summary>
        public virtual string Name => GetType().Name;
        /// <summary>
        /// The language code for this culture.<para/>
        /// Vanilla examples:<br/>
        /// <list type="bullet">
        /// <item>en-US</item>
        /// <item>de-DE</item>
        /// <item>it-IT</item>
        /// <item>zh-Hans</item>
        /// </list>
        /// </summary>
        public abstract string LanguageCode { get; }
        /// <summary>
        /// Allows you to set different parameters about this culture right before it is registered.
        /// </summary>
        /// <param name="fallbackCulture">
        /// The <see cref="GameCulture.LegacyId"/> of the culture this culture should fall back to in case keys for this culture aren't found.<br/>
        /// Examples: All vanilla cultures have this set to 1 (English), while Traditional Chinese in MoreLocales has this set to 7 (Simplified Chinese)<para/>
        /// Defaults to 1 (English).
        /// </param>
        /// <param name="hasSubtitle">
        /// Whether or not this culture should have a subtitle in the language menu.<br/>
        /// Typically, subtitles add information on the specific nature of the language. en-US's subtitle is 'United States' for example.<para/>
        /// If this is true, the mod will look for, or generate a subtitle key in <code>Mods.YourMod.Cultures.MyCulture.Subtitle</code><para/>
        /// Defaults to <see langword="true"/>.
        /// </param>
        /// <param name="hasDescription">
        /// Whether or not this culture should show some text (a description, for example) in the language menu when hovered.<para/>
        /// Defaults to <see langword="false"/>.
        /// </param>
        public virtual void SetCultureData(ref int fallbackCulture, ref bool hasSubtitle, ref bool hasDescription)
        {

        }
        /// <summary>
        /// Allows you to define some aspects of this culture's grammar right before it is registered, including pluralization style and the adjective order formatter.<para/>
        /// <b>Note:</b> If <paramref name="pluralizationStyle"/> is set to <see cref="PluralizationStyle.Custom"/>, you also need to override <see cref="CustomPluralizationRule(int, int, int)"/>
        /// </summary>
        public virtual void SetGrammarData(ref PluralizationStyle pluralizationStyle, ref AdjectiveOrder adjectiveOrder)
        {

        }
        /// <summary>
        /// Allows you to define a custom pluralization rule for this culture.<br/>
        /// This is necessary if you set this culture's pluralization style to <see cref="PluralizationStyle.Custom"/>.<para/>
        /// If this culture represents a real language, this rule should mirror the real one.<br/>
        /// You can find rules for different languages <a href="https://docs.translatehouse.org/projects/localization-guide/en/latest/l10n/pluralforms.html">here.</a>
        /// </summary>
        /// <param name="count">The amount of <i>something</i>.</param>
        /// <param name="mod10"><paramref name="count"/> % 10</param>
        /// <param name="mod100"><paramref name="mod100"/> % 100</param>
        /// <returns></returns>
        public virtual int CustomPluralizationRule(int count, int mod10, int mod100)
        {
            return 0;
        }
        /// <summary>
        /// <b>Note:</b> Overriding this method is not necessary, but it might avoid the game making unnecessary calculations.<para/>
        /// Called early on when attempting to inflect a prefix. If this returns false, the regular form of the prefix will be used.
        /// </summary>
        /// <returns>Whether or not the given data can change an adjective's form in this culture.</returns>
        public virtual bool ContextChangesAdjective(GrammaticalGender gender, Pluralization pluralization)
        {
            return true;
        }
        /// <summary>
        /// Whether or not this culture should show up on the language menu.
        /// </summary>
        public virtual bool IsAvailable()
        {
            return true;
        }
        /// <summary>
        /// Allows you to set different parameters for drawing this culture's respective button in the language menu, right before it is registered.
        /// </summary>
        /// <param name="sheet">The texture that will be used to draw this button.</param>
        /// <param name="sheetFrameCount">The amount of vertical frames in the texture.</param>
        /// <param name="sheetFrame">The frame that this specific language button will use.</param>
        public virtual void SetButtonDrawData(ref Asset<Texture2D> sheet, ref int? sheetFrameCount, ref int? sheetFrame)
        {

        }
        /// <inheritdoc cref="ButtonPanelDraw"/>
        public virtual bool? PreDrawButtonPanel(ref DrawData drawData)
        {
            return true;
        }
        /// <inheritdoc cref="ILoadable.Load"/>
        public virtual void Load()
        {

        }
        /// <inheritdoc cref="ILoadable.Unload"/>
        public virtual void Unload()
        {

        }
        /// <summary>
        /// Registers this <see cref="ModCulture"/> instance.<br/>
        /// If this instance is already registered, it does nothing.
        /// </summary>
        /// <param name="mod">The mod to register it under.</param>
        public void Register(Mod mod = null)
        {
            Type t = GetType();

            MoreLocalesAPI._autoloadedCulturesRegistry ??= [];
            if (MoreLocalesAPI._autoloadedCulturesRegistry.ContainsKey(t))
                return;

            int fallbackCulture = 1;
            bool hasSubtitle = true;
            bool hasDescription = false;

            SetCultureData(ref fallbackCulture, ref hasSubtitle, ref hasDescription);

            PluralizationStyle pluralization = PluralizationStyle.Simple;
            AdjectiveOrder orderFormatter = new();

            SetGrammarData(ref pluralization, ref orderFormatter);

            bool hasCustomPluralizationRule = pluralization == PluralizationStyle.Custom;
            // checking for overrides is easy,
            // but checking for meaningful overrides requires reflection of both the base method and the implementation by the inheriting class.
            // TileLoader does this all the time to create performant global hooks.
            bool hasCustomContextCuller = t.GetMethod(nameof(ContextChangesAdjective)) != BaseContextCullerMethod;
            bool hasCustomAvailabilityMethod = t.GetMethod(nameof(IsAvailable)) != BaseAvailabilityMethod;
            bool hasCustomButtonPanelDrawMethod = t.GetMethod(nameof(PreDrawButtonPanel)) != BaseButtonPanelHijackMethod;

            GrammarData data = new(pluralization, hasCustomPluralizationRule ? CustomPluralizationRule : null, orderFormatter, hasCustomContextCuller ? ContextChangesAdjective : null);

            Asset<Texture2D> sheet = null;
            int? sheetFrameCount = null;
            int? sheetFrame = null;

            SetButtonDrawData(ref sheet, ref sheetFrameCount, ref sheetFrame);
            LanguageButtonDrawData drawData = new(sheet, sheetFrameCount, sheetFrame, hasCustomButtonPanelDrawMethod ? PreDrawButtonPanel : null);

            int index = mod
                .RegisterCulture
                (Name, LanguageCode, fallbackCulture, hasSubtitle, hasDescription, data, hasCustomAvailabilityMethod ? IsAvailable : null, drawData)
                .Culture
                .LegacyId;

            MoreLocalesAPI._autoloadedCulturesRegistry.Add(t, index);
        }
        void ILoadable.Load(Mod mod)
        {
            _mod = mod;
            Register(mod);
            Load();
        }
        void ILoadable.Unload()
        {
            Unload();
        }
        #region Sealed
        public sealed override bool Equals(object obj)
        {
            if (obj is ModCulture culture)
                return culture.LanguageCode == LanguageCode;
            return false;
        }
        public sealed override int GetHashCode()
        {
            return LanguageCode.GetHashCode();
        }
        public sealed override string ToString()
        {
            return LanguageCode;
        }
        #endregion
    }
}
