using System.Globalization;
using System.Text;
using Avalonia;
using Avalonia.Media;
using DocumentsPlease.Logic;
using DocumentsPlease.Models;
using static DocumentsPlease.UI.Palette;

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
        Rgb(255, 224, 189), Rgb(234, 192, 134), Rgb(198, 134, 66), Rgb(141, 85, 36)
    };

    private static readonly Color[] HairColors =
    {
        Rgb(35, 28, 24), Rgb(88, 56, 32), Rgb(206, 168, 84), Rgb(158, 70, 34), Rgb(170, 170, 172)
    };

    private static readonly Color[] Jackets =
    {
        Rgb(58, 66, 78), Rgb(86, 54, 50), Rgb(52, 78, 66), Rgb(74, 74, 70), Rgb(96, 82, 52), Rgb(66, 54, 78)
    };

    private static readonly HashSet<ZombieSign> NoSigns = new();

    private static readonly FontFamily UiFamily = FontFamily.Default;
    private static readonly FontFamily MonoFamily = new("Menlo, Consolas, Courier New, monospace");

    private static readonly Fnt Title = new(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 44);
    private static readonly Fnt Big = new(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 28);
    private static readonly Fnt H2 = new(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 18);
    private static readonly Fnt Txt = new(new Typeface(UiFamily), 14);
    private static readonly Fnt Bold = new(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 14);
    private static readonly Fnt Italic = new(new Typeface(UiFamily, FontStyle.Italic), 13);
    private static readonly Fnt Small = new(new Typeface(UiFamily), 12);
    private static readonly Fnt SmallBold = new(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 11);
    private static readonly Fnt Tiny = new(new Typeface(UiFamily), 10);
    private static readonly Fnt Mono = new(new Typeface(MonoFamily, FontStyle.Normal, FontWeight.Bold), 15);
    private static readonly Fnt MonoSmall = new(new Typeface(MonoFamily), 12);

    private static readonly Dictionary<string, string> SpacedCache = new();

    private readonly Geometry _grain;
    private readonly Geometry _scanlines;

    public GameRenderer()
    {
        _grain = BuildGrain();
        _scanlines = BuildScanlines();
    }

    // ------------------------------------------------------------------ vstupní bod

    public void Render(DrawingContext ctx, Size size, GameEngine engine, double time)
    {
        ctx.DrawRectangle(Br(BgDeep), null, new Rect(0, 0, size.Width, size.Height));

        double scale = Math.Min(size.Width / W, size.Height / H);
        if (scale <= 0.01) return;

        var matrix = Matrix.CreateScale(scale, scale)
                     * Matrix.CreateTranslation((size.Width - W * scale) / 2, (size.Height - H * scale) / 2);

        using (ctx.PushTransform(matrix))
        using (ctx.PushClip(new Rect(0, 0, W, H)))
        {
            Fill(ctx, BgRoom, 0, 0, W, H);

            switch (engine.State)
            {
                case GameState.MainMenu:
                    DrawMainMenu(ctx, time);
                    break;
                case GameState.ShiftIntro:
                    DrawShiftIntro(ctx, engine);
                    break;
                case GameState.Inspecting:
                case GameState.ShowingResult:
                    DrawDesk(ctx, engine, time);
                    break;
                case GameState.ShiftSummary:
                    DrawShiftSummary(ctx, engine);
                    break;
                case GameState.GameOver:
                    DrawGameOver(ctx, engine);
                    break;
            }

            DrawFilmGrain(ctx);
            DrawVignette(ctx);
        }
    }

    // ------------------------------------------------------------------ hlavní menu

    private void DrawMainMenu(DrawingContext ctx, double time)
    {
        // tmavé pozadí s nádechem zeleně
        VerticalGradient(ctx, new Rect(0, 0, W, H), Rgb(26, 32, 29), Rgb(12, 15, 14));

        DrawEmblem(ctx, 600, 78, 30);

        // název – prokládaný, s občasným záblesvem jako vadná zářivka
        double flicker = Math.Sin(time * 17) * Math.Sin(time * 5.3);
        Color titleColor = flicker > 0.93 ? Rgb(150, 44, 38) : Danger;
        TrackedCentered(ctx, "DOCUMENTS, PLEASE", Title, Br(titleColor), 600, 118, 6);

        Line(ctx, Rule, 1, 330, 192, 870, 192);
        TextC(ctx, Spaced("MINISTERSTVO KARANTÉNY · OKÉNKO 3 · ZÓNA 7"),
            SmallBold, Br(InkDim), 0, 202, W);

        // levý panel – instrukce
        var left = new Rect(120, 246, 500, 352);
        DrawPanel(ctx, left);
        double y = SectionBar(ctx, "SLUŽEBNÍ INSTRUKCE", left.X + 1, left.Y + 1, left.Width - 2);
        y += 8;

        string[] steps =
        {
            "Přečti si Denní direktivu – povolený sektor, barvu razítka a minimální platnost dokladů.",
            "Porovnej občanský průkaz s povolením: jméno, datum narození, fotografii a platnost.",
            "Hledej příznaky infekce. Prohlídka odhalí i to, co není vidět – chladnou kůži, chybějící tep.",
            "Rozhodni se. Pozor: nakažený může mít dokonale platné doklady."
        };

        for (int i = 0; i < steps.Length; i++)
        {
            y = NumberedStep(ctx, i + 1, steps[i], left.X + 20, y, left.Width - 40) + 12;
        }

        // pravý panel – sazebník
        var right = new Rect(660, 246, 420, 352);
        DrawPanel(ctx, right);
        double ry = SectionBar(ctx, "SAZEBNÍK KREDITŮ", right.X + 1, right.Y + 1, right.Width - 2);
        ry += 8;

        double rx = right.X + 20, rw = right.Width - 40;
        ry = Ledger(ctx, "Správné rozhodnutí", EconomyManager.RewardCorrect, null, rx, ry, rw);
        ry = Ledger(ctx, "Poplach na zombii", EconomyManager.RewardAlarmBonus, null, rx, ry, rw);
        ry = Ledger(ctx, "Vpuštěná zombie", EconomyManager.PenaltyZombieLetIn, "+ varování", rx, ry, rw);
        ry = Ledger(ctx, "Přehlédnutá chyba v dokladech", EconomyManager.PenaltyMissedAdminError, null, rx, ry, rw);
        ry = Ledger(ctx, "Zadržený nevinný", EconomyManager.PenaltyDetainInnocent, null, rx, ry, rw);
        ry = Ledger(ctx, "Planý poplach", EconomyManager.PenaltyFalseAlarm, null, rx, ry, rw);
        ry = Ledger(ctx, "Prohlídka návštěvníka", -EconomyManager.InspectionCost, null, rx, ry, rw);
        ry = Ledger(ctx, "Nájem a živobytí za směnu", -EconomyManager.DailyUpkeep, null, rx, ry, rw);

        ry += 6;
        Line(ctx, Rule, 1, rx, ry, rx + rw, ry);
        ry += 10;
        Text(ctx, "Počáteční zůstatek", Small, Br(InkDim), rx, ry);
        Text(ctx, $"{EconomyManager.StartingCredits} kr.", Bold, Br(Ink), rx, ry - 2, rw, TextAlignment.Right);
        ry += 22;
        Text(ctx, $"Po {PlayerStats.MaxWarnings} varováních nebo v mínusu končíš.", Tiny, Br(InkFaint), rx, ry);

        // ovládání
        DrawKeycapRow(ctx, 620);

        TextC(ctx, "ENTER  –  NASTOUPIT DO SLUŽBY", Bold, Br(Accent), 0, 674, W);
    }

    private void DrawKeycapRow(DrawingContext ctx, double y)
    {
        (string Key, string Label)[] keys =
        {
            ("1", "PUSTIT"), ("2", "ZADRŽET"), ("3", "POPLACH"), ("I", "PROHLÍDKA"), ("ENTER", "DALŠÍ")
        };

        const double gap = 26;
        var caps = new FormattedText[keys.Length];
        var labels = new FormattedText[keys.Length];
        var capWidths = new double[keys.Length];
        double total = 0;

        for (int i = 0; i < keys.Length; i++)
        {
            caps[i] = Ft(keys[i].Key, SmallBold, Br(Ink));
            labels[i] = Ft(keys[i].Label, SmallBold, Br(InkDim));
            capWidths[i] = Math.Max(32, caps[i].Width + 18);
            total += capWidths[i] + 8 + labels[i].Width + (i < keys.Length - 1 ? gap : 0);
        }

        double x = (W - total) / 2;
        for (int i = 0; i < keys.Length; i++)
        {
            var cap = new Rect(x, y, capWidths[i], 28);
            ctx.DrawRectangle(Br(PanelHi), new Pen(Br(Rule), 1), cap, 3, 3);
            ctx.DrawText(caps[i], new Point(cap.X + (cap.Width - caps[i].Width) / 2,
                cap.Y + (cap.Height - caps[i].Height) / 2));

            ctx.DrawText(labels[i], new Point(x + capWidths[i] + 8, y + (28 - labels[i].Height) / 2));
            x += capWidths[i] + 8 + labels[i].Width + gap;
        }
    }

    private double NumberedStep(DrawingContext ctx, int number, string text, double x, double y, double w)
    {
        var badge = new Rect(x, y + 1, 22, 22);
        ctx.DrawRectangle(null, new Pen(Br(Accent), 1), badge, 2, 2);
        var nf = Ft(number.ToString(), SmallBold, Br(Accent));
        ctx.DrawText(nf, new Point(badge.X + (badge.Width - nf.Width) / 2, badge.Y + (badge.Height - nf.Height) / 2));

        double h = Text(ctx, text, Txt, Br(Ink), x + 34, y, w - 34);
        return y + Math.Max(h, 24);
    }

    private double Ledger(DrawingContext ctx, string label, int value, string? note, double x, double y, double w)
    {
        Text(ctx, label, Txt, Br(InkDim), x, y);
        var brush = Br(value >= 0 ? Good : Danger);
        Text(ctx, Signed(value), Bold, brush, x, y - 1, w, TextAlignment.Right);
        if (note != null)
            Text(ctx, note, Tiny, Br(Warn), x, y + 1, w - 42, TextAlignment.Right);
        return y + 26;
    }

    private void DrawEmblem(DrawingContext ctx, double cx, double cy, double r)
    {
        var center = new Point(cx, cy);
        ctx.DrawEllipse(Br(Panel), new Pen(Br(Accent), 2), center, r, r);
        ctx.DrawEllipse(null, new Pen(Br(Rule), 1), center, r - 6, r - 6);

        var sf = Ft("7", new Fnt(new Typeface(UiFamily, FontStyle.Normal, FontWeight.Bold), 26), Br(Accent));
        ctx.DrawText(sf, new Point(cx - sf.Width / 2, cy - sf.Height / 2));

        for (int i = -1; i <= 1; i++)
            Line(ctx, Accent, 2, cx + i * 8 - 3, cy + r - 9, cx + i * 8 + 3, cy + r - 9);
    }

    // ------------------------------------------------------------------ zahájení směny

    private void DrawShiftIntro(DrawingContext ctx, GameEngine e)
    {
        VerticalGradient(ctx, new Rect(0, 0, W, H), Rgb(26, 32, 29), Rgb(12, 15, 14));
        DrawHud(ctx, e);

        TextC(ctx, Spaced($"SMĚNA {e.Stats.Day}"), SmallBold, Br(InkDim), 0, 68, W);
        TextC(ctx, "DENNÍ DIREKTIVA", Big, Br(Ink), 0, 84, W);

        DrawManual(ctx, e.Directive, new Rect(300, 128, 600, 548));

        TextC(ctx, "ENTER  –  OTEVŘÍT OKÉNKO", Bold, Br(Accent), 0, 686, W);
    }

    // ------------------------------------------------------------------ pracoviště

    private void DrawDesk(DrawingContext ctx, GameEngine e, double time)
    {
        DrawHud(ctx, e);

        var v = e.CurrentVisitor;
        if (v == null) return;

        var booth = new Rect(12, 66, 356, 642);
        DrawBooth(ctx, booth, v, time);

        DrawPassport(ctx, v.Passport, new Rect(382, 66, 352, 212));
        DrawPermit(ctx, v.Permit, new Rect(382, 290, 352, 248));
        DrawInspection(ctx, e, new Rect(382, 550, 352, 158));
        DrawManual(ctx, e.Directive, new Rect(746, 66, 442, 642));

        if (e.State == GameState.ShowingResult && e.LastResult != null)
            DrawResultCard(ctx, e, booth);
    }

    private void DrawHud(DrawingContext ctx, GameEngine e)
    {
        var bar = new Rect(0, 0, W, 54);
        Fill(ctx, Rgb(13, 16, 15), bar);
        Line(ctx, Accent, 2, 0, 54, W, 54);

        // kredity
        int credits = e.Stats.Credits;
        Text(ctx, Spaced("KREDITY"), Tiny, Br(InkFaint), 20, 9);
        Text(ctx, $"{credits} kr.", H2, Br(credits >= 0 ? Good : Danger), 20, 24);

        Line(ctx, Rule, 1, 150, 12, 150, 42);

        // varování
        Text(ctx, Spaced("VAROVÁNÍ"), Tiny, Br(InkFaint), 172, 9);
        for (int i = 0; i < PlayerStats.MaxWarnings; i++)
        {
            var slot = new Rect(172 + i * 24, 26, 18, 18);
            if (i < e.Stats.Warnings)
                ctx.DrawRectangle(Br(Danger), new Pen(Br(Rgb(220, 90, 80)), 1), slot, 2, 2);
            else
                ctx.DrawRectangle(null, new Pen(Br(Rule), 1), slot, 2, 2);
        }

        Line(ctx, Rule, 1, 268, 12, 268, 42);

        // den
        Text(ctx, Spaced("SMĚNA"), Tiny, Br(InkFaint), 290, 9);
        Text(ctx, $"Den {e.Stats.Day}  ·  {e.Directive.Date:dd.MM.yyyy}", H2, Br(Ink), 290, 24);

        Line(ctx, Rule, 1, 520, 12, 520, 42);

        // direktiva ve zkratce – pořád na očích
        Text(ctx, Spaced("DNES PLATÍ"), Tiny, Br(InkFaint), 542, 9);
        Text(ctx, $"Sektor {e.Directive.AllowedSector.ToString().ToUpperInvariant()}  ·  " +
                  $"razítko {e.Directive.AllowedStamp.Czech()}  ·  platnost od {e.Directive.MinValidityYear}",
            Bold, Br(Warn), 542, 26);

        // počítadlo návštěvníků
        Text(ctx, Spaced("NÁVŠTĚVNÍK"), Tiny, Br(InkFaint), W - 240, 9, 220, TextAlignment.Right);
        Text(ctx, $"{e.VisitorNumber} / {e.VisitorsTotal}", H2, Br(Ink), W - 240, 24, 220, TextAlignment.Right);
    }

    private void DrawBooth(DrawingContext ctx, Rect r, Visitor v, double time)
    {
        VerticalGradient(ctx, r, Rgb(40, 47, 44), Rgb(15, 18, 17));

        using (ctx.PushClip(r))
        {
            // obkladové pruhy na zdi
            for (double x = r.X + 26; x < r.Right; x += 52)
                Line(ctx, Rgb(255, 255, 255, 12), 1, x, r.Y, x, r.Bottom);
            Line(ctx, Rgb(0, 0, 0, 60), 2, r.X, r.Y + 250, r.Right, r.Y + 250);

            // kužel světla shora
            Poly(ctx, Br(Rgb(255, 240, 200, 14)), null,
                new Point(r.X + r.Width / 2 - 40, r.Y),
                new Point(r.X + r.Width / 2 + 40, r.Y),
                new Point(r.Right - 20, r.Bottom),
                new Point(r.X + 20, r.Bottom));

            var person = new Rect(r.X + (r.Width - 300) / 2, 250, 300, 400);
            DrawPerson(ctx, person, v.Face, v.ZombieSigns, v.JacketColor, time, true);

            // pult
            var counter = new Rect(r.X, r.Bottom - 74, r.Width, 74);
            VerticalGradient(ctx, counter, Rgb(68, 52, 40), Rgb(44, 33, 26));
            Line(ctx, Rgb(112, 88, 64), 3, counter.X, counter.Y + 1, counter.Right, counter.Y + 1);

            var plaque = new Rect(counter.X + 70, counter.Y + 24, counter.Width - 140, 28);
            ctx.DrawRectangle(Br(Rgb(30, 26, 22)), new Pen(Br(Rgb(96, 78, 58)), 1), plaque, 2, 2);
            TextC(ctx, Spaced("OKÉNKO 3"), SmallBold, Br(Rgb(188, 162, 118)), plaque.X, plaque.Y + 7, plaque.Width);
        }

        // rám okénka s nýty
        Frame(ctx, Rgb(72, 80, 76), 3, r);
        foreach (var p in new[]
                 {
                     new Point(r.X + 10, r.Y + 10), new Point(r.Right - 10, r.Y + 10),
                     new Point(r.X + 10, r.Bottom - 10), new Point(r.Right - 10, r.Bottom - 10)
                 })
            ctx.DrawEllipse(Br(Rgb(96, 104, 100)), null, p, 3, 3);

        DrawSpeech(ctx, new Rect(r.X + 18, r.Y + 20, r.Width - 36, 92), v.Greeting);
    }

    private void DrawSpeech(DrawingContext ctx, Rect r, string text)
    {
        ctx.DrawRectangle(Br(Rgb(16, 20, 19, 232)), new Pen(Br(Rule), 1), r, 2, 2);
        Fill(ctx, Accent, r.X, r.Y, 3, r.Height);

        Text(ctx, Spaced("NÁVŠTĚVNÍK ŘÍKÁ"), Tiny, Br(InkFaint), r.X + 14, r.Y + 9);
        Text(ctx, "„" + text + "“", Italic, Br(Ink), r.X + 14, r.Y + 26, r.Width - 28);
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
                skin = Blend(skin, Rgb(148, 176, 150), 0.72);

            Color hair = HairColors[face.HairColor % HairColors.Length];
            Color coat = Jackets[jacket % Jackets.Length];

            var skinB = Br(skin);
            var hairB = Br(hair);

            // ramena a kabát
            Poly(ctx, Br(coat), null,
                new Point(16, 260), new Point(32, 206), new Point(74, 186),
                new Point(126, 186), new Point(168, 206), new Point(184, 260));
            Poly(ctx, Br(Blend(coat, Colors.Black, 0.3)), null,
                new Point(86, 186), new Point(114, 186), new Point(100, 232));
            Poly(ctx, Br(Rgb(206, 206, 198)), null,
                new Point(88, 184), new Point(112, 184), new Point(100, 210));

            // dlouhé vlasy – za hlavou
            if (face.Hair == HairStyle.Long)
                Ell(ctx, hairB, null, 42 + dx, 40 + dy, 116, 150);

            // krk, uši, hlava
            Fill(ctx, Blend(skin, Colors.Black, 0.18), 84 + dx * 0.4, 148, 32, 42);
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
            var whiteB = Br(milky ? Rgb(205, 210, 205) : Rgb(252, 252, 250));
            Ell(ctx, whiteB, null, 72 + dx, 90 + dy, 22, 14);
            Ell(ctx, whiteB, null, 106 + dx, 90 + dy, 22, 14);

            if (milky)
            {
                var pale = Br(Rgb(228, 232, 236));
                Ell(ctx, pale, null, 78 + dx, 91 + dy, 10, 12);
                Ell(ctx, pale, null, 112 + dx, 91 + dy, 10, 12);
            }
            else
            {
                var iris = Br(Rgb(52, 72, 104));
                Ell(ctx, iris, null, 78 + dx, 91 + dy, 10, 12);
                Ell(ctx, iris, null, 112 + dx, 91 + dy, 10, 12);
                Ell(ctx, Brushes.Black, null, 81 + dx, 94 + dy, 4, 6);
                Ell(ctx, Brushes.Black, null, 115 + dx, 94 + dy, 4, 6);
            }

            if (signs.Contains(ZombieSign.Pallor))
            {
                var bags = Br(Rgb(70, 40, 90, 95));
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
                Ell(ctx, Br(Rgb(70, 20, 25)), null, 88 + dx, 126 + dy, 24, 20);
            else
                Line(ctx, Blend(skin, Colors.Black, 0.55), 2.5, 88 + dx, 135 + dy, 112 + dx, 135 + dy);

            // brýle
            if (face.Glasses)
            {
                var glassPen = new Pen(Br(Rgb(40, 40, 46)), 2.5);
                Ell(ctx, Br(Rgb(180, 200, 210, 40)), glassPen, 66 + dx, 86 + dy, 32, 24);
                Ell(ctx, Br(Rgb(180, 200, 210, 40)), glassPen, 102 + dx, 86 + dy, 32, 24);
                ctx.DrawLine(glassPen, new Point(98 + dx, 96 + dy), new Point(102 + dx, 96 + dy));
            }

            // jizvy se stehy
            if (signs.Contains(ZombieSign.Scars))
            {
                var scar = Rgb(150, 40, 40);
                var stitch = Rgb(40, 20, 20);

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
                var drool = Rgb(190, 225, 232, 210);
                Line(ctx, drool, 3, 106 + dx, 140 + dy, 108 + dx, 140 + len + dy);
                Ell(ctx, Br(drool), null, 104 + dx, 138 + len + dy, 8, 8);
            }
        }
    }

    private void DrawPhoto(DrawingContext ctx, FaceTraits face, Rect r)
    {
        VerticalGradient(ctx, r, Rgb(188, 206, 222), Rgb(150, 172, 192));
        DrawPerson(ctx, r, face, NoSigns, 3, 0, false);
        using (ctx.PushClip(r))
            ctx.DrawGeometry(Br(Rgb(0, 0, 0, 26)), null, _grain);
        Frame(ctx, Rgb(70, 90, 120), 1.5, r);
    }

    // ------------------------------------------------------------------ doklady

    private void DrawPassport(DrawingContext ctx, PassportDocument d, Rect r)
    {
        DrawPaper(ctx, r, PaperId, Rgb(72, 100, 142));
        DocHeader(ctx, r, "OBČANSKÝ PRŮKAZ", "KARANTÉNNÍ ZÓNA 7", Rgb(50, 76, 112));

        DrawPhoto(ctx, d.Photo, new Rect(r.X + 14, r.Y + 44, 94, 120));

        double x = r.X + 124, w = r.Width - 138, y = r.Y + 44;
        y = Field(ctx, "Jméno a příjmení", d.FullName, x, y, w);
        y = Field(ctx, "Datum narození", d.BirthDate.ToString("dd.MM.yyyy"), x, y, w);
        y = Field(ctx, "Číslo dokladu", d.IdNumber, x, y, w);
        Field(ctx, "Platnost do roku", d.ValidUntilYear.ToString(), x, y, w);

        Text(ctx, Spaced("FOTOGRAFIE MUSÍ ODPOVÍDAT DRŽITELI"), Tiny, Br(PaperDim), r.X + 14, r.Bottom - 22);
    }

    private void DrawPermit(DrawingContext ctx, EntryPermit d, Rect r)
    {
        DrawPaper(ctx, r, PaperPermit, Rgb(82, 118, 80));
        DocHeader(ctx, r, "POVOLENÍ KE VSTUPU", "FORMULÁŘ 14-B", Rgb(56, 92, 60));

        double x1 = r.X + 16, x2 = r.X + 192, cw = 160, y = r.Y + 44;
        Field(ctx, "Jméno a příjmení", d.FullName, x1, y, 330);
        y += 40;
        Field(ctx, "Datum narození", d.BirthDate.ToString("dd.MM.yyyy"), x1, y, cw);
        Field(ctx, "Cílový sektor", d.TargetSector.ToString().ToUpperInvariant(), x2, y, cw);
        y += 40;
        Field(ctx, "Účel cesty", d.Purpose, x1, y, cw);
        Field(ctx, "Číslo povolení", d.PermitNumber, x2, y, cw);
        y += 40;
        Field(ctx, "Platnost do roku", d.ValidUntilYear.ToString(), x1, y, cw);

        DrawStamp(ctx, d.Stamp, r.Right - 76, r.Bottom - 62);
    }

    private void DocHeader(DrawingContext ctx, Rect r, string title, string subtitle, Color bar)
    {
        Fill(ctx, bar, r.X, r.Y, r.Width, 26);
        Text(ctx, Spaced(title), SmallBold, Br(Rgb(240, 240, 232)), r.X + 12, r.Y + 7);
        Text(ctx, Spaced(subtitle), Tiny, Br(Rgb(240, 240, 232, 170)), r.X, r.Y + 8, r.Width - 12, TextAlignment.Right);
    }

    private void DrawStamp(DrawingContext ctx, StampColor color, double cx, double cy)
    {
        Color c = color switch
        {
            StampColor.Red => Rgb(178, 36, 44, 205),
            StampColor.Green => Rgb(36, 128, 56, 205),
            _ => Rgb(44, 72, 178, 205)
        };

        var matrix = Matrix.CreateRotation(-14 * Math.PI / 180.0) * Matrix.CreateTranslation(cx, cy);
        using (ctx.PushTransform(matrix))
        {
            var brush = Br(c);
            ctx.DrawEllipse(null, new Pen(brush, 3.5), new Point(0, 0), 46, 46);
            ctx.DrawEllipse(null, new Pen(brush, 1.5), new Point(0, 0), 38, 38);
            TextC(ctx, "SCHVÁLENO", SmallBold, brush, -44, -17, 88);
            Line(ctx, c, 1, -26, 0, 26, 0);
            TextC(ctx, "• • •", Small, brush, -46, 4, 92);
        }
    }

    // ------------------------------------------------------------------ manuál a prohlídka

    private void DrawManual(DrawingContext ctx, DailyDirective d, Rect r)
    {
        DrawPaper(ctx, r, PaperManual, Rgb(140, 122, 78));

        double x = r.X + 18, w = r.Width - 36, y = r.Y + 16;

        Text(ctx, "MANUÁL OBČANA", H2, Br(PaperInk), x, y);
        Text(ctx, Spaced("PŘÍSNĚ SLUŽEBNÍ"), Tiny, Br(Rgb(150, 60, 50)), x, y + 4, w, TextAlignment.Right);
        y += 26;
        Text(ctx, $"Denní direktiva  ·  Den {d.Day}  ·  {d.Date:dd.MM.yyyy}", Small, Br(PaperDim), x, y);
        y += 20;
        Line(ctx, Rgb(140, 122, 78), 1.5, x, y, x + w, y);
        y += 10;

        y += Text(ctx, "„" + d.Bulletin + "“", Italic, Br(Rgb(112, 76, 44)), x, y, w);
        y += 10;

        y = PaperSection(ctx, "PRAVIDLA PRO DNEŠNÍ SMĚNU", x, y, w);
        foreach (var rule in d.DynamicRules())
            y = Bullet(ctx, rule, Bold, Rgb(138, 44, 38), x, y, w) + 5;
        foreach (var rule in d.StaticRules())
            y = Bullet(ctx, rule, Txt, PaperInk, x, y, w) + 5;

        y += 8;
        y = PaperSection(ctx, "HLÁŠENÉ PŘÍZNAKY INFEKCE", x, y, w);
        foreach (var sign in d.WarningSigns)
        {
            string text = sign.Czech() + (sign.IsVisible() ? "" : "  (jen při prohlídce)");
            y = Bullet(ctx, text, Txt, PaperInk, x, y, w) + 4;
        }

        y += 2;
        y += Text(ctx, "Ne každý nakažený vypadá nemocně. Doklady samy o sobě nic nedokazují.",
            Small, Br(PaperDim), x, y, w);
        y += 12;

        y = PaperSection(ctx, "SAZEBNÍK", x, y, w);

        (string Label, int Value)[] tariffs =
        {
            ("Správné rozhodnutí", EconomyManager.RewardCorrect),
            ("Poplach na zombii", EconomyManager.RewardAlarmBonus),
            ("Vpuštěná zombie", EconomyManager.PenaltyZombieLetIn),
            ("Přehlédnutá chyba", EconomyManager.PenaltyMissedAdminError),
            ("Zadržený nevinný", EconomyManager.PenaltyDetainInnocent),
            ("Planý poplach", EconomyManager.PenaltyFalseAlarm),
            ("Prohlídka", -EconomyManager.InspectionCost),
            ("Nájem po směně", -EconomyManager.DailyUpkeep)
        };

        foreach (var (label, value) in tariffs)
        {
            Text(ctx, label, Small, Br(PaperInk), x + 6, y);
            Text(ctx, Signed(value), SmallBold, Br(value >= 0 ? Rgb(48, 104, 48) : Rgb(150, 50, 44)), x, y, w, TextAlignment.Right);
            y += 17;
        }
    }

    private double PaperSection(DrawingContext ctx, string label, double x, double y, double w)
    {
        Fill(ctx, PaperInk, x, y, w, 18);
        Text(ctx, Spaced(label), Tiny, Br(PaperManual), x + 7, y + 4);
        return y + 26;
    }

    private void DrawInspection(DrawingContext ctx, GameEngine e, Rect r)
    {
        DrawPanel(ctx, r);
        double y = SectionBar(ctx, "DETAILNÍ PROHLÍDKA", r.X + 1, r.Y + 1, r.Width - 2);
        y += 6;

        if (e.InspectionLines == null)
        {
            Text(ctx, "Neprovedena.", Txt, Br(InkDim), r.X + 16, y);
            Text(ctx, $"Klávesa [I] – stojí {EconomyManager.InspectionCost} kreditů.", Small, Br(InkFaint), r.X + 16, y + 22);
            Text(ctx, "Odhalí tep, teplotu kůže a zápach.", Small, Br(InkFaint), r.X + 16, y + 40);
            return;
        }

        foreach (var line in e.InspectionLines)
        {
            bool alarming = line.Contains("nenalezen") || line.Contains("chladná")
                            || line.Contains("rozkladu") || line.Contains("záškuby");
            Line(ctx, alarming ? Danger : Accent, 2, r.X + 14, y + 4, r.X + 14, y + 16);
            Text(ctx, line, MonoSmall, Br(alarming ? Rgb(226, 118, 110) : Ink), r.X + 24, y, r.Width - 40);
            y += 24;
        }
    }

    // ------------------------------------------------------------------ výsledek, souhrn, konec

    private void DrawResultCard(DrawingContext ctx, GameEngine e, Rect booth)
    {
        var res = e.LastResult!;

        Fill(ctx, Rgb(0, 0, 0, 190), booth);

        var card = new Rect(booth.X + 12, booth.Y + 54, booth.Width - 24, 530);
        Color accent = res.WasCorrect ? Good : Danger;

        ctx.DrawRectangle(Br(Rgb(22, 27, 25, 248)), new Pen(Br(accent), 2), card, 3, 3);
        Fill(ctx, accent, card.X, card.Y, card.Width, 4);

        double x = card.X + 18, w = card.Width - 36, y = card.Y + 20;

        Text(ctx, Spaced(res.WasCorrect ? "SPRÁVNĚ" : "CHYBA"), Tiny, Br(accent), x, y);
        y += 18;
        y += Text(ctx, res.Headline, H2, Br(Ink), x, y, w);
        y += 8;

        Text(ctx, Signed(res.CreditsDelta) + " kr.", Big, Br(accent), x, y);
        y += 44;

        if (res.WarningIssued)
        {
            var warnBox = new Rect(x, y, w, 28);
            ctx.DrawRectangle(Br(Rgb(70, 36, 12)), new Pen(Br(Warn), 1), warnBox, 2, 2);
            Text(ctx, Spaced($"VAROVÁNÍ {e.Stats.Warnings} / {PlayerStats.MaxWarnings} UDĚLENO"),
                SmallBold, Br(Warn), x + 10, y + 8);
            y += 38;
        }

        y += Text(ctx, res.Explanation, Txt, Br(InkDim), x, y, w);
        y += 16;

        y = SectionBar(ctx, "ZJIŠTĚNÍ", x, y, w);
        y += 4;

        foreach (var finding in res.Findings)
            y = Bullet(ctx, finding, Small, Ink, x + 4, y, w - 8) + 6;

        TextC(ctx, "ENTER  –  DALŠÍ NÁVŠTĚVNÍK", SmallBold, Br(InkFaint), card.X, card.Bottom - 28, card.Width);
    }

    private void DrawShiftSummary(DrawingContext ctx, GameEngine e)
    {
        VerticalGradient(ctx, new Rect(0, 0, W, H), Rgb(26, 32, 29), Rgb(12, 15, 14));
        DrawHud(ctx, e);

        var report = e.LastShiftReport;
        if (report == null) return;

        var panel = new Rect(330, 92, 540, 540);
        DrawPanel(ctx, panel);

        double y = SectionBar(ctx, $"VYÚČTOVÁNÍ SMĚNY  –  DEN {report.Day}", panel.X + 1, panel.Y + 1, panel.Width - 2);
        y += 18;

        double x = panel.X + 32, w = panel.Width - 64;

        Text(ctx, Spaced("VÝKON"), Tiny, Br(InkFaint), x, y);
        y += 20;
        y = Row(ctx, "Obslouženo návštěvníků", report.Processed.ToString(), Br(Ink), x, y, w);
        y = Row(ctx, "Správných rozhodnutí", report.Correct.ToString(), Br(Good), x, y, w);
        y = Row(ctx, "Chybných rozhodnutí", report.Wrong.ToString(), Br(report.Wrong > 0 ? Danger : Ink), x, y, w);

        y += 16;
        Text(ctx, Spaced("ÚČETNICTVÍ"), Tiny, Br(InkFaint), x, y);
        y += 20;
        y = Row(ctx, "Odměny a pokuty", Signed(report.DecisionCredits), Br(report.DecisionCredits >= 0 ? Good : Danger), x, y, w);
        y = Row(ctx, "Prohlídky", Signed(report.InspectionCredits), Br(InkDim), x, y, w);
        y = Row(ctx, "Nájem a živobytí", Signed(report.UpkeepCredits), Br(Danger), x, y, w);

        y += 10;
        Line(ctx, Rule, 1.5, x, y, x + w, y);
        y += 16;

        Text(ctx, "ZŮSTATEK", H2, Br(Ink), x, y);
        Text(ctx, $"{report.EndingCredits} kr.", Big, Br(report.EndingCredits >= 0 ? Good : Danger), x, y - 6, w, TextAlignment.Right);
        y += 52;

        if (report.GameOver)
            Text(ctx, e.GameOverReason, Bold, Br(Danger), x, y, w);
        else
            Text(ctx, "Zítra tě čeká nová směna a nová direktiva. Pravidla se mění každý den.",
                Txt, Br(InkDim), x, y, w);

        TextC(ctx, report.GameOver ? "ENTER  –  ZOBRAZIT VÝSLEDEK" : "ENTER  –  DALŠÍ SMĚNA",
            Bold, Br(Accent), 0, 664, W);
    }

    private void DrawGameOver(DrawingContext ctx, GameEngine e)
    {
        VerticalGradient(ctx, new Rect(0, 0, W, H), Rgb(32, 20, 19), Rgb(10, 8, 8));

        TrackedCentered(ctx, "KONEC HRY", Title, Br(Danger), 600, 96, 8);
        Line(ctx, Rgb(90, 40, 36), 1, 380, 168, 820, 168);

        var panel = new Rect(340, 200, 520, 380);
        DrawPanel(ctx, panel);
        double y = SectionBar(ctx, "ZÁVĚREČNÁ ZPRÁVA", panel.X + 1, panel.Y + 1, panel.Width - 2);
        y += 18;

        double x = panel.X + 32, w = panel.Width - 64;
        y += Text(ctx, e.GameOverReason, Txt, Br(Ink), x, y, w);
        y += 24;

        int shifts = Math.Max(0, e.Stats.Day - (e.LastShiftReport is { GameOver: true } ? 0 : 1));
        y = Row(ctx, "Odsloužených směn", shifts.ToString(), Br(Ink), x, y, w);
        y = Row(ctx, "Správných rozhodnutí", e.Stats.TotalCorrect.ToString(), Br(Good), x, y, w);
        y = Row(ctx, "Chybných rozhodnutí", e.Stats.TotalWrong.ToString(), Br(Danger), x, y, w);
        y = Row(ctx, "Zastavených zombie", e.Stats.ZombiesStopped.ToString(), Br(Good), x, y, w);
        Row(ctx, "Vpuštěných zombie", e.Stats.ZombiesLetIn.ToString(), Br(Danger), x, y, w);

        TextC(ctx, "ENTER  –  NOVÁ HRA", Bold, Br(Accent), 0, 626, W);
    }

    // ------------------------------------------------------------------ stavební prvky

    private void DrawPanel(DrawingContext ctx, Rect r)
    {
        Fill(ctx, Rgb(0, 0, 0, 70), r.X + 3, r.Y + 3, r.Width, r.Height);
        ctx.DrawRectangle(Br(Panel), new Pen(Br(Rule), 1), r, 2, 2);
    }

    private double SectionBar(DrawingContext ctx, string label, double x, double y, double w)
    {
        Fill(ctx, PanelHi, x, y, w, 24);
        Line(ctx, Accent, 2, x, y, x, y + 24);
        Text(ctx, Spaced(label), SmallBold, Br(Ink), x + 12, y + 6);
        return y + 32;
    }

    private void DrawPaper(DrawingContext ctx, Rect r, Color paper, Color border)
    {
        Fill(ctx, Rgb(0, 0, 0, 90), r.X + 4, r.Y + 5, r.Width, r.Height);
        VerticalGradient(ctx, r, Lighten(paper, 0.05), Darken(paper, 0.07));

        using (ctx.PushClip(r))
            ctx.DrawGeometry(Br(Rgb(90, 80, 60, 34)), null, _grain);

        Frame(ctx, border, 1.5, r);
        Frame(ctx, Rgb(255, 255, 255, 40), 1, new Rect(r.X + 3, r.Y + 3, r.Width - 6, r.Height - 6));
    }

    private double Field(DrawingContext ctx, string label, string value, double x, double y, double w)
    {
        Text(ctx, Spaced(label.ToUpperInvariant()), Tiny, Br(PaperDim), x, y);
        Text(ctx, value, Mono, Br(PaperInk), x, y + 13, w);
        Line(ctx, Rgb(0, 0, 0, 40), 1, x, y + 33, x + w, y + 33);
        return y + 40;
    }

    private double Row(DrawingContext ctx, string label, string value, IBrush valueBrush, double x, double y, double w)
    {
        Text(ctx, label, Txt, Br(InkDim), x, y);
        Text(ctx, value, Bold, valueBrush, x, y - 1, w, TextAlignment.Right);
        return y + 26;
    }

    private double Bullet(DrawingContext ctx, string text, Fnt font, Color color, double x, double y, double width)
    {
        Fill(ctx, color, x + 2, y + 7, 4, 4);
        return y + Text(ctx, text, font, Br(color), x + 14, y, width - 14);
    }

    // ------------------------------------------------------------------ atmosféra

    private void DrawFilmGrain(DrawingContext ctx)
    {
        ctx.DrawGeometry(Br(Rgb(255, 255, 255, 10)), null, _grain);
        ctx.DrawGeometry(Br(Rgb(0, 0, 0, 16)), null, _scanlines);
    }

    private void DrawVignette(DrawingContext ctx)
    {
        EdgeFade(ctx, new Rect(0, 0, W, 90), 0, 1);
        EdgeFade(ctx, new Rect(0, H - 90, W, 90), 1, 0);
        SideFade(ctx, new Rect(0, 0, 90, H), 0, 1);
        SideFade(ctx, new Rect(W - 90, 0, 90, H), 1, 0);
    }

    private void EdgeFade(DrawingContext ctx, Rect r, double from, double to)
    {
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, from, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, to, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Rgb(0, 0, 0, 72), 0),
                new GradientStop(Rgb(0, 0, 0, 0), 1)
            }
        }, null, r);
    }

    private void SideFade(DrawingContext ctx, Rect r, double from, double to)
    {
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(from, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(to, 0, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Rgb(0, 0, 0, 64), 0),
                new GradientStop(Rgb(0, 0, 0, 0), 1)
            }
        }, null, r);
    }

    private static Geometry BuildGrain()
    {
        var rng = new Random(20411101);
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            for (int i = 0; i < 900; i++)
            {
                double x = rng.NextDouble() * W;
                double y = rng.NextDouble() * H;
                double s = rng.NextDouble() < 0.25 ? 2 : 1;
                g.BeginFigure(new Point(x, y), true);
                g.LineTo(new Point(x + s, y));
                g.LineTo(new Point(x + s, y + s));
                g.LineTo(new Point(x, y + s));
                g.EndFigure(true);
            }
        }

        return geometry;
    }

    private static Geometry BuildScanlines()
    {
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            for (double y = 0; y < H; y += 3)
            {
                g.BeginFigure(new Point(0, y), true);
                g.LineTo(new Point(W, y));
                g.LineTo(new Point(W, y + 1));
                g.LineTo(new Point(0, y + 1));
                g.EndFigure(true);
            }
        }

        return geometry;
    }

    // ------------------------------------------------------------------ text a tvary

    private static FormattedText Ft(string s, Fnt f, IBrush brush) =>
        new(s, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, f.Face, f.Size, brush);

    /// <summary>Vykreslí text a vrátí jeho výšku. width &gt; 0 zapne zalamování a zarovnání.</summary>
    private static double Text(DrawingContext ctx, string s, Fnt f, IBrush brush, double x, double y,
        double width = 0, TextAlignment align = TextAlignment.Left)
    {
        var ft = Ft(s, f, brush);
        if (width > 0) ft.MaxTextWidth = width;
        ft.TextAlignment = align;
        ctx.DrawText(ft, new Point(x, y));
        return ft.Height;
    }

    private static double TextC(DrawingContext ctx, string s, Fnt f, IBrush brush, double x, double y, double width)
        => Text(ctx, s, f, brush, x, y, width, TextAlignment.Center);

    /// <summary>Vykreslí text znak po znaku s prokládáním, vycentrovaný na cx.</summary>
    private static void TrackedCentered(DrawingContext ctx, string s, Fnt f, IBrush brush, double cx, double y, double spacing)
    {
        var glyphs = new FormattedText[s.Length];
        double total = 0;

        for (int i = 0; i < s.Length; i++)
        {
            glyphs[i] = Ft(s[i].ToString(), f, brush);
            total += glyphs[i].Width + (i < s.Length - 1 ? spacing : 0);
        }

        double x = cx - total / 2;
        foreach (var glyph in glyphs)
        {
            ctx.DrawText(glyph, new Point(x, y));
            x += glyph.Width + spacing;
        }
    }

    /// <summary>Levné prokládání pro krátké popisky: "SAZEBNÍK" -> "S A Z E B N Í K".</summary>
    private static string Spaced(string s)
    {
        if (SpacedCache.TryGetValue(s, out var cached)) return cached;

        var sb = new StringBuilder(s.Length * 2);
        foreach (char ch in s)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(ch);
        }

        string result = sb.ToString();
        if (SpacedCache.Count < 256) SpacedCache[s] = result;
        return result;
    }

    private static string Signed(int value) => value >= 0 ? $"+{value}" : $"−{-value}";

    private static IBrush Br(Color c) => new SolidColorBrush(c);

    private static Color Blend(Color a, Color b, double t) => Color.FromRgb(
        (byte)(a.R + (b.R - a.R) * t),
        (byte)(a.G + (b.G - a.G) * t),
        (byte)(a.B + (b.B - a.B) * t));

    private static Color Lighten(Color c, double t) => Blend(c, Colors.White, t);

    private static Color Darken(Color c, double t) => Blend(c, Colors.Black, t);

    private static void Fill(DrawingContext ctx, Color c, Rect r) => ctx.DrawRectangle(Br(c), null, r);

    private static void Fill(DrawingContext ctx, Color c, double x, double y, double w, double h)
        => ctx.DrawRectangle(Br(c), null, new Rect(x, y, w, h));

    private static void Frame(DrawingContext ctx, Color c, double thickness, Rect r)
        => ctx.DrawRectangle(null, new Pen(Br(c), thickness), r);

    private static void Line(DrawingContext ctx, Color c, double thickness, double x1, double y1, double x2, double y2)
        => ctx.DrawLine(new Pen(Br(c), thickness), new Point(x1, y1), new Point(x2, y2));

    private static void Ell(DrawingContext ctx, IBrush? brush, IPen? pen, double x, double y, double w, double h)
        => ctx.DrawEllipse(brush, pen, new Point(x + w / 2, y + h / 2), w / 2, h / 2);

    private static void VerticalGradient(DrawingContext ctx, Rect r, Color top, Color bottom)
    {
        ctx.DrawRectangle(new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(top, 0),
                new GradientStop(bottom, 1)
            }
        }, null, r);
    }

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
