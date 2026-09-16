using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkillFocus
{
    /// <summary>
    /// A click-through pulsing rectangle outline drawn directly over a skill bar slot.
    /// Never captures mouse input, so it never interferes with clicking the real skill bar.
    /// </summary>
    public class HighlightControl : Control
    {
        private const int BorderThickness = 3;

        public Color BorderColor { get; set; } = Color.Gold;

        private double _pulseTime;

        public HighlightControl()
        {
            ZIndex = int.MaxValue - 10;
        }

        protected override CaptureType CapturesInput() => CaptureType.None;

        public override void DoUpdate(GameTime gameTime)
        {
            _pulseTime += gameTime.ElapsedGameTime.TotalSeconds;
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            // Pulse opacity between ~55% and 100% so the box stays noticeable in peripheral vision
            // without a moving/animated shape that would pull focus away from the skill bar itself.
            float pulse = 0.775f + 0.225f * (float)System.Math.Sin(_pulseTime * 4.0);
            Color color = BorderColor * pulse;

            var pixel = ContentService.Textures.Pixel;

            // Top
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, 0, bounds.Width, BorderThickness), color);
            // Bottom
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, bounds.Height - BorderThickness, bounds.Width, BorderThickness), color);
            // Left
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, 0, BorderThickness, bounds.Height), color);
            // Right
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(bounds.Width - BorderThickness, 0, BorderThickness, bounds.Height), color);
        }
    }
}
