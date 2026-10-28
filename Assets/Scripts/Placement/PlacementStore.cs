using System;
using System.IO;
using UnityEngine;

namespace Godzilla.Placement
{
    public static class PlacementStore
    {
        public enum LoadResult { None, Restored, NeedsPlacement }

        const string FileName = "placement.json";

        public static string PathFor(string dir) => Path.Combine(dir, FileName);

        public static void Save(PlacementData d, string dir)
        {
            d.savedAtUtc = DateTime.UtcNow.ToString("o");
            Directory.CreateDirectory(dir);
            File.WriteAllText(PathFor(dir), JsonUtility.ToJson(d, true));
        }

        public static PlacementData LoadOrNull(string dir)
        {
            var p = PathFor(dir);
            if (!File.Exists(p)) return null;
            try { return JsonUtility.FromJson<PlacementData>(File.ReadAllText(p)); }
            catch (Exception e) { Debug.LogWarning($"[Place] unreadable {p}: {e.Message}"); return null; }
        }

        public static LoadResult Load(string dir, string currentAnchorUuid, out PlacementData data)
        {
            var loaded = LoadOrNull(dir);
            if (loaded == null || loaded.version != PlacementData.CurrentVersion)
            {
                if (loaded != null) Debug.LogWarning($"[Place] placement.json version {loaded.version} unsupported; using defaults");
                data = PlacementData.Default();
                return LoadResult.None;
            }
            data = loaded.Sanitize();
            return loaded.anchorUuid == currentAnchorUuid ? LoadResult.Restored : LoadResult.NeedsPlacement;
        }
    }
}
