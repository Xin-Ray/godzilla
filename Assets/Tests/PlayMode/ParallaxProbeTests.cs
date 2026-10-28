using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Godzilla.Core;
using Godzilla.Placement;

namespace Godzilla.Tests
{
    /// <summary>
    /// T-MVP-003 (editor half): render the view through the window from three head positions 0.5 m apart.
    /// The window hole edge must move a lot on screen; the 100 m giant 320 m away must barely move.
    /// </summary>
    public class ParallaxProbeTests
    {
        const string TestId = "T-MVP-003";
        const int W = 640, H = 480;

        [UnityTest]
        public IEnumerator Giant_IsStableWhileWindowEdgeShifts()
        {
            SceneManager.LoadScene("City");
            yield return null;
            var window = Object.FindFirstObjectByType<WindowCalibration>();
            var placement = Object.FindFirstObjectByType<CharacterPlacement>();
            var city = Object.FindFirstObjectByType<CitySpaceManager>();
            float t = 0f;
            while (!window.Ready && t < 10f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(window.Ready);
            Assert.AreEqual(FrameMode.Window, window.Mode);

            var d = PlacementData.Default(); // 100 m tall, 320 m out, drop 120 m
            d.anchorUuid = window.AnchorUuid; d.frameMode = "Window";
            placement.Set(d);
            window.SetOccluderVisible(true); // cyan walls so the hole edge is measurable
            yield return null; yield return null;

            // Probe: 1.5 m inside the room from the window centre, looking straight out.
            var f = city.Frame;
            var winCenter = f.Origin + Vector3.up * 1.5f; // frame origin = window centre projected to the floor
            var probeGo = new GameObject("Probe");
            var cam = probeGo.AddComponent<Camera>();
            cam.fieldOfView = 60f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 3000f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;

            var results = new List<(float dx, float giantX, float holeX)>();
            foreach (var dx in new[] { -0.5f, 0f, 0.5f })
            {
                cam.transform.position = winCenter - f.Forward * 1.5f + f.Right * dx;
                cam.transform.rotation = Quaternion.LookRotation(f.Forward, Vector3.up);
                yield return null;
                var tex = ProbeCapture.Capture(cam, W, H);
                ProbeCapture.Save(tex, TestId, "parallax_dx" + (dx < 0 ? "m" : "p") + Mathf.Abs(dx).ToString("0.0"));
                var giantX = Centroid(tex, IsGiant);
                var holeX = HoleCenterAtRow(tex, H / 2);
                Debug.Log("[Evidence] dx=" + dx + " giantX=" + giantX + " holeX=" + holeX);
                results.Add((dx, giantX, holeX));
                Object.Destroy(tex);
            }

            Assert.IsTrue(results.TrueForAll(r => r.giantX >= 0f), "giant visible in every frame");
            Assert.IsTrue(results.TrueForAll(r => r.holeX >= 0f), "window hole measurable in every frame");
            float holeShift = Mathf.Abs(results[2].holeX - results[0].holeX);
            float giantShift = Mathf.Abs(results[2].giantX - results[0].giantX);
            Debug.Log("[Evidence] holeShift=" + holeShift + "px giantShift=" + giantShift + "px");
            Assert.Greater(holeShift, 40f, "0.5 m head sway moves a 1.5 m-away window edge a lot");
            Assert.Less(giantShift, 0.1f * holeShift, "a 320 m-away giant barely moves relative to the hole");
            Assert.Less(results[2].holeX, results[0].holeX, "hole slides left on screen when the head moves right");

            // --- Occlusion: with the real depth-only occluder (debug off) the giant must only appear inside the hole.
            // Measure the hole rect from the cyan frame first, then switch to the real occluder and count giant pixels outside it.
            cam.transform.position = winCenter - f.Forward * 1.5f;
            cam.transform.rotation = Quaternion.LookRotation(f.Forward, Vector3.up);
            yield return null;
            var cyanTex = ProbeCapture.Capture(cam, W, H);
            var hole = HoleRect(cyanTex, Mathf.RoundToInt(results[1].giantX));
            Object.Destroy(cyanTex);
            Assert.Greater(hole.width, 50f, "hole rect measurable");

            window.SetOccluderVisible(false);
            yield return null; yield return null;
            var occTex = ProbeCapture.Capture(cam, W, H);
            ProbeCapture.Save(occTex, TestId, "occlusion_debug_off");
            int inside = 0, outside = 0;
            var px = occTex.GetPixels32();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (IsGiant(px[y * W + x])) { if (hole.Contains(new Vector2(x, y))) inside++; else outside++; }
            Debug.Log("[Evidence] occlusion: giant px inside hole=" + inside + " outside=" + outside + " hole=" + hole);
            Object.Destroy(occTex);
            Assert.Greater(inside, 200, "giant visible through the window");
            Assert.AreEqual(0, outside, "no giant pixels outside the window hole: walls occlude");
        }

        /// <summary>
        /// Window hole rect (texture coords, y up): vertical extent = the non-cyan run through the giant's column,
        /// horizontal extent = the non-cyan run along that run's middle row. Both runs must be bounded by cyan.
        /// </summary>
        static Rect HoleRect(Texture2D tex, int giantColumn)
        {
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            bool Cyan(int x, int y) => IsCyan(px[y * w + x]);
            // vertical run containing the giant (search from the image centre row upwards/downwards)
            int cy = h / 2;
            int x0 = Mathf.Clamp(giantColumn, 0, w - 1);
            if (Cyan(x0, cy)) return new Rect();
            int top = cy; while (top + 1 < h && !Cyan(x0, top + 1)) top++;
            int bottom = cy; while (bottom - 1 >= 0 && !Cyan(x0, bottom - 1)) bottom--;
            if (top >= h - 1 || bottom <= 0) return new Rect(); // not bounded by cyan
            int midRow = (top + bottom) / 2;
            int right = x0; while (right + 1 < w && !Cyan(right + 1, midRow)) right++;
            int left = x0; while (left - 1 >= 0 && !Cyan(left - 1, midRow)) left--;
            if (right >= w - 1 || left <= 0) return new Rect();
            return Rect.MinMaxRect(left, bottom, right + 1, top + 1);
        }

        static bool IsGiant(Color32 c) => c.r > 200 && c.g > 200 && c.b > 200;
        static bool IsCyan(Color32 c) => c.b > 120 && c.g > 120 && c.r < 110;

        static float Centroid(Texture2D tex, System.Func<Color32, bool> pred)
        {
            var px = tex.GetPixels32();
            long sx = 0; int n = 0;
            for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                    if (pred(px[y * tex.width + x])) { sx += x; n++; }
            return n == 0 ? -1f : (float)sx / n;
        }

        /// <summary>Centre x of the first non-cyan run bounded by cyan on the left, on one row; -1 if none.</summary>
        static float HoleCenterAtRow(Texture2D tex, int row)
        {
            var px = tex.GetPixels32();
            int w = tex.width;
            int x = 0;
            while (x < w && !IsCyan(px[row * w + x])) x++;
            while (x < w && IsCyan(px[row * w + x])) x++;
            if (x >= w) return -1f;
            int start = x;
            while (x < w && !IsCyan(px[row * w + x])) x++;
            int end = x - 1;
            return (start + end) * 0.5f;
        }
    }
}
