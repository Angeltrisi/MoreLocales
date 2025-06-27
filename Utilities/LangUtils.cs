using System;
using Terraria.Localization;
using Terraria.ModLoader.Core;

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
    public static class LangUtils
    {
        /// <summary>
        /// Allows you to add a comment before a localization key in a localization file in this fashion:<para/>
        /// <code>
        /// // My super cool comment!
        /// Key: Value
        /// </code>
        /// <para/>
        /// If you want to include localized values in your comment, you can use the substitution format like usual: <c>{$KeyHere}</c><br/>
        /// MoreLocales extends the functionality of the format so that if found in comments, it is replaced with the actual localized value.<br/>
        /// This is how MoreLocales' inflection data localization file works to add the helpful <c># DisplayName</c> comments.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="commentType"></param>
        /// <returns>Whether or not the comment was successfully added.</returns>
        public static bool AddComment(string key, string value, HjsonCommentType commentType = HjsonCommentType.Slashes)
        {
            if (!Language.Exists(key))
                return false;

            // LocalizationLoader.FindHJSONFileForKey
            return true;
        }
        /// <summary>
        /// Returns an array of the files inside the given mod which are considered localization files by tModLoader (those with the .hjson extension).<para/>
        /// If the given mod's file has already been closed (for example, during <see cref="Mod.Unload"/>) this will return null.
        /// </summary>
        /// <param name="mod">The mod to fetch localization files from.</param>
        /// <param name="onlyBase">Whether or not only base localization files (en-US) should be returned.<para/>
        /// This is <see langword="false"/> by default, which means localization files from all cultures will be returned.</param>
        public static TmodFile.FileEntry[] GetLocalizationFiles(Mod mod, bool onlyBase = false)
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
    }
}
