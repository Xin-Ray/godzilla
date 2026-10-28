using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Godzilla.Input;

namespace Godzilla.UI
{
    public class MenuRow
    {
        public string Label;
        public Func<string> Value;        // null = plain row
        public Action<float> Adjust;      // stick X (-1..1) per second, null = not adjustable
        public Action Activate;           // confirm (A)
        public static MenuRow Text(string label, Func<string> value = null) => new MenuRow { Label = label, Value = value };
        public static MenuRow Button(string label, Action activate) => new MenuRow { Label = label, Activate = activate };
        public static MenuRow Toggle(string label, Func<bool> get, Action<bool> set) =>
            new MenuRow { Label = label, Value = () => get() ? "ON" : "off", Activate = () => set(!get()), Adjust = v => { if (Mathf.Abs(v) > 0.5f) set(v > 0); } };
        public static MenuRow Slider(string label, Func<float> get, Action<float> set, float min, float max, float perSecond, string fmt = "0.##") =>
            new MenuRow { Label = label, Value = () => get().ToString(fmt), Adjust = v => set(Mathf.Clamp(get() + v * perSecond, min, max)) };
    }

    /// <summary>
    /// A world-space text panel navigated with the sticks: left stick Y = move cursor, left stick X = adjust, A = activate.
    /// No raycasting needed, so it works identically on Quest and in the editor sim.
    /// </summary>
    public class MenuPanel : MonoBehaviour
    {
        [SerializeField] Text text;
        [SerializeField] float repeatSeconds = 0.18f;

        readonly List<MenuRow> _rows = new List<MenuRow>();
        int _cursor;
        float _nextNav;
        string _title = "";
        public bool Visible => gameObject.activeSelf;
        public int Cursor => _cursor;
        public IReadOnlyList<MenuRow> Rows => _rows;

        public static MenuPanel Create(string name, Transform parent, Vector3 localPos, float widthM = 0.6f, float heightM = 0.45f, int fontSize = 18)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvas.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(widthM * 1000f, heightM * 1000f);
            go.transform.localScale = Vector3.one * 0.001f;

            var bg = new GameObject("BG").AddComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            bg.color = new Color(0f, 0f, 0f, 0.75f);
            Stretch(bg.rectTransform);

            var t = new GameObject("Text").AddComponent<Text>();
            t.transform.SetParent(go.transform, false);
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            Stretch(t.rectTransform, 16f);

            var panel = go.AddComponent<MenuPanel>();
            panel.text = t;
            go.SetActive(false);
            return panel;
        }

        static void Stretch(RectTransform rt, float pad = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(pad, pad); rt.offsetMax = new Vector2(-pad, -pad);
        }

        public void SetRows(string title, IEnumerable<MenuRow> rows)
        {
            _title = title;
            _rows.Clear();
            _rows.AddRange(rows);
            _cursor = Mathf.Clamp(_cursor, 0, Mathf.Max(0, _rows.Count - 1));
            Render();
        }

        public void Show(bool on) { gameObject.SetActive(on); if (on) Render(); }

        /// <summary>Feed one input frame. Returns true when the panel consumed the Confirm press.</summary>
        public bool Navigate(InputFrame i, float dt)
        {
            if (!Visible || _rows.Count == 0) return false;
            bool consumed = false;
            if (Time.unscaledTime >= _nextNav && Mathf.Abs(i.Move.y) > 0.6f)
            {
                _cursor = (_cursor - (int)Mathf.Sign(i.Move.y) + _rows.Count) % _rows.Count;
                _nextNav = Time.unscaledTime + repeatSeconds;
            }
            var row = _rows[_cursor];
            if (Mathf.Abs(i.Move.x) > 0.25f && row.Adjust != null) row.Adjust(i.Move.x * dt);
            if (i.ConfirmPressed && row.Activate != null) { row.Activate(); consumed = true; }
            Render();
            return consumed;
        }

        public void Render()
        {
            if (text == null) return;
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(_title)) sb.Append("<b>").Append(_title).Append("</b>\n");
            for (int k = 0; k < _rows.Count; k++)
            {
                var r = _rows[k];
                sb.Append(k == _cursor ? "> " : "  ").Append(r.Label);
                if (r.Value != null) sb.Append(": ").Append(r.Value());
                sb.Append('\n');
            }
            text.text = sb.ToString();
        }
    }
}
