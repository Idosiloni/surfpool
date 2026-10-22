using UnityEngine;
using UnityEngine.UIElements;

namespace SurfPool
{
    /// <summary>
    /// A ring that fills clockwise from the top: around the crosshair while you hold E (blending, shaping, repairing),
    /// and around the park level badge for XP (with its own colors and width).
    /// </summary>
    [UxmlElement]
    public partial class HoldRing : VisualElement
    {
        float progress;

        public float LineWidth { get; set; } = 6f;
        public Color TrackColor { get; set; } = new(0.04f, 0.18f, 0.23f, 0.55f);
        public Color FillColor { get; set; } = new(0.08f, 0.72f, 0.65f);
        public Color FullColor { get; set; } = new(1f, 0.78f, 0.34f);

        public float Progress
        {
            get => progress;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value, progress)) return;
                progress = value;
                MarkDirtyRepaint();
            }
        }

        public HoldRing()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) / 2f - LineWidth * 0.5f - 1f;
            var painter = context.painter2D;
            painter.lineWidth = LineWidth;
            painter.lineCap = LineCap.Round;
            painter.strokeColor = TrackColor;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(0f), Angle.Degrees(360f));
            painter.Stroke();
            if (progress <= 0f) return;
            painter.strokeColor = progress >= 1f ? FullColor : FillColor;
            painter.BeginPath();
            painter.Arc(center, radius, Angle.Degrees(-90f), Angle.Degrees(-90f + 360f * progress));
            painter.Stroke();
        }
    }
}
