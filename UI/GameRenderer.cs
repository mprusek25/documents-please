using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using DocumentsPlease.Logic;
using DocumentsPlease.Models;

namespace DocumentsPlease.UI;

/// <summary>
/// Veškeré vykreslování (GDI+). Čte stav z GameEngine, sám nic neřídí.
/// Kreslí do logického plátna 1200x720, které se rovnoměrně škáluje do okna.
/// </summary>
public sealed class GameRenderer : IDisposable
{
    public const int W = 1200;
    public const int H = 720;

    private static readonly Color[] SkinTones =
    {
        Color.FromArgb(255, 224, 189), Color.FromArgb(234, 192, 134),
        Color.FromArgb(198, 134, 66), Color.FromArgb(141, 85, 36)
    };

    private static readonly Color[] HairColors =
    {
        Color.FromArgb(35, 28, 24), Color.FromArgb(88, 56, 32), Color.FromArgb(206, 168, 84),
        Color.FromArgb(158, 70, 34), Color.FromArgb(170, 170, 172)
    };

    private static readonly Color[] Jackets =
    {
        Color.FromArgb(60, 70, 110), Color.FromArgb(90, 50, 50), Color.FromArgb(50, 90, 70),
        Color.FromArgb(80, 80, 85), Color.FromArgb(110, 90, 50), Color.FromArgb(70, 50, 90)
    };

    private static readonly HashSet<ZombieSign> NoSigns = new();

    private static readonly StringFormat Centered = new() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
    private static readonly StringFormat CenterTop = new() { Alignment = StringAlignment.Center };
    private static readonly StringFormat RightAlign = new() { Alignment = StringAlignment.Far };

