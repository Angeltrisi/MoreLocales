using MoreLocales.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Terraria;
using Terraria.Localization;
using static Terraria.Localization.GameCulture;
using static MoreLocales.Core.LangFeaturesPlus;
using static MoreLocales.Core.CultureNamePlus;
using static Terraria.Localization.GameCulture.CultureName;
using ReLogic.Content;
using Terraria.DataStructures;

namespace MoreLocales.Core
{
    #region Substructures
    /// <summary>
    /// Allows you to do stuff before drawing a certain culture's button.<para/>
    /// Return <see langword="true"/> to draw the button normally, using the <see cref="DrawData"/> provided.<br/>
    /// Return <see langword="null"/> to stop only the drawing of this button's panel.<br/>
    /// Return <see langword="false"/> to stop the drawing of this button entirely.<para/>
    /// The only fields the instance of <see cref="DrawData"/> already has set are <see cref="DrawData.texture"/>, <see cref="DrawData.position"/> and <see cref="DrawData.color"/> (Set to <see cref="Color.White"/>)<br/>
    /// Nothing that you change in this <see cref="DrawData"/> instance will affect other steps.
    /// </summary>
    public delegate bool? ButtonPanelDraw(ref DrawData drawData);
    /// <summary>
    /// A structure used to provide information about a certain <see cref="MoreLocalesCulture"/>'s grammar.<br/>
    /// Used mainly for advanced localization features like adjective form inflection and adjective ordering.<para/>
    /// <b>Note:</b> Setting the pluralization rule to <see cref="PluralizationStyle.Custom"/> requires you to also set <see cref="GrammarData.CustomPluralizationRule"/>
    /// </summary>
    /// <param name="pluralizationStyle">
    /// <inheritdoc cref="GrammarData.PluralizationRule"/>
    /// </param>
    /// <param name="customPluralizationRule">
    /// <inheritdoc cref="GrammarData.CustomPluralizationRule"/>
    /// </param>
    /// <param name="adjectiveOrder">
    /// <inheritdoc cref="GrammarData.AdjectiveOrder"/>
    /// </param>
    /// <param name="contextChangesAdjective">
    /// <inheritdoc cref="GrammarData.ContextChangesAdjective"/>
    /// </param>
    public readonly struct GrammarData(PluralizationStyle pluralizationStyle = PluralizationStyle.Simple, Func<int, int, int, int> customPluralizationRule = null,
        AdjectiveOrder adjectiveOrder = new(), Func<GrammaticalGender, Pluralization, bool> contextChangesAdjective = null)
    {
        /// <summary>
        /// The pluralization style that should be used for this <see cref="MoreLocalesCulture"/>.<para/>
        /// If the value of this is <see cref="PluralizationStyle.Custom"/>, setting the value of <see cref="CustomPluralizationRule"/> <b>is mandatory.</b>
        /// </summary>
        public readonly PluralizationStyle PluralizationRule = pluralizationStyle;
        /// <summary>
        /// The pluralization rule function for a <see cref="MoreLocalesCulture"/> with a <see cref="PluralizationRule"/> of value <see cref="PluralizationStyle.Custom"/>.<para/>
        /// This function should take in 'count, mod10, mod100' as parameters, and return the index of the final pluralization type.<br/>
        /// If your culture represents a language that already exists, refer to this list to learn how to write this function: <see href="https://docs.translatehouse.org/projects/localization-guide/en/latest/l10n/pluralforms.html"/>
        /// </summary>
        public readonly Func<int, int, int, int> CustomPluralizationRule = customPluralizationRule;
        /// <summary>
        /// The adjective-noun order formatter for this <see cref="MoreLocalesCulture"/>.
        /// </summary>
        public readonly AdjectiveOrder AdjectiveOrder = adjectiveOrder;
        /// <summary>
        /// Whether or not a pair of gender and pluralization data from a noun should change the form of the adjective attached to it. <br/>
        /// For example, in English, adjective form never changes, so you'd always return true. In Spanish, the adjective changes if the gender isn't masculine or if the noun is plural. <para/>
        /// This function should take in 'gender, pluralizationType' as parameters, and return the result.<br/>
        /// For more info on pluralization type, see <see cref="CustomPluralizationRule"/>.<para/>
        /// <b>Note:</b> Setting this is not necessary for a new culture to function. It simply allows for culling unnecessary calculations for slighly better performance.
        /// </summary>
        public readonly Func<GrammaticalGender, Pluralization, bool> ContextChangesAdjective = contextChangesAdjective;
        #region what's a sourcegen
        /// <summary>
        /// Makes a new <see cref="GrammarData"/> instance with only <see cref="GrammarData.ContextChangesAdjective"/> set.
        /// </summary>
        /// <param name="contextChangesAdjective">
        /// <inheritdoc cref="GrammarData.ContextChangesAdjective"/>
        /// </param>
        /// <returns></returns>
        public static GrammarData Context(Func<GrammaticalGender, Pluralization, bool> contextChangesAdjective) => new(contextChangesAdjective: contextChangesAdjective);
        /// <summary>
        /// Makes a new <see cref="GrammarData"/> instance with only <see cref="GrammarData.PluralizationRule"/> and <see cref="GrammarData.AdjectiveOrder"/> set.
        /// </summary>
        /// <param name="pluralizationStyle">
        /// <inheritdoc cref="GrammarData.PluralizationRule"/>
        /// </param>
        /// <param name="adjectiveOrder">
        /// <inheritdoc cref="GrammarData.AdjectiveOrder"/>
        /// </param>
        /// <returns></returns>
        public static GrammarData StyleOrder(PluralizationStyle pluralizationStyle, AdjectiveOrder adjectiveOrder)
            => new(pluralizationStyle: pluralizationStyle, adjectiveOrder: adjectiveOrder);
        /// <summary>
        /// Makes a new <see cref="GrammarData"/> instance with only <see cref="GrammarData.AdjectiveOrder"/> and <see cref="GrammarData.ContextChangesAdjective"/> set.
        /// </summary>
        /// <param name="adjectiveOrder">
        /// <inheritdoc cref="GrammarData.AdjectiveOrder"/>
        /// </param>
        /// <param name="contextChangesAdjective">
        /// <inheritdoc cref="GrammarData.ContextChangesAdjective"/>
        /// </param>
        /// <returns></returns>
        public static GrammarData OrderContext
            (AdjectiveOrder adjectiveOrder, Func<GrammaticalGender, Pluralization, bool> contextChangesAdjective)
            => new(adjectiveOrder: adjectiveOrder, contextChangesAdjective: contextChangesAdjective);
        /// <summary>
        /// Makes a new <see cref="GrammarData"/> instance with only <see cref="GrammarData.PluralizationRule"/> and <see cref="GrammarData.ContextChangesAdjective"/> set.
        /// </summary>
        /// <param name="pluralizationStyle">
        /// <inheritdoc cref="GrammarData.PluralizationRule"/>
        /// </param>
        /// <param name="contextChangesAdjective">
        /// <inheritdoc cref="GrammarData.ContextChangesAdjective"/>
        /// </param>
        /// <returns></returns>
        public static GrammarData StyleContext(PluralizationStyle pluralizationStyle, Func<GrammaticalGender, Pluralization, bool> contextChangesAdjective)
            => new(pluralizationStyle: pluralizationStyle, contextChangesAdjective: contextChangesAdjective);
        /// <summary>
        /// Makes a new <see cref="GrammarData"/> instance with <see cref="GrammarData.PluralizationRule"/>, <see cref="AdjectiveOrder"/> and <see cref="GrammarData.ContextChangesAdjective"/> set.
        /// </summary>
        /// <param name="pluralizationStyle">
        /// <inheritdoc cref="GrammarData.PluralizationRule"/>
        /// </param>
        /// <param name="adjectiveOrder">
        /// <inheritdoc cref="GrammarData.AdjectiveOrder"/>
        /// </param>
        /// <param name="contextChangesAdjective">
        /// <inheritdoc cref="GrammarData.ContextChangesAdjective"/>
        /// </param>
        /// <returns></returns>
        public static GrammarData StyleOrderContext
            (PluralizationStyle pluralizationStyle, AdjectiveOrder adjectiveOrder,
            Func<GrammaticalGender, Pluralization, bool> contextChangesAdjective) => new(pluralizationStyle, null, adjectiveOrder, contextChangesAdjective);
        #endregion
    }
    /// <summary>
    /// A structure used for control over the drawing of the language button for a certain culture.<para/>
    /// Can control basic stuff (using the basic fields) and also more advanced stuff at the different button draw steps (using the delegate fields).
    /// </summary>
    public struct LanguageButtonDrawData(Asset<Texture2D> sheet = null, int? sheetFrameCount = null,
        int? sheetFrame = null, ButtonPanelDraw hijackPanelDraw = null)
    {
        /// <summary>
        /// The sheet where the language symbol graphic will be taken from. If this field is left null, this will be Flags.png
        /// </summary>
        public Asset<Texture2D> Sheet = sheet;
        /// <summary>
        /// The amount of vertical frames in <see cref="Sheet"/>.<para/>
        /// Defaults to 28 if <see cref="Sheet"/> is also null, otherwise defaults to 1.
        /// </summary>
        public readonly int SheetFrameCount = sheetFrameCount.HasValue ? Math.Max(sheetFrameCount.Value, 1) : sheet is null ? BetterLangMenuV2.FlagsCount : 1;
        /// <summary>
        /// The index of the vertical frame that this language symbol should use. Defaults to 0.
        /// </summary>
        public readonly int SheetFrame = sheetFrame.HasValue ? Math.Max(sheetFrame.Value, 0) : 0;
        /// <summary>
        /// Allows you to do a range of things before drawing the main button panel. Leave null to not do anything.
        /// </summary>
        public readonly ButtonPanelDraw HijackPanelDraw = hijackPanelDraw;
    }
    #endregion
    /// <summary>
    /// A structure used to significantly extend the functionality of <see cref="GameCulture"/>.<br/>
    /// Cultures registered through <see cref="MoreLocales"/> will create localization keys inside the <see cref="MoreLocalesCulture.Mod"/>'s localization file.<br/>
    /// These keys are needed for correct display inside <see cref="MoreLocales"/>'s UI.
    /// </summary>
    public struct MoreLocalesCulture(GameCulture culture, string name, int fallback = 1,
        bool subtitle = false, bool description = false, GrammarData grammarData = new(),
        Func<bool> available = null, LanguageButtonDrawData buttonDrawData = new(), Mod mod = null)
    {
        /// <summary>
        /// The child culture of this <see cref="MoreLocalesCulture"/>.
        /// </summary>
        public readonly GameCulture Culture = culture;
        /// <summary>
        /// The internal name of this <see cref="MoreLocalesCulture"/>. Used for certain language info lookups.
        /// </summary>
        public readonly string Name = name;
        /// <summary>
        /// The fallback culture of this <see cref="MoreLocalesCulture"/>.<br/>
        /// If localizations for this culture aren't found, localizations from the fallback culture will be used instead.
        /// </summary>
        public readonly int FallbackCulture = fallback;
        /// <summary>
        /// Used for display in <see cref="BetterLangMenuUI"/>.<br/>
        /// If this is true for a custom culture, <see cref="MoreLocales"/> will search for (or create) a subtitle key using <see cref="Mod.GetLocalization(string, Func{string})"/> using the "Cultures.{Name}.Subtitle" suffix.
        /// </summary>
        public readonly bool HasSubtitle = subtitle;
        /// <summary>
        /// Used for hover text in <see cref="BetterLangMenuUI"/>.<br/>
        /// If this is true for a custom culture, the mod will search for (or create) a description key using <see cref="Mod.GetLocalization(string, Func{string})"/> using the "Cultures.{Name}.Description" suffix.
        /// </summary>
        public readonly bool HasDescription = description;
        public readonly GrammarData GrammarData = grammarData;
        /// <summary>
        /// Whether or not this culture should be visible on the language menu. Defaults to null (always available).
        /// </summary>
        public readonly Func<bool> Available = available;
        public LanguageButtonDrawData ButtonDrawData = buttonDrawData;
        /// <summary>
        /// The parent mod for this <see cref="MoreLocalesCulture"/>. Null if this represents a vanilla culture.
        /// </summary>
        public readonly Mod Mod = mod;
        /// <summary>
        /// Whether or not this <see cref="MoreLocalesCulture"/> was registered by an external source that is not Terraria nor <see cref="MoreLocales"/>.
        /// </summary>
        public readonly bool OtherCustom => Mod != null && Mod != MoreLocales.Instance;
        /// <summary>
        /// Whether or not this <see cref="MoreLocalesCulture"/> was registered as part of the set of languages defined by <see cref="MoreLocales"/> in <see cref="CultureNamePlus"/>.
        /// </summary>
        public readonly bool NativeCustom => Mod == MoreLocales.Instance;
        /// <summary>
        /// Whether or not this <see cref="MoreLocalesCulture"/> was registered as part of the set of languages defined by Terraria in <see cref="CultureName"/>.
        /// </summary>
        public readonly bool Vanilla => Mod is null;

        internal readonly string OwnerFunctionalName => Vanilla ? "MoreLocales" : Mod.Name;
    }

