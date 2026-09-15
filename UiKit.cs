using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrickshotMenu
{
    internal static class UiKit
    {
        public static readonly Color Accent = new Color(0.29f, 0.94f, 1f, 1f);
        public static readonly Color Purple = new Color(0.68f, 0.55f, 1f, 1f);
        public static readonly Color Green = new Color(0.36f, 0.95f, 0.5f, 1f);
        public static readonly Color Gold = new Color(1f, 0.82f, 0.35f, 1f);
        public static readonly Color Red = new Color(0.95f, 0.45f, 0.5f, 1f);
        public static readonly Color TextHi = new Color(0.93f, 0.96f, 0.98f, 1f);
        public static readonly Color TextDim = new Color(0.62f, 0.68f, 0.74f, 1f);
        public static readonly Color PanelBg = new Color(0.045f, 0.06f, 0.085f, 0.965f);
        public static readonly Color PanelBorder = new Color(0.15f, 0.5f, 0.68f, 0.95f);

        private const float PanelW = 444f;
        private const float PadX = 18f;
        private const float RowGap = 3f;

        private static readonly Dictionary<string, Texture2D> RoundedCache = new Dictionary<string, Texture2D>();
        private static Texture2D _whiteTex;
        private static Texture2D _circleTex;
        private static Texture2D _radialTex;
        private static Texture2D _sweepTex;
        private static Texture2D _vignetteTex;
        private static Texture2D _logTex;

        private static GUIStyle _stTitle;
        private static GUIStyle _stH2;
        private static GUIStyle _stXL;
        private static GUIStyle _stTab;
        private static GUIStyle _stBody;
        private static GUIStyle _stSmall;
        private static GUIStyle _stMicro;
        private static GUIStyle _stBtn;
        private static GUIStyle _stLog;

        private static int _tab;
        private static float _y;
        private static float _contentX;
        private static float _pa = 1f;
        private static float _winOY;
        private static float _panelH;
        private static bool _measure;
        private static int _sliderIdCounter;
        private static int _dragId = -1;
        private static Vector2 _logScroll;
        private static Vector2 _guideScroll;
        private static bool _stylesReady;

        private sealed class ToastEntry
        {
            public string Message;
            public Color Color;
            public float Start;
        }

        private static readonly List<ToastEntry> Toasts = new List<ToastEntry>();

        public static void Toast(string message, Color color)
        {
            Toasts.Add(new ToastEntry { Message = message, Color = color, Start = Time.time });
            while (Toasts.Count > 4)
            {
                Toasts.RemoveAt(0);
            }
        }

        private static Color T(Color c, float extra = 1f)
        {
            return new Color(c.r, c.g, c.b, c.a * _pa * extra);
        }

        private static void DrawTex(Rect r, Texture2D t, Color color)
        {
            if (_measure)
            {
                return;
            }
            GUI.color = color;
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill);
            GUI.color = Color.white;
        }

        private static void Fill(Rect r, Color color)
        {
            if (_whiteTex == null)
            {
                _whiteTex = GetWhite(2, 2);
            }
            DrawTex(r, _whiteTex, color);
        }

        private static void Label(GUIStyle st, string text, Rect r, Color color)
        {
            if (_measure)
            {
                return;
            }
            st.normal.textColor = T(color);
            GUI.Label(r, Fit(st, text, r.width), st);
        }

        private static string Fit(GUIStyle st, string text, float maxWidth)
        {
            if (maxWidth <= 0f || st.CalcSize(new GUIContent(text)).x <= maxWidth)
            {
                return text;
            }
            string cut = text;
            while (cut.Length > 1 && st.CalcSize(new GUIContent(cut + "…")).x > maxWidth)
            {
                cut = cut.Substring(0, cut.Length - 1);
            }
            return cut + "…";
        }

        private static Texture2D GetWhite(int w, int h)
        {
            var tex = new Texture2D(Mathf.Max(1, w), Mathf.Max(1, h), TextureFormat.RGBA32, false);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++)
            {
                px[i] = new Color32(255, 255, 255, 255);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D Rounded(int w, int h, float radius)
        {
            string key = w + "_" + h + "_" + Mathf.RoundToInt(radius);
            Texture2D tex;
            if (RoundedCache.TryGetValue(key, out tex))
            {
                return tex;
            }
            var t = new Texture2D(Mathf.Max(4, w), Mathf.Max(4, h), TextureFormat.RGBA32, false);
            float rad = Mathf.Min(radius, Mathf.Min(w, h) * 0.5f);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Max(rad - x, rad - (w - 1 - x), 0f);
                    float dy = Mathf.Max(rad - y, rad - (h - 1 - y), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - rad;
                    byte a = (byte)Mathf.Clamp(Mathf.RoundToInt((0.5f - d) * 255f), 0, 255);
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            RoundedCache[key] = t;
            return t;
        }

        private static Texture2D RoundedBorder(int w, int h, float radius, float borderPx)
        {
            string key = "b" + w + "_" + h + "_" + Mathf.RoundToInt(radius) + "_" + Mathf.RoundToInt(borderPx);
            Texture2D tex;
            if (RoundedCache.TryGetValue(key, out tex))
            {
                return tex;
            }
            var t = new Texture2D(Mathf.Max(4, w), Mathf.Max(4, h), TextureFormat.RGBA32, false);
            float rad = Mathf.Min(radius, Mathf.Min(w, h) * 0.5f);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Max(rad - x, rad - (w - 1 - x), 0f);
                    float dy = Mathf.Max(rad - y, rad - (h - 1 - y), 0f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - rad;
                    float af = 1f;
                    float outer = d;
                    if (outer > 0f)
                    {
                        af *= Mathf.Clamp01(1f - outer);
                    }
                    float inner = -borderPx - d;
                    if (inner > 0f)
                    {
                        af *= Mathf.Clamp01(1f - inner);
                    }
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(af * 255f), 0, 255));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            RoundedCache[key] = t;
            return t;
        }

        private static Texture2D Circle()
        {
            if (_circleTex != null)
            {
                return _circleTex;
            }
            const int s = 48;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color32[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt((1f - d) * 255f), 0, 255));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            _circleTex = t;
            return t;
        }

        private static Texture2D Radial()
        {
            if (_radialTex != null)
            {
                return _radialTex;
            }
            const int s = 128;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color32[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = Mathf.Clamp01(1f - d * d);
                    px[y * s + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            _radialTex = t;
            return t;
        }

        private static Texture2D Sweep()
        {
            if (_sweepTex != null)
            {
                return _sweepTex;
            }
            const int s = 128;
            var t = new Texture2D(s * 2, s * 2, TextureFormat.RGBA32, false);
            var px = new Color32[s * 2 * s * 2];
            for (int y = 0; y < s * 2; y++)
            {
                for (int x = 0; x < s * 2; x++)
                {
                    float tx = (x - s) / (float)s;
                    float ty = (y - s) / (float)s;
                    float r = Mathf.Sqrt(tx * tx + ty * ty);
                    float ang = Mathf.Clamp01((tx + 1f) * 0.5f);
                    float a = Mathf.Clamp01((1f - ang) * (1f - ang)) * Mathf.Clamp01(1f - r * 0.9f);
                    px[y * s * 2 + x] = new Color32(255, 255, 255, (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            _sweepTex = t;
            return t;
        }

        private static Texture2D Vignette()
        {
            if (_vignetteTex != null)
            {
                return _vignetteTex;
            }
            const int s = 128;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            var px = new Color32[s * s];
            float c = (s - 1) * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float a = Mathf.Clamp01((d - 0.78f) / 0.22f);
                    px[y * s + x] = new Color32(0, 0, 0, (byte)Mathf.Clamp(Mathf.RoundToInt(a * a * 0.6f * 255f), 0, 255));
                }
            }
            t.SetPixels32(px);
            t.Apply(false, true);
            _vignetteTex = t;
            return t;
        }

        private static void EnsureStyles()
        {
            if (_stylesReady)
            {
                return;
            }
            var baseStyle = GUI.skin.label;
            GUIStyle S(int fontSize, FontStyle fontStyle, TextAnchor anchor)
            {
                return new GUIStyle(baseStyle)
                {
                    fontSize = fontSize,
                    fontStyle = fontStyle,
                    alignment = anchor,
                    clipping = TextClipping.Overflow,
                    wordWrap = false,
                    padding = new RectOffset(2, 2, 2, 2)
                };
            }
            _stTitle = S(20, FontStyle.Bold, TextAnchor.MiddleLeft);
            _stH2 = S(13, FontStyle.Bold, TextAnchor.MiddleLeft);
            _stXL = S(22, FontStyle.Bold, TextAnchor.MiddleLeft);
            _stTab = S(10, FontStyle.Bold, TextAnchor.MiddleCenter);
            _stBody = S(13, FontStyle.Normal, TextAnchor.MiddleLeft);
            _stSmall = S(11, FontStyle.Normal, TextAnchor.MiddleLeft);
            _stMicro = S(10, FontStyle.Normal, TextAnchor.MiddleLeft);
            _stBtn = S(13, FontStyle.Bold, TextAnchor.MiddleCenter);
            _stLog = S(11, FontStyle.Normal, TextAnchor.UpperLeft);
            _stLog.wordWrap = true;
            _stylesReady = true;
        }

        private static Rect Abs(Rect r)
        {
            return r;
        }

        private static bool Hit(Rect r)
        {
            Event e = Event.current;
            return !_measure && e != null && e.type == EventType.MouseDown && e.button == 0 && r.Contains(e.mousePosition);
        }

        private static bool Hover(Rect r)
        {
            Event e = Event.current;
            return !_measure && e != null && r.Contains(e.mousePosition);
        }

        private static Rect Row(float h)
        {
            Rect r = new Rect(_contentX + PadX, _winOY + _y, PanelW - PadX * 2f, h);
            _y += h + RowGap;
            return r;
        }

        private static float Ease(float t)
        {
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }

        // =====================================================================
        // HUD
        // =====================================================================
        public static void DrawHud(TrickshotMenuPlugin plugin)
        {
            _pa = 1f;
            EnsureStyles();
            if (!TrickshotMenuPlugin.ShowHud.Value)
            {
                return;
            }
            if (plugin.KillsConfirmed > 0 || plugin.HasTargetLock || plugin.IsSpinning)
            {
                DrawStatusCard(plugin);
            }

            if (plugin.IsSpinning)
            {
                DrawSpinRing(plugin);
            }

            if (plugin.HasTargetLock && plugin.TargetScreenPos.HasValue)
            {
                DrawReticle(plugin, plugin.TargetScreenPos.Value);
            }

            if (plugin.Combo >= 2)
            {
                DrawCombo(plugin);
            }
        }

        private static void DrawStatusCard(TrickshotMenuPlugin plugin)
        {
            Rect card = new Rect(18f, 18f, 336f, 64f);
            DrawTex(card, Rounded(336, 64, 14), T(new Color(0.045f, 0.06f, 0.085f, 0.92f)));
            Fill(new Rect(card.x, card.y + 6f, 3f, card.height - 12f), T(Gold, 0.9f));
            Fill(new Rect(card.x, card.y, card.width, 1f), T(TextHi, 0.08f));

            _stTitle.normal.textColor = T(Gold);
            GUI.Label(new Rect(card.x + 14f, card.y + 4f, card.width - 20f, 22f), Fit(_stTitle, "x" + Mathf.Max(1f, plugin.LastMult).ToString("0.00"), card.width - 20f), _stTitle);
            _stSmall.normal.textColor = T(TextHi);
            GUI.Label(new Rect(card.x + 14f, card.y + 28f, card.width - 20f, 16f), Fit(_stSmall, plugin.GetStatus(), card.width - 20f), _stSmall);
            _stMicro.normal.textColor = T(Accent);
            GUI.Label(new Rect(card.x + 14f, card.y + 44f, card.width - 20f, 16f), Fit(_stMicro, plugin.LastBonusSummary, card.width - 20f), _stMicro);
        }

        private static void DrawSpinRing(TrickshotMenuPlugin plugin)
        {
            float pulse = 1f + 0.04f * Mathf.Sin(Time.time * 9f);
            Rect ring = new Rect(Screen.width * 0.5f - 34f * pulse, 150f, 68f * pulse, 68f * pulse);
            Vector2 c = ring.center;
            float rad = ring.width * 0.5f - 5f;
            float th = 7f;
            const int segs = 44;
            for (int i = 0; i <= segs; i++)
            {
                float u = i / (float)segs;
                float a = (-90f + u * 360f) * Mathf.Deg2Rad;
                bool on = u <= plugin.SpinFrac;
                Color col = on ? Accent : new Color(1f, 1f, 1f, 0.12f);
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                DrawTex(new Rect(p.x - th * 0.5f, p.y - th * 0.5f, th, th), Circle(), T(col, on ? 0.95f : 0.5f));
            }
            Vector2 tipA = c + new Vector2(Mathf.Cos((-90f + plugin.SpinFrac * 360f) * Mathf.Deg2Rad), Mathf.Sin((-90f + plugin.SpinFrac * 360f) * Mathf.Deg2Rad)) * rad;
            DrawTex(new Rect(tipA.x - 13f, tipA.y - 13f, 26f, 26f), Radial(), T(Accent, 0.55f));

            string label = Mathf.RoundToInt(Mathf.Lerp(0f, TrickshotMenuPlugin.SpinExtra.Value, plugin.SpinFrac)) + "°";
            Rect lr = new Rect(ring.x - 30f, ring.y - 4f + ring.height, ring.width + 60f, 26f);
            _stH2.normal.textColor = T(TextHi);
            GUI.Label(new Rect(lr.x, lr.y, lr.width, 20f), label, _stH2);
            _stSmall.normal.textColor = T(TextDim);
            GUI.Label(new Rect(lr.x, lr.y + 16f, lr.width, 14f), "360 TRICKS", _stSmall);
        }

        private static void DrawReticle(TrickshotMenuPlugin plugin, Vector2 pos)
        {
            float pulse = 1f + 0.1f * Mathf.Sin(Time.time * 5f);
            float half = 15f * pulse;
            Color col = T(plugin.IsSpinning ? Gold : Accent, 0.85f);

            float bx = 3f;
            Fill(new Rect(pos.x - half - 12f, pos.y - bx, 10f, 2f), col);   // left top
            Fill(new Rect(pos.x + half + 2f, pos.y - bx, 10f, 2f), col);    // right top
            Fill(new Rect(pos.x - half - 12f, pos.y + bx - 2f, 10f, 2f), col);
            Fill(new Rect(pos.x + half + 2f, pos.y + bx - 2f, 10f, 2f), col);
            Fill(new Rect(pos.x - bx, pos.y - half - 12f, 2f, 10f), col);   // left vertical
            Fill(new Rect(pos.x + bx - 2f, pos.y - half - 12f, 2f, 10f), col);
            Fill(new Rect(pos.x - bx, pos.y + half + 2f, 2f, 10f), col);
            Fill(new Rect(pos.x + bx - 2f, pos.y + half + 2f, 2f, 10f), col);
            DrawTex(new Rect(pos.x - 5f, pos.y - 5f, 10f, 10f), Circle(), col);

            Rect info = new Rect(pos.x - 80f, pos.y + half + 18f, 160f, 30f);
            _stSmall.normal.textColor = T(TextHi);
            GUI.Label(info, plugin.TargetName + "\n" + plugin.TargetDist.ToString("0.0") + " m", _stSmall);
        }

        private static void DrawCombo(TrickshotMenuPlugin plugin)
        {
            float age = plugin.ComboAge;
            float pop = 1f + Mathf.Clamp01(1f - age * 3f) * 0.35f;
            Rect r = new Rect(18f, Screen.height - 86f, 240f, 66f);
            _stTitle.normal.textColor = T(Purple);
            GUI.Label(new Rect(r.x, r.y + 6f, r.width, 34f * pop), "COMBO  x" + plugin.Combo, _stTitle);
            _stSmall.normal.textColor = T(TextDim);
            GUI.Label(new Rect(r.x, r.y + 38f, r.width, 16f), "best chain  x" + plugin.BestCombo, _stSmall);
        }

        // =====================================================================
        // Effects
        // =====================================================================
        public static void DrawFx(TrickshotMenuPlugin plugin)
        {
            if (!plugin.IsSpinning || !TrickshotMenuPlugin.SpinFx.Value)
            {
                return;
            }
            _pa = 1f;
            EnsureStyles();
            float vig = 0.5f + 0.04f * Mathf.Sin(Time.time * 2.1f);
            DrawTex(new Rect(0f, 0f, Screen.width, Screen.height), Vignette(), new Color(1f, 1f, 1f, vig));
            Vector2 c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            DrawSweep(c, Time.time * 700f, Accent, 0.16f);
            DrawSweep(c, -Time.time * 520f, Purple, 0.10f);
        }

        private static void DrawSweep(Vector2 center, float angle, Color color, float intensity)
        {
            GUI.BeginGroup(new Rect(center.x - 320f, center.y - 320f, 640f, 640f));
            GUIUtility.RotateAroundPivot(angle + 90f, new Vector2(320f, 320f));
            DrawTex(new Rect(0f, 0f, 640f, 640f), Sweep(), T(color, intensity));
            GUI.EndGroup();
        }

        // =====================================================================
        // Toasts
        // =====================================================================
        public static void DrawToasts()
        {
            _pa = 1f;
            EnsureStyles();
            const float life = 2.4f;
            for (int i = Toasts.Count - 1; i >= 0; i--)
            {
                ToastEntry t = Toasts[i];
                float age = Time.time - t.Start;
                if (age > life)
                {
                    Toasts.RemoveAt(i);
                    continue;
                }
                float alpha = Mathf.Clamp01(age / 0.12f) * Mathf.Clamp01((life - age) / 0.5f);
                float w = Mathf.Min(420f, Screen.width - 40f);
                float x = (Screen.width - w) * 0.5f;
                float y = 128f + i * 50f - (1f - alpha) * 26f;
                Rect card = new Rect(x, y, w, 42f);
                Fill(card, new Color(0.04f, 0.055f, 0.08f, alpha));
                Fill(new Rect(x, y + 4f, 3f, 34f), new Color(t.Color.r, t.Color.g, t.Color.b, alpha));
                Fill(new Rect(x + 3f, y, w - 3f, 1f), new Color(1f, 1f, 1f, alpha * 0.08f));
                _stBody.normal.textColor = new Color(t.Color.r, t.Color.g, t.Color.b, alpha);
                GUI.Label(new Rect(x + 14f, y + 10f, w - 20f, 24f), t.Message, _stBody);
            }
        }

        // =====================================================================
        // Main panel
        // =====================================================================
        public static void DrawPanel(TrickshotMenuPlugin plugin)
        {
            float pa = plugin.MenuAnim;
            if (pa <= 0.001f)
            {
                return;
            }
            _pa = Ease(pa);
            EnsureStyles();

            if (!_measure && Event.current != null && Event.current.type == EventType.MouseUp)
            {
                _dragId = -1;
            }

            float h = Mathf.Clamp(PanelMaxContentEnd() + 18f, 220f, Screen.height - 60f);
            _panelH = h;
            Rect panel = new Rect(Screen.width - PanelW - 16f, 30f, PanelW, h);
            _winOY = panel.y;
            _contentX = panel.x + (1f - pa) * PanelW * 0.35f;
            _y = 0f;

            DrawTex(new Rect(_contentX, _winOY, PanelW, h), Rounded(Mathf.RoundToInt(PanelW), Mathf.RoundToInt(h), 18), T(PanelBg));
            DrawTex(new Rect(_contentX, _winOY, PanelW, h), RoundedBorder(Mathf.RoundToInt(PanelW), Mathf.RoundToInt(h), 18, 1.5f), T(PanelBorder, 0.9f));
            Fill(new Rect(_contentX + 3f, _winOY + 14f, 2.5f, h - 28f), T(Accent, 0.5f));
            Fill(new Rect(_contentX, _winOY, PanelW, 2f), T(Accent, 0.16f));

            _y = 14f;
            Rect title = Row(26f);
            Label(_stTitle, "TRICKSHOT  OPS", new Rect(title.x, title.y, title.width, 22f), T(TextHi));
            Label(_stMicro, "HOW TO FISH  //  V2.0.0", new Rect(title.x, title.y + 20f, title.width, 12f), T(TextDim, 0.9f));

            _y += 8f;
            _tab = TabBar(new[] { "CONTROL", "BONUS", "STATS", "ROULETTE", "SETTINGS", "LOG" }, _tab);
            _y += 8f;

            switch (_tab)
            {
                case 0: DrawControlTab(plugin); break;
                case 1: DrawBonusTab(); break;
                case 2: DrawStatsTab(plugin); break;
                case 3: DrawRouletteTab(plugin); break;
                case 4: DrawSettingsTab(plugin); break;
                default: DrawLogTab(); break;
            }
        }

        private static float PanelMaxContentEnd()
        {
            _measure = true;
            float maxH = 80f;
            for (int t = 0; t < 6; t++)
            {
                _y = 14f;
                Row(26f);
                _y += 8f;
                TabBar(new[] { "CONTROL", "BONUS", "STATS", "ROULETTE", "SETTINGS", "LOG" }, t);
                _y += 8f;
                switch (t)
                {
                    case 0: DrawControlTab(TrickshotMenuPlugin.Instance); break;
                    case 1: DrawBonusTab(); break;
                    case 2: DrawStatsTab(TrickshotMenuPlugin.Instance); break;
                    case 3: DrawRouletteTab(TrickshotMenuPlugin.Instance); break;
                    case 4: DrawSettingsTab(TrickshotMenuPlugin.Instance); break;
                    default: DrawLogTab(); break;
                }
                maxH = Mathf.Max(maxH, _y);
            }
            _measure = false;
            return maxH;
        }

        public static int TabBar(string[] labels, int current)
        {
            float w = PanelW - PadX * 2f;
            float itemW = w / labels.Length;
            Rect bar = Row(28f);
            if (_measure)
            {
                return current;
            }
            const float gap = 5f;
            for (int i = 0; i < labels.Length; i++)
            {
                Rect it = new Rect(bar.x + i * itemW + gap * 0.5f, bar.y, itemW - gap, bar.height);
                Rect abs = Abs(it);
                bool sel = i == current;
                if (sel)
                {
                    Fill(it, T(Accent, 0.16f));
                    Fill(new Rect(it.x, it.y + it.height - 2f, it.width, 2f), T(Accent, 0.85f));
                }
                else if (Hover(abs))
                {
                    Fill(it, T(TextHi, 0.06f));
                }
                _stTab.normal.textColor = T(sel ? Accent : TextDim);
                GUI.Label(new Rect(it.x + 1f, it.y + 2f, it.width - 2f, it.height - 4f), labels[i], _stTab);
                if (Hit(abs))
                {
                    return i;
                }
            }
            return current;
        }

        // ---------------- CONTROL ----------------
        private static void DrawControlTab(TrickshotMenuPlugin plugin)
        {
            Section("AUTOMATION");
            TrickshotMenuPlugin.AutoTrick.Value = Switch("AUTO TRICKSHOT", TrickshotMenuPlugin.AutoTrick.Value);
            TrickshotMenuPlugin.AirborneOnly.Value = Switch("AIRBORNE ONLY", TrickshotMenuPlugin.AirborneOnly.Value);
            TrickshotMenuPlugin.AutoJump.Value = Switch("JUMP FOR AERIAL", TrickshotMenuPlugin.AutoJump.Value);
            TrickshotMenuPlugin.OneShotHelper.Value = Switch("ONE-SHOT HELPER", TrickshotMenuPlugin.OneShotHelper.Value);
            TrickshotMenuPlugin.ForceLastBullet.Value = Switch("FORCE LAST BULLET", TrickshotMenuPlugin.ForceLastBullet.Value);
            TrickshotMenuPlugin.AimHead.Value = Switch("AIM HEADS", TrickshotMenuPlugin.AimHead.Value);
            TrickshotMenuPlugin.SpinFx.Value = Switch("SPIN LIGHT FX", TrickshotMenuPlugin.SpinFx.Value);
            TrickshotMenuPlugin.ShowHud.Value = Switch("HUD", TrickshotMenuPlugin.ShowHud.Value);

            Section("TUNING");
            TrickshotMenuPlugin.SpinSpeed.Value = Slider("SPIN SPEED", TrickshotMenuPlugin.SpinSpeed.Value, 500f, 7200f, " deg/s");
            TrickshotMenuPlugin.SpinExtra.Value = Slider("SPIN ROTATION", TrickshotMenuPlugin.SpinExtra.Value, 360f, 1080f, "°");
            TrickshotMenuPlugin.LeadTime.Value = Slider("TARGET LEAD", TrickshotMenuPlugin.LeadTime.Value, -1f, 1f, "s");
            TrickshotMenuPlugin.RangeLimit.Value = Slider("MAX RANGE", TrickshotMenuPlugin.RangeLimit.Value, 5f, 200f, " m");
            TrickshotMenuPlugin.ComboWindow.Value = Slider("COMBO WINDOW", TrickshotMenuPlugin.ComboWindow.Value, 1f, 30f, "s");

            Section("DIRECTION");
            Rect dirRow = Row(28f);
            float half = (dirRow.width - 6f) / 2f;
            bool cw = TrickshotMenuPlugin.SpinDirection.Value >= 0;
            if (ActChip("CLOCKWISE", new Rect(dirRow.x, dirRow.y, half, dirRow.height), cw))
            {
                cw = true;
            }
            if (ActChip("COUNTER", new Rect(dirRow.x + half + 6f, dirRow.y, half, dirRow.height), !cw))
            {
                cw = false;
            }
            TrickshotMenuPlugin.SpinDirection.Value = cw ? 1 : -1;

            Section("WEAPONS");
            Rect wRow = Row(28f);
            string[] presets = { "PISTOL", "SNIPER", "SHOTGUN", "ALL" };
            float wu = (wRow.width - 18f) / presets.Length;
            bool isAll = TrickshotMenuPlugin.WeaponFilter.Value.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) >= 0
                && TrickshotMenuPlugin.WeaponFilter.Value.IndexOf("sniper", StringComparison.OrdinalIgnoreCase) >= 0
                && TrickshotMenuPlugin.WeaponFilter.Value.IndexOf("shotgun", StringComparison.OrdinalIgnoreCase) >= 0;
            for (int i = 0; i < presets.Length; i++)
            {
                Rect chip = new Rect(wRow.x + i * (wu + 6f), wRow.y, wu, wRow.height);
                bool active = presets[i] == "ALL" ? isAll : TrickshotMenuPlugin.WeaponFilter.Value.IndexOf(presets[i], StringComparison.OrdinalIgnoreCase) >= 0;
                if (ActChip(presets[i], chip, active))
                {
                    if (presets[i] == "ALL")
                    {
                        TrickshotMenuPlugin.WeaponFilter.Value = "pistol,sniper,shotgun";
                    }
                    else
                    {
                        TrickshotMenuPlugin.WeaponFilter.Value = presets[i].ToLowerInvariant();
                    }
                }
            }
            Label(_stMicro, "current filter: [" + TrickshotMenuPlugin.WeaponFilter.Value + "]", new Rect(wRow.x, _y + 2f, wRow.width, 14f), T(TextDim));

            _y += 8f;
            if (Btn("MANUAL TRICKSHOT  (" + TrickshotMenuPlugin.ManualKey.Value + ")", 34f))
            {
                plugin.TriggerManual();
            }
        }

        // ---------------- BONUS ----------------
        private static void DrawBonusTab()
        {
            Section("MULTIPLIER CHAIN");
            bool oneshot = TrickshotMenuPlugin.OneShotHelper.Value;
            bool head = TrickshotMenuPlugin.AimHead.Value;
            bool last = TrickshotMenuPlugin.ForceLastBullet.Value;
            bool jump = TrickshotMenuPlugin.AutoJump.Value;

            string[,] rows =
            {
                { "360° Spin", "full turn before the shot", "1.5", "1" },
                { "No Scope", "never down sights", "1.2", "1" },
                { "Headshot", "hit the head", "1.25", head ? "1" : "0" },
                { "One Shot One Kill", "single round kill", "1.25", oneshot ? "1" : "0" },
                { "Overkill", "mass damage", "1.25", oneshot ? "1" : "0" },
                { "Last Bullet", "the killing round", "1.25", last ? "1" : "0" },
                { "Fly Fishing", "airborne target", "1.25", "1" },
                { "Aerial", "jumped before shot", "1.25", jump ? "1" : "0" },
                { "Dogfight", "airborne duel", "1.5", jump ? "1" : "0" },
                { "Impressive", "5+ bonuses stacked", "2.0", "1" }
            };

            float total = 1f;
            int armed = 0;
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                bool on = rows[i, 3] == "1";
                if (on)
                {
                    total *= float.Parse(rows[i, 2]);
                    armed++;
                }
                BonusRow(rows[i, 0], rows[i, 1], rows[i, 2], on);
                _y += 2f;
            }

            _y += 8f;
            Rect sum = Row(40f);
            Fill(new Rect(sum.x, sum.y, sum.width, sum.height), T(TextHi, 0.05f));
            Fill(new Rect(sum.x, sum.y, 3f, sum.height), T(Gold, 0.9f));
            _stH2.normal.textColor = T(Gold);
            GUI.Label(new Rect(sum.x + 10f, sum.y + 4f, sum.width - 10f, 16f), "PREDICTED  x" + total.ToString("0.00"), _stH2);
            _stMicro.normal.textColor = T(TextDim);
            GUI.Label(new Rect(sum.x + 10f, sum.y + 22f, sum.width - 10f, 14f), armed + " bonuses armed · kill value x" + total.ToString("0.0"), _stMicro);
        }

        private static void BonusRow(string name, string desc, string mult, bool armed)
        {
            Rect r = Row(24f);
            if (_measure)
            {
                return;
            }
            Fill(new Rect(r.x, r.y + 7f, 6f, 6f), T(armed ? Green : Red, 0.9f));
            _stBody.normal.textColor = T(TextHi);
            GUI.Label(new Rect(r.x + 16f, r.y, r.width * 0.64f, 15f), name, _stBody);
            _stSmall.normal.textColor = T(TextDim);
            GUI.Label(new Rect(r.x + 16f, r.y + 11f, r.width * 0.64f, 13f), desc, _stSmall);
            _stBody.normal.textColor = T(armed ? Green : TextDim, armed ? 1f : 0.65f);
            GUI.Label(new Rect(r.x + r.width * 0.64f, r.y, r.width * 0.36f - 6f, 24f), "x" + mult, _stBody);
        }

        // ---------------- STATS ----------------
        private static void DrawStatsTab(TrickshotMenuPlugin plugin)
        {
            Section("SESSION");
            StatRow("TRICKS FIRED", plugin.TricksFired.ToString());
            StatRow("KILLS CONFIRMED", plugin.KillsConfirmed.ToString());
            StatRow("BEST MULTIPLIER", "x" + plugin.BestMult.ToString("0.00"));
            StatRow("BEST COMBO", "x" + plugin.BestCombo);
            StatRow("EST. EARNED", "$" + plugin.EstEarned.ToString("0"));

            _y += 8f;
            Section("LAST CHAIN");
            Rect bx = Row(46f);
            Fill(new Rect(bx.x, bx.y, bx.width, bx.height), T(TextHi, 0.05f));
            _stSmall.normal.textColor = T(Accent);
            GUI.Label(new Rect(bx.x + 10f, bx.y + 8f, bx.width - 20f, 30f), Fit(_stSmall, plugin.LastBonusSummary, bx.width - 20f), _stSmall);

            _y += 10f;
            if (Btn("RESET SESSION", 34f))
            {
                plugin.ResetStats();
                Toast("Session stats reset", Accent);
            }
        }

        private static void StatRow(string name, string value)
        {
            Rect r = Row(26f);
            if (_measure)
            {
                return;
            }
            _stBody.normal.textColor = T(TextDim);
            GUI.Label(new Rect(r.x, r.y, r.width * 0.6f, 26f), name, _stBody);
            _stBody.normal.textColor = T(TextHi);
            GUI.Label(new Rect(r.x + r.width * 0.6f, r.y, r.width * 0.4f, 26f), value, _stBody);
            Fill(new Rect(r.x, r.y + r.height - 1f, r.width, 1f), T(TextHi, 0.06f));
        }

        // ---------------- ROULETTE ----------------
        private static void DrawRouletteTab(TrickshotMenuPlugin plugin)
        {
            TrickshotMenuPlugin.RouletteState st = plugin.GetRouletteState();
            Section("ROULETTE TABLE");

            if (!st.Present)
            {
                Rect card = Row(96f);
                if (!_measure)
                {
                    Fill(card, T(TextHi, 0.05f));
                    DrawTex(card, RoundedBorder(Mathf.RoundToInt(card.width), Mathf.RoundToInt(card.height), 12, 1.5f), T(TextDim, 0.5f));
                    _stBody.normal.textColor = T(TextHi);
                    GUI.Label(new Rect(card.x + 16f, card.y + 14f, card.width - 32f, 20f), "NO CASINO TABLE", _stBody);
                    _stSmall.normal.textColor = T(TextDim);
                    GUI.Label(new Rect(card.x + 16f, card.y + 44f, card.width - 32f, 16f), "Stand by the roulette table in the casino.", _stSmall);
                    GUI.Label(new Rect(card.x + 16f, card.y + 64f, card.width - 32f, 16f), "It arms the live predictor the moment it loads.", _stSmall);
                }
                return;
            }

            bool betKnown = st.BetColor >= 0;
            bool live = st.Playing && st.BallColor >= 0;
            int landing = live ? st.BallColor : -1;
            bool hasResult = plugin.RouletteResultAge < 5f;
            string[] rp = hasResult ? plugin.RouletteResult.Split('|') : new string[0];
            bool won = hasResult && rp.Length > 1 && rp[1] != "0";
            if (hasResult && !live)
            {
                int.TryParse(rp[0], out landing);
            }

            string statusText;
            Color statusColor;
            if (st.Playing && st.Spinning)
            {
                statusText = "SPINNING";
                statusColor = Accent;
            }
            else if (st.Playing)
            {
                statusText = "SETTLING";
                statusColor = Purple;
            }
            else if (hasResult)
            {
                statusText = (won ? "WON" : "LOST") + "  " + RouletteColorName(landing) + (won ? "  ·  PAYS x" + rp[1] : "");
                statusColor = won ? Gold : Red;
            }
            else if (st.BetPlaced)
            {
                statusText = "BET PLACED";
                statusColor = TextDim;
            }
            else
            {
                statusText = "NO BET";
                statusColor = Accent;
            }

            // big landing display
            Rect big = Row(54f);
            if (!_measure)
            {
                Fill(big, T(TextHi, 0.05f));
                Fill(new Rect(big.x, big.y, 3f, big.height), T(Accent, 0.9f));
                _stH2.normal.textColor = T(TextDim);
                GUI.Label(new Rect(big.x + 16f, big.y + 6f, 160f, 16f), "NEXT LANDING", _stH2);
                _stMicro.normal.textColor = T(live ? Accent : TextDim, live ? 1f : 0.7f);
                GUI.Label(new Rect(big.x + 16f, big.y + 26f, 120f, 14f), live ? "LIVE" : hasResult ? "SETTLED" : "WAITING", _stMicro);
                if (landing >= 0)
                {
                    Color c = RouletteColorOf(landing);
                    DrawTex(new Rect(big.x + big.width - 176f, big.y + 18f, 18f, 18f), Circle(), T(c, 1f));
                    DrawTex(new Rect(big.x + big.width - 184f, big.y + 10f, 34f, 34f), Radial(), T(c, 0.2f));
                    _stXL.normal.textColor = T(c);
                    GUI.Label(new Rect(big.x + big.width - 152f, big.y + 10f, 130f, 34f), RouletteColorName(landing), _stXL);
                }
                else
                {
                    _stXL.normal.textColor = T(TextDim);
                    GUI.Label(new Rect(big.x + big.width - 152f, big.y + 10f, 130f, 34f), "—", _stXL);
                }
            }

            RouletteRow("BET", betKnown ? RouletteColorName(st.BetColor) : "no bet", RouletteColorOf(st.BetColor), st.BetPlaced);
            RouletteRow("WAGER", "$" + st.TotalWorth, Gold, st.BetPlaced);
            RouletteRow("PAYOUT", st.BetPlaced ? "x" + (betKnown && st.BetColor == 2 ? 35 : 2) + " on " + (betKnown ? RouletteColorName(st.BetColor) : "your color") : "red/black x2 · green x35", Gold, st.BetPlaced);

            // status banner
            Rect sb = Row(36f);
            if (_measure)
            {
                DrawRouletteExtras(plugin);
                return;
            }
            DrawTex(sb, RoundedBorder(Mathf.RoundToInt(sb.width), Mathf.RoundToInt(sb.height), 10, 1.5f), T(statusColor, 0.9f));
            Fill(sb, T(statusColor, 0.06f));
            _stH2.normal.textColor = T(statusColor);
            GUI.Label(new Rect(sb.x + 14f, sb.y, sb.width - 28f, sb.height), "●  " + statusText, _stH2);

            DrawRouletteExtras(plugin);
        }

        private static readonly string[] SlotForceOptions = { "OFF", "RED", "GOLD" };

        private static void DrawRouletteExtras(TrickshotMenuPlugin plugin)
        {
            _y += 4f;
            Label(_stMicro, "live = the exact slot under the ball · same value the table pays out", new Rect(_contentX + PadX, _y, PanelW - PadX * 2f, 14f), T(TextDim, 0.85f));
            _y += 4f;
            TrickshotMenuPlugin.AlwaysWinRoulette.Value = Switch("ALWAYS WIN ROULETTE", TrickshotMenuPlugin.AlwaysWinRoulette.Value);

            _y += 8f;
            Section("SLOT MACHINE");
            _y += 2f;
            TrickshotMenuPlugin.SlotFreeSpins.Value = Switch("FREE SPINS", TrickshotMenuPlugin.SlotFreeSpins.Value);
            string forced = TrickshotMenuPlugin.SlotForce != null ? TrickshotMenuPlugin.SlotForce.Value : "OFF";
            TrickshotMenuPlugin.SlotForce.Value = Cycle("FORCE RARITY", forced, SlotForceOptions);
            _y += 2f;
            Label(_stMicro, "red = rare-marked · gold = legendary-marked · no fish needed with free spins", new Rect(_contentX + PadX, _y, PanelW - PadX * 2f, 14f), T(TextDim, 0.85f));
        }

        private static string Cycle(string label, string value, string[] options)
        {
            Rect row = Row(26f);
            if (_measure)
            {
                return value;
            }
            _stBody.normal.textColor = T(TextDim);
            GUI.Label(new Rect(row.x, row.y, row.width - 150f, 26f), label, _stBody);
            Rect btn = new Rect(row.x + row.width - 138f, row.y + 3f, 126f, 20f);
            Color c = value == "OFF" ? TextDim : value == "RED" ? Red : Gold;
            DrawTex(btn, RoundedBorder(126, 20, 10, 1.5f), T(c, 0.95f));
            Fill(btn, T(c, 0.16f));
            _stTab.normal.textColor = T(value == "OFF" ? TextHi : c);
            GUI.Label(new Rect(btn.x, btn.y - 1f, btn.width, 20f), value == "OFF" ? "OFF" : value == "RED" ? "RED MARKED" : "GOLD MARKED", _stTab);
            if (Hit(Abs(btn)))
            {
                Event.current.Use();
                int i = System.Array.IndexOf(options, value);
                return options[(i + 1) % options.Length];
            }
            return value;
        }

        private static void RouletteRow(string name, string value, Color chipColor, bool lit)
        {
            Rect r = Row(28f);
            if (_measure)
            {
                return;
            }
            _stBody.normal.textColor = T(TextDim);
            GUI.Label(new Rect(r.x, r.y, 120f, 28f), name, _stBody);
            if (chipColor.a > 0f)
            {
                DrawTex(new Rect(r.x + 132f, r.y + 9f, 10f, 10f), Circle(), T(chipColor, lit ? 1f : 0.5f));
            }
            _stBody.normal.textColor = T(TextHi);
            GUI.Label(new Rect(r.x + 154f, r.y, r.width - 154f, 28f), value, _stBody);
            Fill(new Rect(r.x, r.y + r.height - 1f, r.width, 1f), T(TextHi, 0.06f));
        }

        private static string RouletteColorName(int c)
        {
            switch (c)
            {
                case 0: return "BLACK";
                case 1: return "RED";
                case 2: return "GREEN";
                default: return "—";
            }
        }

        private static Color RouletteColorOf(int c)
        {
            switch (c)
            {
                case 0: return new Color(0.55f, 0.6f, 0.68f, 1f);
                case 1: return Red;
                case 2: return Green;
                default: return Color.clear;
            }
        }

        // ---------------- SETTINGS ----------------
        private static void DrawSettingsTab(TrickshotMenuPlugin plugin)
        {
            Section("KEYBINDS");
            KeyRow(plugin, "Menu", "MENU TOGGLE", "open / close the menu");
            KeyRow(plugin, "Manual", "MANUAL TRICKSHOT", "fire a trickshot now");
            KeyRow(plugin, "Log", "LOG WINDOW", "open / close the log");
            KeyRow(plugin, "Debug", "DEBUG DUMP", "dump state to the log");

            _y += 6f;
            Label(_stSmall, "click a key, then press any key to rebind · ESC cancels", new Rect(_contentX + PadX, _y, PanelW - PadX * 2f, 18f), T(TextHi, 0.7f));
            _y += 10f;

            Rect guideBtn = Row(44f);
            if (!_measure)
            {
                Fill(guideBtn, T(Gold, 0.12f));
                DrawTex(guideBtn, RoundedBorder(Mathf.RoundToInt(guideBtn.width), Mathf.RoundToInt(guideBtn.height), 10, 1.5f), T(Gold, 0.95f));
                Fill(guideBtn, T(Gold, 0.14f));
                _stBtn.normal.textColor = T(new Color(0.1f, 0.08f, 0.04f, 1f));
                GUI.Label(new Rect(guideBtn.x + 12f, guideBtn.y + 10f, guideBtn.width - 24f, 24f), "OPEN GUIDE  →", _stBtn);
                if (Hit(Abs(guideBtn)))
                {
                    plugin.ToggleGuide();
                    Event.current.Use();
                }
            }
            _y += 8f;
            Label(_stMicro, "every function and how the menu works", new Rect(_contentX + PadX, _y, PanelW - PadX * 2f, 16f), T(TextHi, 0.6f));
        }

        private static void KeyRow(TrickshotMenuPlugin plugin, string entry, string name, string desc)
        {
            Rect r = Row(74f);
            if (_measure)
            {
                return;
            }
            bool cap = plugin.RebindingEntry == entry;
            Color bg = cap ? new Color(Accent.r, Accent.g, Accent.b, 0.11f) : new Color(1f, 1f, 1f, 0.045f);
            Fill(r, T(bg));
            DrawTex(r, RoundedBorder(Mathf.RoundToInt(r.width), Mathf.RoundToInt(r.height), 11, 1.5f), T(cap ? Accent : TextHi, cap ? 0.75f : 0.28f));
            Fill(new Rect(r.x, r.y, 4f, r.height), T(cap ? Accent : TextDim, cap ? 1f : 0.55f));

            float tx = r.x + 18f;
            float tw = r.width - 230f;
            _stH2.normal.textColor = T(TextHi);
            GUI.Label(new Rect(tx, r.y + 11f, tw, 20f), name, _stH2);
            _stSmall.normal.textColor = T(TextHi, 0.88f);
            GUI.Label(new Rect(tx, r.y + 33f, tw, 30f), desc, _stSmall);

            // right: big key pill
            Rect pill = new Rect(r.x + r.width - 196f, r.y + 12f, 120f, 34f);
            string keyText = cap ? "PRESS ANY…" : plugin.KeybindValue(entry);
            if (cap)
            {
                DrawTex(new Rect(pill.x - 7f, pill.y - 7f, pill.width + 14f, pill.height + 14f), Radial(), T(Accent, 0.3f));
            }
            Fill(pill, T(cap ? new Color(Accent.r, Accent.g, Accent.b, 0.28f) : new Color(1f, 1f, 1f, 0.09f)));
            DrawTex(pill, RoundedBorder(120, 34, 17, 1.5f), T(cap ? Accent : TextHi, cap ? 1f : 0.5f));
            _stTab.normal.textColor = T(cap ? Accent : TextHi);
            GUI.Label(new Rect(pill.x, pill.y, pill.width, 34f), keyText, _stTab);

            // right: change / cancel button
            Rect ch = new Rect(r.x + r.width - 88f, r.y + 52f, 76f, 26f);
            DrawTex(ch, RoundedBorder(76, 26, 12, 1.5f), T(cap ? Red : Accent, 0.95f));
            Fill(ch, T(cap ? new Color(Red.r, Red.g, Red.b, 0.2f) : new Color(Accent.r, Accent.g, Accent.b, 0.2f)));
            _stTab.normal.textColor = T(cap ? Red : Accent);
            GUI.Label(new Rect(ch.x, ch.y, ch.width, 26f), cap ? "CANCEL" : "CHANGE", _stTab);

            if (Hit(Abs(pill)) || Hit(Abs(ch)))
            {
                plugin.BeginRebind(entry);
                Event.current.Use();
            }
        }

        // ---------------- LOG ----------------
        private static void DrawLogTab()
        {
            if (_measure)
            {
                _y = _panelH - 14f;
                return;
            }
            string[] lines = TrickshotLog.GetLines(220);
            Rect head = Row(20f);
            _stMicro.normal.textColor = T(TextDim);
            GUI.Label(new Rect(head.x, head.y, head.width * 0.7f, 16f), "LIVE LOG  ·  " + TrickshotLog.FilePath, _stMicro);
            Rect clr = new Rect(head.x + head.width - 64f, head.y - 2f, 64f, 20f);
            if (Hit(Abs(clr)))
            {
                TrickshotLog.Clear();
            }
            _stSmall.normal.textColor = T(TextHi);
            GUI.Label(new Rect(clr.x, clr.y, clr.width, 20f), "CLEAR", _stSmall);

            float areaH = Mathf.Max(80f, _panelH - _y - 14f);
            Rect area = new Rect(_contentX + PadX, _winOY + _y, PanelW - PadX * 2f, areaH);
            float contentH = lines.Length * 15f + 8f;
            _logScroll.y = 99999f;
            Vector2 scroll = GUI.BeginScrollView(area, _logScroll, new Rect(0f, 0f, area.width, contentH));
            _logScroll = scroll;
            _stLog.normal.textColor = T(TextHi, 0.85f);
            for (int i = 0; i < lines.Length; i++)
            {
                GUI.Label(new Rect(4f, 4f + i * 15f, area.width, 15f), lines[i], _stLog);
            }
            GUI.EndScrollView();
            _y = _panelH - 14f;
        }

        // =====================================================================
        // Widgets
        // =====================================================================
        private static void Section(string title)
        {
            Rect r = Row(20f);
            if (_measure)
            {
                return;
            }
            _stH2.normal.textColor = T(Accent);
            GUI.Label(new Rect(r.x, r.y, r.width, 16f), title, _stH2);
            Fill(new Rect(r.x, r.y + 17f, r.width, 1f), T(Accent, 0.28f));
        }

        private static bool Switch(string label, bool value)
        {
            Rect row = Row(20f);
            if (_measure)
            {
                return value;
            }
            _stBody.normal.textColor = T(value ? TextHi : TextDim);
            GUI.Label(new Rect(row.x, row.y, row.width - 76f, 20f), label, _stBody);

            Rect pill = new Rect(row.x + row.width - 60f, row.y + 2f, 48f, 16f);
            Rect abs = Abs(pill);
            float on = value ? 1f : 0f;
            Color bg = value ? new Color(Accent.r, Accent.g, Accent.b, 0.26f) : new Color(1f, 1f, 1f, 0.10f);
            Fill(pill, T(bg));
            DrawTex(pill, RoundedBorder(48, 16, 8, 1.5f), T(value ? Accent : TextDim, value ? 0.9f : 0.35f));
            float kx = pill.x + 3f + Mathf.Lerp(0f, 28f, on);
            Rect knob = new Rect(kx, pill.y + 3f, 12f, 12f);
            DrawTex(knob, Circle(), T(value ? Accent : TextHi, value ? 1f : 0.7f));
            if (value)
            {
                DrawTex(new Rect(knob.x - 6f, knob.y - 6f, 24f, 24f), Radial(), T(Accent, 0.28f));
            }
            if (Hover(abs) && !value)
            {
                DrawTex(pill, RoundedBorder(48, 18, 9, 1.5f), T(TextHi, 0.35f));
            }
            if (Hit(abs))
            {
                Event.current.Use();
                return !value;
            }
            return value;
        }

        private static float Slider(string label, float value, float min, float max, string unit)
        {
            Rect row = Row(30f);
            if (_measure)
            {
                return value;
            }
            _stSmall.normal.textColor = T(TextHi);
            GUI.Label(new Rect(row.x, row.y, row.width, 13f), label, _stSmall);

            int myId = _sliderIdCounter;

            Rect track = new Rect(row.x, row.y + 17f, row.width, 5f);
            Rect abs = Abs(track);
            float frac = Mathf.InverseLerp(min, max, value);

            Fill(track, T(TextHi, 0.12f));
            Rect fill = new Rect(track.x, track.y, track.width * frac, track.height);
            Fill(fill, T(Accent, 0.9f));
            DrawTex(track, Rounded(Mathf.RoundToInt(track.width), 5, 2.5f), new Color(0f, 0f, 0f, 0.15f));

            Event e = Event.current;
            if (e != null)
            {
                if (_dragId == myId)
                {
                    float n = Mathf.InverseLerp(abs.x, abs.x + abs.width, e.mousePosition.x);
                    value = Mathf.Clamp(Mathf.Lerp(min, max, n), min, max);
                    if (e.type == EventType.MouseUp)
                    {
                        _dragId = -1;
                        e.Use();
                    }
                    else if (e.type == EventType.MouseDrag)
                    {
                        e.Use();
                    }
                }
                else if (e.type == EventType.MouseDown && e.button == 0 && abs.Contains(e.mousePosition))
                {
                    _dragId = myId;
                    float n = Mathf.InverseLerp(abs.x, abs.x + abs.width, e.mousePosition.x);
                    value = Mathf.Clamp(Mathf.Lerp(min, max, n), min, max);
                    e.Use();
                }
            }

            float cx = track.x + frac * track.width;
            DrawTex(new Rect(cx - 7f, track.y + 5f - 7.5f, 15f, 15f), Circle(), T(Accent, 0.9f));
            DrawTex(new Rect(cx - 11f, track.y + 5f - 11.5f, 23f, 23f), Radial(), T(Accent, 0.22f));

            _stBody.normal.textColor = T(Accent);
            string fmt = max > 100 ? "0" : "0.00";
            if (unit.StartsWith(" deg")) fmt = "0";
            GUI.Label(new Rect(row.x + row.width - 92f, row.y + 4f, 92f, 14f), value.ToString(fmt) + unit, _stBody);
            _sliderIdCounter++;
            return value;
        }

        private static bool Chip(string label, Rect r, bool active)
        {
            Rect abs = Abs(r);
            if (_measure)
            {
                return active;
            }
            Color bg = active ? new Color(Accent.r, Accent.g, Accent.b, 0.22f) : new Color(1f, 1f, 1f, 0.06f);
            Fill(r, T(bg));
            DrawTex(r, RoundedBorder(Mathf.RoundToInt(r.width), Mathf.RoundToInt(r.height), 7, 1f), T(active ? Accent : TextDim, active ? 0.85f : 0.4f));
            if (!active && Hover(abs))
            {
                DrawTex(r, RoundedBorder(Mathf.RoundToInt(r.width), Mathf.RoundToInt(r.height), 7, 1f), T(TextHi, 0.4f));
            }
            _stH2.normal.textColor = T(active ? Accent : TextDim);
            GUI.Label(new Rect(r.x, r.y + 5f, r.width, 20f), label, _stH2);
            if (Hit(abs))
            {
                Event.current.Use();
                return !active;
            }
            return active;
        }

        private static bool ActChip(string label, Rect r, bool active)
        {
            Rect abs = Abs(r);
            if (_measure)
            {
                return false;
            }
            Color bg = active ? new Color(Accent.r, Accent.g, Accent.b, 0.3f) : new Color(1f, 1f, 1f, 0.06f);
            Fill(r, T(bg));
            DrawTex(r, RoundedBorder(Mathf.RoundToInt(r.width), Mathf.RoundToInt(r.height), 7, 1f), T(active ? Accent : TextDim, active ? 0.95f : 0.4f));
            if (!active && Hover(abs))
            {
                DrawTex(r, RoundedBorder(Mathf.RoundToInt(r.width), Mathf.RoundToInt(r.height), 7, 1f), T(TextHi, 0.4f));
            }
            _stBtn.normal.textColor = T(active ? Accent : TextDim);
            GUI.Label(r, label, _stBtn);
            return Hit(abs);
        }

        private static bool Btn(string label, float height)
        {
            Rect r = Row(height);
            if (_measure)
            {
                return false;
            }
            Rect abs = Abs(r);
            bool hover = Hover(abs);
            DrawTex(r, Rounded(Mathf.RoundToInt(r.width), Mathf.RoundToInt(height), 9), T(Accent, hover ? 0.85f : 0.65f));
            Fill(new Rect(r.x, r.y, r.width, 1f), T(TextHi, 0.25f));
            _stBtn.normal.textColor = T(new Color(0.03f, 0.09f, 0.12f, 1f));
            GUI.Label(new Rect(r.x, r.y, r.width, height), label, _stBtn);
            if (Hit(abs))
            {
                Event.current.Use();
                return true;
            }
            return false;
        }

        // =====================================================================
        // Standalone log window (F-key)
        // =====================================================================
        public static void DrawLogWindow(TrickshotMenuPlugin plugin)
        {
            _pa = 1f;
            EnsureStyles();
            const float w = 600f;
            const float h = 360f;
            if (_logTex == null)
            {
                _logTex = Rounded(Mathf.RoundToInt(w), Mathf.RoundToInt(h), 14);
            }
            Rect panel = new Rect(18f, Screen.height - h - 18f, w, h);
            GUI.BeginGroup(panel);
            DrawTex(new Rect(0f, 0f, w, h), _logTex, T(Color.black));
            DrawTex(new Rect(0f, 0f, w, h), RoundedBorder(Mathf.RoundToInt(w), Mathf.RoundToInt(h), 14, 1.5f), T(PanelBorder, 0.8f));
            _stH2.normal.textColor = T(TextHi);
            GUI.Label(new Rect(16f, 12f, 300f, 22f), "LIVE LOG  -  " + TrickshotLog.FilePath, _stH2);
            if (GUI.Button(new Rect(w - 130f, 12f, 54f, 22f), "CLEAR"))
            {
                TrickshotLog.Clear();
            }
            if (GUI.Button(new Rect(w - 68f, 12f, 38f, 22f), "X"))
            {
                plugin.ToggleLogWindow();
            }
            string[] lines = TrickshotLog.GetLines(180);
            Rect area = new Rect(12f, 44f, w - 24f, h - 58f);
            float contentH = lines.Length * 15f + 6f;
            _logScroll.y = 99999f;
            Vector2 scroll = GUI.BeginScrollView(area, _logScroll, new Rect(0f, 0f, area.width - 20f, contentH));
            _logScroll = scroll;
            _stLog.normal.textColor = T(TextHi, 0.9f);
            for (int i = 0; i < lines.Length; i++)
            {
                GUI.Label(new Rect(2f, 2f + i * 15f, area.width, 15f), lines[i], _stLog);
            }
            GUI.EndScrollView();
            GUI.EndGroup();
        }

        // =====================================================================
        // Mod guide overlay (fancy)
        // =====================================================================
        public static void DrawGuidePanel(TrickshotMenuPlugin plugin)
        {
            float a = plugin.GuideAnim;
            if (a <= 0.001f)
            {
                return;
            }
            _pa = 1f;
            EnsureStyles();
            float e = 1f - (1f - a) * (1f - a) * (1f - a);

            const float w = 500f;
            float h = Mathf.Min(580f, Screen.height - 60f);
            float x0 = (Screen.width - w) * 0.5f;
            float y0 = (Screen.height - h) * 0.5f - 24f + (1f - e) * 150f;

            DrawTex(new Rect(x0, y0, w, h), Rounded(Mathf.RoundToInt(w), Mathf.RoundToInt(h), 20), T(new Color(0.045f, 0.06f, 0.085f, 0.985f)));
            DrawTex(new Rect(x0, y0, w, h), RoundedBorder(Mathf.RoundToInt(w), Mathf.RoundToInt(h), 20, 1.5f), T(PanelBorder, 0.9f));
            Fill(new Rect(x0 + 18f, y0 + 10f, w - 36f, 2f), T(Accent, 0.5f + 0.15f * Mathf.Sin(Time.time * 2.2f)));
            Fill(new Rect(x0, y0 + h - 1f, w, 2f), T(Accent, 0.2f));

            _stXL.normal.textColor = T(TextHi);
            GUI.Label(new Rect(x0 + 22f, y0 + 22f, 320f, 34f), "MOD  GUIDE", _stXL);
            _stMicro.normal.textColor = T(TextDim);
            GUI.Label(new Rect(x0 + 24f, y0 + 58f, 380f, 14f), "HOW TO FISH  //  TRICKSHOT OPS  ·  v" + TrickshotMenuPlugin.VERSION, _stMicro);

            Rect xr = new Rect(x0 + w - 66f, y0 + 18f, 42f, 32f);
            DrawTex(xr, RoundedBorder(42, 32, 10, 1.5f), T(TextHi, 0.5f));
            Fill(xr, T(new Color(1f, 1f, 1f, 0.08f)));
            if (Hover(Abs(xr)))
            {
                Fill(xr, T(TextHi, 0.08f));
            }
            _stTab.normal.textColor = T(TextHi);
            GUI.Label(new Rect(xr.x, xr.y - 2f, xr.width, xr.height), "✕", _stTab);
            if (Hit(Abs(xr)))
            {
                plugin.ToggleGuide();
            }

            Rect area = new Rect(x0 + 20f, y0 + 84f, w - 40f, h - 138f);
            float contentW = area.width - 24f;
            float contentH = GuideContentHeight();
            Vector2 scroll = GUI.BeginScrollView(area, _guideScroll, new Rect(0f, 0f, contentW, contentH));
            _guideScroll = scroll;

            float yc = 0f;
            yc = GuideSection(yc, contentW, "THE IDEA", new[] { "The mod turns every shot into a styled 360° trickshot on a qualifying fish,", "automatically stacking score multipliers. Bigger chains = bigger pay.", "Open/close this menu with your MENU key and tune everything live." });
            yc = GuideSection(yc, contentW, "CONTROL TAB", new[] { "AUTO TRICKSHOT - arms automatically on a launched / free fish in range.", "AIRBORNE ONLY - targets untethered fish; OFF also trickshots hooked fish", "       (never while reeling in or wound up close to the reel).", "JUMP FOR AERIAL - adds Aerial / Dogfight to the chain.", "ONE-SHOT HELPER - loads huge damage so the fish dies in one round.", "FORCE LAST BULLET - loads exactly one round (Last Bullet bonus).", "AIM HEADS - headshot assist for the Headshot bonus.", "SPIN LIGHT FX / HUD - cosmetic toggles.", "SLIDERS - spin speed, spin rotation, target lead, max range, combo window.", "DIRECTION / WEAPONS - spin clockwise/counter and which guns count." });
            yc = GuideSection(yc, contentW, "BONUS TAB", new[] { "The panel predicts your chain before you shoot:", "360 x1.5 · No Scope x1.2 · Headshot x1.25 · One Shot x1.25 · Overkill x1.25", "Last Bullet x1.25 · Fly Fishing x1.25 · Aerial x1.25 · Dogfight x1.5", "IMPRESSIVE x2 when 5+ bonuses are stacked. Everything multiplies.", "Best value: jump + head + one-shot + last bullet all at once." });
            yc = GuideSection(yc, contentW, "STATS TAB", new[] { "Tracks tricks fired, kills confirmed, best multiplier, best combo and", "estimated earnings for the session. RESET SESSION wipes them." });
            yc = GuideSection(yc, contentW, "ROULETTE TAB", new[] { "The fish you put on the table is your wager. Pick a color:", "BLACK/RED pay x2 · GREEN pays x35.", "LIVE = the exact slot under the ball - the same value the table pays.", "ALWAYS WIN ROULETTE (host) forces the ball onto your color each spin." });
            yc = GuideSection(yc, contentW, "KEYBINDS", new[] {
                "MENU " + plugin.KeybindValue("Menu") + " · MANUAL " + plugin.KeybindValue("Manual"),
                "LOG " + plugin.KeybindValue("Log") + " · DEBUG " + plugin.KeybindValue("Debug"),
                "Rebind any of them in the SETTINGS tab - click, then press a key." });

            GUI.EndScrollView();
            _guideScroll = scroll;

            Fill(new Rect(x0, y0 + h - 44f, w, 1f), T(TextHi, 0.08f));
            _stMicro.normal.textColor = T(TextDim, 0.85f);
            GUI.Label(new Rect(x0 + 20f, y0 + h - 36f, w - 40f, 20f), "ESC closes · ✕ closes · panel slides in on the menu", _stMicro);
        }

        private static float GuideSection(float yc, float areaW, string title, string[] bullets)
        {
            Fill(new Rect(6f, yc + 5f, 8f, 8f), T(Accent, 0.9f));
            _stH2.normal.textColor = T(Accent);
            GUI.Label(new Rect(22f, yc, areaW - 30f, 18f), title, _stH2);
            yc += 24f;
            _stSmall.normal.textColor = T(TextDim, 0.92f);
            for (int i = 0; i < bullets.Length; i++)
            {
                GUI.Label(new Rect(14f, yc, areaW - 28f, 16f), bullets[i], _stSmall);
                yc += 17f;
            }
            return yc + 12f;
        }

        private static float GuideContentHeight()
        {
            float h = 0f;
            h += 24 + 3 * 17 + 12;
            h += 24 + 10 * 17 + 12;
            h += 24 + 5 * 17 + 12;
            h += 24 + 2 * 17 + 12;
            h += 24 + 4 * 17 + 12;
            h += 24 + 3 * 17 + 12;
            return h;
        }
    }
}