    private readonly Font _title = new("Segoe UI", 46, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _big = new("Segoe UI", 30, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _h2 = new("Segoe UI", 19, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _text = new("Segoe UI", 14, FontStyle.Regular, GraphicsUnit.Pixel);
    private readonly Font _bold = new("Segoe UI", 14, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _italic = new("Segoe UI", 13, FontStyle.Italic, GraphicsUnit.Pixel);
    private readonly Font _small = new("Segoe UI", 12, FontStyle.Regular, GraphicsUnit.Pixel);
    private readonly Font _smallBold = new("Segoe UI", 12, FontStyle.Bold, GraphicsUnit.Pixel);
    private readonly Font _mono = new("Consolas", 15, FontStyle.Bold, GraphicsUnit.Pixel);

    public void Dispose()
    {
        _title.Dispose();
        _big.Dispose();
        _h2.Dispose();
        _text.Dispose();
        _bold.Dispose();
        _italic.Dispose();
        _small.Dispose();
        _smallBold.Dispose();
        _mono.Dispose();
    }

    // ------------------------------------------------------------------ vstupní bod

    public void Render(Graphics g, Size size, GameEngine engine, float time)
    {
        g.Clear(Color.FromArgb(8, 9, 11));

        float scale = Math.Min(size.Width / (float)W, size.Height / (float)H);
        if (scale <= 0.01f) return;

        g.TranslateTransform((size.Width - W * scale) / 2f, (size.Height - H * scale) / 2f);
        g.ScaleTransform(scale, scale);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.SetClip(new Rectangle(0, 0, W, H));

        using (var bg = new SolidBrush(Color.FromArgb(24, 26, 31)))
            g.FillRectangle(bg, 0, 0, W, H);

        switch (engine.State)
        {
            case GameState.MainMenu:
                DrawMainMenu(g, time);
                break;
            case GameState.ShiftIntro:
                DrawHud(g, engine);
                DrawManual(g, engine.Directive, new Rectangle((W - 520) / 2, 70, 520, H - 90));
                break;
            case GameState.Inspecting:
            case GameState.ShowingResult:
                DrawDesk(g, engine, time);
                break;
            case GameState.ShiftSummary:
                DrawHud(g, engine);
                DrawShiftSummary(g, engine);
                break;
            case GameState.GameOver:
                DrawGameOver(g, engine);
                break;
        }
    }

    // ------------------------------------------------------------------ obrazovky

    private void DrawMainMenu(Graphics g, float time)
    {
        float gate = MathF.Sin(time * 17f) * MathF.Sin(time * 5.3f);
        int alpha = gate > 0.92f ? 140 : 255;

        using (var titleBrush = new SolidBrush(Color.FromArgb(alpha, 230, 70, 60)))
            g.DrawString("DOCUMENTS, PLEASE", _title, titleBrush, new RectangleF(0, 60, W, 60), CenterTop);

        g.DrawString("Hraniční kontrola. Papíry. Zombie.", _h2, Brushes.Gainsboro, new RectangleF(0, 128, W, 30), CenterTop);

        var panel = new Rectangle(230, 190, 740, 440);
        using (var fill = new SolidBrush(Color.FromArgb(34, 37, 46)))
            g.FillRectangle(fill, panel);
        using (var border = new Pen(Color.FromArgb(80, 90, 110), 2))
            g.DrawRectangle(border, panel);

        float x = panel.X + 24, w = panel.Width - 48, y = panel.Y + 18;
        g.DrawString("JAK HRÁT", _h2, Brushes.White, x, y);
        y += 34;

        string[] lines =
        {
            "Každou směnu dostaneš Direktivu: povolený sektor, barvu razítka a minimální rok platnosti dokladů.",
            "Porovnej občanský průkaz a povolení – jména, data narození, fotografii, platnost, sektor i razítko.",
            "Pozor: zombie mohou mít dokonale platné doklady! Sleduj vzhled a chování. Prohlídka odhalí i skryté příznaky, ale stojí kredity.",
            "Rozhoduj: PUSTIT [1]   ZADRŽET [2]   POPLACH [3]   PROHLÍDKA [I]   DALŠÍ [Enter]"
        };

        foreach (var line in lines)
            y = Paragraph(g, line, _text, Brushes.Gainsboro, x, y, w) + 10;

        y += 6;
        g.DrawString("SAZEBNÍK", _bold, Brushes.White, x, y);
        y += 24;

        string[] tariffs =
        {
            $"Správné rozhodnutí {Signed(EconomyManager.RewardCorrect)}   •   Poplach na zombie {Signed(EconomyManager.RewardAlarmBonus)}",
            $"Vpuštěná zombie {Signed(EconomyManager.PenaltyZombieLetIn)} a varování (po {PlayerStats.MaxWarnings} je konec hry)",
            $"Zadržený nevinný {Signed(EconomyManager.PenaltyDetainInnocent)}   •   Přehlédnutá chyba v dokladech {Signed(EconomyManager.PenaltyMissedAdminError)}",
            $"Planý poplach {Signed(EconomyManager.PenaltyFalseAlarm)}   •   Prohlídka {Signed(-EconomyManager.InspectionCost)}   •   Nájem po směně {Signed(-EconomyManager.DailyUpkeep)}"
        };

        foreach (var line in tariffs)
        {
            g.DrawString(line, _text, Brushes.Khaki, x, y);
            y += 22;
        }

        g.DrawString("Stiskni ENTER nebo klikni na tlačítko dole.", _text, Brushes.Gray,
            new RectangleF(panel.X, panel.Bottom - 34, panel.Width, 24), CenterTop);
    }

    private void DrawHud(Graphics g, GameEngine e)
    {
        using (var bar = new SolidBrush(Color.FromArgb(14, 15, 18)))
            g.FillRectangle(bar, 0, 0, W, 54);

        int credits = e.Stats.Credits;
        g.DrawString($"KREDITY  {credits}", _h2, credits >= 0 ? Brushes.LimeGreen : Brushes.OrangeRed, 18, 14);

        g.DrawString("VAROVÁNÍ", _bold, Brushes.Gainsboro, 270, 17);
        for (int i = 0; i < PlayerStats.MaxWarnings; i++)
        {
            var circle = new RectangleF(360 + i * 26, 16, 18, 18);
            if (i < e.Stats.Warnings) g.FillEllipse(Brushes.OrangeRed, circle);
            else g.DrawEllipse(Pens.Gray, circle);
        }

        g.DrawString($"DEN {e.Stats.Day}  •  {e.Directive.Date:dd.MM.yyyy}", _h2, Brushes.White, 520, 14);

        g.DrawString($"Návštěvník {e.VisitorNumber} / {e.VisitorsTotal}", _bold, Brushes.Gainsboro,
            new RectangleF(W - 260, 17, 242, 24), RightAlign);
    }

    private void DrawDesk(Graphics g, GameEngine e, float time)
    {
        DrawHud(g, e);

        var v = e.CurrentVisitor;
        if (v == null) return;

        var booth = new Rectangle(10, 64, 360, H - 74);
        DrawBooth(g, booth, v, time);

        DrawPassport(g, v.Passport, new Rectangle(384, 70, 350, 210));
        DrawPermit(g, v.Permit, new Rectangle(384, 292, 350, 250));
        DrawInspection(g, e, new Rectangle(384, 554, 350, H - 564));
        DrawManual(g, e.Directive, new Rectangle(748, 64, W - 758, H - 74));

        if (e.State == GameState.ShowingResult && e.LastResult != null)
            DrawResultCard(g, e, booth);
    }

    private void DrawShiftSummary(Graphics g, GameEngine e)
    {
        var report = e.LastShiftReport;
        if (report == null) return;

        var panel = new Rectangle(300, 90, 600, 560);
        using (var fill = new SolidBrush(Color.FromArgb(34, 37, 46)))
            g.FillRectangle(fill, panel);
        using (var border = new Pen(Color.FromArgb(80, 90, 110), 2))
            g.DrawRectangle(border, panel);

        float x = panel.X + 30, w = panel.Width - 60, y = panel.Y + 24;
        g.DrawString($"KONEC SMĚNY – DEN {report.Day}", _big, Brushes.White, x, y);
        y += 62;

        y = Row(g, "Obslouženo návštěvníků", report.Processed.ToString(), Brushes.White, x, y, w);
        y = Row(g, "Správných rozhodnutí", report.Correct.ToString(), Brushes.LimeGreen, x, y, w);
        y = Row(g, "Chybných rozhodnutí", report.Wrong.ToString(), report.Wrong > 0 ? Brushes.OrangeRed : Brushes.White, x, y, w);
        y += 14;
        y = Row(g, "Příjem a pokuty z rozhodnutí", Signed(report.DecisionCredits), report.DecisionCredits >= 0 ? Brushes.LimeGreen : Brushes.OrangeRed, x, y, w);
        y = Row(g, "Prohlídky", Signed(report.InspectionCredits), Brushes.Gainsboro, x, y, w);
        y = Row(g, "Nájem a živobytí", Signed(report.UpkeepCredits), Brushes.OrangeRed, x, y, w);
        y += 14;

        using (var sep = new Pen(Color.FromArgb(80, 90, 110), 1.5f))
            g.DrawLine(sep, x, y, x + w, y);
        y += 14;

        g.DrawString("ZŮSTATEK", _h2, Brushes.White, x, y);
        g.DrawString($"{report.EndingCredits} kr.", _big,
            report.EndingCredits >= 0 ? Brushes.LimeGreen : Brushes.OrangeRed,
            new RectangleF(x, y - 8, w, 40), RightAlign);
        y += 56;

        if (report.GameOver)
            Paragraph(g, e.GameOverReason, _bold, Brushes.OrangeRed, x, y, w);
        else
            Paragraph(g, "Zítra tě čeká další směna a nová direktiva. Odpočiň si – zombie ne.", _text, Brushes.Gainsboro, x, y, w);
    }

    private void DrawGameOver(Graphics g, GameEngine e)
    {
        g.DrawString("KONEC HRY", _title, Brushes.OrangeRed, new RectangleF(0, 110, W, 60), CenterTop);

        var panel = new Rectangle(300, 210, 600, 360);
        using (var fill = new SolidBrush(Color.FromArgb(34, 37, 46)))
            g.FillRectangle(fill, panel);
        using (var border = new Pen(Color.FromArgb(120, 50, 50), 2))
            g.DrawRectangle(border, panel);

        float x = panel.X + 30, w = panel.Width - 60, y = panel.Y + 26;
        y = Paragraph(g, e.GameOverReason, _h2, Brushes.White, x, y, w) + 24;

        y = Row(g, "Odsloužených směn", Math.Max(0, e.Stats.Day - (e.LastShiftReport is { GameOver: true } ? 0 : 1)).ToString(), Brushes.White, x, y, w);
        y = Row(g, "Správných rozhodnutí", e.Stats.TotalCorrect.ToString(), Brushes.LimeGreen, x, y, w);
        y = Row(g, "Chybných rozhodnutí", e.Stats.TotalWrong.ToString(), Brushes.OrangeRed, x, y, w);
        y = Row(g, "Zastavených zombie", e.Stats.ZombiesStopped.ToString(), Brushes.LimeGreen, x, y, w);
        y = Row(g, "Vpuštěných zombie", e.Stats.ZombiesLetIn.ToString(), Brushes.OrangeRed, x, y, w);

        g.DrawString("Stiskni ENTER pro novou hru.", _text, Brushes.Gray,
            new RectangleF(panel.X, panel.Bottom - 40, panel.Width, 24), CenterTop);
    }

    // ------------------------------------------------------------------ okénko a návštěvník

    private void DrawBooth(Graphics g, Rectangle r, Visitor v, float time)
    {
        using (var bg = new LinearGradientBrush(r, Color.FromArgb(44, 48, 64), Color.FromArgb(18, 20, 28), 90f))
            g.FillRectangle(bg, r);

        // zeď za návštěvníkem
        using (var stripe = new Pen(Color.FromArgb(24, 255, 255, 255), 1))
        {
            for (int x = r.X + 30; x < r.Right; x += 60)
                g.DrawLine(stripe, x, r.Y, x, r.Bottom);
        }

        var person = new RectangleF(r.X + (r.Width - 300) / 2f, 260, 300, 390);
        DrawPerson(g, person, v.Face, v.ZombieSigns, v.JacketColor, time, true);

        // pult
        var counter = new Rectangle(r.X, r.Bottom - 70, r.Width, 70);
        using (var wood = new SolidBrush(Color.FromArgb(62, 46, 36)))
            g.FillRectangle(wood, counter);
        using (var edge = new Pen(Color.FromArgb(96, 72, 54), 3))
            g.DrawLine(edge, counter.X, counter.Y, counter.Right, counter.Y);
        g.DrawString("OKÉNKO č. 3  –  KARANTÉNNÍ ZÓNA 7", _smallBold, Brushes.Tan,
            new RectangleF(counter.X, counter.Y + 24, counter.Width, 20), CenterTop);

        using (var frame = new Pen(Color.FromArgb(70, 76, 96), 3))
            g.DrawRectangle(frame, r);

        DrawSpeech(g, new RectangleF(r.X + 16, r.Y + 14, r.Width - 32, 100), v.Greeting);
    }

    private void DrawSpeech(Graphics g, RectangleF r, string text)
    {
        using var path = RoundedRect(r, 12);
        using var fill = new SolidBrush(Color.FromArgb(240, 240, 232));
        g.FillPath(fill, path);
        using (var pen = new Pen(Color.FromArgb(90, 90, 90), 2))
            g.DrawPath(pen, path);

        var tail = new[]
        {
            new PointF(r.X + 150, r.Bottom - 1),
            new PointF(r.X + 182, r.Bottom - 1),
            new PointF(r.X + 166, r.Bottom + 20)
        };
        g.FillPolygon(fill, tail);

        g.DrawString(text, _text, Brushes.Black, new RectangleF(r.X + 12, r.Y + 10, r.Width - 24, r.Height - 20));
    }

    /// <summary>
    /// Nakreslí postavu v souřadnicích 200x260 škálovaných do rect.
    /// Příznaky zombie (signs) mění vzhled a animaci – fotka v OP je kreslí s prázdnou množinou.
    /// </summary>
    private void DrawPerson(Graphics g, RectangleF rect, FaceTraits face, ISet<ZombieSign> signs, int jacket, float time, bool animate)
    {
        var saved = g.Save();
        g.SetClip(rect, CombineMode.Intersect);
        g.TranslateTransform(rect.X, rect.Y);
        g.ScaleTransform(rect.Width / 200f, rect.Height / 260f);

        float dx = 0f;
        float dy = animate ? MathF.Sin(time * 1.8f) * 1.5f : 0f;

        if (animate && signs.Contains(ZombieSign.Twitching))
        {
            float gate = MathF.Sin(time * 11f) * MathF.Sin(time * 3.7f);
            if (gate > 0.55f)
            {
                dx = MathF.Sin(time * 97f) * 3.5f;
                dy += MathF.Cos(time * 83f) * 2.5f;
            }
        }

        Color skin = SkinTones[face.SkinTone % SkinTones.Length];
        if (signs.Contains(ZombieSign.Pallor))
            skin = Blend(skin, Color.FromArgb(148, 176, 150), 0.72f);

        Color hair = HairColors[face.HairColor % HairColors.Length];

        using var skinB = new SolidBrush(skin);
        using var hairB = new SolidBrush(hair);
        using var jacketB = new SolidBrush(Jackets[jacket % Jackets.Length]);

        // tělo a límec
        g.FillEllipse(jacketB, 8f, 182f, 184f, 170f);
        g.FillPolygon(Brushes.WhiteSmoke, new[] { new PointF(86, 178), new PointF(114, 178), new PointF(100, 212) });

        // vlasy dlouhé – za hlavou
        if (face.Hair == HairStyle.Long)
            g.FillEllipse(hairB, 42 + dx, 40 + dy, 116, 150);

        // krk, uši, hlava
        g.FillRectangle(skinB, 84f + dx * 0.4f, 146f, 32f, 44f);
        g.FillEllipse(skinB, 45 + dx, 96 + dy, 14, 26);
        g.FillEllipse(skinB, 141 + dx, 96 + dy, 14, 26);
        g.FillEllipse(skinB, 52 + dx, 38 + dy, 96, 124);

        // vlasy – přední část
        switch (face.Hair)
        {
            case HairStyle.Short:
            case HairStyle.Long:
                g.FillPie(hairB, 50 + dx, 30 + dy, 100, 90, 180, 180);
                break;
            case HairStyle.Curly:
                for (int i = 0; i < 7; i++)
                {
                    float a = MathF.PI * (1f + i / 6f);
                    g.FillEllipse(hairB, 100 + dx + MathF.Cos(a) * 46 - 13, 76 + dy + MathF.Sin(a) * 44 - 13, 26, 26);
                }
                break;
        }

        // oči
        bool milky = signs.Contains(ZombieSign.MilkyEyes);
        Color white = milky ? Color.FromArgb(205, 210, 205) : Color.White;
        using (var whiteB = new SolidBrush(white))
        {
            g.FillEllipse(whiteB, 72 + dx, 90 + dy, 22, 14);
            g.FillEllipse(whiteB, 106 + dx, 90 + dy, 22, 14);
        }

        if (milky)
        {
            using var pale = new SolidBrush(Color.FromArgb(228, 232, 236));
            g.FillEllipse(pale, 78 + dx, 91 + dy, 10, 12);
            g.FillEllipse(pale, 112 + dx, 91 + dy, 10, 12);
        }
        else
        {
            using var iris = new SolidBrush(Color.FromArgb(52, 72, 104));
            g.FillEllipse(iris, 78 + dx, 91 + dy, 10, 12);
            g.FillEllipse(iris, 112 + dx, 91 + dy, 10, 12);
            g.FillEllipse(Brushes.Black, 81 + dx, 94 + dy, 4, 6);
            g.FillEllipse(Brushes.Black, 115 + dx, 94 + dy, 4, 6);
        }

        if (signs.Contains(ZombieSign.Pallor))
        {
            using var bags = new SolidBrush(Color.FromArgb(95, 70, 40, 90));
            g.FillEllipse(bags, 70 + dx, 101 + dy, 26, 9);
            g.FillEllipse(bags, 104 + dx, 101 + dy, 26, 9);
        }

        // obočí a nos
        using (var browPen = new Pen(hair, 3f))
        {
            g.DrawLine(browPen, 70 + dx, 84 + dy, 94 + dx, 82 + dy);
            g.DrawLine(browPen, 106 + dx, 82 + dy, 130 + dx, 84 + dy);
        }

        using (var nosePen = new Pen(Blend(skin, Color.Black, 0.28f), 2f))
        {
            g.DrawLine(nosePen, 100 + dx, 100 + dy, 96 + dx, 120 + dy);
            g.DrawLine(nosePen, 96 + dx, 120 + dy, 104 + dx, 121 + dy);
        }

        // vousy, ústa
        if (face.Beard)
        {
            g.FillPie(hairB, 54 + dx, 78 + dy, 92, 88, 0, 180);
            g.FillEllipse(skinB, 87 + dx, 125 + dy, 26, 16);
        }

        if (signs.Contains(ZombieSign.Groaning))
        {
            using var dark = new SolidBrush(Color.FromArgb(70, 20, 25));
            g.FillEllipse(dark, 88 + dx, 126 + dy, 24, 20);
        }
        else
        {
            using var mouthPen = new Pen(Blend(skin, Color.Black, 0.55f), 2.5f);
            g.DrawLine(mouthPen, 88 + dx, 135 + dy, 112 + dx, 135 + dy);
        }

        // brýle
        if (face.Glasses)
        {
            using var glassPen = new Pen(Color.FromArgb(40, 40, 46), 2.5f);
            g.DrawEllipse(glassPen, 66 + dx, 86 + dy, 32, 24);
            g.DrawEllipse(glassPen, 102 + dx, 86 + dy, 32, 24);
            g.DrawLine(glassPen, 98 + dx, 96 + dy, 102 + dx, 96 + dy);
        }

        // jizvy se stehy
        if (signs.Contains(ZombieSign.Scars))
        {
            using var scar = new Pen(Color.FromArgb(150, 40, 40), 3f);
            using var stitch = new Pen(Color.FromArgb(40, 20, 20), 1.5f);

            g.DrawLine(scar, 62 + dx, 54 + dy, 90 + dx, 116 + dy);
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                float sx = 62 + 28 * t + dx;
                float sy = 54 + 62 * t + dy;
                g.DrawLine(stitch, sx - 4, sy + 3, sx + 4, sy - 3);
            }

            g.DrawLine(scar, 124 + dx, 112 + dy, 138 + dx, 148 + dy);
            for (int i = 0; i < 3; i++)
            {
                float t = i / 2f;
                float sx = 124 + 14 * t + dx;
                float sy = 112 + 36 * t + dy;
                g.DrawLine(stitch, sx - 4, sy + 2, sx + 4, sy - 2);
            }
        }

        // slintání
        if (signs.Contains(ZombieSign.Drooling))
        {
            float len = 14f + (animate ? (MathF.Sin(time * 1.5f) + 1f) * 7f : 0f);
            using var dropPen = new Pen(Color.FromArgb(210, 190, 225, 232), 3f);
            using var dropB = new SolidBrush(Color.FromArgb(210, 190, 225, 232));
            g.DrawLine(dropPen, 106 + dx, 140 + dy, 108 + dx, 140 + len + dy);
            g.FillEllipse(dropB, 104 + dx, 138 + len + dy, 8, 8);
        }

        g.Restore(saved);
    }

    private void DrawPhoto(Graphics g, FaceTraits face, Rectangle r)
    {
        using (var bg = new SolidBrush(Color.FromArgb(170, 200, 225)))
            g.FillRectangle(bg, r);

        DrawPerson(g, r, face, NoSigns, 0, 0f, false);

        using var pen = new Pen(Color.FromArgb(70, 90, 120), 2);
        g.DrawRectangle(pen, r);
    }

    // ------------------------------------------------------------------ doklady a manuál

    private void DrawPassport(Graphics g, PassportDocument d, Rectangle r)
    {
        DrawPaper(g, r, Color.FromArgb(214, 226, 240), Color.FromArgb(60, 90, 140));

        using (var header = new SolidBrush(Color.FromArgb(60, 90, 140)))
            g.FillRectangle(header, r.X, r.Y, r.Width, 28);
        g.DrawString("OBČANSKÝ PRŮKAZ", _bold, Brushes.White, r.X + 10, r.Y + 6);
        g.DrawString("KARANTÉNNÍ ZÓNA 7", _smallBold, Brushes.White, new RectangleF(r.X, r.Y + 8, r.Width - 10, 20), RightAlign);

        DrawPhoto(g, d.Photo, new Rectangle(r.X + 12, r.Y + 40, 96, 125));

        float x = r.X + 122, y = r.Y + 38;
        y = Field(g, "Jméno", d.FullName, x, y);
        y = Field(g, "Datum narození", d.BirthDate.ToString("dd.MM.yyyy"), x, y);
        y = Field(g, "Číslo OP", d.IdNumber, x, y);
        Field(g, "Platnost do roku", d.ValidUntilYear.ToString(), x, y);
    }

    private void DrawPermit(Graphics g, EntryPermit d, Rectangle r)
    {
        DrawPaper(g, r, Color.FromArgb(226, 238, 220), Color.FromArgb(70, 110, 70));

        using (var header = new SolidBrush(Color.FromArgb(70, 110, 70)))
            g.FillRectangle(header, r.X, r.Y, r.Width, 28);
        g.DrawString("POVOLENÍ KE VSTUPU", _bold, Brushes.White, r.X + 10, r.Y + 6);

        float x1 = r.X + 16, x2 = r.X + 184, y = r.Y + 40;
        Field(g, "Jméno", d.FullName, x1, y);
        y += 38;
        Field(g, "Datum narození", d.BirthDate.ToString("dd.MM.yyyy"), x1, y);
        Field(g, "Sektor", d.TargetSector.ToString().ToUpperInvariant(), x2, y);
        y += 38;
        Field(g, "Účel cesty", d.Purpose, x1, y);
        Field(g, "Číslo povolení", d.PermitNumber, x2, y);
        y += 38;
        Field(g, "Platnost do roku", d.ValidUntilYear.ToString(), x1, y);

        DrawStamp(g, d.Stamp, r.Right - 78, r.Bottom - 70);
    }

    private void DrawStamp(Graphics g, StampColor color, float cx, float cy)
    {
        Color c = color switch
        {
            StampColor.Red => Color.FromArgb(210, 190, 30, 40),
            StampColor.Green => Color.FromArgb(210, 30, 140, 50),
            _ => Color.FromArgb(210, 40, 70, 190)
        };

        var saved = g.Save();
        g.TranslateTransform(cx, cy);
        g.RotateTransform(-14f);

        using (var outer = new Pen(c, 4f))
            g.DrawEllipse(outer, -50, -50, 100, 100);
        using (var inner = new Pen(c, 2f))
            g.DrawEllipse(inner, -42, -42, 84, 84);
        using (var brush = new SolidBrush(c))
        {
            g.DrawString("SCHVÁLENO", _smallBold, brush, new RectangleF(-50, -14, 100, 20), Centered);
            g.DrawString("* * *", _small, brush, new RectangleF(-50, 8, 100, 18), Centered);
        }

        g.Restore(saved);
    }

    private void DrawManual(Graphics g, DailyDirective d, Rectangle r)
    {
        DrawPaper(g, r, Color.FromArgb(242, 235, 208), Color.FromArgb(120, 100, 60));

        float x = r.X + 18, w = r.Width - 36, y = r.Y + 14;
        g.DrawString("MANUÁL OBČANA", _h2, Brushes.Black, x, y);
        y += 28;
        g.DrawString($"Denní direktiva  •  Den {d.Day}  •  {d.Date:dd.MM.yyyy}", _small, Brushes.DimGray, x, y);
        y += 22;

        using (var sep = new Pen(Color.FromArgb(120, 100, 60), 1.5f))
            g.DrawLine(sep, x, y, x + w, y);
        y += 8;

        y = Paragraph(g, d.Bulletin, _italic, Brushes.SaddleBrown, x, y, w) + 12;

        g.DrawString("PRAVIDLA PRO DNEŠNÍ SMĚNU", _bold, Brushes.Black, x, y);
        y += 22;
        foreach (var rule in d.DynamicRules())
            y = Bullet(g, rule, _bold, Brushes.Firebrick, x, y, w) + 4;
        foreach (var rule in d.StaticRules())
            y = Bullet(g, rule, _text, Brushes.Black, x, y, w) + 4;

        y += 8;
        g.DrawString("VAROVÁNÍ – ZNÁMÉ PŘÍZNAKY INFEKCE", _bold, Brushes.Black, x, y);
        y += 22;
        foreach (var sign in d.WarningSigns)
        {
            string text = sign.Czech() + (sign.IsVisible() ? "" : "  (jen při prohlídce)");
            y = Bullet(g, text, _text, Brushes.Black, x, y, w) + 2;
        }

        y = Paragraph(g, "Doklady neodhalí všechno – ne každý nakažený vypadá nemocně.", _small, Brushes.DimGray, x, y + 4, w) + 12;

        g.DrawString("SAZEBNÍK KREDITŮ", _bold, Brushes.Black, x, y);
        y += 22;

        string[] tariffs =
        {
            $"Správné rozhodnutí {Signed(EconomyManager.RewardCorrect)}, poplach na zombie {Signed(EconomyManager.RewardAlarmBonus)}",
            $"Vpuštěná zombie {Signed(EconomyManager.PenaltyZombieLetIn)} + varování (max. {PlayerStats.MaxWarnings})",
            $"Zadržený nevinný {Signed(EconomyManager.PenaltyDetainInnocent)}, planý poplach {Signed(EconomyManager.PenaltyFalseAlarm)}",
            $"Přehlédnutá chyba v dokladech {Signed(EconomyManager.PenaltyMissedAdminError)}",
            $"Prohlídka {Signed(-EconomyManager.InspectionCost)}, nájem po směně {Signed(-EconomyManager.DailyUpkeep)}"
        };

        foreach (var line in tariffs)
        {
            g.DrawString(line, _small, Brushes.Black, x, y);
            y += 17;
        }
    }

    private void DrawInspection(Graphics g, GameEngine e, Rectangle r)
    {
        using (var bg = new SolidBrush(Color.FromArgb(30, 34, 42)))
            g.FillRectangle(bg, r);
        using (var pen = new Pen(Color.FromArgb(80, 90, 110), 1))
            g.DrawRectangle(pen, r);

        g.DrawString("PROHLÍDKA", _bold, Brushes.Gainsboro, r.X + 12, r.Y + 8);

        if (e.InspectionLines == null)
        {
            g.DrawString($"Zatím neprovedena. Stojí {EconomyManager.InspectionCost} kr. [I]", _small, Brushes.Gray, r.X + 12, r.Y + 34);
            return;
        }

        float y = r.Y + 32;
        foreach (var line in e.InspectionLines)
        {
            g.DrawString(line, _text, Brushes.White, r.X + 12, y);
            y += 22;
        }
    }

    private void DrawResultCard(Graphics g, GameEngine e, Rectangle booth)
    {
        var res = e.LastResult!;

        using (var dim = new SolidBrush(Color.FromArgb(175, 0, 0, 0)))
            g.FillRectangle(dim, booth);

        var card = new RectangleF(booth.X + 10, booth.Y + 60, booth.Width - 20, 520);
        Color accent = res.WasCorrect ? Color.FromArgb(76, 175, 80) : Color.FromArgb(229, 57, 53);

        using var path = RoundedRect(card, 10);
        using (var fill = new SolidBrush(Color.FromArgb(244, 26, 29, 36)))
            g.FillPath(fill, path);
        using (var pen = new Pen(accent, 3))
            g.DrawPath(pen, path);

        using var accentB = new SolidBrush(accent);
        float x = card.X + 16, w = card.Width - 32, y = card.Y + 14;

        y = Paragraph(g, res.Headline, _h2, accentB, x, y, w) + 6;
        g.DrawString($"Kredity: {Signed(res.CreditsDelta)}", _big, accentB, x, y);
        y += 46;

        if (res.WarningIssued)
        {
            y = Paragraph(g, $"VAROVÁNÍ {e.Stats.Warnings}/{PlayerStats.MaxWarnings} UDĚLENO", _bold, Brushes.Orange, x, y, w) + 8;
        }

        y = Paragraph(g, res.Explanation, _text, Brushes.Gainsboro, x, y, w) + 12;

        g.DrawString("ZJIŠTĚNÍ", _bold, Brushes.White, x, y);
        y += 22;
        foreach (var finding in res.Findings)
            y = Bullet(g, finding, _text, Brushes.Gainsboro, x, y, w) + 3;

        g.DrawString("Pokračuj: Enter", _small, Brushes.Gray,
            new RectangleF(card.X, card.Bottom - 30, card.Width, 20), CenterTop);
    }

    // ------------------------------------------------------------------ pomocné funkce

    private void DrawPaper(Graphics g, Rectangle r, Color paper, Color border)
    {
        using (var shadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
            g.FillRectangle(shadow, r.X + 4, r.Y + 4, r.Width, r.Height);
        using (var fill = new SolidBrush(paper))
            g.FillRectangle(fill, r);
        using (var pen = new Pen(border, 2f))
            g.DrawRectangle(pen, r);
    }

    private float Field(Graphics g, string label, string value, float x, float y)
    {
        g.DrawString(label.ToUpperInvariant(), _small, Brushes.DimGray, x, y);
        g.DrawString(value, _mono, Brushes.Black, x, y + 14);
        return y + 38;
    }

    private float Row(Graphics g, string label, string value, Brush valueBrush, float x, float y, float w)
    {
        g.DrawString(label, _text, Brushes.Gainsboro, x, y);
        g.DrawString(value, _bold, valueBrush, new RectangleF(x, y, w, 24), RightAlign);
        return y + 28;
    }

    private float Paragraph(Graphics g, string text, Font font, Brush brush, float x, float y, float width)
    {
        float height = g.MeasureString(text, font, (int)width).Height;
        g.DrawString(text, font, brush, new RectangleF(x, y, width, height + 2));
        return y + height;
    }

    private float Bullet(Graphics g, string text, Font font, Brush brush, float x, float y, float width)
    {
        g.DrawString("•", font, brush, x, y);
        return Paragraph(g, text, font, brush, x + 14, y, width - 14);
    }

    private static string Signed(int value) => value >= 0 ? $"+{value}" : $"−{-value}";

    private static Color Blend(Color a, Color b, float t) => Color.FromArgb(
        (int)(a.R + (b.R - a.R) * t),
        (int)(a.G + (b.G - a.G) * t),
        (int)(a.B + (b.B - a.B) * t));

    private static GraphicsPath RoundedRect(RectangleF r, float radius)
    {
        float d = radius * 2f;
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
