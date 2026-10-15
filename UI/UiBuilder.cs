using System;
using UnityEngine;

namespace LiarsBarMod.UI
{
    public class UiBuilder
    {
        private readonly Rect _bounds;
        private float _curX;
        private float _curY;
        private bool _inRow;
        private float _rowHeight;

        public float CurrentX => _curX;
        public float CurrentY => _curY;
        public float TotalHeight => _curY - _bounds.y;
        public float RemainingHeight => Mathf.Max(0, (_bounds.y + _bounds.height) - _curY);

        public UiBuilder(Rect bounds)
        {
            _bounds = bounds;
            _curX = bounds.x;
            _curY = bounds.y;
            _inRow = false;
            _rowHeight = 0f;
        }

        public void Space(float pixels = 6f)
        {
            if (!_inRow)
            {
                _curY += pixels;
            }
            else
            {
                _curX += pixels;
            }
        }

        public void Header(string title, float height = 24f)
        {
            Rect r = new Rect(_bounds.x, _curY, _bounds.width, height);
            Theme.DrawPanel(r, new Color(0.08f, 0.11f, 0.18f, 0.90f), new Color(0.00f, 0.90f, 0.65f, 0.40f));
            Theme.DrawRect(new Rect(r.x, r.y, 4f, r.height), Theme.CyanAccent); // Accent bar

            GUI.Label(new Rect(r.x + 10f, r.y + 2f, r.width - 15f, r.height), $"<b><color=#00FFB0>{title}</color></b>");
            _curY += height + 5f;
        }

        public void SubHeader(string title, float height = 20f)
        {
            GUI.Label(new Rect(_bounds.x, _curY, _bounds.width, height), $"<b><color=#55FFAA>▸ {title}</color></b>");
            _curY += height + 2f;
        }

        public void Box(string text, float height = 28f)
        {
            Rect r = new Rect(_bounds.x, _curY, _bounds.width, height);
            Theme.DrawPanel(r, new Color(0.05f, 0.07f, 0.12f, 0.85f), Theme.BorderMuted);
            GUI.Label(new Rect(r.x + 8f, r.y + 4f, r.width - 16f, r.height - 8f), text);
            _curY += height + 4f;
        }

        public void Label(string text, float height = 20f)
        {
            GUI.Label(new Rect(_bounds.x, _curY, _bounds.width, height), text);
            _curY += height + 2f;
        }

        public string TextField(string label, string current, float height = 24f)
        {
            GUI.Label(new Rect(_bounds.x, _curY, 120f, height), label);
            string val = GUI.TextField(new Rect(_bounds.x + 125f, _curY, _bounds.width - 130f, height), current ?? "");
            _curY += height + 4f;
            return val;
        }

        public bool Toggle(ref bool value, string label, float height = 24f)
        {
            Rect r = new Rect(_bounds.x, _curY, _bounds.width, height);
            bool hover = r.Contains(Event.current.mousePosition);

            Color bg = value ? new Color(0.06f, 0.20f, 0.22f, 0.70f) : (hover ? new Color(0.12f, 0.15f, 0.24f, 0.60f) : new Color(0.08f, 0.10f, 0.16f, 0.50f));
            Color border = value ? new Color(0.00f, 1.00f, 0.70f, 0.60f) : new Color(0.20f, 0.25f, 0.35f, 0.40f);
            Theme.DrawPanel(r, bg, border);

            string indicator = value ? "<color=#00FFAA>[● ON]</color>" : "<color=#667788>[○ OFF]</color>";
            string formattedText = $"  {indicator}  <color=#E0E8F0>{label}</color>";

            if (GUI.Button(r, formattedText, GUI.skin.label))
            {
                value = !value;
            }

            _curY += height + 4f;
            return value;
        }

        public bool Toggle(string label, bool value, float height = 24f)
        {
            bool v = value;
            return Toggle(ref v, label, height);
        }

        public string RowTextField(string current, float width)
        {
            string val = GUI.TextField(new Rect(_curX, _curY, width, _rowHeight), current ?? "");
            _curX += width + 4f;
            return val;
        }

        public float Slider(string label, ref float value, float min, float max, string format = "F1", float height = 36f)
        {
            GUI.Label(new Rect(_bounds.x, _curY, _bounds.width, 18f), $"<b>{label}:</b> <color=#00FFB0>{value.ToString(format)}</color>");
            _curY += 18f;
            value = GUI.HorizontalSlider(new Rect(_bounds.x, _curY, _bounds.width, 16f), value, min, max);
            _curY += 18f + 4f;
            return value;
        }

        public bool Button(string text, float width = -1f, float height = 26f)
        {
            float w = width > 0 ? width : _bounds.width;
            Rect r = new Rect(_bounds.x, _curY, w, height);

            bool hover = r.Contains(Event.current.mousePosition);
            Color bg = hover ? Theme.ButtonHoverBg : Theme.ButtonNormalBg;
            Color border = hover ? Theme.CyanAccent : Theme.BorderMuted;
            Theme.DrawPanel(r, bg, border);

            bool pressed = GUI.Button(r, text, GUI.skin.label);
            _curY += height + 4f;
            return pressed;
        }

        public void BeginRow(float height = 26f)
        {
            _inRow = true;
            _curX = _bounds.x;
            _rowHeight = height;
        }