    /// <summary>
    /// <see href="https://bit.ly/4kPK4NQ"/>
    /// </summary>
    public readonly ref struct Ref<T>(ref T value)
    {
        public readonly ref T Value = ref value;
    }
    public static class MoreLocalesAPI
    {
        private const string customCultureDataName = "LocalizationPlusData.dat";
        private static int loadedCulture = 9999;
        internal static int cachedVanillaCulture = 1; // english by default
        internal static MoreLocalesCulture[] extraCulturesV2 = new MoreLocalesCulture[28]; // entry 0 is a dummy default entry
        private static int _registeredCount = 1; // starts at one because CultureName.English is 1
        internal static Dictionary<Type, int> _autoloadedCulturesRegistry;
        public static MoreLocalesCulture ActiveCulture => extraCulturesV2[LanguageManager.Instance.ActiveCulture.LegacyId];
        /// <summary>
        /// Returns a reference to the requested <see cref="MoreLocalesCulture"/> based on its <see cref="GameCulture.LegacyId"/>.
        /// </summary>
        public static ref MoreLocalesCulture GetCulture(int legacyID) => ref extraCulturesV2[legacyID];
        /// <summary>
        /// Returns a reference to the requested autoloaded <see cref="MoreLocalesCulture"/> based on its <see cref="Type"/>.
        /// </summary>
        public static ref MoreLocalesCulture GetCulture<T>() where T : ModCulture
        {
            return ref GetCulture(_autoloadedCulturesRegistry[typeof(T)]);
        }
        /// <summary>
        /// Attempts to get a reference to the requested autoloaded <see cref="MoreLocalesCulture"/> based on its <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="T">A type inheriting from <see cref="ModCulture"/></typeparam>
        /// <param name="culture">
        /// A <see cref="Ref{T}"/> containing a reference to the requested <see cref="MoreLocalesCulture"/>.<br/>
        /// If this method fails to find the requested culture, the value of this will be a reference to a default <see cref="MoreLocalesCulture"/>.
        /// </param>
        /// <returns>Whether or not the requested <see cref="MoreLocalesCulture"/> was found.</returns>
        public static bool TryGetCulture<T>(out Ref<MoreLocalesCulture> culture) where T : ModCulture
        {
            if (_autoloadedCulturesRegistry.TryGetValue(typeof(T), out var c))
            {
                culture = new(ref GetCulture(c));
                return true;
            }
            culture = new(ref extraCulturesV2[0]);
            return false;
        }
        /// <summary>
        /// Returns a reference to the <see cref="MoreLocalesCulture"/> that contains this <see cref="GameCulture"/>.
        /// </summary>
        public static ref MoreLocalesCulture GetCultureExtra(this GameCulture culture) => ref GetCulture(culture.LegacyId);
        internal static void DoLoad()
        {
            IL_LanguageManager.ReloadLanguage += AddFallbacks;
            On_Main.SaveSettings += Save;

            _registerNative = true;
            RegisterVanillaCultures();
            RegisterNativeCustomCultures();
            _registerNative = false;
        }
        private static void RegisterVanillaCultures()
        {
            var basicRomance = GrammarData.OrderContext(AdjectiveOrder.AfterWithSpace, gpChangesWhenNotDefault);

            RegisterCulture(nameof(English),
                grammarData: GrammarData.Context(gpNeverChanges),
                buttonDrawData: new(sheetFrame: (int)English));

            RegisterCulture(nameof(German),
                grammarData: GrammarData.Context(gpChangesWhenNotDefault),
                buttonDrawData: new(sheetFrame: (int)German));

            RegisterCulture(nameof(Italian),
                grammarData: basicRomance,
                buttonDrawData: new(sheetFrame: (int)Italian));

            RegisterCulture(nameof(French),
                grammarData: GrammarData.StyleOrderContext(PluralizationStyle.SimpleWithSingularZero, AdjectiveOrder.AfterWithSpace, gpChangesWhenNotDefault),
                buttonDrawData: new(sheetFrame: (int)French));

            RegisterCulture(nameof(Spanish),
                grammarData: basicRomance,
                buttonDrawData: new(sheetFrame: (int)Spanish));

            RegisterCulture(nameof(Russian),
                grammarData: GrammarData.StyleContext(PluralizationStyle.RussianThreeway, gpChangesWhenNotDefault),
                buttonDrawData: new(sheetFrame: (int)Russian));

            RegisterCulture(nameof(Chinese),
                grammarData: GrammarData.StyleOrderContext(PluralizationStyle.None, AdjectiveOrder.Before, gpNeverChanges),
                buttonDrawData: new(sheetFrame: (int)Chinese));

            RegisterCulture(nameof(Portuguese),
                grammarData: basicRomance,
                buttonDrawData: new(sheetFrame: (int)Portuguese));

            RegisterCulture(nameof(Polish),
                grammarData: GrammarData.StyleContext(PluralizationStyle.PolishThreeway, gpChangesWhenNotDefault),
                buttonDrawData: new(sheetFrame: (int)Polish));
        }
        private static void RegisterNativeCustomCultures()
        {
            Mod mod = MoreLocales.Instance;

            var basicRomance = GrammarData.OrderContext(AdjectiveOrder.AfterWithSpace, gpChangesWhenNotDefault);

            // MoreLocales provides you with this extension method: Mod.RegisterCulture, for simplicity (mod parameter automatically gets filled).

            mod.RegisterCulture(nameof(BritishEnglish),
                "en-GB",
                grammarData: GrammarData.Context(gpNeverChanges),
                buttonDrawData: new(sheetFrame: (int)BritishEnglish));

            mod.RegisterCulture(nameof(Japanese),
                "ja-JP",
                grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.Before),
                buttonDrawData: new(sheetFrame: (int)Japanese));

