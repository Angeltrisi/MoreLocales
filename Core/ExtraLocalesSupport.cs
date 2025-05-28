using MoreLocales.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Terraria;
using Terraria.Localization;
using static Terraria.Localization.GameCulture;
using static MoreLocales.Core.FeaturesPlus;
using static MoreLocales.Core.CultureNamePlus;
using static Terraria.Localization.GameCulture.CultureName;

namespace MoreLocales.Core
{
    /// <summary>
    /// A structure used to significantly extend the functionality of <see cref="GameCulture"/>.<br/>
    /// Cultures registered through <see cref="MoreLocales"/> will create localization keys inside the <see cref="MoreLocalesCulture.Mod"/>'s localization file.<br/>
    /// These keys are needed for correct display inside <see cref="MoreLocales"/>'s UI.
    /// </summary>
    public readonly struct MoreLocalesCulture(GameCulture culture, string name, int fallback = 1,
        bool subtitle = false, bool description = false, PluralizationStyle pluralization = PluralizationStyle.Simple,
        Func<int, int, int, int> customPluralization = null, AdjectiveOrder adjectiveOrder = default, Func<int, int, bool> genderPluralizationChangesAdjectiveForm = null,
        Mod mod = null)
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
        /// <summary>
        /// The pluralization style that should be used for this <see cref="MoreLocalesCulture"/>.<para/>
        /// If the value of this is <see cref="PluralizationStyle.Custom"/>, setting the value of <see cref="CustomPluralizationRule"/> <b>is mandatory.</b>
        /// </summary>
        public readonly PluralizationStyle PluralizationRule = pluralization;
        /// <summary>
        /// The pluralization rule function for a <see cref="MoreLocalesCulture"/> with a <see cref="PluralizationRule"/> of value <see cref="PluralizationStyle.Custom"/>.<para/>
        /// This function should take in 'count, mod10, mod100' as parameters, and return the index of the final pluralization type.<br/>
        /// If your culture represents a language that already exists, refer to this list to learn how to write this function: <see href="https://docs.translatehouse.org/projects/localization-guide/en/latest/l10n/pluralforms.html"/>
        /// </summary>
        public readonly Func<int, int, int, int> CustomPluralizationRule = customPluralization;
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
        public readonly Func<int, int, bool> GenderPluralizationChangesAdjectiveForm = genderPluralizationChangesAdjectiveForm;
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
    }
    public class ExtraLocalesSupport
    {
        private const string customCultureDataName = "LocalizationPlusData.dat";
        private static int loadedCulture = 9999;
        internal static int cachedVanillaCulture = 1; // english by default
        internal static MoreLocalesCulture[] extraCulturesV2 = new MoreLocalesCulture[28]; // entry 0 is a dummy default entry
        private static int _registeredCount = 1; // starts at one because CultureName.English is 1
        public static MoreLocalesCulture ActiveCulture => extraCulturesV2[LanguageManager.Instance.ActiveCulture.LegacyId];

        internal static void DoLoad()
        {
            IL_LanguageManager.ReloadLanguage += AddFallbacks;
            On_Main.SaveSettings += Save;

            RegisterVanillaCultures();
        }
        private static void RegisterVanillaCultures()
        {
            RegisterCulture(nameof(English), contextChangesAdjective: gpNeverChanges);
            RegisterCulture(nameof(German), contextChangesAdjective: gpChangesWhenNotDefault);
            RegisterCulture(nameof(Italian), pluralizationStyle: PluralizationStyle.SimpleWithSingularZero, adjectiveOrder: AdjectiveOrder.AfterWithSpace, contextChangesAdjective: gpChangesWhenNotDefault);
            RegisterCulture(nameof(French), adjectiveOrder: AdjectiveOrder.AfterWithSpace, contextChangesAdjective: gpChangesWhenNotDefault);
            RegisterCulture(nameof(Spanish), adjectiveOrder: AdjectiveOrder.AfterWithSpace, contextChangesAdjective: gpChangesWhenNotDefault);
            RegisterCulture(nameof(Russian), pluralizationStyle: PluralizationStyle.RussianThreeway, contextChangesAdjective: gpChangesWhenNotDefault);
            RegisterCulture(nameof(Chinese), pluralizationStyle: PluralizationStyle.None, adjectiveOrder: AdjectiveOrder.Before, contextChangesAdjective: gpNeverChanges);
            RegisterCulture(nameof(Portuguese), adjectiveOrder: AdjectiveOrder.AfterWithSpace, contextChangesAdjective: gpChangesWhenNotDefault);
            RegisterCulture(nameof(Polish), pluralizationStyle: PluralizationStyle.PolishThreeway, contextChangesAdjective: gpChangesWhenNotDefault);
        }
        private static void RegisterNativeCustomCultures()
        {
            Mod mod = MoreLocales.Instance;

            mod.RegisterCulture(nameof(BritishEnglish), "en-GB", contextChangesAdjective: gpNeverChanges);
            mod.RegisterCulture(nameof(Japanese), "ja-JP", pluralizationStyle: PluralizationStyle.None, adjectiveOrder: AdjectiveOrder.Before);
            mod.RegisterCulture(nameof(Korean), "ko-KR", pluralizationStyle: PluralizationStyle.None);
            mod.RegisterCulture(nameof(TraditionalChinese), "zh-Hant", (int)Chinese, pluralizationStyle: PluralizationStyle.None, adjectiveOrder: AdjectiveOrder.Before);
            mod.RegisterCulture(nameof(Turkish), "tr-TR", pluralizationStyle: PluralizationStyle.Custom, customPluralizationRule: CultureHelper.turkishPlural);
            mod.RegisterCulture(nameof(Thai), "th-TH", pluralizationStyle: PluralizationStyle.None, adjectiveOrder: AdjectiveOrder.After);
            mod.RegisterCulture(nameof(Ukrainian), "uk-UA", (int)Russian, pluralizationStyle: PluralizationStyle.RussianThreeway);
            mod.RegisterCulture(nameof(LatinAmericanSpanish), "en-LA", (int)Spanish, adjectiveOrder: AdjectiveOrder.AfterWithSpace, contextChangesAdjective: gpChangesWhenNotDefault);
            mod.RegisterCulture(nameof(Czech), "cs-CZ", pluralizationStyle: PluralizationStyle.Custom, customPluralizationRule: CultureHelper.czechPlural);
            mod.RegisterCulture(nameof(Hungarian), "hu-HU");
            mod.RegisterCulture(nameof(PortugalPortuguese), "pt-PT", (int)Portuguese, adjectiveOrder: AdjectiveOrder.AfterWithSpace);
            mod.RegisterCulture(nameof(Swedish), "sv-SE");
            mod.RegisterCulture(nameof(Dutch), "nl-NL");
            mod.RegisterCulture(nameof(Danish), "da-DK");
            mod.RegisterCulture(nameof(Vietnamese), "vi-VN", hasSubtitle: false, pluralizationStyle: PluralizationStyle.None, adjectiveOrder: AdjectiveOrder.AfterWithSpace);
            mod.RegisterCulture(nameof(Finnish), "fi-FI");
            mod.RegisterCulture(nameof(Romanian), "ro-RO", pluralizationStyle: PluralizationStyle.Custom, customPluralizationRule: CultureHelper.romanianPlural, adjectiveOrder: AdjectiveOrder.AfterWithSpace);
            mod.RegisterCulture(nameof(Indonesian), "id-ID", pluralizationStyle: PluralizationStyle.None, adjectiveOrder: AdjectiveOrder.AfterWithSpace);
        }
        internal static void DoSafeLoad()
        {
            RegisterNativeCustomCultures();
            IL_LocalizedText.CardinalPluralRule += SupportForNewPluralization;
        }
        public static ref MoreLocalesCulture RegisterCulture
        (
            string internalName,
            string languageCode = null,
            int fallbackCulture = 1,
            bool hasSubtitle = true,
            bool hasDescription = false,
            PluralizationStyle pluralizationStyle = PluralizationStyle.Simple,
            Func<int, int, int, int> customPluralizationRule = null,
            AdjectiveOrder adjectiveOrder = default,
            Func<int, int, bool> contextChangesAdjective = null,
            Mod mod = null
        )
        {
            if (adjectiveOrder == default)
                adjectiveOrder = AdjectiveOrder.BeforeWithSpace;

            GameCulture childCulture;
            if (_legacyCultures.TryGetValue(_registeredCount, out GameCulture vanillaCulture))
            {
                childCulture = vanillaCulture;
                // this culture is already fully registered, nothing else is needed
            }
            else if (languageCode != null)
            {
                childCulture = new GameCulture(languageCode, _registeredCount);
                _NamedCultures.Add((CultureName)_registeredCount, childCulture);
            }
            else
            {
                throw new NullReferenceException($"The parameter {languageCode} cannot be null for cultures that do not copy existing {nameof(GameCulture)}s.");
            }

            MoreLocalesCulture newCulture = new(childCulture, internalName, fallbackCulture, hasSubtitle, hasDescription, pluralizationStyle, customPluralizationRule, adjectiveOrder, contextChangesAdjective, mod);

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
        public static void LoadCustomCultureData()
        {
            string pathToCustomCultureData = Path.Combine(Main.SavePath, customCultureDataName);

            if (!File.Exists(pathToCustomCultureData))
                return;

            using var reader = new BinaryReader(File.Open(pathToCustomCultureData, FileMode.Open));
            byte culture = reader.ReadByte();

            if (!CultureHelper.IsValid(culture))
                return;

            loadedCulture = culture;

            LanguageManager.Instance.SetLanguage(extraCulturesV2[loadedCulture].Culture);
            Main.instance.SetTitle();
        }
        private static void SaveCustomCultureData()
        {
            string pathToCustomCultureData = Path.Combine(Main.SavePath, customCultureDataName);

            void WriteFile()
            {
                using var writer = new BinaryWriter(File.Open(pathToCustomCultureData, FileMode.OpenOrCreate));
                byte id = (byte)LanguageManager.Instance.ActiveCulture.LegacyId;
                writer.Write(id);
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