        public bool RowButton(string text, float width)
        {
            if (width <= 0) width = 160f;
            Rect r = new Rect(_curX, _curY, width, _rowHeight);
            bool hover = r.Contains(Event.current.mousePosition);
            Color bg = hover ? Theme.ButtonHoverBg : Theme.ButtonNormalBg;
            Color border = hover ? Theme.CyanAccent : Theme.BorderMuted;
            Theme.DrawPanel(r, bg, border);

            bool pressed = GUI.Button(r, text, GUI.skin.label);
            _curX += width + 4f;
            return pressed;
        }

        public bool RowToggle(ref bool value, string text, float width)
        {
            Rect r = new Rect(_curX, _curY, width, _rowHeight);
            Color bg = value ? new Color(0.06f, 0.20f, 0.22f, 0.70f) : new Color(0.08f, 0.10f, 0.16f, 0.50f);
            Color border = value ? Theme.CyanAccent : Theme.BorderMuted;
            Theme.DrawPanel(r, bg, border);

            string indicator = value ? "<color=#00FFAA>●</color>" : "<color=#667788>○</color>";
            if (GUI.Button(r, $"{indicator} {text}", GUI.skin.label))
            {
                value = !value;
            }

            _curX += width + 4f;
            return value;
        }

        public void RowLabel(string text, float width)
        {
            GUI.Label(new Rect(_curX, _curY, width, _rowHeight), text);
            _curX += width + 4f;
        }

        public void EndRow()
        {
            _inRow = false;
            _curY += _rowHeight + 4f;
            _curX = _bounds.x;
        }

        public Color ColorSlider(string label, Color c)
        {
            GUI.Label(new Rect(_bounds.x, _curY, _bounds.width, 18f), $"<b>{label}:</b> R={c.r:F2} G={c.g:F2} B={c.b:F2} A={c.a:F2}");
            _curY += 18f;

            float w = (_bounds.width - 24f) / 4f;
            c.r = GUI.HorizontalSlider(new Rect(_bounds.x + 0 * (w + 6f), _curY, w, 14f), c.r, 0f, 1f);
            c.g = GUI.HorizontalSlider(new Rect(_bounds.x + 1 * (w + 6f), _curY, w, 14f), c.g, 0f, 1f);
            c.b = GUI.HorizontalSlider(new Rect(_bounds.x + 2 * (w + 6f), _curY, w, 14f), c.b, 0f, 1f);
            c.a = GUI.HorizontalSlider(new Rect(_bounds.x + 3 * (w + 6f), _curY, w, 14f), c.a, 0f, 1f);
            _curY += 18f;

            return c;
        }

        // Tactical Visual Badges
        public static string GetCardBadge(int type)
        {
            return type switch
            {
                1 => "<b><color=#FFD700>[ K ]</color></b>",
                2 => "<b><color=#FF66CC>[ Q ]</color></b>",
                3 => "<b><color=#00E5FF>[ A ]</color></b>",
                4 => "<b><color=#FFB300>[ J ]</color></b>",
                -1 or 5 => "<b><color=#FF3344>[ DEVIL ]</color></b>",
                _ => $"<b><color=#AAAAAA>[ {type} ]</color></b>"
            };
        }

        public static string GetTexasCardBadge(int cardId)
        {
            if (cardId <= 0) return "<color=#667788>[Hidden]</color>";
            if (cardId > 52) return $"<b><color=#AAAAAA>[ #{cardId} ]</color></b>";

            int val = ((cardId - 1) / 4) + 2;
            int rem = cardId % 4;
            int suit = rem switch
            {
                1 => 0, // Clubs (♣)
                2 => 1, // Diamonds (♦)
                3 => 2, // Hearts (♥)
                0 => 3, // Spades (♠)
                _ => 0
            };

            string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            string[] suits = { "♣", "♦", "♥", "♠" };
            string[] colors = { "#55FF55", "#44AAFF", "#FF5555", "#CCCCCC" };

            return $"<b><color={colors[suit]}>[{ranks[val - 2]}{suits[suit]}]</color></b>";
        }

        public static string GetDiceBadge(int val)
        {
            return val switch
            {
                1 => "<color=#55FFAA>[ ⚀ 1 ]</color>",
                2 => "<color=#55FFAA>[ ⚁ 2 ]</color>",
                3 => "<color=#55FFAA>[ ⚂ 3 ]</color>",
                4 => "<color=#55FFAA>[ ⚃ 4 ]</color>",
                5 => "<color=#55FFAA>[ ⚄ 5 ]</color>",
                6 => "<color=#55FFAA>[ ⚅ 6 ]</color>",
                _ => $"<color=#888888>[ {val} ]</color>"
            };
        }

        public static string GetRevolverRadar(int curChamber, int bullet)
        {
            string s = "[ ";
            for (int i = 0; i < 6; i++)
            {
                bool isCur = (i == curChamber);
                bool isBul = (i == bullet);

                if (isCur && isBul) s += "<color=#FF3333>●</color> "; // Deadly!
                else if (isCur) s += "<color=#00FFB0>◉</color> "; // Current chamber
                else if (isBul) s += "<color=#FF8800>●</color> "; // Bullet
                else s += "<color=#556677>○</color> "; // Empty
            }
            s += "]";
            return s;
        }
    }
}
