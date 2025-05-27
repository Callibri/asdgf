// File: GraphRenderer.cs
using GraphSolver.Entities;
using GraphSolver.GraphComponents;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Linq; // Додано для використання Linq

namespace GraphSolver.Rendering
{
    public static class GraphRenderer
    {
        private static readonly Brush DefaultVertexFill = Brushes.LightGreen;
        private static readonly Brush DefaultVertexStroke = Brushes.DarkGreen;
        private static readonly Brush EdgeWeightForeground = Brushes.DimGray;
        private static readonly Brush EdgeWeightBackground = Brushes.Transparent;
        private static readonly Brush MstEdgeBrush = Brushes.Red;

        private const double MIN_VERTEX_DRAW_SIZE = 30; // Збільшено мінімальний розмір вершини

        public static void Draw(Graph graph, Canvas canvas, double baseVertexSize = 50) // Оновлено значення за замовчуванням
        {
            canvas.Children.Clear();

            double vertexSize = CalculateVertexSize(graph.Vertices.Count, baseVertexSize);

            // 1. Малюємо ребра (лінії)
            foreach (var edge in graph.Edges)
            {
                var line = new Line
                {
                    X1 = edge.Source.X,
                    Y1 = edge.Source.Y,
                    X2 = edge.Destination.X,
                    Y2 = edge.Destination.Y,
                    Stroke = edge.IsInSpanningTree ? MstEdgeBrush : Brushes.Black,
                    StrokeThickness = edge.IsInSpanningTree ? 3 : 1
                };
                // Встановлюємо Z-індекс для ребер на низьке значення
                Canvas.SetZIndex(line, 0); // Ребра на самому задньому плані
                canvas.Children.Add(line);
            }

            // 2. Малюємо вершини (кола та їх ID)
            foreach (var vertex in graph.Vertices)
            {
                var ellipse = new Ellipse
                {
                    Width = vertexSize,
                    Height = vertexSize,
                    Fill = DefaultVertexFill,
                    Stroke = DefaultVertexStroke,
                    StrokeThickness = 2
                };

                canvas.Children.Add(ellipse);
                Canvas.SetLeft(ellipse, vertex.X - vertexSize / 2);
                Canvas.SetTop(ellipse, vertex.Y - vertexSize / 2);

                var idText = new TextBlock
                {
                    Text = vertex.Id.ToString(),
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold,
                    FontSize = vertexSize * 0.4
                };

                canvas.Children.Add(idText);
                idText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                double textWidth = idText.DesiredSize.Width;
                double textHeight = idText.DesiredSize.Height;

                Canvas.SetLeft(idText, vertex.X - textWidth / 2);
                Canvas.SetTop(idText, vertex.Y - textHeight / 2);

                // Встановлюємо Z-індекс для тексту ID вершин на вище значення, ніж для кіл вершин
                Canvas.SetZIndex(ellipse, 1); // Кола вершин на середньому плані
                Canvas.SetZIndex(idText, 2); // Текст ID вершин на передньому плані, над колами
            }

            // 3. Малюємо ваги ребер (текст) - ЦЕ МАЄ БУТИ ОСТАННІМ
            foreach (var edge in graph.Edges)
            {
                // Розраховуємо середину ребра
                Point midPoint = new Point((edge.Source.X + edge.Destination.X) / 2, (edge.Source.Y + edge.Destination.Y) / 2);

                TextBlock weightText = new TextBlock
                {
                    Text = edge.Weight.ToString("F0"),
                    Foreground = EdgeWeightForeground, // Використовуємо існуючу змінну
                    FontSize = 12, // Зменшимо розмір шрифту ваги, щоб краще поміщався
                    FontWeight = FontWeights.Bold,
                    Background = EdgeWeightBackground, // Використовуємо існуючу змінну
                    Padding = new Thickness(2)
                };

                // Встановлюємо позицію тексту ваги
                weightText.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(weightText, midPoint.X - weightText.DesiredSize.Width / 2);
                Canvas.SetTop(weightText, midPoint.Y - weightText.DesiredSize.Height / 2);

                Canvas.SetZIndex(weightText, 3); // Ваги ребер на передньому плані
                canvas.Children.Add(weightText);
            }
        }

        public static double CalculateVertexSize(int vertexCount, double baseSize)
        {
            // Зменшуємо розмір вершин, якщо їх багато, до мінімального
            if (vertexCount > 10)
                return Math.Max(MIN_VERTEX_DRAW_SIZE, baseSize - (vertexCount - 10) * 1.5);
            return baseSize;
        }
    }
}