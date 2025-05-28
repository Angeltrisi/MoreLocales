using CsvHelper;
using ReLogic.Content;
using ReLogic.Content.Readers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Terraria.ModLoader.Assets;
using Terraria.ModLoader.Core;
using Terraria.WorldBuilding;

namespace MoreLocales.Utilities
{
    public static class AssetHelper
    {
        private static Mod mod;
        private static AssetRepository repo;
        internal static void Setup(Mod mod)
        {
            AssetHelper.mod = mod;
            repo = mod.Assets;
        }
        /// <summary>
        /// The provided path must already be clean.
        /// </summary>
        /// <returns></returns>
        public static Asset<DynamicSpriteFont> UnsafeRequestSpriteFont(string cleanPath)
        {
            Asset<DynamicSpriteFont> asset = null;

            lock (repo._requestLock)
            {
                asset = new Asset<DynamicSpriteFont>(cleanPath);
                repo._assets[cleanPath] = asset;
                var loadTask = LoadSpriteFontWithPotentialAsync(asset);
                asset.Wait = () => repo.SafelyWaitForLoad(asset, loadTask, tracked: true);
            }

            return asset;
        }
        public static async Task LoadSpriteFontWithPotentialAsync(Asset<DynamicSpriteFont> asset)
        {
            repo.TotalAssets++;
            Interlocked.Increment(ref repo._Remaining);

            var mainThreadCtx = new MainThreadCreationContext(new(asset, repo));

            TModContentSource source = (TModContentSource)mod.RootContentSource;
            XnbReader reader = (XnbReader)repo._readers._readersByExtension[".xnb"];

            await Task.Yield();

            if (Monitor.IsEntered(repo._requestLock) && !AssetRepository.IsMainThread)
                await Task.Yield();

            DynamicSpriteFont resultAsset;
            using (var stream = source.OpenStream(asset.Name + ".xnb"))
            {
                resultAsset = await reader.FromStream<DynamicSpriteFont>(stream, mainThreadCtx);
            }

            await mainThreadCtx;

            asset.SubmitLoadedContent(resultAsset, source);
            repo.LoadedAssets++;

            Interlocked.Decrement(ref repo._Remaining);
        }
    }
}
