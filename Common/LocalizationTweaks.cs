using Hjson;
using Mono.Cecil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria.Localization;
using Terraria.ModLoader;
using static Terraria.ModLoader.LocalizationLoader;

namespace MoreLocales.Common
{
    public static class LocalizationTweaks
    {
        internal static void Apply()
        {
            Type[] mParams =
            [           
                typeof(Mod),
                typeof(string),
                typeof(GameCulture)
            ];
            MethodInfo peskyLegacyMarker = typeof(LocalizationLoader).GetMethod("UpdateLocalizationFilesForMod", BindingFlags.Static | BindingFlags.NonPublic, mParams);
            MonoModHooks.Modify(peskyLegacyMarker, FixPeskyLegacyMarking);

            MethodInfo nested = typeof(LocalizationLoader).GetMethod("LocalizationFileToHjsonText", BindingFlags.Static | BindingFlags.NonPublic);
            MonoModHooks.Modify(nested, FixHjsonToStringMethod);
        }
        internal static void PlaceCommentAboveNewEntryNew(LocalizationEntry entry, CommentedWscJsonObject parent, Dictionary<string, string> localizationsForCulture)
        {
            // the original method doesn't take the dictionary as a parameter
            // so i replace all calls to the method inside LocalizationFileToHjsonText with this one

            string sub = LangUtils.Substitute(entry.comment, entry.key, localizationsForCulture);

            /*
            Console.WriteLine("--DICTSTART--");
            foreach (var kvp in localizationsForCulture)
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            Console.WriteLine("--DICTEND--");
            */
            //Console.WriteLine(sub);

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
        private static readonly FieldInfo _entries = typeof(Dictionary<string, JsonValue>).GetField("_entries", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo _key = typeof(Dictionary<,>).GetNestedType("Entry", BindingFlags.NonPublic).MakeGenericType([typeof(string), typeof(JsonValue)]).GetField("key");
        private static void FixHjsonToStringMethod(ILContext il)
        {
            MethodReference newCommentMethod = il.Import(typeof(LocalizationTweaks).GetMethod(nameof(PlaceCommentAboveNewEntryNew), BindingFlags.Static | BindingFlags.NonPublic));

            var c = new ILCursor(il);

            while (c.TryGotoNext(i => i.MatchCall(out MethodReference method) && method.FullName.Contains("PlaceCommentAboveNewEntry|")))
            {
                c.EmitLdarg1();
                c.Next.Operand = newCommentMethod;
                c.Index++;
            }
        }
        // fixes problem where tmod would mark the vanilla translation files as legacy since they don't have an en-US equivalent
        private static void FixPeskyLegacyMarking(ILContext il)
        {
            var c = new ILCursor(il);

            MethodInfo move = typeof(File).GetMethod("Move", [typeof(string), typeof(string)]);
            PropertyInfo getTMLprop = typeof(Logging).GetProperty("tML", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo getTML = getTMLprop.GetGetMethod(true);

            if (!c.TryGotoNext(i => i.MatchCall(getTML)))
            {
                Logging.tML.Warn("FixPeskyLegacyMarking: Couldn't find start of legacy marking");
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
                Logging.tML.Warn("FixPeskyLegacyMarking: Couldn't find branch target");
                return;
            }

            c.MarkLabel(skipLabel);
        }
        internal static void Unapply()
        {

        }
    }
}
