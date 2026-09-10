using System;
using System.IO;
using Bloodhound.Engine;
using GridInfect.Core;
using UnityEngine;

namespace GridInfect.Game
{
    // The level cache's file: boards the device has already generated, so a
    // daily warmed yesterday is still instant today. Unreadable or stale
    // (another generator version) starts empty; nothing here is truth, the
    // seed math is.
    public sealed class LevelCachePort
    {
        readonly string _path;

        public LevelCachePort(string directory)
        {
            _path = Path.Combine(directory, "gridinfect_levels.json");
        }

        // Ahead of every prefetch: nothing may generate a board the file
        // already holds for want of having read it yet.
        const int LoadPriority = -1000;

        // The read, on the worker; `then` runs on the main thread once it
        // has landed (Work.Pump, from GameApp.Update). This is the one cost
        // at boot that grows with play — up to LevelCache.Capacity boards of
        // JSON — and the only reason the game would ever need a loading
        // screen in front of its first frame. It does not: the first frame
        // goes up on an empty cache and the file catches up behind it.
        public void LoadAsync(LevelCache cache, Action then)
        {
            Work.Shared.Run(() =>
            {
                Load(cache);
                return true;
            }, LoadPriority, "levels:load", _ => then());
        }

        public void Load(LevelCache cache)
        {
            try
            {
                if (File.Exists(_path)) cache.Load(File.ReadAllText(_path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[levels] unreadable cache, starting empty: {e.Message}");
            }
        }

        public void SaveIfDirty(LevelCache cache)
        {
            if (!cache.Dirty) return;
            cache.Dirty = false;
            try
            {
                File.WriteAllText(_path, cache.ToJson());
            }
            catch (Exception e)
            {
                cache.Dirty = true;
                Debug.LogWarning($"[levels] write failed (will retry): {e.Message}");
            }
        }
    }
}
