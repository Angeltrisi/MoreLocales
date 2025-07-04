using Hjson;
using Mono.Cecil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria.Localization;
using static Terraria.ModLoader.LocalizationLoader;

namespace MoreLocales.Common
{
    public static class LocalizationTweaks
    {
        // internal static GameCulture _currentProcessedCulture;
        private static MethodReference terriblyUnperformantMethod;
        internal static void Apply()
        {
            Type[] mParams =
            [           
                typeof(Mod),
                typeof(string),
                typeof(GameCulture)
            ];
            MethodInfo bigOlMethord = typeof(LocalizationLoader).GetMethod("UpdateLocalizationFilesForMod", BindingFlags.Static | BindingFlags.NonPublic, mParams);
            MonoModHooks.Modify(bigOlMethord, FixPeskyLegacyMarking);

            MethodInfo nested = typeof(LocalizationLoader).GetMethod("LocalizationFileToHjsonText", BindingFlags.Static | BindingFlags.NonPublic);
            MonoModHooks.Modify(nested, LookForActualMethod);

            MonoModHooks.Modify(terriblyUnperformantMethod.ResolveReflection(), DontUseLINQForAGiganticDictionary);
        }
        /// <summary>
        /// System.Linq.Last, now hyperoptimized! (final time per mod load goes from around 2 seconds to 10 milliseconds)
        /// </summary>
        private static void EmitJsonObjectLastKey(ILCursor c)
        {
            // assuming we're right after the jsonobject was loaded (and arg1 is the jsonobject)

            // get the map field to load it
            FieldInfo mapField = typeof(JsonObject)
                .GetField("map", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new NullReferenceException("null");
            // first we load the "map" field from the object, which is a Dictionary<string, JsonValue>
            c.EmitLdfld(mapField);
            // then, we access an internal array dictionaries have, "_entries", which is a Dictionary<,>.Entry<string, JsonValue>[]
            c.EmitLdfld(typeof(Dictionary<string, JsonValue>)
                .GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new NullReferenceException("null"));
            // load the map again to get the count
            c.EmitLdarg1();
            c.EmitLdfld(mapField);
            // then, we get the count of the total key value pairs
            c.EmitCallvirt(typeof(Dictionary<string, JsonValue>).GetMethod("get_Count") ?? throw new NullReferenceException("null"));
            // make it usable for indexing
            c.EmitLdcI4(1);
            c.EmitSub();
            // we get the entry's type to index into the array
            Type entryType = typeof(Dictionary<,>).GetNestedType("Entry", BindingFlags.NonPublic)
                .MakeGenericType([typeof(string), typeof(JsonValue)]) ?? throw new NullReferenceException("null");
            // load the element at our index
            c.EmitLdelema(entryType);
            // load the key field from it, which will be a string
            c.EmitLdfld(entryType.GetField("key") ?? throw new NullReferenceException("null"));
            // you can, uh, wipe the sweat off your system now.
        }
        private static void DontUseLINQForAGiganticDictionary(ILContext il) // throwing shade
        {
            var c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchRet(), i => i.MatchLdarg1()))
            {
                Logging.tML.Warn("[MoreLocales] DontUseLINQForAGiganticDictionary: Couldn't find place to remove instructions from");
                return;
            }
            c.RemoveRange(2); // lol
            EmitJsonObjectLastKey(c);
        }
        /*
        internal static void PlaceCommentAboveNewEntryNew(LocalizationEntry entry, CommentedWscJsonObject parent, Dictionary<string, string> localizationsForCulture, LocalizationFile file)
        {
            // the original method doesn't take the dictionary as a parameter
            // so i replace all calls to the method inside LocalizationFileToHjsonText with this one

            string sub;

            Match m = LocalizationLoader.referenceRegex.Match(entry.comment);
            if (m.Success) // regex matched, but we don't know if it's a valid key yet, so let's look in the scope
            {
                var culture = _currentProcessedCulture;
                // add vanilla keys
                Dictionary<string, string>[] allVanillaKeys = LangUtils.GetVanillaLanguageFilesForCultureFlattened(culture);
                for (int i = 0; i < allVanillaKeys.Length; i++)
                {
                    foreach (var kvp in allVanillaKeys[i])
                        localizationsForCulture[kvp.Key] = kvp.Value;
                }

                string validKey = LangUtils.FindKeyInScope(m.Groups[1].Value, entry.key, [.. localizationsForCulture.Keys]);
                if (validKey is null)
                    sub = entry.comment;
                else
                    sub = m.Result(localizationsForCulture[validKey]);

                Console.WriteLine(culture.Name);
            }
            else
            {
                sub = entry.comment;
            }

            if (parent.Count == 0)
            {
                parent.Comments[""] = sub;
            }
            else
            {
                var dict = parent.map;
                var entries = _entries.GetValue(dict) as Array;

                string actualCommentKey = (string)_key.GetValue(entries.GetValue(parent.Count - 1));
                parent.Comments[actualCommentKey] = sub;
            }
        }
        */
        private static void LookForActualMethod(ILContext il)
        {
            var c = new ILCursor(il);

            //c.GotoNext(i => i.MatchLdloc3(), i => i.MatchCall(out testingMethodDifference));

            c.GotoNext
            (
                i => i.MatchBrtrue(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchLdloc(out _),
                i => i.MatchCall(out terriblyUnperformantMethod)
            );

            /*

            MethodReference newCommentMethod = il.Import(typeof(LocalizationTweaks).GetMethod(nameof(PlaceCommentAboveNewEntryNew), BindingFlags.Static | BindingFlags.NonPublic));

            var c = new ILCursor(il);

            while (c.TryGotoNext(i => i.MatchCall(out MethodReference method) && method.FullName.Contains("PlaceCommentAboveNewEntry|")))
            {
                c.EmitLdarg1();
                c.EmitLdarg0();
                c.Next.Operand = newCommentMethod;
                c.Index++;
            }

            */
        }
        // fixes problem where tmod would mark the vanilla translation files as legacy since they don't have an en-US equivalent
        // also allows referencing localizations from all languages even if a given language isn't the active language (nope!)
        private static void FixPeskyLegacyMarking(ILContext il)
        {
            var c = new ILCursor(il);

            /*

            // first part: to avoid changing calls to LocalizationFileToHjsonText, we store the current iterated target culture to a global variable

            int cultureVar = 0; // will be index to the culture local, which gets reused multiple times within the method so it doesn't matter when we capture it

            // get the culture local index
            if (!c.TryGotoNext(i => i.MatchCallvirt<IEnumerator<GameCulture>>("get_Current"), i => i.MatchStloc(out cultureVar)))
                throw new Exception("how");

            // the 'baseLocalizationFiles' local apparently gets put in a compiler generated class for some reason,
            // so it's not that easy to match to
            FieldReference compGen = null;

            if (!c.TryGotoNext(MoveType.AfterLabel,
                i => i.MatchLdloc0(),
                i => i.MatchLdfld(out compGen),
                i => i.MatchCallvirt<List<LocalizationFile>>("GetEnumerator"),
                i => i.MatchStloc(out _)
                ) || compGen.Name != "baseLocalizationFiles") // check if the name match
            {
                throw new Exception(":O");
            }

            c.EmitLdloc(cultureVar);
            c.EmitStsfld(typeof(LocalizationTweaks).GetField(nameof(_currentProcessedCulture), BindingFlags.Static | BindingFlags.NonPublic));

            */

            MethodInfo move = typeof(File).GetMethod("Move", [typeof(string), typeof(string)]);
            PropertyInfo getTMLprop = typeof(Logging).GetProperty("tML", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo getTML = getTMLprop.GetGetMethod(true);

            if (!c.TryGotoNext(i => i.MatchCall(getTML)))
            {
                Logging.tML.Warn("[MoreLocales] FixPeskyLegacyMarking: Couldn't find start of legacy marking");
                return;
            }

            var skipLabel = il.DefineLabel();

            c.EmitLdarg0();

            c.EmitDelegate<Func<Mod, bool>>(m =>
            {
                return m.Name == ModContent.GetInstance<MoreLocales>().Name;
            });

            c.EmitBrtrue(skipLabel);

            if (!c.TryGotoNext(MoveType.After, i => i.MatchCall(move)))
            {
                Logging.tML.Warn("[MoreLocales] FixPeskyLegacyMarking: Couldn't find branch target");
                return;
            }

            c.MarkLabel(skipLabel);
        }
        internal static void Unapply()
        {

        }
    }
}
