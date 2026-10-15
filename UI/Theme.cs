using System;
using UnityEngine;

namespace LiarsBarMod.UI
{
    public static class Theme
    {
        private static Texture2D _whiteTex;
        public static Texture2D WhiteTex
        {
            get
            {
                if (_whiteTex == null)
                {
                    _whiteTex = new Texture2D(1, 1);
                    _whiteTex.SetPixel(0, 0, Color.white);
                    _whiteTex.Apply();
                }
                return _whiteTex;
            }
        }

        // Palette
        public static readonly Color WindowBg = new Color(0.04f, 0.05f, 0.09f, 0.96f);
        public static readonly Color HeaderBg = new Color(0.07f, 0.09f, 0.15f, 0.98f);
        public static readonly Color ContentBg = new Color(0.06f, 0.07f, 0.13f, 0.96f);
        public static readonly Color BorderCol = new Color(0.00f, 0.90f, 0.65f, 0.35f);
        public static readonly Color BorderMuted = new Color(0.18f, 0.22f, 0.35f, 0.50f);

        public static readonly Color CyanAccent = new Color(0.00f, 1.00f, 0.70f, 1.0f);
        public static readonly Color MintAccent = new Color(0.33f, 1.00f, 0.66f, 1.0f);
        public static readonly Color GoldAccent = new Color(1.00f, 0.84f, 0.00f, 1.0f);
        public static readonly Color CrimsonAccent = new Color(1.00f, 0.25f, 0.30f, 1.0f);

        public static readonly Color TabActiveBg = new Color(0.08f, 0.22f, 0.32f, 0.95f);
        public static readonly Color TabInactiveBg = new Color(0.09f, 0.11f, 0.18f, 0.90f);
        public static readonly Color ButtonNormalBg = new Color(0.11f, 0.14f, 0.24f, 0.85f);
        public static readonly Color ButtonHoverBg = new Color(0.16f, 0.22f, 0.36f, 0.95f);

        public static void DrawRect(Rect rect, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTex);
            GUI.color = old;
        }

        public static void DrawBorder(Rect rect, Color color, float thickness = 1f)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color); // Top
            DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color); // Bottom
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color); // Left
            DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color); // Right
        }

        public static void DrawPanel(Rect rect, Color bg, Color border, float borderThickness = 1f)
        {
            DrawRect(rect, bg);
            if (border.a > 0.01f)
            {
                DrawBorder(rect, border, borderThickness);
            }
        }
    }
}
