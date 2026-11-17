using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Tablet.App.Views;

/// <summary>Offline illustrative map. Contains no customer map or robot telemetry.</summary>
public sealed class SampleMap : FrameworkElement
{
    private static Brush Color(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        if (ActualWidth <= 0 || ActualHeight <= 0) return;
        double scale = Math.Min(ActualWidth / 900, ActualHeight / 700);
        dc.PushTransform(new TranslateTransform((ActualWidth - 900 * scale) / 2, (ActualHeight - 700 * scale) / 2));
        dc.PushTransform(new ScaleTransform(scale, scale));
        dc.DrawRoundedRectangle(Color("#F7F8FC"), null, new Rect(0, 0, 900, 700), 16, 16);
        var grid = new Pen(Color("#ECEFF7"), 1);
        for (int x = 20; x < 900; x += 40) dc.DrawLine(grid, new(x, 0), new(x, 700));
        for (int y = 20; y < 700; y += 40) dc.DrawLine(grid, new(0, y), new(900, y));
        var wall = new Pen(Color("#B8C0D2"), 7) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawRoundedRectangle(Color("#FFFFFF"), wall, new Rect(70, 70, 760, 540), 10, 10);
        dc.DrawRectangle(Color("#EDF0F6"), wall, new Rect(125, 125, 200, 120));
        dc.DrawRectangle(Color("#EDF0F6"), wall, new Rect(545, 125, 200, 120));
        dc.DrawRectangle(Color("#EDF0F6"), wall, new Rect(545, 425, 200, 100));
        dc.DrawRoundedRectangle(Color("#FFF1F1"), new Pen(Color("#F4A8A8"), 2), new Rect(125, 425, 150, 100), 8, 8);
        dc.DrawRoundedRectangle(Color("#FFF8EB"), new Pen(Color("#E8C077"), 2), new Rect(350, 275, 120, 260), 8, 8);
        Label(dc, "주행 금지", 151, 462, "#D66464", 17);
        Label(dc, "감속", 391, 450, "#C08430", 17);
        Label(dc, "작업 구역 A", 164, 170, "#8B95AB", 18);
        Label(dc, "작업 구역 B", 580, 170, "#8B95AB", 18);
        Poi(dc, 230, 320, "조립 라인 A");
        Poi(dc, 645, 320, "조립 라인 B");
        Poi(dc, 350, 570, "충전 스테이션");
        dc.DrawEllipse(Color("#E0E2FF"), null, new(420, 350), 47, 47);
        dc.DrawRoundedRectangle(Color("#5B5EF4"), new Pen(Brushes.White, 4), new Rect(399, 327, 42, 46), 12, 12);
        dc.DrawLine(new Pen(Brushes.White, 3), new(410, 344), new(420, 334));
        dc.DrawLine(new Pen(Brushes.White, 3), new(420, 334), new(430, 344));
        Label(dc, "MobED (예시)", 366, 396, "#5B5EF4", 17);
        dc.Pop(); dc.Pop();
    }

    private void Poi(DrawingContext dc, double x, double y, string name)
    {
        dc.DrawEllipse(Brushes.White, new Pen(Color("#5A6379"), 3), new(x, y), 12, 12);
        dc.DrawEllipse(Color("#5B5EF4"), null, new(x, y), 4, 4);
        Label(dc, name, x - 56, y + 22, "#5A6379", 17);
    }

    private void Label(DrawingContext dc, string text, double x, double y, string color, double size) =>
        dc.DrawText(new FormattedText(text, CultureInfo.GetCultureInfo("ko-KR"), FlowDirection.LeftToRight,
            new Typeface("Malgun Gothic"), size, Color(color), VisualTreeHelper.GetDpi(this).PixelsPerDip), new(x, y));
}
