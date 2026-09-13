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

        // A board landed: write the file. The JSON of up to Capacity boards
        // is built and written on the worker, not on the frame that noticed
        // (a burst of warm boards at boot would otherwise be a burst of
        // hitches); one write in flight at a time, and a board that lands
        // while one is in flight is picked up by the next call. Runs behind
        // whatever the player is waiting on and ahead of the idle warming.
        const int SavePriority = 5;
        bool _saving;

        public void SaveIfDirty(LevelCache cache)
        {
            if (!cache.Dirty || _saving) return;
            cache.Dirty = false;
            _saving = true;
            Work.Shared.Run(() => Write(cache), SavePriority, "levels:save", ok =>
            {
                _saving = false;
                if (!ok) cache.Dirty = true;
            });
        }

        // Leaving the process (pause, quit): the write happens here and now,
        // whatever is in flight — a job the worker has not reached yet will
        // not land before Android takes the process.
        public void SaveNow(LevelCache cache)
        {
            if (!cache.Dirty && !_saving) return;
            cache.Dirty = false;
            if (!Write(cache)) cache.Dirty = true;
        }

        bool Write(LevelCache cache)
        {
            try
            {
                File.WriteAllText(_path, cache.ToJson());
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[levels] write failed (will retry): {e.Message}");
                return false;
            }
        }
    }
}
