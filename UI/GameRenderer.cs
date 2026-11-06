using System.Globalization;
using Avalonia;
using Avalonia.Media;
using DocumentsPlease.Logic;
using DocumentsPlease.Models;

namespace DocumentsPlease.UI;

/// <summary>
/// Veškeré vykreslování (Avalonia DrawingContext). Čte stav z GameEngine, sám nic neřídí.
/// Kreslí do logického plátna 1200x720, které se rovnoměrně škáluje do okna.
/// </summary>
public sealed class GameRenderer
{
    public const int W = 1200;
    public const int H = 720;

    private readonly record struct Fnt(Typeface Face, double Size);

    private static readonly Color[] SkinTones =
    {
        C(255, 224, 189), C(234, 192, 134), C(198, 134, 66), C(141, 85, 36)
    };

    private static readonly Color[] HairColors =
    {
        C(35, 28, 24), C(88, 56, 32), C(206, 168, 84), C(158, 70, 34), C(170, 170, 172)
    };

    private static readonly Color[] Jackets =
    {
        C(60, 70, 110), C(90, 50, 50), C(50, 90, 70), C(80, 80, 85), C(110, 90, 50), C(70, 50, 90)
    };

    private static readonly HashSet<ZombieSign> NoSigns = new();

    private static readonly FontFamily UiFamily = FontFamily.Default;
    private static readonly FontFamily MonoFamily = new("Menlo, Consolas, Courier New");

    private static readonly Fnt Title = new(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 46);
    private static readonly Fnt Big = new(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 30);
    private static readonly Fnt H2 = new(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 19);
    private static readonly Fnt Txt = new(new Typeface(UiFamily), 14);
    private static readonly Fnt Bold = new(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 14);
    private static readonly Fnt Italic = new(new Typeface(UiFamily, FontStyle.Italic), 13);
    private static readonly Fnt Small = new(new Typeface(UiFamily), 12);
    private static readonly Fnt SmallBold = new(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 12);
    private static readonly Fnt Mono = new(new Typeface(MonoFamily, FontStyle.Normal, FontWeight.Bold), 15);

    // ------------------------------------------------------------------ vstupní bod

    public void Render(DrawingContext ctx, Size size, GameEngine engine, double time)
    {
        ctx.DrawRectangle(Br(C(8, 9, 11)), null, new Rect(0, 0, size.Width, size.Height));

        double scale = Math.Min(size.Width / W, size.Height / H);
        if (scale <= 0.01) return;

        var matrix = Matrix.CreateScale(scale, scale)
                     * Matrix.CreateTranslation((size.Width - W * scale) / 2, (size.Height - H * scale) / 2);

        using (ctx.PushTransform(matrix))
        using (ctx.PushClip(new Rect(0, 0, W, H)))
        {
            Fill(ctx, C(24, 26, 31), 0, 0, W, H);

            switch (engine.State)
            {
                case GameState.MainMenu:
                    DrawMainMenu(ctx, time);
                    break;
                case GameState.ShiftIntro:
                    DrawHud(ctx, engine);
                    DrawManual(ctx, engine.Directive, new Rect((W - 520) / 2, 70, 520, H - 90));
                    break;
                case GameState.Inspecting:
                case GameState.ShowingResult:
                    DrawDesk(ctx, engine, time);
                    break;
                case GameState.ShiftSummary:
                    DrawHud(ctx, engine);
                    DrawShiftSummary(ctx, engine);
                    break;
                case GameState.GameOver:
                    DrawGameOver(ctx, engine);
                    break;
            }
        }
    }

    // ------------------------------------------------------------------ obrazovky

