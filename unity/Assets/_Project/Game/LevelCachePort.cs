using System;
using System.IO;
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
