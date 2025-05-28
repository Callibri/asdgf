using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GraphSolver.Entities;
using GraphSolver.GraphComponents;
using GraphSolver.Algorithms;
using GraphSolver.Rendering;

namespace GraphSolver
{

    public partial class MainWindow : Window
    {
        private Graph currentGraph = new Graph();
        private ISpanningTreeAlgorithm[] algorithms;
        private Random rand = new Random();

        private Dictionary<int, Point> fixedVertexPositions = new Dictionary<int, Point>();
        private int lastFixedVertexCount = 0;

        public MainWindow()
        {
            InitializeComponent();
            algorithms = new ISpanningTreeAlgorithm[]
            {
                new PrimAlgorithm(),
                new KruskalAlgorithm(),
                new BoruvkaAlgorithm()
            };
            DrawGraph();
            AttachInputValidationHandlers(); 
        }

    
        private void AttachInputValidationHandlers()
        {
            if (NumVerticesTextBox != null) NumVerticesTextBox.PreviewTextInput += NumberValidationTextBox;
            if (EdgeSourceIdTextBox != null) EdgeSourceIdTextBox.PreviewTextInput += NumberValidationTextBox;
            if (EdgeDestinationIdTextBox != null) EdgeDestinationIdTextBox.PreviewTextInput += NumberValidationTextBox;
            if (DeleteVertexIdTextBox != null) DeleteVertexIdTextBox.PreviewTextInput += NumberValidationTextBox;
            if (DeleteEdgeSourceIdTextBox != null) DeleteEdgeSourceIdTextBox.PreviewTextInput += NumberValidationTextBox;
            if (DeleteEdgeDestinationIdTextBox != null) DeleteEdgeDestinationIdTextBox.PreviewTextInput += NumberValidationTextBox;

            if (EdgeWeightTextBox != null) EdgeWeightTextBox.PreviewTextInput += DecimalValidationTextBox;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            bool isNumber = Regex.IsMatch(e.Text, "[0-9]");
            bool isDot = (e.Text == ".");

            if (isDot && textBox.Text.Contains("."))
            {
                e.Handled = true;
            }
            else if (!isNumber && !isDot)
            {
                e.Handled = true;
            }
        }

        private int GetNextAvailableVertexId()
        {
            if (currentGraph.Vertices.Count == 0)
            {
                return 1;
            }
            return currentGraph.Vertices.Max(v => v.Id) + 1;
        }