            mod.RegisterCulture(nameof(Korean),
                "ko-KR",
                grammarData: new(PluralizationStyle.None),
                buttonDrawData: new(sheetFrame: (int)Korean));

            mod.RegisterCulture(nameof(TraditionalChinese),
                "zh-Hant",
                (int)Chinese,
                grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.Before),
                buttonDrawData: new(sheetFrame: (int)TraditionalChinese));

            mod.RegisterCulture(nameof(Turkish),
                "tr-TR",
                grammarData: new(PluralizationStyle.Custom, CultureHelper.turkishPlural),
                buttonDrawData: new(sheetFrame: (int)Turkish));

            mod.RegisterCulture(nameof(Thai),
                "th-TH",
                grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.After),
                buttonDrawData: new(sheetFrame: (int)Thai));

            mod.RegisterCulture(nameof(Ukrainian),
                "uk-UA",
                (int)Russian,
                grammarData: new(PluralizationStyle.RussianThreeway),
                buttonDrawData: new(sheetFrame: (int)Ukrainian));

            mod.RegisterCulture(nameof(MexicanSpanish),
                "es-MX",
                (int)Spanish,
                grammarData: basicRomance,
                buttonDrawData: new(sheetFrame: (int)MexicanSpanish));

            mod.RegisterCulture(nameof(Czech),
                "cs-CZ",
                grammarData: new(PluralizationStyle.Custom, CultureHelper.czechPlural),
                buttonDrawData: new(sheetFrame: (int)Czech));

            mod.RegisterCulture(nameof(Hungarian),
                "hu-HU",
                buttonDrawData: new(sheetFrame: (int)Hungarian));

            mod.RegisterCulture(nameof(PortugalPortuguese),
                "pt-PT",
                (int)Portuguese,
                grammarData: basicRomance,
                buttonDrawData: new(sheetFrame: (int)PortugalPortuguese));

            mod.RegisterCulture(nameof(Swedish),
                "sv-SE",
                buttonDrawData: new(sheetFrame: (int)Swedish));

            mod.RegisterCulture(nameof(Dutch),
                "nl-NL",
                buttonDrawData: new(sheetFrame: (int)Dutch));

            mod.RegisterCulture(nameof(Danish),
                "da-DK",
                buttonDrawData: new(sheetFrame: (int)Danish));

            mod.RegisterCulture(nameof(Vietnamese),
                "vi-VN",
                hasSubtitle: false,
                grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.AfterWithSpace),
                buttonDrawData: new(sheetFrame: (int)Vietnamese));

            mod.RegisterCulture(nameof(Finnish),
                "fi-FI",
                buttonDrawData: new(sheetFrame: (int)Finnish));

            mod.RegisterCulture(nameof(Romanian),
                "ro-RO",
                grammarData: new(PluralizationStyle.Custom, customPluralizationRule: CultureHelper.romanianPlural,AdjectiveOrder.AfterWithSpace),
                buttonDrawData: new(sheetFrame: (int)Romanian));

            mod.RegisterCulture(nameof(Indonesian),
                "id-ID", grammarData: GrammarData.StyleOrder(PluralizationStyle.None, AdjectiveOrder.AfterWithSpace),
                buttonDrawData: new(sheetFrame: (int)Indonesian));
        }
        internal static void DoSafeLoad()
        {
            IL_LocalizedText.CardinalPluralRule += SupportForNewPluralization;
        }
        internal static bool _canRegister = false;
        internal static bool _registerNative = false;
        public static ref MoreLocalesCulture RegisterCulture
        (
            string internalName,
            string languageCode = null,
            int fallbackCulture = 1,
            bool hasSubtitle = true,
            bool hasDescription = false,
            GrammarData grammarData = new(),
            Func<bool> available = null,
            LanguageButtonDrawData buttonDrawData = new(),
            Mod mod = null
        )
        {
            if (!_canRegister)
                throw new InvalidOperationException("You cannot register a culture outside of a Load method.");
            if (!_registerNative && (mod is null || mod == MoreLocales.Instance))
                throw new InvalidOperationException("Mods registered by external mods should have a valid mod instance. It cannot be left null or be MoreLocales.");

            GameCulture childCulture;
            if (_legacyCultures.TryGetValue(_registeredCount, out GameCulture vanillaCulture))
            {
                childCulture = vanillaCulture;
                // this culture is already fully registered, nothing else is needed
            }
            else if (!string.IsNullOrEmpty(languageCode))
            {
                childCulture = new GameCulture(languageCode, _registeredCount);
                _NamedCultures.Add((CultureName)_registeredCount, childCulture);
            }
            else
            {
                throw new NullReferenceException($"The parameter {languageCode} cannot be null for cultures that do not copy existing {nameof(GameCulture)}s.");
            }

            MoreLocalesCulture newCulture = new(childCulture, internalName, fallbackCulture, hasSubtitle, hasDescription, grammarData, available, buttonDrawData, mod);

            if (extraCulturesV2.Length < _registeredCount + 1)
                Array.Resize(ref extraCulturesV2, _registeredCount + 1);

            extraCulturesV2[_registeredCount] = newCulture;
            return ref extraCulturesV2[_registeredCount++];
        }
        public static void SupportForNewPluralization(ILContext il)
        {
            Mod mod = ModContent.GetInstance<MoreLocales>();
            try
            {
                var c = new ILCursor(il);

                // i will forever love my previous implementation but unfortunately since others can register their own cultures now, we need to do it in a hacky way

                // find where GameCulture.LegacyId is loaded for use with the switch statement, right before 1 gets subtracted from it
                if (!c.TryGotoNext(MoveType.After, i => i.MatchLdloc2()))
                {
                    mod.Logger.Warn("SupportForNewPluralization: Couldn't find GameCulture.LegacyId loading");
                    return;
                }
                c.EmitCall(typeof(CultureHelper).GetMethod("MapLegacyIDToPluralizationID")); // get the ID of a valid vanilla culture or 10 for custom

                ILLabel[] targets = null;

                if (!c.TryGotoNext(i => i.MatchSwitch(out targets)))
                {
                    mod.Logger.Warn("SupportForNewPluralization: Couldn't find switch statement position");
                    return;
                }

                ILLabel customPlural = il.DefineLabel();

                // we resize the array to include our new cultures
                var newTargets = new ILLabel[targets.Length + 1]; // entry 10 will be custom
                targets.CopyTo(newTargets, 0); // and make sure the old cultures are in it too
                newTargets[^1] = customPlural;

                // finally, we assign the new switch table to the switch instruction
                c.Next.Operand = newTargets;

                // now we inject our code for custom rules somewhere it won't interfere with other stuff. an easy way to do that is by adding it at the end
                
                c.Index = il.Instrs.Count; // common mistake: don't subtract one, because the cursor will end up before the last ret, not after it. remember how cursor indices work
                int labelIndex = c.Index;

                // normally you would mark the label here. however, we'll get a nullref exception if we do this.
                // this is because the label's target wants to be c.Next but it's null since we're on the end of the method.

                c.EmitLdloc2(); // legacy id (the original one)
                c.EmitLdloc0(); // mod10
                c.EmitLdloc1(); // mod100
                c.EmitLdarg1(); // count
                c.EmitCall(typeof(CultureHelper).GetMethod("CustomPluralization"));
                c.EmitRet();

                c.Index = labelIndex;
                c.MarkLabel(customPlural); // finally we mark the label
                
                // the IL edit would normally be done here, but we actually have one more thing left to do
                // the issue with editing switch statements and adding your own cases is that every instruction emitted by monomod has the IL_0000 offset. this is REALLY bad for labels, so switch will die
                // to solve this, we can recalculate all of the offsets so that the labels won't be all messed up. thank you absoluteAquarian aka the MonoSound guy

                ILHelper.UpdateInstructionOffsets(c);
            }
            catch
            {
                MonoModHooks.DumpIL(mod, il);
            }
        }
        private static bool Save(On_Main.orig_SaveSettings orig)
        {
            // So, why do we need this?
            // The game will actually save our custom culture by default, using GameCulture.Name, but it won't recognize it when loading, and revert back to English.
            // First, we can save our custom culture data in our file.
            SaveCustomCultureData();
            // Second, we can revert the culture by ourselves before the game has the chance to save it.
            RevertCustomCulture(false, out var customCulture);
            bool result = orig();
            // Then, bring it back (if settings are saved outside of game exit, this is necessary)
            LanguageManager.Instance?.SetLanguage(customCulture);
            return result;
        }
        private static void AddFallbacks(ILContext il)
        {
            Mod mod = ModContent.GetInstance<MoreLocales>();
            try
            {
                // first we need to add a local var for our custom GameCulture
                var localGameCulture = new VariableDefinition(il.Import(typeof(GameCulture)));
                il.Body.Variables.Add(localGameCulture);

                var c = new ILCursor(il);

                // this is inside the if statement, so we already know that the active culture isn't english
                if (!c.TryGotoNext(i => i.MatchLdarg0(), i => i.MatchLdarg0(), i => i.MatchCall<LanguageManager>("get_ActiveCulture")))
                {
                    mod.Logger.Warn("AddFallbacks: Couldn't find in-between step insertion position");
                    return;
                }

                // load this in order to consume it for our delegate
                c.EmitLdarg0();

                // figure out if the current lang has a fallback defined
                c.EmitDelegate<Func<LanguageManager, GameCulture>>(l =>
                {
                    int possibleFallback = extraCulturesV2[l.ActiveCulture.LegacyId].FallbackCulture;
                    if (possibleFallback != 1)
                        return _legacyCultures[possibleFallback];
                    return null;
                });

                // store that value in the variable
                c.EmitStloc(localGameCulture.Index);

                var skipLabel = il.DefineLabel();

                // load the variable to check if it's null
                c.EmitLdloc(localGameCulture.Index);

                // if it's null, skip the call
                c.EmitBrfalse(skipLabel);

                // otherwise, load arguments
                c.EmitLdarg0();
                c.EmitLdloc(localGameCulture.Index);

                // then call the method
                c.EmitCall(typeof(LanguageManager).GetMethod("LoadFilesForCulture", BindingFlags.Instance | BindingFlags.NonPublic));

                // it should skip to after the call
                c.MarkLabel(skipLabel);
            }
            catch
            {
                MonoModHooks.DumpIL(mod, il);
            }
        }
        /// <summary>
        /// Sets the game's language without calling <see cref="LanguageManager.SetLanguage(GameCulture)"/>
        /// </summary>
        /// <param name="culture"></param>
        public static void SetLanguageSoft(GameCulture culture)
        {
            var lang = LanguageManager.Instance;
            lang.ActiveCulture = culture;
            Thread.CurrentThread.CurrentCulture = culture.CultureInfo;
            Thread.CurrentThread.CurrentUICulture = culture.CultureInfo;
        }
        private const byte FileVersion = 0;
        public static void LoadCustomCultureData()
        {
            string pathToCustomCultureData = Path.Combine(Main.SavePath, customCultureDataName);

            if (!File.Exists(pathToCustomCultureData))
                return;

            using var reader = new BinaryReader(File.Open(pathToCustomCultureData, FileMode.Open));
            if (reader.BaseStream.Length == 1) // oldest file version stored a single byte as language id
            {
                byte culture = reader.ReadByte();
                if (!CultureHelper.IsValid(culture))
                    return;

                loadedCulture = culture;

                // if it's somehow larger than the range of things that was available then, default back to vanilla thing
                if (loadedCulture > (int)CultureNamePlus.Indonesian)
                    loadedCulture = LanguageManager.Instance.ActiveCulture.LegacyId;

                // no reason to re-set the language if it's vanill
                if (loadedCulture < (int)CultureNamePlus.BritishEnglish)
                    return;
            }
            else
            {
                byte version = reader.ReadByte();

                string langCode = reader.ReadString();
                for (int i = 1; i < extraCulturesV2.Length; i++)
                {
                    GameCulture culture = extraCulturesV2[i].Culture;
                    if (langCode == culture.Name)
                    {
                        loadedCulture = culture.LegacyId;
                        break;
                    }
                }
            }

            LanguageManager.Instance.SetLanguage(extraCulturesV2[loadedCulture].Culture);
            Main.instance.SetTitle();
        }
        private static void SaveCustomCultureData()
        {
            if (!LanguageManager.Instance.ActiveCulture.IsCustom()) // no reason to save anything if not custom
                return;

            string pathToCustomCultureData = Path.Combine(Main.SavePath, customCultureDataName);

            void WriteFile()
            {
                using var writer = new BinaryWriter(File.Open(pathToCustomCultureData, FileMode.OpenOrCreate));

                writer.Write(FileVersion);

                string langCode = LanguageManager.Instance.ActiveCulture.Name;
                writer.Write(langCode);
            }

            if (!File.Exists(pathToCustomCultureData))
            {
                WriteFile();
            }
            else
            {
                File.WriteAllText(pathToCustomCultureData, "");
                WriteFile();
            }
        }
        internal static void DoUnload()
        {
            SaveCustomCultureData();
            UnregisterCultures();
            _autoloadedCulturesRegistry = null;
            extraCulturesV2 = null;
        }
        private static void RevertCustomCulture(bool setTitle, out GameCulture customCulture, bool soft = false)
        {
            customCulture = LanguageManager.Instance.ActiveCulture;
            if (!customCulture.IsCustom())
                return;

            if (soft)
                SetLanguageSoft(FromLegacyId(cachedVanillaCulture));
            else
                LanguageManager.Instance.SetLanguage(cachedVanillaCulture);

            if (setTitle)
                Main.instance.SetTitle();
        }
        private static void UnregisterCultures()
        {
            RevertCustomCulture(true, out _, true);

            for (int i = 1; i < extraCulturesV2.Length; i++)
            {
                MoreLocalesCulture culture = extraCulturesV2[i];

                if (culture.Vanilla)
                    continue;

                _legacyCultures.Remove(i);
                _NamedCultures.Remove((CultureName)i);
            }
        }
    }
}
