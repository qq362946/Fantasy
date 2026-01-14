using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace Fantasy.ProtocolEditor.Services;

/// <summary>
/// 文本错误标记服务 - 用于在编辑器中显示错误波浪线
/// </summary>
public class TextMarkerService : DocumentColorizingTransformer, IBackgroundRenderer
{
    private readonly List<TextMarker> _markers = new();
    private readonly TextDocument _document;

    public TextMarkerService(TextDocument document)
    {
        _document = document;
    }

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView.Document == null || !textView.VisualLinesValid)
            return;

        var visualLines = textView.VisualLines;
        if (visualLines.Count == 0)
            return;

        foreach (var marker in _markers)
        {
            if (marker.StartOffset >= _document.TextLength)
                continue;

            foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker))
            {
                // 绘制红色波浪线
                DrawWavyLine(drawingContext, rect, marker.MarkerBrush);
            }
        }
    }

    private void DrawWavyLine(DrawingContext drawingContext, Rect rect, IBrush brush)
    {
        const double waveHeight = 2.0;
        const double waveWidth = 4.0;

        var pen = new Pen(brush, 1.0);
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            var y = rect.Bottom - waveHeight;
            context.BeginFigure(new Point(rect.Left, y), false);

            for (double x = rect.Left; x < rect.Right; x += waveWidth)
            {
                context.LineTo(new Point(x + waveWidth / 2, y + waveHeight));
                context.LineTo(new Point(x + waveWidth, y));
            }
        }

        drawingContext.DrawGeometry(null, pen, geometry);
    }

    protected override void ColorizeLine(DocumentLine line)
    {
        // 不需要在这里做颜色处理
    }

    public void AddMarker(int offset, int length, string message, IBrush? brush = null)
    {
        var marker = new TextMarker
        {
            StartOffset = offset,
            Length = length,
            Message = message,
            MarkerBrush = brush ?? Brushes.Red
        };

        _markers.Add(marker);
    }

    public void Clear()
    {
        _markers.Clear();
    }

    public IEnumerable<TextMarker> GetMarkersAtOffset(int offset)
    {
        return _markers.Where(m => m.StartOffset <= offset && offset < m.StartOffset + m.Length);
    }
}

/// <summary>
/// 文本标记
/// </summary>
public class TextMarker : TextSegment
{
    public string Message { get; set; } = string.Empty;
    public IBrush MarkerBrush { get; set; } = Brushes.Red;
}
