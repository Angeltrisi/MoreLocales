using Hjson;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader.Core;
using static Terraria.ModLoader.LocalizationLoader;

namespace MoreLocales.Utilities
{
    public enum HjsonCommentType
    {
        /// <summary>
        /// // This is my comment!
        /// </summary>
        Slashes,
        /// <summary>
        /// # This is my comment!
        /// </summary>
        Hash,
    }
    /// <summary>
    /// Contains various helper methods for working with a mod's localization files.<para/>
    /// Many of the methods seen here are actually parts of <see cref="LocalizationLoader.UpdateLocalizationFilesForMod(Mod, string, GameCulture)"/>, which may or may not be more performant than the original versions.<br/>
    /// For whatever reason, the method I mention above does not separate potentially helpful logic into other methods, instead choosing to do it all in a single method.
    /// </summary>
    public static class LangUtils
    {
        public static bool FilesWillBeReloadedDueToCommentsChange { get; internal set; }
        public record struct QueuedComment(Mod Mod, string Key, string Comment, HjsonCommentType CommentType, bool OverwriteComment);
        private static readonly HashSet<Mod> _probablyValidMods = [];
        private static readonly ConcurrentQueue<QueuedComment> _commentsQueue = [];
        internal static readonly Dictionary<GameCulture, Dictionary<string, string>[]> _flattenedCache = [];
        private static GameCulture[] _vanillaCultures;
        public static GameCulture[] VanillaCultures
        {
            get
            {
                if (_vanillaCultures != null)
                    return _vanillaCultures;
                _vanillaCultures = new GameCulture[(int)GameCulture.CultureName.Polish];
                for (int i = 0; i < _vanillaCultures.Length; i++)
                {
                    _vanillaCultures[i] = GameCulture.FromLegacyId(i + 1);
                }
                return _vanillaCultures;
            }
        }
        internal static void ConsumeCommentsQueue()
        {
            // if anyone's reading this pls tell me if i'm stupid
            // can more than one mod even add comments at the same time?? if not then i have no idea why i'm doing this

            if (!_commentsQueue.IsEmpty)
                FilesWillBeReloadedDueToCommentsChange = true;
            else
                return;

            Dictionary<Mod, List<(string key, string comment, HjsonCommentType commentType, bool overwriteComment)>> batches = [];

            while (_commentsQueue.TryDequeue(out var comment))
            {
                if (!batches.ContainsKey(comment.Mod))
                    batches[comment.Mod] = [];
                batches[comment.Mod].Add((comment.Key, comment.Comment, comment.CommentType, comment.OverwriteComment));
            }

            foreach (var kvp in batches)
            {
                var mod = kvp.Key;

                // get all the files in the mod
                var filesArray = mod.GetLocalizationFiles(true);
                // allocate a list of the same length as the array, convert the file entries to localization files and add them to the list
                List<LocalizationFile> filesList = new(filesArray.Length);
                for (int i = 0; i < filesArray.Length; i++)
                {
                    filesList.Add(filesArray[i].ToLocalizationFile(mod));
                }

                Dictionary<LocalizationFile, List<(string key, string comment, HjsonCommentType commentType, bool overwriteComment)>> fileActions = [];

                foreach (var fileAction in CollectionsMarshal.AsSpan(kvp.Value))
                {
                    // find the .hjson file that contains the given key
                    var file = FindHJSONFileForKey(filesList, fileAction.key);

                    if (!fileActions.ContainsKey(file))
                        fileActions[file] = [];
                    fileActions[file].Add(fileAction);
                }

                foreach (var kvp2 in fileActions)
                {
                    var file = kvp2.Key;

                    foreach (var (key, comment, commentType, overwriteComment) in kvp2.Value)
                    {
                        // find the entry. if it's not found (somehow???), exit
                        if (!file.TryGetEntry(key, out var entry))
                            continue;

                        // get a ref to the entry for reassignment
                        ref LocalizationEntry entryRef = ref entry.Value;

                        // find out comment style
                        string finalComment;
                        if (overwriteComment)
                        {
                            finalComment = (commentType == HjsonCommentType.Slashes ? "// " : "# ") + comment;
                        }
                        else
                        {
                            finalComment = entryRef.comment + comment;
                        }
                        // do not freeze the comment here because then it won't change when it needs to,
                        // instead freeze it during LocalizationLoader.UpdateLocalizationFilesForMod
                        // (which is called automatically since tMod detects file writing with the file watchers)

                        // reassign
                        entryRef =
                            new(entryRef.key,
                                entryRef.value,
                                finalComment,
                                entryRef.type);
                    }
                }

                foreach (var finalAction in fileActions.Keys)
                {
                    // write to disk
                    finalAction.WriteToDisk(filesList, mod.SourceFolder, GameCulture.DefaultCulture);
                }
            }
        }
        /// <summary>
        /// Attempts to add a comment before a localization key in a localization file in this fashion:<para/>
        /// <code>
        /// // My super cool comment!
        /// Key: Value
        /// </code>
        /// </summary>
        /// <param name="key">The key of the localization entry to add a comment to.</param>
        /// <param name="suffix">The key of the localization entry to add a comment to, not including the 'Mods.ModName' prefix.</param>
        /// <param name="comment">The comment to add.</param>
        /// <param name="commentType">The style of the comment (which Hjson comment delimiter to use).</param>
        /// <param name="overwriteComment">Whether the comment should be overwritten or added to. Defaults to true (overwrite).</param>
        /// <returns>
        /// Whether or not the comment was successfully added.<para/>
        /// Adding comments will fail if:
        /// <list type="bullet">
        /// <item>The given key doesn't exist.</item>
        /// <item>The mod associated with the key does not have its <see cref="TmodFile"/> open (during <see cref="Mod.Unload"/> for example)</item>
        /// <item>The mod associated with the key is not locally built.</item>
        /// </list>
        /// </returns>
        public static bool AddComment(string key, string comment, HjsonCommentType commentType = HjsonCommentType.Slashes, bool overwriteComment = true)
        {
            // currently can't be added to categories. make that possible

            if (!Language.Exists(key))
                return false;

            string[] parts = key.Split('.');

            // check if this is a modded key. if it's not, there's no mod to do stuff with, so exit
            if (parts[0] != "Mods")
                return false;

            // check if the mod exists, has an associated file, and that file is open. on failure to verify, exit
            if (!ModLoader.TryGetMod(parts[1], out var mod) || mod.File == null || !mod.File.IsOpen)
                return false;

            // skip looking for files if we already know those files exist
            if (!_probablyValidMods.Contains(mod))
            {
                // check if the source folder exists on disk. if it doesn't, exit
                if (!Directory.Exists(mod.SourceFolder))
                    return false;

                // check if the locally built version of the tmod file exists. if it doesn't, exit
                string localBuiltTModFile = Path.Combine(ModLoader.ModPath, mod.Name + ".tmod");
                if (!File.Exists(localBuiltTModFile))
                    return false;
            }
            // mark the files as existing
            _probablyValidMods.Add(mod);

            // add the comment request to a queue
            // this is needed because operating systems seemingly do not enjoy the same file being accessed like a hundred times in the same frame
            _commentsQueue.Enqueue(new(mod, key, comment, commentType, overwriteComment));

            return true;
        }
        /// <inheritdoc cref="AddComment(string, string, HjsonCommentType, bool)"/>
        public static bool AddComment(this Mod mod, string suffix, string comment, HjsonCommentType commentType = HjsonCommentType.Slashes, bool overwriteComment = true)
        {
            return AddComment(mod.GetLocalizationKey(suffix), comment, commentType, overwriteComment);
        }
        /// <summary>
        /// Formats a string containing a substitution (e.g. <c>'{$Mods.ExampleMod.ExampleSubstitution}'</c>) with the key that's inside it,<br/>
        /// optionally with a pre-defined scope and/or lookup table.
        /// </summary>
        /// <param name="containsSubstitution"></param>
        /// <param name="scope"></param>
        /// <param name="specificSearch"></param>
        /// <returns>The string with the correct substituted value, or the original string if the given key isn't found.</returns>
        public static string Substitute(string containsSubstitution, string scope = null, Dictionary<string, string> specificSearch = null)
        {
            string result = LocalizationLoader.referenceRegex.Replace(containsSubstitution, (Match match) =>
            {
                HashSet<string> keysCollection;
                if (specificSearch is null)
                {
                    var dict = LanguageManager.Instance._localizedTexts;
                    int count = dict.Count;

                    specificSearch = new(count);
                    keysCollection = new(count);

                    foreach (var kvp in dict)
                    {
                        keysCollection.Add(kvp.Key);
                        specificSearch.Add(kvp.Key, kvp.Value.Value);
                    }
                }
                else
                {
                    keysCollection = new(specificSearch.Count);

                    foreach (var kvp in specificSearch)
                    {
                        keysCollection.Add(kvp.Key);
                    }
                }

                string validKey = FindKeyInScope(match.Groups[1].Value, scope, keysCollection);

                if (validKey is null)
                {
                    return match.Value; // don't replace at all if it's not a valid key
                }

                return specificSearch[validKey];
            });

            return result;
        }
        /// <summary>
        /// Finds and returns a valid key given a key in any scope and a scope.<para/>
        /// Original is a nested method inside <see cref="LanguageManager.ProcessCopyCommandsInTexts"/> for some reason.
        /// </summary>
        /// <param name="key">A key in any scope.</param>
        /// <param name="scope">A scope to search in.</param>
        /// <param name="specificSearch">A specific lookup table. If left null, <see cref="LanguageManager._localizedTexts"/> will be used.</param>
        /// <returns>A valid localization key, or <see langword="null"/> if one isn't found.</returns>
        public static string FindKeyInScope(string key, string scope, HashSet<string> specificSearch = null)
        {
            specificSearch ??= [.. LanguageManager.Instance._localizedTexts.Keys];

            if (specificSearch.Contains(key))
                return key;

            string[] splitKey = scope.Split('.');
            for (int i = splitKey.Length - 1; i >= 0; i--)
            {
                string combinedKey = string.Join('.', splitKey.Take(i + 1)) + '.' + key;
                if (specificSearch.Contains(combinedKey))
                {
                    return combinedKey;
                }
            }
            // change: returns null instead of the original key
            return null;
        }
        /// <summary>
        /// Returns an array of the files inside the given mod which are considered localization files by tModLoader (those with the .hjson extension).<para/>
        /// If the given mod's file has already been closed (for example, during <see cref="Mod.Unload"/>) this will return null.
        /// </summary>
        /// <param name="mod">The mod to fetch localization files from.</param>
        /// <param name="onlyBase">Whether or not only base localization files (en-US) should be returned.<para/>
        /// This is <see langword="false"/> by default, which means localization files from all cultures will be returned.</param>
        public static TmodFile.FileEntry[] GetLocalizationFiles(this Mod mod, bool onlyBase = false)
        {
            TmodFile file = mod.File;

            if (!file.IsOpen)
                return null;

            // allocate the span for comparison
            ReadOnlySpan<char> hjsonExtension = ['.', 'h', 'j', 's', 'o', 'n'];

            // it is preferable to overestimate and only have to resize once
            var arr = new TmodFile.FileEntry[file.Count];
            // actual localization file count
            int j = 0;
            for (int i = 0; i < file.Count; i++)
            {
                TmodFile.FileEntry entry = file.fileTable[i];
                string path = entry.Name;
                // check if the file name contains "en-US" if we only want the base localization files. if not, skip
                // for Contains, string.Contains(string) is actually fastest
                // note: initing "en-US" outside the loop might actually be worse for performance due to the ternary branch-
                // -because constants like this are interned. only using the bool once then is the best decision
                if (onlyBase && !path.Contains("en-US"))
                    continue;
                // check if the file name ends with .hjson
                if (path.AsSpan().EndsWith(hjsonExtension))
                    arr[j++] = entry;
            }

            // resize to not give null entries
            Array.Resize(ref arr, j);

            return arr;
        }
        /// <summary>
        /// Returns the embedded resource paths for all vanilla localization files for a certain culture.<para/>
        /// These can be read using <see cref="Utils.ReadEmbeddedResource(string)"/> if you wish to parse them yourself or for whatever other reason.<br/>
        /// However, you may also use <see cref="ParseVanillaLanguageFile(string)"/> to quickly parse them. <para/>
        /// To save yourself the trouble, you can also use the sister methods <see cref="GetVanillaLanguageFilesForCultureParsed(GameCulture)"/> and <see cref="GetVanillaLanguageFilesForCultureFlattened(GameCulture)"/> depending on your needs.
        /// </summary>
        /// <param name="culture">The culture whose language code should be used to look for embedded vanilla .json files.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string[] GetVanillaLanguageFilesForCulture(GameCulture culture)
        {
            if (culture.IsCustom())
                throw new InvalidOperationException("You cannot get an embedded language file from a custom culture. Utilize the Mod.GetLocalizationFiles method and/or other LangUtils helpers.");
            return LanguageManager.Instance.GetLanguageFilesForCulture(culture);
        }
        /// <summary>
        /// Returns all vanilla localization files for a given culture, returned as the direct Json deserialization of said files.<para/>
        /// <see cref="GetVanillaLanguageFilesForCultureFlattened(GameCulture)"/> may be more helpful.<br/>
        /// (read <see cref="FlattenVanillaLanguageDict(Dictionary{string, Dictionary{string, string}})"/>'s docs for more info)
        /// </summary>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string, string>>[] GetVanillaLanguageFilesForCultureParsed(GameCulture culture)
        {
            var arr = GetVanillaLanguageFilesForCulture(culture);
            Dictionary<string, Dictionary<string, string>>[] result = new Dictionary<string, Dictionary<string, string>>[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                result[i] = ParseVanillaLanguageFile(arr[i]);
            }
            return result;
        }
        /// <summary>
        /// Returns all vanilla localization files for a given culture, formatted as string-string dictionaries.
        /// </summary>
        /// <param name="culture">The culture whose language code should be used to look for embedded vanilla .json files.</param>
        /// <returns></returns>
        public static Dictionary<string, string>[] GetVanillaLanguageFilesForCultureFlattened(GameCulture culture)
        {
            if (_flattenedCache.TryGetValue(culture, out var value))
                return value;
            var arr = GetVanillaLanguageFilesForCulture(culture);
            Dictionary<string, string>[] result = new Dictionary<string, string>[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                result[i] = FlattenVanillaLanguageDict(ParseVanillaLanguageFile(arr[i]));
            }
            _flattenedCache[culture] = result;
            return result;
        }
        /// <summary>
        /// Allows you to get an array of the localization values for a given key, as long as all the target cultures are vanilla cultures.<para/>
        /// If you wish for something similar to this for modded cultures, scream at me in the Discord because I am so incredibly tired from writing all of this code<br/>
        /// or just impl it urself
        /// </summary>
        /// <param name="key"></param>
        /// <param name="targetCultures"></param>
        /// <returns></returns>
        public static string[] GetVanillaLocalizationValues(string key, params GameCulture[] targetCultures)
        {
            string[] values = new string[targetCultures.Length];
            for (int i = 0; i < targetCultures.Length; i++)
            {
                var culture = targetCultures[i];
                var dicts = GetVanillaLanguageFilesForCultureFlattened(culture);
                for (int j = 0; j < dicts.Length; j++)
                {
                    if (dicts[j].TryGetValue(key, out string real))
                        values[i] = real;
                }
            }
            return values;
        }
        /// <summary>
        /// Attempts to parse a vanilla localization file, and returns the result as a dictionary.<para/>
        /// The dictionary's keys are the topmost localization categories, e. g. <c>'ItemTooltip'</c>, <c>'MapObject'</c>, <c>'WorldGeneration'</c>, etc.<br/>
        /// The dictionary's values are dicts containing the actual keys (not including the topmost category), and the localization values for those keys.<para/>
        /// So, to access a localized value from this dictionary, for example, <c>'ItemTooltip.CopperAxe'</c>, you'd do <c>'dict["ItemTooltip"]["CopperAxe"]'</c>.<para/>
        /// If you wish to flatten this into a simpler string-string dictionary, use <see cref="FlattenVanillaLanguageDict(Dictionary{string, Dictionary{string, string}})"/>
        /// </summary>
        /// <param name="embeddedPath">The path for the embedded .json file. May be obtained using <see cref="GetVanillaLanguageFilesForCulture(GameCulture)"/>.</param>
        /// <returns></returns>
        public static Dictionary<string, Dictionary<string, string>> ParseVanillaLanguageFile(string embeddedPath)
        {
            string fileContents;
            try
            {
                fileContents = Utils.ReadEmbeddedResource(embeddedPath);
            }
            catch
            {
                return null;
            }

            if (fileContents == null || fileContents.Length < 2)
                return null;

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(fileContents);
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// Flattens a vanilla localization dictionary (like the one provided by <see cref="ParseVanillaLanguageFile(string)"/>) to a more workable format.<para/>
        /// If you read the example in the mentioned method, the same value could be accessed by using <c>'dict["ItemTooltip.CopperAxe"]'</c> with this dictionary.
        /// </summary>
        /// <param name="original"></param>
        /// <returns></returns>
        public static Dictionary<string, string> FlattenVanillaLanguageDict(Dictionary<string, Dictionary<string, string>> original)
        {
            Dictionary<string, string> result = new(original.Count); // doesn't hurt to start with a guaranteed number
            foreach (var kvp in original)
            {
                foreach (var kvp2 in kvp.Value)
                {
                    result.Add($"{kvp.Key}.{kvp2.Key}", kvp2.Value);
                }
            }
            return result;
        }
        /// <summary>
        /// Reads a file assuming UTF8 encoding and returns the result as a string.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string ReadFileUTF8(Mod mod, TmodFile.FileEntry file)
        {
            using Stream stream = mod.File.GetStream(file);
            using StreamReader reader = new(stream, Encoding.UTF8, true);
            return reader.ReadToEnd();
        }
        /// <summary>
        /// Attempts to parse a localization file and returns the result as a <see cref="WscJsonObject"/> if successful.<para/>
        /// Throws an exception on failure to parse.
        /// </summary>
        /// <param name="mod">The mod the file is from.</param>
        /// <param name="file">The file entry.</param>
        /// <exception cref="Exception"></exception>
        public static WscJsonObject ParseLocalizationFile(Mod mod, TmodFile.FileEntry file) => ParseLocalizationFile(ReadFileUTF8(mod, file));
        /// <inheritdoc cref="ParseLocalizationFile(Mod, TmodFile.FileEntry)"/>
        /// <param name="fileContents">The HJSON object as a string.</param>
        /// <exception cref="Exception"></exception>
        public static WscJsonObject ParseLocalizationFile(string fileContents)
        {
            try
            {
                return (WscJsonObject)HjsonValue.Parse(fileContents, new HjsonOptions { KeepWsc = true });
            }
            catch (Exception e)
            {
                throw new Exception("The localization file is malformed and couldn't be parsed: ", e);
            }
        }
        /// <summary>
        /// Turns a parsed .hjson file into a list of <see cref="LocalizationEntry"/>.
        /// </summary>
        /// <param name="jsonObjectEng">The parsed .hjson file (may be obtained through <see cref="ParseLocalizationFile(string)"/>)</param>
        /// <param name="prefix">The prefix (e.g. 'Mods.ExampleMod') associated with this localization file.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<LocalizationEntry> ParseLocalizationEntries(WscJsonObject jsonObjectEng, string prefix)
        {
            // i'm not sure whether to rewrite this method or not, or what the best way to do that is, so i just call the private method
            // i kinda wanna give LocalizationLoader.UpdateLocalizationFilesForMod some touch-ups at some point but it looks too messy to work with already lol
            return LocalizationLoader.ParseLocalizationEntries(jsonObjectEng, prefix);
        }
        /// <summary>
        /// Turns an appropriate <see cref="TmodFile.FileEntry"/> into its temporary <see cref="LocalizationFile"/> equivalent (used exclusively for changing the .hjson file in some way)
        /// </summary>
        /// <param name="entry">The file entry. It must be already associated with an .hjson file and it <b>must be a base localization file (en-US).</b></param>
        /// <param name="prefix">The prefix (e.g. 'Mods.ExampleMod') associated with this localization file.<para/>
        /// If left null, the most appropriate prefix will be retrieved and used.</param>
        /// <param name="entries">The individual localization entries that make up this file.</param>
        /// <param name="jsonObjectEng">The parsed .hjson file (may be obtained through <see cref="ParseLocalizationFile(string)"/>)</param>
        /// <param name="fileContents">The .hjson file contents (may be obtained through <see cref="ReadFileUTF8(Mod, TmodFile.FileEntry)"/>)</param>
        /// <param name="mod">The mod associated with the given file.</param>
        /// <returns></returns>
        public static LocalizationFile ToLocalizationFile(this TmodFile.FileEntry entry, List<LocalizationEntry> entries, string prefix = null)
        {
            if (prefix is null)
                if (!LocalizationLoader.TryGetCultureAndPrefixFromPath(entry.Name, out _, out prefix))
                    throw new Exception($"The provided file, {entry.Name}, is not a localization file.");
            return new(entry.Name, prefix, entries);
        }
        /// <inheritdoc cref="ToLocalizationFile(TmodFile.FileEntry, List{LocalizationEntry}, string)"/>
        public static LocalizationFile ToLocalizationFile(this TmodFile.FileEntry entry, WscJsonObject jsonObjectEng, string prefix = null)
        {
            if (prefix is null)
                if (!LocalizationLoader.TryGetCultureAndPrefixFromPath(entry.Name, out _, out prefix))
                    throw new Exception($"The provided file, {entry.Name}, is not a localization file.");
            return ToLocalizationFile(entry, ParseLocalizationEntries(jsonObjectEng, prefix), prefix);
        }
        /// <inheritdoc cref="ToLocalizationFile(TmodFile.FileEntry, List{LocalizationEntry}, string)"/>
        public static LocalizationFile ToLocalizationFile(this TmodFile.FileEntry entry, string fileContents, string prefix = null)
        {
            return ToLocalizationFile(entry, ParseLocalizationFile(fileContents), prefix);
        }
        /// <inheritdoc cref="ToLocalizationFile(TmodFile.FileEntry, List{LocalizationEntry}, string)"/>
        public static LocalizationFile ToLocalizationFile(this TmodFile.FileEntry entry, Mod mod, string prefix = null)
        {
            return ToLocalizationFile(entry, ReadFileUTF8(mod, entry), prefix);
        }
        /// <summary>
        /// Attempts to find a localization file from the given list that contains the given localization key.<para/>
        /// <b>Note: If a matching file isn't found, a new base file will be created (in memory, not in disk) and added to the list.</b>
        /// </summary>
        /// <param name="files">The list to search in.</param>
        /// <param name="key">The key to search for.</param>
        /// <returns>The matching localization file.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static LocalizationFile FindHJSONFileForKey(List<LocalizationFile> files, string key)
        {
            // same situation as LocalizationLoader.ParseLocalizationEntries
            return LocalizationLoader.FindHJSONFileForKey(files, key);
        }
        /// <summary>
        /// Finds the 'closeness' factor of the given key inside the given file sequentially.<br/>
        /// Closeness here meaning the amount of levels sequentially down the file where it still matches the given key.<para/>
        /// <b>Example:</b><para/>
        /// Let's say a file has a localization key <c>'Misc.CharacterDialogue.DialogueFirstTime'</c>, and we pass in the key <c>'Misc.CharacterDialogue.DialogueLastTime'</c>.<br/>
        /// In this example we would get <c>2</c>, because <c>Misc</c> matches, and <c>CharacterDialogue</c> also matches, but <c>DialogueFirstTime/LastTime</c> are different.<para/>
        /// <b>Note: The above example depends on the file's localization prefix.<br/>
        /// In the example we say the prefix is <see cref="string.Empty"/>, but we would get a higher number (<c>4</c>) if the prefix was <c>'Mods.ExampleMod'</c> for example.</b>
        /// </summary>
        /// <param name="file">The file to match in.</param>
        /// <param name="key">The key to match for. It must not include the file's localization prefix. (<see cref="LocalizationFile.prefix"/>)</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LongestMatchingPrefix(this LocalizationFile file, string key)
        {
            return LocalizationLoader.LongestMatchingPrefix(file, key);
        }
        /// <summary>
        /// Returns a fully formatted version of the given localization file as a valid Hjson string.
        /// </summary>
        /// <param name="file">The file to format as an Hjson string.</param>
        /// <param name="localizationsForCulture">A dictionary containing all relevant modded localization entries. (See <see cref="EntriesListToDictionary"/>)</param>
        /// <returns>The file formatted as an Hjson string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string LocalizationFileToHJSONText(LocalizationFile file, Dictionary<string, string> localizationsForCulture)
        {
            return LocalizationLoader.LocalizationFileToHjsonText(file, localizationsForCulture);
        }
        /// <summary>
        /// Returns a list of localization entries as a dictionary.
        /// </summary>
        /// <param name="entries">The entries to format as a dictionary.</param>
        /// <returns>The entries formatted as a dictionary.</returns>
        public static Dictionary<string, string> EntriesListToDictionary(List<LocalizationEntry> entries)
        {
            Dictionary<string, string> dict = new(entries.Count);

            foreach (var entry in CollectionsMarshal.AsSpan(entries))
                dict.Add(entry.key, entry.value);

            return dict;
        }
        /// <summary>
        /// Attempts to find the <see cref="LocalizationEntry"/> in <see cref="LocalizationFile.Entries"/> which exactly matches the given key.
        /// </summary>
        /// <param name="file">The file to search in.</param>
        /// <param name="key">The key to search for. May or may not include the file prefix (doesn't matter).</param>
        /// <param name="entry">A reference to the found entry. It is returned as <see cref="Ref{T}"/> so you can change it.</param>
        /// <returns>Whether or not the entry was found.</returns>
        public static bool TryGetEntry(this LocalizationFile file, string key, out Core.Ref<LocalizationEntry> entry)
        {
            // again, StartsWith and EndsWith are slightly faster if we use spans (though StringComparison.Ordinal comes close and is kinda the same thing)
            var prefixSpan = file.prefix.AsSpan();

            // if the user gave a key that already contains the prefix, sanitize it
            if (!key.AsSpan().StartsWith(prefixSpan))
                key = $"{file.prefix}.key"; // add 1 to take into account the '.' after the prefix

            // loop through a span of the entries and try to find an exact match
            foreach (ref LocalizationEntry e in CollectionsMarshal.AsSpan(file.Entries))
            {
                if (e.key == key)
                {
                    entry = new(ref e);
                    return true;
                }
            }

            // fail, so return null
            entry = new();
            return false;
        }
        /// <summary>
        /// Gets the appropriate path to write to for a specific culture, no matter the given file's original culture.
        /// </summary>
        /// <param name="file">The original file.</param>
        /// <param name="culture">The culture to adapt the path for.</param>
        /// <returns>The path adapted for the given culture.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetPathForCulture(LocalizationFile file, GameCulture culture)
        {
            return LocalizationLoader.GetPathForCulture(file, culture);
        }
        /// <summary>
        /// Attempts to write the temporary file's contents back to disk.<para/>
        /// Due to how tModLoader constructs the Hjson string, you also need to provide files from the same mod and culture as the target file.
        /// </summary>
        /// <param name="file">The file to write to disk.</param>
        /// <param name="culture">The culture the file belongs to. If left null, it will be searched for based on the file name.</param>
        /// <param name="outputFolder">The output folder. Make this <see cref="Mod.SourceFolder"/> if you have the mod's instance.</param>
        /// <param name="sameCultureFiles">Files belonging to the same culture as the target file.</param>
        /// <returns></returns>
        public static bool WriteToDisk(this LocalizationFile file, IEnumerable<LocalizationFile> sameCultureFiles, string outputFolder, GameCulture culture = null)
        {
            // gets all of the entries from every provided file, flattens them to a list, converts them to a dictionary,
            // and finally, replaces all line endings with their platform-appropriate version
            string hjson = LocalizationFileToHJSONText(file, EntriesListToDictionary([.. sameCultureFiles.SelectMany(f => f.Entries)])).ReplaceLineEndings();

            if (culture is null)
                if (!TryGetCultureAndPrefixFromPath(file.path, out culture, out _))
                    return false;

            string filePath = GetPathForCulture(file, culture);
            string finalPath = Path.Combine(outputFolder, filePath);

            File.WriteAllText(finalPath, hjson);

            return true;
        }
        // note: rewrite LocalizationLoader.TryGetCultureAndPrefixFromPath when tMod moves to .NET 10?
        // i tried right now, and, changing all the Split() and Replace() calls to use their char overloads only gives a small advantage over the original
        // .NET 10 has MemoryExtensions.Split() which allows for faster splitting and iteration
    }
}
