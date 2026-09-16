using System;
using Blish_HUD;
using Blish_HUD.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkillFocus
{
    /// <summary>
    /// A draggable labeled box used only during calibration to let the player mark
    /// where each skill bar slot actually is on their screen. Unlike HighlightControl,
    /// this one intentionally captures mouse input so it can be dragged.
    /// </summary>
    public class CalibrationMarker : Control
    {
        public Slot Slot { get; }

        private bool _dragging;
        private Point _dragOffset;

        public CalibrationMarker(Slot slot)
        {
            Slot = slot;
            ZIndex = int.MaxValue - 5;

            LeftMouseButtonPressed += (s, e) =>
            {
                _dragging = true;
                _dragOffset = Input.Mouse.Position - this.Location;
            };

            LeftMouseButtonReleased += (s, e) => { _dragging = false; };
        }

        protected override CaptureType CapturesInput() => CaptureType.Mouse;

        public override void DoUpdate(GameTime gameTime)
        {
            if (_dragging)
            {
                this.Location = Input.Mouse.Position - _dragOffset;
            }
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds)
        {
            var pixel = ContentService.Textures.Pixel;
            Color fill = _dragging ? Color.LimeGreen * 0.55f : Color.OrangeRed * 0.45f;
            Color border = _dragging ? Color.LimeGreen : Color.OrangeRed;

            spriteBatch.DrawOnCtrl(this, pixel, bounds, fill);

            const int t = 2;
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, 0, bounds.Width, t), border);
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, bounds.Height - t, bounds.Width, t), border);
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(0, 0, t, bounds.Height), border);
            spriteBatch.DrawOnCtrl(this, pixel, new Rectangle(bounds.Width - t, 0, t, bounds.Height), border);

            spriteBatch.DrawStringOnCtrl(
                this,
                Slot.ShortLabel(),
                GameService.Content.DefaultFont14,
                bounds,
                Color.White,
                false,
                Blish_HUD.Controls.HorizontalAlignment.Center,
                Blish_HUD.Controls.VerticalAlignment.Middle);
        }
    }
}