        private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            const int MAX_VERTICES_MANUAL = 40; 
            if (currentGraph.Vertices.Count >= MAX_VERTICES_MANUAL)
            {
                MessageBox.Show($"Досягнуто максимальну кількість вершин ({MAX_VERTICES_MANUAL}). Нові вершини не можуть бути додані вручну.", "Обмеження вершин", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Point clickPosition = e.GetPosition(GraphCanvas);
            currentGraph.AddVertex(new Vertex(GetNextAvailableVertexId(), clickPosition.X, clickPosition.Y));
            DrawGraph();
            ClearResults();
        }

        private void GenerateGraph_Click(object sender, RoutedEventArgs e)
        {
            currentGraph.Clear(); 
            fixedVertexPositions.Clear(); 
            lastFixedVertexCount = 0; 

            const int MAX_VERTICES = 40; 
            double baseVertexSize = 50;

            if (!int.TryParse(NumVerticesTextBox.Text, out int numVertices) || numVertices <= 0 || numVertices > MAX_VERTICES)
            {
                MessageBox.Show($"Будь ласка, введіть коректну кількість вершин (від 1 до {MAX_VERTICES}).", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (numVertices == 1)
            {
                currentGraph.Clear();
                fixedVertexPositions.Clear();
                lastFixedVertexCount = 0;

                double center_x = GraphCanvas.ActualWidth / 2;
                double center_y = GraphCanvas.ActualHeight / 2;
                fixedVertexPositions[1] = new Point(center_x, center_y);
                currentGraph.AddVertex(new Vertex(1, center_x, center_y));
                DrawGraph(baseVertexSize);
                MessageBox.Show("Згенеровано граф з однією вершиною.", "Генерація графу", MessageBoxButton.OK, MessageBoxImage.Information);
                return; 
            }


            if (numVertices != lastFixedVertexCount || fixedVertexPositions.Count == 0)
            {
                fixedVertexPositions.Clear();
                currentGraph.Clear();

                double padding = 50;
                double canvasWidth = GraphCanvas.ActualWidth;
                double canvasHeight = GraphCanvas.ActualHeight;

                double center_x = canvasWidth / 2;
                double center_y = canvasHeight / 2;
                double max_radius = Math.Min(canvasWidth, canvasHeight) / 2 - padding;

                List<double> radii = new List<double>();
                double innerRadius = max_radius * 0.3;
                double middleRadius = max_radius * 0.6;
                double outerRadius = max_radius;

                radii.Add(outerRadius);
                radii.Add(middleRadius);
                radii.Add(innerRadius);

                
                int[] counts = new int[3]; 

                counts[0] = (int)Math.Floor(numVertices * 0.60);
                counts[1] = (int)Math.Floor(numVertices * 0.30);
                counts[2] = numVertices - counts[0] - counts[1]; 
  
                for (int i = 0; i < counts.Length; i++)
                {
                    if (counts[i] < 0) counts[i] = 0;
                }

                int currentSum = counts.Sum();
                while (currentSum < numVertices)
                {
                    
                    if (counts[0] < numVertices) { counts[0]++; }
                    else if (counts[1] < numVertices) { counts[1]++; }
                    else { counts[2]++; } 
                    currentSum = counts.Sum();
                }

                while (currentSum > numVertices)
                {
                    if (counts[2] > 0) { counts[2]--; }
                    else if (counts[1] > 0) { counts[1]--; }
                    else { counts[0]--; } 
                    currentSum = counts.Sum();
                }

                int outerCircleVertices = counts[0];
                int middleCircleVertices = counts[1];
                int innerCircleVertices = counts[2];


                List<int> verticesPerCircleList = new List<int> { outerCircleVertices, middleCircleVertices, innerCircleVertices };


                int currentVertexId = 1;
                for (int c = 0; c < radii.Count; c++) 
                {
                    double currentRadius = radii[c];
                    int verticesInThisCircle = 0;
                    if (c < verticesPerCircleList.Count) 
                    {
                        verticesInThisCircle = verticesPerCircleList[c];
                    }

                    if (verticesInThisCircle <= 0) continue;

                    double angularStep = (2 * Math.PI) / verticesInThisCircle;

                    for (int i = 0; i < verticesInThisCircle; i++)
                    {
                        double angle = i * angularStep + rand.NextDouble() * 0.1; 
                        double x = center_x + currentRadius * Math.Cos(angle);
                        double y = center_y + currentRadius * Math.Sin(angle);


                        x = Math.Max(padding, Math.Min(canvasWidth - padding, x));
                        y = Math.Max(padding, Math.Min(canvasHeight - padding, y));

                        fixedVertexPositions[currentVertexId] = new Point(x, y);
                        currentVertexId++;
                    }
                }
                lastFixedVertexCount = numVertices;
            }
            else
            {
                currentGraph.ClearEdges();
            }

            foreach (var entry in fixedVertexPositions)
            {
                currentGraph.AddVertex(new Vertex(entry.Key, entry.Value.X, entry.Value.Y));
            }

            double densityFactor;
            if (numVertices >= 1 && numVertices <= 15)
            {
                densityFactor = 0.4;
            }
            else if (numVertices >= 30 && numVertices <= 40)
            {
                densityFactor = 0.1;
            }
            else
            {
                densityFactor = 0.2;
            }


            int targetEdges = (int)(numVertices * (numVertices - 1) / 2 * densityFactor);
            if (targetEdges < numVertices - 1 && numVertices > 1)
            {
                targetEdges = numVertices - 1;
            }


            List<Tuple<Vertex, Vertex, double>> potentialEdges = new List<Tuple<Vertex, Vertex, double>>();

            for (int i = 0; i < currentGraph.Vertices.Count; i++)
            {
                for (int j = i + 1; j < currentGraph.Vertices.Count; j++)
                {
                    Vertex v1 = currentGraph.Vertices[i];
                    Vertex v2 = currentGraph.Vertices[j];
                    double distance = Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2));
                    potentialEdges.Add(Tuple.Create(v1, v2, distance));
                }
            }

            potentialEdges = potentialEdges.OrderBy(e => e.Item3).ToList();

            foreach (var potentialEdge in potentialEdges)
            {
                if (currentGraph.Edges.Count >= targetEdges && currentGraph.IsConnected())
                {
                    break;
                }

                Vertex v1 = potentialEdge.Item1;
                Vertex v2 = potentialEdge.Item2;
                double weight = rand.Next(1, 50);

                if (!currentGraph.Edges.Any(e =>
                    (e.Source.Equals(v1) && e.Destination.Equals(v2)) ||
                    (e.Source.Equals(v2) && e.Destination.Equals(v1))))
                {
                    currentGraph.AddEdge(v1, v2, weight);
                }
            }

            int connectivityAttempts = 0;
            int maxConnectivityAttempts = numVertices * (numVertices - 1);
            while (!currentGraph.IsConnected() && connectivityAttempts < maxConnectivityAttempts)
            {
                Vertex v1 = currentGraph.Vertices[rand.Next(currentGraph.Vertices.Count)];
                Vertex v2 = currentGraph.Vertices[rand.Next(currentGraph.Vertices.Count)];

                if (v1.Equals(v2)) continue;

                if (!currentGraph.Edges.Any(e =>
                    (e.Source.Equals(v1) && e.Destination.Equals(v2)) ||
                    (e.Source.Equals(v2) && e.Destination.Equals(v1))))
                {
                    currentGraph.AddEdge(v1, v2, rand.Next(1, 50));
                }
                connectivityAttempts++;
            }

            if (!currentGraph.IsConnected() && currentGraph.Vertices.Count > 1)
            {
                MessageBox.Show("Не вдалося зробити граф зв'язним після генерації. Спробуйте збільшити кількість вершин або перегенерувати.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            DrawGraph(baseVertexSize);
        }

        private void AddEdgeManual_Click(object sender, RoutedEventArgs e)
        {
            if (currentGraph.Vertices.Count < 2)
            {
                MessageBox.Show("Для додавання ребра потрібно мінімум дві вершини.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(EdgeSourceIdTextBox.Text, out int sourceId) ||
                !int.TryParse(EdgeDestinationIdTextBox.Text, out int destinationId) ||
                !double.TryParse(EdgeWeightTextBox.Text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double weight))
            {
                MessageBox.Show("Будь ласка, введіть коректні ID вершин та вагу ребра.", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (sourceId == destinationId)
            {
                MessageBox.Show("Вершини джерела та призначення не можуть бути однаковими.", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Vertex? sourceVertex = currentGraph.Vertices.FirstOrDefault(v => v.Id == sourceId);
            Vertex? destinationVertex = currentGraph.Vertices.FirstOrDefault(v => v.Id == destinationId);

            if (sourceVertex == null || destinationVertex == null)
            {
                MessageBox.Show("Одна або обидві зазначені вершини не існують.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (currentGraph.Edges.Any(edge =>
                (edge.Source.Equals(sourceVertex) && edge.Destination.Equals(destinationVertex)) ||
                (edge.Source.Equals(destinationVertex) && edge.Destination.Equals(sourceVertex))))
            {
                MessageBox.Show("Ребро між цими вершинами вже існує. Оновіть його вагу, якщо потрібно.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            currentGraph.AddEdge(sourceVertex, destinationVertex, weight);
            DrawGraph();
            ClearResults();
        }

        private void AddRandomEdge_Click(object sender, RoutedEventArgs e)
        {
            if (currentGraph.Vertices.Count < 2)
            {
                MessageBox.Show("Для додавання рандомного ребра потрібно мінімум дві вершини.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Vertex v1, v2;
            int attempts = 0;
            const int MAX_ATTEMPTS = 200;

            do
            {
                v1 = currentGraph.Vertices[rand.Next(currentGraph.Vertices.Count)];
                v2 = currentGraph.Vertices[rand.Next(currentGraph.Vertices.Count)];
                attempts++;
            } while (v1.Equals(v2) ||
                     currentGraph.Edges.Any(edge =>
                         (edge.Source.Equals(v1) && edge.Destination.Equals(v2)) ||
                         (edge.Source.Equals(v2) && edge.Destination.Equals(v1))) && attempts < MAX_ATTEMPTS);

            if (attempts == MAX_ATTEMPTS)
            {
                MessageBox.Show("Не вдалося знайти унікальну пару вершин для додавання рандомного ребра. Граф може бути занадто щільним.", "Попередження", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            double weight = rand.Next(1, 100);
            currentGraph.AddEdge(v1, v2, weight);
            DrawGraph();
            ClearResults();
        }

        private void BuildMST_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentGraph.Vertices.Count < 2)
                {
                    MessageBox.Show("Мінімальне остовне дерево можна побудувати тільки для графа, що містить щонайменше дві вершини.", "Помилка MST", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearResults(); 
                    AlgorithmComplexityTextBlock.Text = "Практична складність: -"; 
                    return;
                }


                if (!currentGraph.IsConnected())
                {
                    MessageBox.Show("Граф не є зв'язним. Остовне дерево не існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ISpanningTreeAlgorithm selectedAlgorithm;
                string practicalComplexity = "Не визначено"; 

                if (PrimRadioButton.IsChecked == true)
                {
                    selectedAlgorithm = algorithms[0];
                    int V = currentGraph.Vertices.Count;
                    if (V < 10)
                    {
                        practicalComplexity = "O(V²)";
                    }
                    else
                    {
                        practicalComplexity = "O(E + V log V)";
                    }
                }
                else if (KruskalRadioButton.IsChecked == true)
                {
                    selectedAlgorithm = algorithms[1];

                    bool areVerticesAlmostOnSameHeight = false;
                    if (currentGraph.Vertices.Count > 1)
                    {
                        var yCoordinates = currentGraph.Vertices.Select(v => v.Y).ToList();
                        double minY = yCoordinates.Min();
                        double maxY = yCoordinates.Max();
                        double heightThreshold = 10.0;

                        if ((maxY - minY) < heightThreshold)
                        {
                            areVerticesAlmostOnSameHeight = true;
                        }
                    }

                    if (areVerticesAlmostOnSameHeight)
                    {
                        practicalComplexity = "O(E log* V)";
                    }
                    else
                    {
                        practicalComplexity = "O(E log E)";
                    }
                }
                else if (BoruvkaRadioButton.IsChecked == true)
                {
                    selectedAlgorithm = algorithms[2];
                    practicalComplexity = "O(E log V)";
                }
                else
                {
                    MessageBox.Show("Будь ласка, оберіть алгоритм.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var (mstEdges, totalWeight, timeTaken) = selectedAlgorithm.FindMST(currentGraph);

                foreach (var edge in currentGraph.Edges)
                    edge.IsInSpanningTree = mstEdges.Contains(edge);

                ResultOutputTextBox.Text = $"МОД ({selectedAlgorithm.GetType().Name.Replace("Algorithm", "")}):\n" +
                                           $"Загальна вага: {totalWeight:F2}\n" +
                                           $"Час виконання: {timeTaken.TotalMilliseconds:F4} мс\n" +
                                           "Ребра МОД:\n" +
                                           string.Join("\n", mstEdges.Select(edge =>
                                               $"{edge.Source.Id} -- {edge.Weight:F0} -- {edge.Destination.Id}"));

                ExecutionTimeTextBlock.Text = $"Час виконання: {timeTaken.TotalMilliseconds:F4} мс";
                AlgorithmComplexityTextBlock.Text = $"Практична складність: {practicalComplexity}"; 
                DrawGraph();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}", "Помилка виконання", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveResult_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Текстові файли (*.txt)|*.txt",
                FileName = "MST_Result.txt"
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, ResultOutputTextBox.Text);
                MessageBox.Show("Результати збережено!");
            }
        }

        private void ClearGraph_Click(object sender, RoutedEventArgs e)
        {
            currentGraph.Clear();
            DrawGraph();
            ClearResults();
            fixedVertexPositions.Clear();
            lastFixedVertexCount = 0;
            AlgorithmComplexityTextBlock.Text = "Практична складність: -"; 
        }

        private void DrawGraph(double baseVertexSize = 50)
        {
            GraphRenderer.Draw(currentGraph, GraphCanvas, baseVertexSize);
        }

        private void ClearResults()
        {
            ResultOutputTextBox.Clear();
            ExecutionTimeTextBlock.Text = "Час виконання:";
            foreach (var edge in currentGraph.Edges)
            {
                edge.IsInSpanningTree = false;
            }
        }

        private void DeleteVertexById_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(DeleteVertexIdTextBox.Text, out int vertexId))
            {
                MessageBox.Show("Будь ласка, введіть дійсний ID вершини для видалення.", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Vertex? vertexToRemove = currentGraph.Vertices.FirstOrDefault(v => v.Id == vertexId);

            if (vertexToRemove != null)
            {
                currentGraph.RemoveVertex(vertexToRemove);
                fixedVertexPositions.Remove(vertexId);
                DrawGraph();
                ClearResults();
                MessageBox.Show($"Вершину з ID {vertexId} успішно видалено.", "Видалення вершини", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Вершина з ID {vertexId} не знайдена.", "Помилка видалення", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            DeleteVertexIdTextBox.Clear();
        }

        private void DeleteEdgeById_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(DeleteEdgeSourceIdTextBox.Text, out int sourceId) ||
                !int.TryParse(DeleteEdgeDestinationIdTextBox.Text, out int destinationId))
            {
                MessageBox.Show("Будь ласка, введіть дійсні ID вершин джерела та призначення для видалення ребра.", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (sourceId == destinationId)
            {
                MessageBox.Show("ID джерела та призначення не можуть бути однаковими для ребра.", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Edge? edgeToRemove = currentGraph.Edges.FirstOrDefault(edge =>
                (edge.Source.Id == sourceId && edge.Destination.Id == destinationId) ||
                (edge.Source.Id == destinationId && edge.Destination.Id == sourceId));

            if (edgeToRemove != null)
            {
                currentGraph.RemoveEdge(edgeToRemove);
                DrawGraph();
                ClearResults();
                MessageBox.Show($"Ребро між вершинами {sourceId} та {destinationId} успішно видалено.", "Видалення ребра", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Ребро між вершинами {sourceId} та {destinationId} не знайдено.", "Помилка видалення", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            DeleteEdgeSourceIdTextBox.Clear();
            DeleteEdgeDestinationIdTextBox.Clear();
        }
    }
}