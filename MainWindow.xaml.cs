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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Graph currentGraph = new Graph();
        private ISpanningTreeAlgorithm[] algorithms;
        private Random rand = new Random();

        // Додаємо словник для зберігання фіксованих позицій вершин
        private Dictionary<int, Point> fixedVertexPositions = new Dictionary<int, Point>();
        // Зберігаємо останню кількість вершин, для якої були зафіксовані позиції
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
            AttachInputValidationHandlers(); // Викликаємо метод для прикріплення обробників
        }

        // Метод для прикріплення обробників валідації вводу
        private void AttachInputValidationHandlers()
        {
            // Для числових полів (макс. 2 цифри). MaxLength задано в XAML.
            if (NumVerticesTextBox != null) NumVerticesTextBox.PreviewTextInput += NumberValidationTextBox;
            if (EdgeSourceIdTextBox != null) EdgeSourceIdTextBox.PreviewTextInput += NumberValidationTextBox;
            if (EdgeDestinationIdTextBox != null) EdgeDestinationIdTextBox.PreviewTextInput += NumberValidationTextBox;
            if (DeleteVertexIdTextBox != null) DeleteVertexIdTextBox.PreviewTextInput += NumberValidationTextBox;
            if (DeleteEdgeSourceIdTextBox != null) DeleteEdgeSourceIdTextBox.PreviewTextInput += NumberValidationTextBox;
            if (DeleteEdgeDestinationIdTextBox != null) DeleteEdgeDestinationIdTextBox.PreviewTextInput += NumberValidationTextBox;

            // Для поля ваги (цифри та одна крапка, макс. 5 символів). MaxLength задано в XAML.
            if (EdgeWeightTextBox != null) EdgeWeightTextBox.PreviewTextInput += DecimalValidationTextBox;
        }

        // Обробник подій для числових TextBox (ID вершин, кількість вершин)
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Дозволяє лише цифри.
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Обробник подій для TextBox ваги (цифри та одна крапка)
        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null) return;

            // Дозволити лише одну крапку та цифри
            bool isNumber = Regex.IsMatch(e.Text, "[0-9]");
            bool isDot = (e.Text == ".");

            if (isDot && textBox.Text.Contains("."))
            {
                // Якщо вже є крапка, не дозволяти додавати ще одну
                e.Handled = true;
            }
            else if (!isNumber && !isDot)
            {
                // Якщо не цифра і не крапка, заборонити
                e.Handled = true;
            }
            // MaxLength, заданий в XAML, подбає про загальну довжину (5 символів).
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
            const int MAX_VERTICES_MANUAL = 40; // Максимальна кількість вершин для додавання вручну
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
            GraphCanvas.Children.Clear();
            ClearResults();

            const int MAX_VERTICES = 40; // Максимальна кількість вершин для генерації та валідації вводу
            double baseVertexSize = 50;

            if (!int.TryParse(NumVerticesTextBox.Text, out int numVertices) || numVertices <= 0 || numVertices > MAX_VERTICES)
            {
                MessageBox.Show($"Будь ласка, введіть коректну кількість вершин (від 1 до {MAX_VERTICES}).", "Помилка вводу", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // --- ПОЧАТОК: Виправлення для графа з однією вершиною ---
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
                return; // Завершуємо виконання методу, оскільки граф з 1 вершиною вже згенеровано
            }
            // --- КІНЕЦЬ: Виправлення для графа з однією вершиною ---


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

                // Розподіл вершин по колах
                // Використовуємо більш надійний розподіл, щоб уникнути від'ємних значень або некоректних сум
                int[] counts = new int[3]; // 0: outer, 1: middle, 2: inner

                // Спочатку спробуємо розподілити відповідно до відсотків
                counts[0] = (int)Math.Floor(numVertices * 0.60);
                counts[1] = (int)Math.Floor(numVertices * 0.30);
                counts[2] = numVertices - counts[0] - counts[1]; // Решта йде у внутрішнє коло

                // Коригуємо, щоб сума була точно numVertices і не було від'ємних значень
                for (int i = 0; i < counts.Length; i++)
                {
                    if (counts[i] < 0) counts[i] = 0;
                }

                int currentSum = counts.Sum();
                while (currentSum < numVertices)
                {
                    // Додаємо по одній, починаючи з зовнішнього кола
                    if (counts[0] < numVertices) { counts[0]++; }
                    else if (counts[1] < numVertices) { counts[1]++; }
                    else { counts[2]++; } // Якщо зовнішнє та середнє заповнені, додаємо у внутрішнє
                    currentSum = counts.Sum();
                }

                while (currentSum > numVertices)
                {
                    // Віднімаємо по одній, починаючи з внутрішнього кола
                    if (counts[2] > 0) { counts[2]--; }
                    else if (counts[1] > 0) { counts[1]--; }
                    else { counts[0]--; } // Якщо внутрішнє та середнє порожні, віднімаємо від зовнішнього
                    currentSum = counts.Sum();
                }

                int outerCircleVertices = counts[0];
                int middleCircleVertices = counts[1];
                int innerCircleVertices = counts[2];


                List<int> verticesPerCircleList = new List<int> { outerCircleVertices, middleCircleVertices, innerCircleVertices };


                int currentVertexId = 1;
                for (int c = 0; c < radii.Count; c++) // Проходимо по всіх радіусах
                {
                    double currentRadius = radii[c];
                    int verticesInThisCircle = 0;
                    if (c < verticesPerCircleList.Count) // Перевірка на межі списку
                    {
                        verticesInThisCircle = verticesPerCircleList[c];
                    }

                    if (verticesInThisCircle <= 0) continue;

                    double angularStep = (2 * Math.PI) / verticesInThisCircle;

                    for (int i = 0; i < verticesInThisCircle; i++)
                    {
                        double angle = i * angularStep + rand.NextDouble() * 0.1; // Додаємо невеликий шум
                        double x = center_x + currentRadius * Math.Cos(angle);
                        double y = center_y + currentRadius * Math.Sin(angle);

                        // Перевірка, щоб вершини не виходили за межі Canvas з урахуванням padding
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

            // Використовуємо CultureInfo.InvariantCulture для правильного парсингу числа з плаваючою комою (з крапкою)
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
                // --- ПОЧАТОК: Виправлення для графа з однією вершиною при побудові MST ---
                if (currentGraph.Vertices.Count < 2)
                {
                    MessageBox.Show("Мінімальне остовне дерево можна побудувати тільки для графа, що містить щонайменше дві вершини.", "Помилка MST", MessageBoxButton.OK, MessageBoxImage.Information);
                    ClearResults(); // Очищаємо результати, якщо була спроба розрахунку
                    AlgorithmComplexityTextBlock.Text = "Практична складність: -"; // Очищаємо складність
                    return;
                }
                // --- КІНЕЦЬ: Виправлення для графа з однією вершиною при побудові MST ---


                if (!currentGraph.IsConnected())
                {
                    MessageBox.Show("Граф не є зв'язним. Остовне дерево не існує.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                ISpanningTreeAlgorithm selectedAlgorithm;
                string practicalComplexity = "Не визначено"; // Для відображення складності

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

                    // ***** ВАШ ПСЕВДОКОД, ПЕРЕРОБЛЕНИЙ В КОД *****
                    bool areVerticesAlmostOnSameHeight = false;
                    if (currentGraph.Vertices.Count > 1)
                    {
                        var yCoordinates = currentGraph.Vertices.Select(v => v.Y).ToList();
                        double minY = yCoordinates.Min();
                        double maxY = yCoordinates.Max();

                        // Визначте поріг "висоти". Це значення потрібно налаштувати
                        // залежно від діапазону Y-координат ваших вершин.
                        // Наприклад, якщо різниця Y-координат менше 10.0 одиниць
                        double heightThreshold = 10.0;

                        if ((maxY - minY) < heightThreshold)
                        {
                            areVerticesAlmostOnSameHeight = true;
                        }
                    }

                    if (areVerticesAlmostOnSameHeight)
                    {
                        // Якщо всі вершини графу лежать практично на одній висоті
                        practicalComplexity = "O(E log* V)";
                    }
                    else
                    {
                        // Інакше
                        practicalComplexity = "O(E log E)";
                    }
                    // ***** КІНЕЦЬ ВАШОГО ПСЕВДОКОДУ *****
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
                AlgorithmComplexityTextBlock.Text = $"Практична складність: {practicalComplexity}"; // Оновлюємо TextBlock зі складністю
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
            AlgorithmComplexityTextBlock.Text = "Практична складність: -"; // Очищаємо складність при очищенні графу
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
            // AlgorithmComplexityTextBlock.Text = "Практична складність: -"; // Можна також тут очищати, якщо потрібно
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