    private void DrawMainMenu(DrawingContext ctx, double time)
    {
        double gate = Math.Sin(time * 17) * Math.Sin(time * 5.3);
        byte alpha = gate > 0.92 ? (byte)140 : (byte)255;

        Text(ctx, "DOCUMENTS, PLEASE", Title, Br(C(230, 70, 60, alpha)), 0, 60, W, TextAlignment.Center);
        Text(ctx, "Hraniční kontrola. Papíry. Zombie.", H2, Brushes.Gainsboro, 0, 128, W, TextAlignment.Center);

        var panel = new Rect(230, 190, 740, 440);
        Fill(ctx, C(34, 37, 46), panel);
        Frame(ctx, C(80, 90, 110), 2, panel);

        double x = panel.X + 24, w = panel.Width - 48, y = panel.Y + 18;
        Text(ctx, "JAK HRÁT", H2, Brushes.White, x, y);
        y += 34;

        string[] lines =
        {
            "Každou směnu dostaneš Direktivu: povolený sektor, barvu razítka a minimální rok platnosti dokladů.",
            "Porovnej občanský průkaz a povolení – jména, data narození, fotografii, platnost, sektor i razítko.",
            "Pozor: zombie mohou mít dokonale platné doklady! Sleduj vzhled a chování. Prohlídka odhalí i skryté příznaky, ale stojí kredity.",
            "Rozhoduj: PUSTIT [1]   ZADRŽET [2]   POPLACH [3]   PROHLÍDKA [I]   DALŠÍ [Enter]"
        };

        foreach (var line in lines)
            y = Paragraph(ctx, line, Txt, Brushes.Gainsboro, x, y, w) + 10;

        y += 6;
        Text(ctx, "SAZEBNÍK", Bold, Brushes.White, x, y);
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
            Text(ctx, line, Txt, Brushes.Khaki, x, y);
            y += 22;
        }

