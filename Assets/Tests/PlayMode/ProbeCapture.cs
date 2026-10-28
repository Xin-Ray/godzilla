using System.IO;
using UnityEngine;

namespace Godzilla.Tests
{
    /// <summary>Renders a camera to a PNG under docs/validation/evidence/&lt;testId&gt;/ so a human (and the agent) can look at it.</summary>
    public static class ProbeCapture
    {
        public static string EvidenceDir(string testId)
        {
            var dir = Path.Combine(Application.dataPath, "..", "docs", "validation", "evidence", testId);
            Directory.CreateDirectory(dir);
            return Path.GetFullPath(dir);
        }

        public static Texture2D Capture(Camera cam, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            var prev = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prev;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prevActive;
            rt.Release();
            Object.Destroy(rt);
            return tex;
        }

        public static string Save(Texture2D tex, string testId, string name)
        {
            var path = Path.Combine(EvidenceDir(testId), name + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Debug.Log($"[Evidence] {path}");
            return path;
        }
    }
}
