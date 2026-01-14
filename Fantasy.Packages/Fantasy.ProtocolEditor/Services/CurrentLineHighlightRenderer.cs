using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using AvaloniaEdit.Rendering;

namespace Fantasy.ProtocolEditor.Services;

/// <summary>
/// 当前行高亮渲染器 - 自定义当前行背景高度
/// </summary>
public class CurrentLineHighlightRenderer : IBackgroundRenderer
{
    private readonly AvaloniaEdit.TextEditor _editor;
    private readonly double _verticalPadding;

    public CurrentLineHighlightRenderer(AvaloniaEdit.TextEditor editor, double verticalPadding = 4)
    {
        _editor = editor;
        _verticalPadding = verticalPadding;
    }

    public KnownLayer Layer => KnownLayer.Background;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_editor.Document == null || !textView.VisualLinesValid)
            return;

        // 获取当前光标所在的行
        var currentLine = _editor.Document.GetLineByOffset(_editor.CaretOffset);

        foreach (var visualLine in textView.VisualLines)
        {
            // 检查是否是当前行
            if (visualLine.FirstDocumentLine.LineNumber == currentLine.LineNumber)
            {
                // 获取行的视觉矩形
                var lineRect = BackgroundGeometryBuilder.GetRectsFromVisualSegment(
                    textView, visualLine, 0, visualLine.VisualLength).FirstOrDefault();

                if (lineRect != default)
                {
                    // 扩展上下边距
                    var expandedRect = new Rect(
                        lineRect.X,
                        lineRect.Y - _verticalPadding,
                        textView.Bounds.Width, // 整行宽度
                        lineRect.Height + _verticalPadding * 2
                    );

                    // 绘制黑色背景
                    drawingContext.DrawRectangle(
                        new SolidColorBrush(Color.Parse("#131314")),
                        null,
                        expandedRect
                    );
                }
                break;
            }
        }
    }
}
