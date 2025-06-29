using Hjson;
using Mono.Cecil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Terraria.Localization;
using static Terraria.ModLoader.LocalizationLoader;

namespace MoreLocales.Common
{
    public static class LocalizationTweaks
    {
        private static ILHook nestedMethodHook;
        private static MethodReference terriblyUnperformantMethod;
        //private static MethodReference testingMethodDifference;
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
            // i cannot use MonoModHooks.Modify because i need to get the value for the methodref immediately
            nestedMethodHook = new ILHook(nested, LookForActualMethod, true);

            MonoModHooks.Modify(terriblyUnperformantMethod.ResolveReflection(), DontUseLINQForAGiganticDictionary);
            /*
            MonoModHooks.Add(testingMethodDifference.ResolveReflection(),
            static
            (Func<LocalizationFile, LocalizationEntry, string> orig, LocalizationFile baseLocalizationFileEntry, LocalizationEntry entry) => 
            {
                Console.WriteLine(entry.key);
                Console.WriteLine(baseLocalizationFileEntry.prefix);
                return orig(baseLocalizationFileEntry, entry);
            });
            */
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
                Logging.tML.Warn("DontUseLINQForAGiganticDictionary: Couldn't find place to remove instructions from");
                return;
            }
            c.RemoveRange(2); // lol
            EmitJsonObjectLastKey(c);
        }
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
            nestedMethodHook.Dispose();
            nestedMethodHook = null;
        }
    }
}
