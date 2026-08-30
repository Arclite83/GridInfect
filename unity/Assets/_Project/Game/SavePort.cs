using System;
using System.IO;
using GridInfect.Core;
using UnityEngine;

namespace GridInfect.Game
{
    /// <summary>
    /// Persistence adapter: write-through of the Profile after any dispatch
    /// that dirtied it (R-501). Format is SaveCodec's versioned JSON in
    /// Application.persistentDataPath (R-502); this class only moves strings.
    /// </summary>
    public sealed class SavePort
    {
        readonly string _path;

        public SavePort(string directory)
        {
            _path = Path.Combine(directory, "gridinfect_save.json");
        }

        public Profile Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    return SaveCodec.Load(File.ReadAllText(_path));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[save] unreadable save, starting fresh: {e.Message}");
            }
            return new Profile();
        }

        public void SaveIfDirty(Profile profile)
        {
            if (!profile.Dirty) return;
            try
            {
                File.WriteAllText(_path, SaveCodec.Save(profile));
                profile.Dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[save] write failed (will retry on next change): {e.Message}");
            }
        }
    }
}
