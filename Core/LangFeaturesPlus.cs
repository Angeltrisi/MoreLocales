using MoreLocales.Config;
using System;
using System.Diagnostics;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using System.Reflection;

namespace MoreLocales.Core
{
    /// <summary>
    /// Container for all features of Localization+ that are not (directly) related to extra language support.
    /// </summary>
    public static class LangFeaturesPlus
    {
        private const string StringToReplace = "{Prefix}";
        private static readonly string[] GenderNames = Enum.GetNames<GrammaticalGender>();
        private delegate void VoidsOrig();
        internal static void DoLoad()
        {
            // prefix stuff
            MonoModHooks.Modify(typeof(Item).GetMethod("get_Name"), RemovePrefixLiteralFromName);
            IL_Item.AffixName += LocalizedPrefixPosition;
            // comment stuff
            MonoModHooks.Add(typeof(SystemLoader).GetMethod("OnLocalizationsLoaded", BindingFlags.Static | BindingFlags.NonPublic), ResetLangUtilsBool);
            MonoModHooks.Add(typeof(LocalizationLoader).GetMethod("Update", BindingFlags.Static | BindingFlags.NonPublic), UpdateLocalizationHook);
        }
        private static void ResetLangUtilsBool(VoidsOrig orig)
        {
            orig();
            LangUtils.FilesWillBeReloadedDueToCommentsChange = false;
        }
        private static void UpdateLocalizationHook(VoidsOrig orig)
        {
            if (!Main.dedServ)
                LangUtils.ConsumeCommentsQueue();
            orig();
        }
        internal static string RemovePrefixLiteral(string input)
        {
            int index = input.IndexOf(StringToReplace);
            if (index == -1)
                return input;

            if (index == 0) // beginning case
            {
                int start = StringToReplace.Length;

                if (input.Length > start && char.IsWhiteSpace(input[start]))
                    start++;

                return input[start..];
            }

            if (index + StringToReplace.Length == input.Length) // end case
            {
                int end = index;

                if (char.IsWhiteSpace(input[end - 1]))
                    end--;

                return input[..end];
            }

            // middle case

            string before = input[..index];
            string after = input[(index + StringToReplace.Length)..];

            if (char.IsWhiteSpace(before[^1]) && char.IsWhiteSpace(after[0]))
                after = after[1..];

            return before + after;
        }
        private static void RemovePrefixLiteralFromName(ILContext il)
        {
            Mod m = MoreLocales.Instance;
            try
            {
                var c = new ILCursor(il);

                c.GotoNext(i => i.MatchRet());

                c.EmitCall(typeof(LangFeaturesPlus).GetMethod("RemovePrefixLiteral"));
            }
            catch
            {
                MonoModHooks.DumpIL(m, il);
            }
        }
        private static void LocalizedPrefixPosition(ILContext il)
        {
            Mod m = MoreLocales.Instance;
            try
            {
                // this edit is a little loaded.
                // there's a case in this method specifically for prefix names that start with (. these names are formatted in a specific way in Terraria (at the end instead of at the start).
                // this case needs to be changed. instead of returning the end-formatted name, we make the case remove the parentheses, store the result, then jump to the normal case for further formatting.

                // for convenience, we can add the config value as a local
                var localConfigOption = new VariableDefinition(il.Import(typeof(bool)));
                il.Body.Variables.Add(localConfigOption);

                var c = new ILCursor(il);

                // init our local
                c.EmitLdsfld(typeof(ClientSideConfig).GetField(nameof(ClientSideConfig.Instance)));
                c.EmitLdfld(typeof(ClientSideConfig).GetField(nameof(ClientSideConfig.LocalizedPrefixPlacement)));
                c.EmitStloc(localConfigOption.Index);

                // let's load the correct (inflected) prefix value first
                if (!c.TryGotoNext(MoveType.After, i => i.MatchLdelemRef()))
                {
                    m.Logger.Warn("LocalizedPrefixPosition: Couldn't find original prefix load for replacement");
                    return;
                }
                c.EmitPop(); // pop the original localizedtext value before the string value is obtained from it
                c.EmitLdarg0(); // get the item
                c.EmitCall(typeof(LangFeaturesPlus).GetMethod(nameof(GetPrefixNameWithItemContext))); // get the new value

                // this is the label for the final case (last line of the method)
                ILLabel finalTextLabel = null;

                // first we get the final case label
                if (!c.TryGotoNext(i => i.MatchCallvirt(out _), i => i.MatchBrfalse(out finalTextLabel)))
                {
                    m.Logger.Warn("LocalizedPrefixPosition: Couldn't find final label for branching");
                    return;
                }

                // then we find where we can do our branching (inside the code block for the parentheses check)
                if (!c.TryGotoNext(i => i.MatchLdarg0(), i => i.MatchCall<Item>("get_Name"), i => i.MatchLdstr(" ")))
                {
                    m.Logger.Warn("LocalizedPrefixPosition: Couldn't find correct location for branching");
                    return;
                }

                // we'll make a label to skip our special parentheses removal. this is for making the config option work.
                var skipParenthesesRemovalLabel = il.DefineLabel();

                // now, we branch according to the config value
                c.EmitLdloc(localConfigOption.Index);

                c.EmitBrfalse(skipParenthesesRemovalLabel);

                // now, we do the parentheses thing
                c.EmitLdloc0(); // load the localized prefix string (we already know it's in parentheses)
                c.EmitDelegate<Func<string, string>>(s =>
                {
                    return s[1..^1]; // return the string without the first and last characters
                });
                c.EmitStloc0(); // store the cleaned-up string back in the local

                c.EmitBr(finalTextLabel);

                // mark the label to continue normally if the config option is off
                c.MarkLabel(skipParenthesesRemovalLabel);

                // this part of the edit is now done. something like "Espada corta de hierro (Pequeño)" will now show up as "Pequeño Espada corta de hierro".

                // part two: replacing occurences of {Prefix} with the actual prefix, and custom formatting.
                // remember that Item.Name now returns the item name with the {Prefix} literal removed, so we have to get the actual lang value.

                c.GotoLabel(finalTextLabel);

                // the original last case code will not run at all: now this label's target will be the code that we emit from here on

                c.EmitLdarg0(); // item
                c.EmitLdloc0(); // prefix name (sanitized)

                c.EmitDelegate<Func<Item, string, string>>((item, prefix) =>
                {
                    string realName = CultureHelper.GetRealName(item);

                    // custom position will take priority over localized order
                    if (realName.Contains(StringToReplace))
                        return realName.Replace(StringToReplace, prefix);

                    // localized order
                    AdjectiveOrder realOrder = MoreLocalesAPI.ActiveCulture.GrammarData.AdjectiveOrder;

                    return realOrder.Apply(realName, prefix);
                });

                c.EmitRet();
            }
            catch
            {
                MonoModHooks.DumpIL(m, il);
            }
        }
        /// <summary>
        /// Retrieves a LocalizedText that contains the gendered and pluralized form of a prefix depending on the item it's applied to (if applicable)
        /// </summary>
        /// <param name="context">The item.</param>
        public static LocalizedText GetPrefixNameWithItemContext(Item context)
        {
            int prefix = context.prefix;

            if (!ClientSideConfig.Instance.LocalizedPrefixGenderPluralization)
                return Lang.prefix[prefix];

            MoreLocalesSets.CachedInflectionData[context.type].Deconstruct(out GrammaticalGender gender, out Pluralization pluralization);

            if (!LanguageManager.Instance.ActiveCulture.GPDataChangesAdjectiveForm(gender, pluralization))
                return Lang.prefix[prefix]; // adjective form stays the same

            bool vanilla = prefix < PrefixID.Count;
            ModPrefix modPrefix = null;

            if (!vanilla)
                modPrefix = PrefixLoader.GetPrefix(prefix);

            string prefixName = vanilla ? PrefixID.Search.GetName(prefix) : modPrefix.Name;

            string genderName = GenderNames[(byte)gender];

            Mod target = vanilla ? MoreLocales.Instance : modPrefix.Mod;
            string preprefix = vanilla ? "VanillaData." : string.Empty;
            return target.GetLocalization($"{preprefix}InflectionData.Prefixes.{prefixName}.{genderName}", () => Lang.prefix[prefix].Value).WithFormatArgs((byte)pluralization);
        }
        public static bool GPDataChangesAdjectiveForm(this GameCulture c, InflectionData data)
        {
            data.Deconstruct(out GrammaticalGender gender, out Pluralization pluralization);
            return c.GPDataChangesAdjectiveForm(gender, pluralization);
        }
        public static bool GPDataChangesAdjectiveForm(this GameCulture c, GrammaticalGender gender, Pluralization pluralization)
        {
            return MoreLocalesAPI.extraCulturesV2[c.LegacyId].GrammarData.ContextChangesAdjective(gender, pluralization);
        }
        public static bool gpNeverChanges(GrammaticalGender gender, Pluralization pluralization) => false;
        public static bool gpChangesWhenNotDefault(GrammaticalGender gender, Pluralization pluralization) => gender > 0 || pluralization > 0;
        /// <summary>
        /// Only items that can be reforged should be able to affect adjectives.
        /// </summary>
        /// <param name="type">The type of the item to look up.</param>
        /// <returns></returns>
        public static bool ItemIsGenderPluralizable(int type)
        {
            Item dummy = ContentSamples.ItemsByType[type];
            return dummy.CanHavePrefixes();
            /*
            if (type < ItemID.Count)
                return dummy.CanHavePrefixes();
            retur
            */
        }
        public static InflectionData GetItemInflection(int type)
        {
            if (!ItemIsGenderPluralizable(type))
                return InflectionData.Default;

            bool vanilla = type < ItemID.Count;

            ModItem modItem = null;
            if (!vanilla)
                modItem = ItemLoader.GetItem(type);

            string itemName = vanilla ? ItemID.Search.GetName(type) : modItem.Name;

            if (itemName == null)
                return InflectionData.Default;

            LocalizedText data = null;

            Mod target = vanilla ? MoreLocales.Instance : modItem.Mod;
            string preprefix = vanilla ? "VanillaData." : string.Empty;

            string key = $"{preprefix}InflectionData.Items.{itemName}";
            data = target.GetLocalization(key, () => "/");
            target.AddComment(key,  $"DisplayName: {Lang.GetItemName(type)}", HjsonCommentType.Hash);

            if (TryParse(data.Value, out InflectionData inflectionData))
                return inflectionData;

            return InflectionData.Default;
        }
        public static bool TryParse(string value, out InflectionData result)
        {
            result = InflectionData.Default;

            string[] values = value.Split('/');
            if (values.Length == 0 || values.Length > 2)
                return false;

            uint finalGender = 0;

            // we want to default to 0 for an entry like "/M" for a language with adjective pluralization but no grammatical gender
            if (!string.IsNullOrEmpty(values[0]))
            {
                char gender = char.ToUpper(values[0][0]);

                finalGender = gender switch
                {
                    '0' or 'M' or 'C' => 0,
                    '1' or 'F' => 1,
                    '2' or 'N' => 2,
                    _ => 0
                };
            }

            uint finalPluralization = 0;

            // we want to default to 0 for an entry like "F/" or "F" for a language with grammatical gender but no adjective pluralization
            if (values.Length == 2 && !string.IsNullOrEmpty(values[1]))
            {
                char plural = char.ToUpper(values[1][0]);

                // special format
                if (values[1].Length > 1 && plural == 'P' && uint.TryParse(values[1].AsSpan(1), out uint specialResult))
                {
                    finalPluralization = specialResult;
                }
                else
                {
                    // custom alias support
                    LocalizedText customAliasEntry = MoreLocales.Instance.GetLocalization("VanillaData.InflectionData.PluralizationAliases");
                    string[] aliasesCollection = new string[3];
                    if (!string.IsNullOrEmpty(customAliasEntry.Value)) // we have aliases
                    {
                        string[] aliases = customAliasEntry.Value.ToUpper().Split('/');

                        if (aliases.Length > aliasesCollection.Length)
                            Array.Resize(ref aliasesCollection, aliases.Length);

                        for (int i = 0; i < aliases.Length; i++)
                        {
                            string alias = aliases[i];
                            if (!string.IsNullOrEmpty(alias))
                            {
                                aliasesCollection[i] += alias;
                            }
                        }
                    }
                    // parse
                    for (int i = 0; i < aliasesCollection.Length; i++)
                    {
                        if (string.IsNullOrEmpty(aliasesCollection[i]))
                            aliasesCollection[i] = i switch
                            {
                                // main aliases
                                0 => "0/S",
                                1 => "1/P/F",
                                2 => "2/M",
                                _ => null,
                            };
                        if (aliasesCollection[i].Split("/").Contains(values[1].ToUpper()))
                        {
                            finalPluralization = (uint)i;
                            break;
                        }
                    }
                }
            }

            result |= (InflectionData)finalGender;
            result |= (InflectionData)(finalPluralization << 4);

            return true;
        }
        public static void Deconstruct(this InflectionData data, out GrammaticalGender gender, out Pluralization pluralization)
        {
            gender = (GrammaticalGender)((byte)data & 0xF);
            pluralization = (Pluralization)((byte)data >> 4);
        }
    }
    /// <summary>
    /// Container for grammatical gender and pluralization.
    /// </summary>
    public enum InflectionData : byte
    {
        Default = 0,
    }
    /// <summary>
    /// Grammatical gender.
    /// </summary>
    public enum GrammaticalGender : byte
    {
        Masculine, Common = 0,
        Feminine = 1,
        Neuter = 2,
    }
    /// <summary>
    /// Grammatical pluralization.
    /// </summary>
    public enum Pluralization : byte
    {
        Singular = 0,
        Plural, Few = 1,
        Many = 2,
    }
}
