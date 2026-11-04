using System.Drawing.Drawing2D;

namespace PawFamily;

public sealed class PetArt : Control
{
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Color Fur { get; set; } = Color.FromArgb(237, 180, 116);
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Animal { get; set; } = "cat";
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public Mood Mood { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public double Phase { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public string Pack { get; set; } = "";
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool Walking { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool FacingLeft { get; set; }
    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    public bool ProjectBadge { get; set; }
    string loaded = ""; Image? sprite; MemoryStream? spriteData;
    public PetArt() { SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true); BackColor = Color.Transparent; Cursor = Cursors.Hand; }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
        string state = Mood switch { Mood.Attention or Mood.Error => "attention", Mood.Done => "done", _ when Walking => "walk", Mood.Working => "working", _ => "idle" };
        var file = Pack == "" ? "" : Path.Combine(Pack, state + ".gif");
        if (state == "walk" && Pack != "" && !File.Exists(file)) file = Path.Combine(Pack, "idle.gif");
        if (loaded != file)
        {
            ReleaseSprite();
            loaded = file;
            if (File.Exists(file)) { try { spriteData = new MemoryStream(File.ReadAllBytes(file)); sprite = Image.FromStream(spriteData); ImageAnimator.Animate(sprite, Frame); } catch { ReleaseSprite(); } }
        }
        if (sprite != null)
        {
            ImageAnimator.UpdateFrames(sprite); var scale = Math.Min((float)Width / sprite.Width, (float)Height / sprite.Height);
            var transform = g.Save();
            // Mirror only walking so text in other GIFs stays readable.
            bool nativeLeft = File.Exists(Path.Combine(Pack, "walk-faces-left.txt"));
            if (Walking && FacingLeft != nativeLeft) { g.TranslateTransform(Width, 0); g.ScaleTransform(-1, 1); }
            g.DrawImage(sprite, (Width - sprite.Width * scale) / 2, (Height - sprite.Height * scale) / 2, sprite.Width * scale, sprite.Height * scale);
            g.Restore(transform);
            if (ProjectBadge) { using var ring = new SolidBrush(Fur); g.FillEllipse(ring, 3, Height - 19, 16, 16); }
            if (Mood is Mood.Attention or Mood.Error)
            {
                float diameter = Math.Min(28, Width * .23f), pulse = (float)(1 + .045 * Math.Sin(Phase * 3)); diameter *= pulse;
                using var badge = new SolidBrush(Mood == Mood.Error ? Color.Salmon : Color.FromArgb(255, 213, 101));
                g.FillEllipse(badge, Width - diameter - 3, 3, diameter, diameter);
                using var font = new Font("Segoe UI", diameter * .55f, FontStyle.Bold, GraphicsUnit.Pixel);
                using var inkBrush = new SolidBrush(Color.FromArgb(35, 31, 43));
                using var centered = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString("!", font, inkBrush, new RectangleF(Width - diameter - 3, 2, diameter, diameter), centered);
            }
            return;
        }
        g.ScaleTransform(Width / 140f, Height / 140f);
        if (Walking && FacingLeft) { g.TranslateTransform(140, 0); g.ScaleTransform(-1, 1); }
        var bounce = Mood == Mood.Done ? Math.Abs(Math.Sin(Phase * 1.9)) * 11 : Walking ? Math.Abs(Math.Sin(Phase * 4)) * 3 : Mood == Mood.Working ? Math.Sin(Phase * 2) * 2 : Math.Sin(Phase) * 1.5;
        using var shadow = new SolidBrush(Color.FromArgb(55, 30, 25, 45)); g.FillEllipse(shadow, 29, 119, 83, 10);
        g.TranslateTransform(0, (float)-bounce);
        using var fur = new SolidBrush(Fur); using var dark = new SolidBrush(Color.FromArgb(40, 34, 49)); using var cream = new SolidBrush(Color.FromArgb(255, 239, 216));
        using var tail = new Pen(Fur, 13) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        g.DrawBezier(tail, 104, 102, 134, 105, 130, 78, 120 + (float)Math.Sin(Phase * 2) * 5, 83);
        g.FillEllipse(fur, 39, 71, 66, 51); g.FillEllipse(cream, 53, 84, 35, 34);
        float stride = Walking ? (float)Math.Sin(Phase * 4) * 7 : 0;
        g.FillEllipse(fur, 32 + stride, 110, 31, 15); g.FillEllipse(fur, 82 - stride, 110, 29, 15);
        if (Animal == "dog")
        {
            using var ear = new SolidBrush(ControlPaint.Dark(Fur, .2f)); g.FillEllipse(ear, 20, 30, 25, 56); g.FillEllipse(ear, 95, 30, 25, 56);
        }
        else
        {
            g.FillPolygon(fur, [new(31, 49), new(30, 11), new(60, 33)]); g.FillPolygon(fur, [new(81, 32), new(111, 12), new(110, 52)]);
            using var pink = new SolidBrush(Color.FromArgb(239, 151, 160)); g.FillPolygon(pink, [new(37, 37), new(36, 21), new(49, 33)]); g.FillPolygon(pink, [new(92, 33), new(105, 22), new(104, 39)]);
        }
        g.FillEllipse(fur, 24, 29, 94, 67);
        var blink = Math.Sin(Phase * .6) > .985;
        using var ink = new Pen(Color.FromArgb(47, 39, 46), 3) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        if (Mood == Mood.Done) { g.DrawArc(ink, 43, 54, 13, 12, 195, 155); g.DrawArc(ink, 84, 54, 13, 12, 195, 155); }
        else { g.FillEllipse(dark, 45, 54, 8, blink ? 2 : 13); g.FillEllipse(dark, 87, 54, 8, blink ? 2 : 13); }
        using var blush = new SolidBrush(Color.FromArgb(244, 143, 149)); g.FillEllipse(blush, 34, 70, 15, 6); g.FillEllipse(blush, 95, 70, 15, 6);
        g.FillEllipse(cream, 56, 67, 30, 22); g.FillEllipse(dark, 66, 69, 9, 6); g.DrawArc(ink, 59, 73, 12, 8, 0, 160); g.DrawArc(ink, 71, 73, 12, 8, 20, 160);
        if (Mood == Mood.Attention || Mood == Mood.Error)
        {
            var lift = (float)Math.Sin(Phase * 3) * 4; g.FillEllipse(fur, 7, 65 + lift, 24, 31);
            using var badge = new SolidBrush(Mood == Mood.Error ? Color.Salmon : Color.FromArgb(255, 215, 103)); g.FillEllipse(badge, 104, 0, 29, 29);
            using var font = new Font("Segoe UI", 16, FontStyle.Bold); g.DrawString("!", font, dark, 112, 0);
        }
        if (Mood == Mood.Done) { using var mint = new SolidBrush(Color.FromArgb(135, 230, 185)); g.FillEllipse(mint, 107, 4, 25, 25); g.DrawLines(ink, [new(112, 17), new(117, 22), new(126, 11)]); }
    }
    void Frame(object? sender, EventArgs e) { if (!IsDisposed && IsHandleCreated) { try { BeginInvoke(Invalidate); } catch (InvalidOperationException) { } } }
    void ReleaseSprite() { if (sprite != null) { ImageAnimator.StopAnimate(sprite, Frame); sprite.Dispose(); sprite = null; } spriteData?.Dispose(); spriteData = null; }
    protected override void Dispose(bool disposing) { if (disposing) ReleaseSprite(); base.Dispose(disposing); }
}