        Text(ctx, "Stiskni ENTER nebo klikni na tlačítko dole.", Txt, Brushes.Gray,
            panel.X, panel.Bottom - 34, panel.Width, TextAlignment.Center);
    }

    private void DrawHud(DrawingContext ctx, GameEngine e)
    {
        Fill(ctx, C(14, 15, 18), 0, 0, W, 54);

        int credits = e.Stats.Credits;
        Text(ctx, $"KREDITY  {credits}", H2, credits >= 0 ? Brushes.LimeGreen : Brushes.OrangeRed, 18, 14);

        Text(ctx, "VAROVÁNÍ", Bold, Brushes.Gainsboro, 270, 17);
        for (int i = 0; i < PlayerStats.MaxWarnings; i++)
        {
            double cx = 369 + i * 26;
            if (i < e.Stats.Warnings)
                ctx.DrawEllipse(Brushes.OrangeRed, null, new Point(cx, 25), 9, 9);
            else
                ctx.DrawEllipse(null, new Pen(Brushes.Gray, 1.5), new Point(cx, 25), 9, 9);
        }

        Text(ctx, $"DEN {e.Stats.Day}  •  {e.Directive.Date:dd.MM.yyyy}", H2, Brushes.White, 520, 14);

        Text(ctx, $"Návštěvník {e.VisitorNumber} / {e.VisitorsTotal}", Bold, Brushes.Gainsboro,
            W - 260, 17, 242, TextAlignment.Right);
    }

    private void DrawDesk(DrawingContext ctx, GameEngine e, double time)
    {
        DrawHud(ctx, e);

        var v = e.CurrentVisitor;
        if (v == null) return;

        var booth = new Rect(10, 64, 360, H - 74);
        DrawBooth(ctx, booth, v, time);

        DrawPassport(ctx, v.Passport, new Rect(384, 70, 350, 210));
        DrawPermit(ctx, v.Permit, new Rect(384, 292, 350, 250));
        DrawInspection(ctx, e, new Rect(384, 554, 350, H - 564));
        DrawManual(ctx, e.Directive, new Rect(748, 64, W - 758, H - 74));

        if (e.State == GameState.ShowingResult && e.LastResult != null)
            DrawResultCard(ctx, e, booth);
    }

    private void DrawShiftSummary(DrawingContext ctx, GameEngine e)
    {
        var report = e.LastShiftReport;
        if (report == null) return;

        var panel = new Rect(300, 90, 600, 560);
        Fill(ctx, C(34, 37, 46), panel);
        Frame(ctx, C(80, 90, 110), 2, panel);

        double x = panel.X + 30, w = panel.Width - 60, y = panel.Y + 24;
        Text(ctx, $"KONEC SMĚNY – DEN {report.Day}", Big, Brushes.White, x, y);
        y += 62;

        y = Row(ctx, "Obslouženo návštěvníků", report.Processed.ToString(), Brushes.White, x, y, w);
        y = Row(ctx, "Správných rozhodnutí", report.Correct.ToString(), Brushes.LimeGreen, x, y, w);
        y = Row(ctx, "Chybných rozhodnutí", report.Wrong.ToString(), report.Wrong > 0 ? Brushes.OrangeRed : Brushes.White, x, y, w);
        y += 14;
        y = Row(ctx, "Příjem a pokuty z rozhodnutí", Signed(report.DecisionCredits), report.DecisionCredits >= 0 ? Brushes.LimeGreen : Brushes.OrangeRed, x, y, w);
        y = Row(ctx, "Prohlídky", Signed(report.InspectionCredits), Brushes.Gainsboro, x, y, w);
        y = Row(ctx, "Nájem a živobytí", Signed(report.UpkeepCredits), Brushes.OrangeRed, x, y, w);
        y += 14;

        Line(ctx, C(80, 90, 110), 1.5, x, y, x + w, y);
        y += 14;

        Text(ctx, "ZŮSTATEK", H2, Brushes.White, x, y);
        Text(ctx, $"{report.EndingCredits} kr.", Big,
            report.EndingCredits >= 0 ? Brushes.LimeGreen : Brushes.OrangeRed, x, y - 8, w, TextAlignment.Right);
        y += 56;

        if (report.GameOver)
            Paragraph(ctx, e.GameOverReason, Bold, Brushes.OrangeRed, x, y, w);
        else
            Paragraph(ctx, "Zítra tě čeká další směna a nová direktiva. Odpočiň si – zombie ne.", Txt, Brushes.Gainsboro, x, y, w);
    }

    private void DrawGameOver(DrawingContext ctx, GameEngine e)
    {
        Text(ctx, "KONEC HRY", Title, Brushes.OrangeRed, 0, 110, W, TextAlignment.Center);

        var panel = new Rect(300, 210, 600, 360);
        Fill(ctx, C(34, 37, 46), panel);
        Frame(ctx, C(120, 50, 50), 2, panel);

        double x = panel.X + 30, w = panel.Width - 60, y = panel.Y + 26;
        y = Paragraph(ctx, e.GameOverReason, H2, Brushes.White, x, y, w) + 24;

        int shifts = Math.Max(0, e.Stats.Day - (e.LastShiftReport is { GameOver: true } ? 0 : 1));
        y = Row(ctx, "Odsloužených směn", shifts.ToString(), Brushes.White, x, y, w);
        y = Row(ctx, "Správných rozhodnutí", e.Stats.TotalCorrect.ToString(), Brushes.LimeGreen, x, y, w);
        y = Row(ctx, "Chybných rozhodnutí", e.Stats.TotalWrong.ToString(), Brushes.OrangeRed, x, y, w);
        y = Row(ctx, "Zastavených zombie", e.Stats.ZombiesStopped.ToString(), Brushes.LimeGreen, x, y, w);
        Row(ctx, "Vpuštěných zombie", e.Stats.ZombiesLetIn.ToString(), Brushes.OrangeRed, x, y, w);

        Text(ctx, "Stiskni ENTER pro novou hru.", Txt, Brushes.Gray,
            panel.X, panel.Bottom - 40, panel.Width, TextAlignment.Center);
    }

    // ------------------------------------------------------------------ okénko a návštěvník

    private void DrawBooth(DrawingContext ctx, Rect r, Visitor v, double time)
    {
        var gradient = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(C(44, 48, 64), 0),
                new GradientStop(C(18, 20, 28), 1)
            }
        };
        ctx.DrawRectangle(gradient, null, r);

        for (double x = r.X + 30; x < r.Right; x += 60)
            Line(ctx, C(255, 255, 255, 24), 1, x, r.Y, x, r.Bottom);

        var person = new Rect(r.X + (r.Width - 300) / 2, 260, 300, 390);
        DrawPerson(ctx, person, v.Face, v.ZombieSigns, v.JacketColor, time, true);

        // pult
        var counter = new Rect(r.X, r.Bottom - 70, r.Width, 70);
        Fill(ctx, C(62, 46, 36), counter);
        Line(ctx, C(96, 72, 54), 3, counter.X, counter.Y, counter.Right, counter.Y);
        Text(ctx, "OKÉNKO č. 3  –  KARANTÉNNÍ ZÓNA 7", SmallBold, Brushes.Tan,
            counter.X, counter.Y + 24, counter.Width, TextAlignment.Center);

        Frame(ctx, C(70, 76, 96), 3, r);

        DrawSpeech(ctx, new Rect(r.X + 16, r.Y + 14, r.Width - 32, 100), v.Greeting);
    }

    private void DrawSpeech(DrawingContext ctx, Rect r, string text)
    {
        var fill = Br(C(240, 240, 232));
        ctx.DrawRectangle(fill, new Pen(Br(C(90, 90, 90)), 2), r, 12, 12);

        Poly(ctx, fill, null,
            new Point(r.X + 150, r.Bottom - 1),
            new Point(r.X + 182, r.Bottom - 1),
            new Point(r.X + 166, r.Bottom + 20));

        Text(ctx, text, Txt, Brushes.Black, r.X + 12, r.Y + 10, r.Width - 24);
    }

    /// <summary>
    /// Nakreslí postavu v souřadnicích 200x260 škálovaných do rect.
    /// Příznaky zombie (signs) mění vzhled a animaci – fotka v OP je kreslí s prázdnou množinou.
    /// </summary>
    private void DrawPerson(DrawingContext ctx, Rect rect, FaceTraits face, ISet<ZombieSign> signs, int jacket, double time, bool animate)
    {
        var matrix = Matrix.CreateScale(rect.Width / 200.0, rect.Height / 260.0)
                     * Matrix.CreateTranslation(rect.X, rect.Y);

        using (ctx.PushTransform(matrix))
        using (ctx.PushClip(new Rect(0, 0, 200, 260)))
        {
            double dx = 0;
            double dy = animate ? Math.Sin(time * 1.8) * 1.5 : 0;

            if (animate && signs.Contains(ZombieSign.Twitching))
            {
                double gate = Math.Sin(time * 11) * Math.Sin(time * 3.7);
                if (gate > 0.55)
                {
                    dx = Math.Sin(time * 97) * 3.5;
                    dy += Math.Cos(time * 83) * 2.5;
                }
            }

            Color skin = SkinTones[face.SkinTone % SkinTones.Length];
            if (signs.Contains(ZombieSign.Pallor))
                skin = Blend(skin, C(148, 176, 150), 0.72);

            Color hair = HairColors[face.HairColor % HairColors.Length];

            var skinB = Br(skin);
            var hairB = Br(hair);
            var jacketB = Br(Jackets[jacket % Jackets.Length]);

            // tělo a límec
            Ell(ctx, jacketB, null, 8, 182, 184, 170);
            Poly(ctx, Brushes.WhiteSmoke, null, new Point(86, 178), new Point(114, 178), new Point(100, 212));

            // dlouhé vlasy – za hlavou
            if (face.Hair == HairStyle.Long)
                Ell(ctx, hairB, null, 42 + dx, 40 + dy, 116, 150);

            // krk, uši, hlava
            Fill(ctx, skin, 84 + dx * 0.4, 146, 32, 44);
            Ell(ctx, skinB, null, 45 + dx, 96 + dy, 14, 26);
            Ell(ctx, skinB, null, 141 + dx, 96 + dy, 14, 26);
            Ell(ctx, skinB, null, 52 + dx, 38 + dy, 96, 124);

            // vlasy – přední část
            switch (face.Hair)
            {
                case HairStyle.Short:
                case HairStyle.Long:
                    Pie(ctx, hairB, 50 + dx, 30 + dy, 100, 90, 180, 180);
                    break;
                case HairStyle.Curly:
                    for (int i = 0; i < 7; i++)
                    {
                        double a = Math.PI * (1.0 + i / 6.0);
                        Ell(ctx, hairB, null,
                            100 + dx + Math.Cos(a) * 46 - 13,
                            76 + dy + Math.Sin(a) * 44 - 13, 26, 26);
                    }
                    break;
            }

            // oči
            bool milky = signs.Contains(ZombieSign.MilkyEyes);
            var whiteB = Br(milky ? C(205, 210, 205) : C(255, 255, 255));
            Ell(ctx, whiteB, null, 72 + dx, 90 + dy, 22, 14);
            Ell(ctx, whiteB, null, 106 + dx, 90 + dy, 22, 14);

            if (milky)
            {
                var pale = Br(C(228, 232, 236));
                Ell(ctx, pale, null, 78 + dx, 91 + dy, 10, 12);
                Ell(ctx, pale, null, 112 + dx, 91 + dy, 10, 12);
            }
            else
            {
                var iris = Br(C(52, 72, 104));
                Ell(ctx, iris, null, 78 + dx, 91 + dy, 10, 12);
                Ell(ctx, iris, null, 112 + dx, 91 + dy, 10, 12);
                Ell(ctx, Brushes.Black, null, 81 + dx, 94 + dy, 4, 6);
                Ell(ctx, Brushes.Black, null, 115 + dx, 94 + dy, 4, 6);
            }

            if (signs.Contains(ZombieSign.Pallor))
            {
                var bags = Br(C(70, 40, 90, 95));
                Ell(ctx, bags, null, 70 + dx, 101 + dy, 26, 9);
                Ell(ctx, bags, null, 104 + dx, 101 + dy, 26, 9);
            }

            // obočí a nos
            Line(ctx, hair, 3, 70 + dx, 84 + dy, 94 + dx, 82 + dy);
            Line(ctx, hair, 3, 106 + dx, 82 + dy, 130 + dx, 84 + dy);

            Color noseColor = Blend(skin, Colors.Black, 0.28);
            Line(ctx, noseColor, 2, 100 + dx, 100 + dy, 96 + dx, 120 + dy);
            Line(ctx, noseColor, 2, 96 + dx, 120 + dy, 104 + dx, 121 + dy);

            // vousy, ústa
            if (face.Beard)
            {
                Pie(ctx, hairB, 54 + dx, 78 + dy, 92, 88, 0, 180);
                Ell(ctx, skinB, null, 87 + dx, 125 + dy, 26, 16);
            }

            if (signs.Contains(ZombieSign.Groaning))
                Ell(ctx, Br(C(70, 20, 25)), null, 88 + dx, 126 + dy, 24, 20);
            else
                Line(ctx, Blend(skin, Colors.Black, 0.55), 2.5, 88 + dx, 135 + dy, 112 + dx, 135 + dy);

            // brýle
            if (face.Glasses)
            {
                var glassPen = new Pen(Br(C(40, 40, 46)), 2.5);
                Ell(ctx, null, glassPen, 66 + dx, 86 + dy, 32, 24);
                Ell(ctx, null, glassPen, 102 + dx, 86 + dy, 32, 24);
                ctx.DrawLine(glassPen, new Point(98 + dx, 96 + dy), new Point(102 + dx, 96 + dy));
            }

            // jizvy se stehy
            if (signs.Contains(ZombieSign.Scars))
            {
                var scar = C(150, 40, 40);
                var stitch = C(40, 20, 20);

                Line(ctx, scar, 3, 62 + dx, 54 + dy, 90 + dx, 116 + dy);
                for (int i = 0; i < 5; i++)
                {
                    double t = i / 4.0;
                    double sx = 62 + 28 * t + dx;
                    double sy = 54 + 62 * t + dy;
                    Line(ctx, stitch, 1.5, sx - 4, sy + 3, sx + 4, sy - 3);
                }

                Line(ctx, scar, 3, 124 + dx, 112 + dy, 138 + dx, 148 + dy);
                for (int i = 0; i < 3; i++)
                {
                    double t = i / 2.0;
                    double sx = 124 + 14 * t + dx;
                    double sy = 112 + 36 * t + dy;
                    Line(ctx, stitch, 1.5, sx - 4, sy + 2, sx + 4, sy - 2);
                }
            }

            // slintání
            if (signs.Contains(ZombieSign.Drooling))
            {
                double len = 14 + (animate ? (Math.Sin(time * 1.5) + 1) * 7 : 0);
                var drool = C(190, 225, 232, 210);
                Line(ctx, drool, 3, 106 + dx, 140 + dy, 108 + dx, 140 + len + dy);
                Ell(ctx, Br(drool), null, 104 + dx, 138 + len + dy, 8, 8);
            }
        }
    }

    private void DrawPhoto(DrawingContext ctx, FaceTraits face, Rect r)
    {
        Fill(ctx, C(170, 200, 225), r);
        DrawPerson(ctx, r, face, NoSigns, 0, 0, false);
        Frame(ctx, C(70, 90, 120), 2, r);
    }

    // ------------------------------------------------------------------ doklady a manuál

    private void DrawPassport(DrawingContext ctx, PassportDocument d, Rect r)
    {
        DrawPaper(ctx, r, C(214, 226, 240), C(60, 90, 140));

        Fill(ctx, C(60, 90, 140), r.X, r.Y, r.Width, 28);
        Text(ctx, "OBČANSKÝ PRŮKAZ", Bold, Brushes.White, r.X + 10, r.Y + 6);
        Text(ctx, "KARANTÉNNÍ ZÓNA 7", SmallBold, Brushes.White, r.X, r.Y + 8, r.Width - 10, TextAlignment.Right);

        DrawPhoto(ctx, d.Photo, new Rect(r.X + 12, r.Y + 40, 96, 125));

        double x = r.X + 122, y = r.Y + 38;
        y = Field(ctx, "Jméno", d.FullName, x, y);
        y = Field(ctx, "Datum narození", d.BirthDate.ToString("dd.MM.yyyy"), x, y);
        y = Field(ctx, "Číslo OP", d.IdNumber, x, y);
        Field(ctx, "Platnost do roku", d.ValidUntilYear.ToString(), x, y);
    }

    private void DrawPermit(DrawingContext ctx, EntryPermit d, Rect r)
    {
        DrawPaper(ctx, r, C(226, 238, 220), C(70, 110, 70));

        Fill(ctx, C(70, 110, 70), r.X, r.Y, r.Width, 28);
        Text(ctx, "POVOLENÍ KE VSTUPU", Bold, Brushes.White, r.X + 10, r.Y + 6);

        double x1 = r.X + 16, x2 = r.X + 184, y = r.Y + 40;
        Field(ctx, "Jméno", d.FullName, x1, y);
        y += 38;
        Field(ctx, "Datum narození", d.BirthDate.ToString("dd.MM.yyyy"), x1, y);
        Field(ctx, "Sektor", d.TargetSector.ToString().ToUpperInvariant(), x2, y);
        y += 38;
        Field(ctx, "Účel cesty", d.Purpose, x1, y);
        Field(ctx, "Číslo povolení", d.PermitNumber, x2, y);
        y += 38;
        Field(ctx, "Platnost do roku", d.ValidUntilYear.ToString(), x1, y);

        DrawStamp(ctx, d.Stamp, r.Right - 78, r.Bottom - 70);
    }

    private void DrawStamp(DrawingContext ctx, StampColor color, double cx, double cy)
    {
        Color c = color switch
        {
            StampColor.Red => C(190, 30, 40, 210),
            StampColor.Green => C(30, 140, 50, 210),
            _ => C(40, 70, 190, 210)
        };

        var matrix = Matrix.CreateRotation(-14 * Math.PI / 180.0) * Matrix.CreateTranslation(cx, cy);
        using (ctx.PushTransform(matrix))
        {
            var brush = Br(c);
            ctx.DrawEllipse(null, new Pen(brush, 4), new Point(0, 0), 50, 50);
            ctx.DrawEllipse(null, new Pen(brush, 2), new Point(0, 0), 42, 42);
            Text(ctx, "SCHVÁLENO", SmallBold, brush, -50, -14, 100, TextAlignment.Center);
            Text(ctx, "* * *", Small, brush, -50, 6, 100, TextAlignment.Center);
        }
    }

    private void DrawManual(DrawingContext ctx, DailyDirective d, Rect r)
    {
        DrawPaper(ctx, r, C(242, 235, 208), C(120, 100, 60));

        double x = r.X + 18, w = r.Width - 36, y = r.Y + 14;
        Text(ctx, "MANUÁL OBČANA", H2, Brushes.Black, x, y);
        y += 28;
        Text(ctx, $"Denní direktiva  •  Den {d.Day}  •  {d.Date:dd.MM.yyyy}", Small, Brushes.DimGray, x, y);
        y += 22;

        Line(ctx, C(120, 100, 60), 1.5, x, y, x + w, y);
        y += 8;

        y = Paragraph(ctx, d.Bulletin, Italic, Brushes.SaddleBrown, x, y, w) + 12;

        Text(ctx, "PRAVIDLA PRO DNEŠNÍ SMĚNU", Bold, Brushes.Black, x, y);
        y += 22;
        foreach (var rule in d.DynamicRules())
            y = Bullet(ctx, rule, Bold, Brushes.Firebrick, x, y, w) + 4;
        foreach (var rule in d.StaticRules())
            y = Bullet(ctx, rule, Txt, Brushes.Black, x, y, w) + 4;

        y += 8;
        Text(ctx, "VAROVÁNÍ – ZNÁMÉ PŘÍZNAKY INFEKCE", Bold, Brushes.Black, x, y);
        y += 22;
        foreach (var sign in d.WarningSigns)
        {
            string text = sign.Czech() + (sign.IsVisible() ? "" : "  (jen při prohlídce)");
            y = Bullet(ctx, text, Txt, Brushes.Black, x, y, w) + 2;
        }

        y = Paragraph(ctx, "Doklady neodhalí všechno – ne každý nakažený vypadá nemocně.", Small, Brushes.DimGray, x, y + 4, w) + 12;

        Text(ctx, "SAZEBNÍK KREDITŮ", Bold, Brushes.Black, x, y);
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
            Text(ctx, line, Small, Brushes.Black, x, y);
            y += 17;
        }
    }

    private void DrawInspection(DrawingContext ctx, GameEngine e, Rect r)
    {
        Fill(ctx, C(30, 34, 42), r);
        Frame(ctx, C(80, 90, 110), 1, r);

        Text(ctx, "PROHLÍDKA", Bold, Brushes.Gainsboro, r.X + 12, r.Y + 8);

        if (e.InspectionLines == null)
        {
            Text(ctx, $"Zatím neprovedena. Stojí {EconomyManager.InspectionCost} kr. [I]", Small, Brushes.Gray, r.X + 12, r.Y + 34);
            return;
        }

        double y = r.Y + 32;
        foreach (var line in e.InspectionLines)
        {
            Text(ctx, line, Txt, Brushes.White, r.X + 12, y);
            y += 22;
        }
    }

    private void DrawResultCard(DrawingContext ctx, GameEngine e, Rect booth)
    {
        var res = e.LastResult!;

        Fill(ctx, C(0, 0, 0, 175), booth);

        var card = new Rect(booth.X + 10, booth.Y + 60, booth.Width - 20, 520);
        Color accent = res.WasCorrect ? C(76, 175, 80) : C(229, 57, 53);
        var accentB = Br(accent);

        ctx.DrawRectangle(Br(C(26, 29, 36, 244)), new Pen(accentB, 3), card, 10, 10);

        double x = card.X + 16, w = card.Width - 32, y = card.Y + 14;

        y = Paragraph(ctx, res.Headline, H2, accentB, x, y, w) + 6;
        Text(ctx, $"Kredity: {Signed(res.CreditsDelta)}", Big, accentB, x, y);
        y += 46;

        if (res.WarningIssued)
            y = Paragraph(ctx, $"VAROVÁNÍ {e.Stats.Warnings}/{PlayerStats.MaxWarnings} UDĚLENO", Bold, Brushes.Orange, x, y, w) + 8;

        y = Paragraph(ctx, res.Explanation, Txt, Brushes.Gainsboro, x, y, w) + 12;

        Text(ctx, "ZJIŠTĚNÍ", Bold, Brushes.White, x, y);
        y += 22;
        foreach (var finding in res.Findings)
            y = Bullet(ctx, finding, Txt, Brushes.Gainsboro, x, y, w) + 3;

        Text(ctx, "Pokračuj: Enter", Small, Brushes.Gray, card.X, card.Bottom - 30, card.Width, TextAlignment.Center);
    }

    // ------------------------------------------------------------------ pomocné funkce

    private void DrawPaper(DrawingContext ctx, Rect r, Color paper, Color border)
    {
        Fill(ctx, C(0, 0, 0, 80), r.X + 4, r.Y + 4, r.Width, r.Height);
        Fill(ctx, paper, r);
        Frame(ctx, border, 2, r);
    }

    private double Field(DrawingContext ctx, string label, string value, double x, double y)
    {
        Text(ctx, label.ToUpperInvariant(), Small, Brushes.DimGray, x, y);
        Text(ctx, value, Mono, Brushes.Black, x, y + 14);
        return y + 38;
    }

    private double Row(DrawingContext ctx, string label, string value, IBrush valueBrush, double x, double y, double w)
    {
        Text(ctx, label, Txt, Brushes.Gainsboro, x, y);
        Text(ctx, value, Bold, valueBrush, x, y, w, TextAlignment.Right);
        return y + 28;
    }

    private double Paragraph(DrawingContext ctx, string text, Fnt font, IBrush brush, double x, double y, double width)
        => y + Text(ctx, text, font, brush, x, y, width);

    private double Bullet(DrawingContext ctx, string text, Fnt font, IBrush brush, double x, double y, double width)
    {
        Text(ctx, "•", font, brush, x, y);
        return Paragraph(ctx, text, font, brush, x + 14, y, width - 14);
    }

    /// <summary>Vykreslí text a vrátí jeho výšku. width &gt; 0 zapne zalamování a zarovnání.</summary>
    private static double Text(DrawingContext ctx, string s, Fnt f, IBrush brush, double x, double y,
        double width = 0, TextAlignment align = TextAlignment.Left)
    {
        var ft = new FormattedText(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, f.Face, f.Size, brush);
        if (width > 0) ft.MaxTextWidth = width;
        ft.TextAlignment = align;
        ctx.DrawText(ft, new Point(x, y));
        return ft.Height;
    }

    private static string Signed(int value) => value >= 0 ? $"+{value}" : $"−{-value}";

    private static Color C(int r, int g, int b, int a = 255) => Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b);

    private static IBrush Br(Color c) => new SolidColorBrush(c);

    private static Color Blend(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + (b.R - a.R) * t),
        (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t));

    private static void Fill(DrawingContext ctx, Color c, Rect r) => ctx.DrawRectangle(Br(c), null, r);

    private static void Fill(DrawingContext ctx, Color c, double x, double y, double w, double h)
        => ctx.DrawRectangle(Br(c), null, new Rect(x, y, w, h));

    private static void Frame(DrawingContext ctx, Color c, double thickness, Rect r)
        => ctx.DrawRectangle(null, new Pen(Br(c), thickness), r);

    private static void Line(DrawingContext ctx, Color c, double thickness, double x1, double y1, double x2, double y2)
        => ctx.DrawLine(new Pen(Br(c), thickness), new Point(x1, y1), new Point(x2, y2));

    private static void Ell(DrawingContext ctx, IBrush? brush, IPen? pen, double x, double y, double w, double h)
        => ctx.DrawEllipse(brush, pen, new Point(x + w / 2, y + h / 2), w / 2, h / 2);

    private static void Poly(DrawingContext ctx, IBrush? brush, IPen? pen, params Point[] points)
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(points[0], true);
            for (int i = 1; i < points.Length; i++)
                g.LineTo(points[i]);
            g.EndFigure(true);
        }

        ctx.DrawGeometry(brush, pen, geometry);
    }

    /// <summary>Výseč elipsy (jako GDI+ FillPie): úhly ve stupních, po směru hodinových ručiček.</summary>
    private static void Pie(DrawingContext ctx, IBrush brush, double x, double y, double w, double h, double startDeg, double sweepDeg)
    {
        double cx = x + w / 2, cy = y + h / 2, rx = w / 2, ry = h / 2;
        var points = new List<Point> { new(cx, cy) };

        const int steps = 24;
        for (int i = 0; i <= steps; i++)
        {
            double a = (startDeg + sweepDeg * i / steps) * Math.PI / 180.0;
            points.Add(new Point(cx + rx * Math.Cos(a), cy + ry * Math.Sin(a)));
        }

        Poly(ctx, brush, null, points.ToArray());
    }
}
