// File: KruskalAlgorithm.cs
using GraphSolver.Entities;
using GraphSolver.GraphComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GraphSolver.Algorithms
{
    public class KruskalAlgorithm : ISpanningTreeAlgorithm // Вже успадковує IComplexityAnalyzable через ISpanningTreeAlgorithm
    {
        public (List<Edge> spanningTree, double totalWeight, TimeSpan timeTaken) FindMST(Graph graph)
        {
            var stopwatch = Stopwatch.StartNew();
            var mst = new List<Edge>();
            double totalWeight = 0;

            if (graph.Vertices.Count == 0)
            {
                stopwatch.Stop();
                return (mst, totalWeight, stopwatch.Elapsed);
            }

            var sortedEdges = graph.Edges.OrderBy(e => e.Weight).ToList();
            var ds = new DisjointSet(graph.Vertices);

            foreach (var edge in sortedEdges)
            {
                var root1 = ds.Find(edge.Source);
                var root2 = ds.Find(edge.Destination);

                if (!root1.Equals(root2))
                {
                    mst.Add(edge);
                    totalWeight += edge.Weight;
                    ds.Union(edge.Source, edge.Destination);

                    if (mst.Count == graph.Vertices.Count - 1)
                        break;
                }
            }

            stopwatch.Stop();
            return (mst, totalWeight, stopwatch.Elapsed);
        }

        // Реалізація методу GetPracticalComplexityFormula для алгоритму Крускала
        public string GetPracticalComplexityFormula(Graph graph)
        {
            int V = graph.Vertices.Count; // Кількість вершин
            int E = graph.Edges.Count;    // Кількість ребер

            if (V == 0) return "O(0) - порожній граф";

            // Часова складність Крускала: O(E log E) або O(E log V)
            // Через сортування ребер (E log E) та операції DisjointSet (які майже константні, O(α(V)) - зворотна функція Аккермана)
            // Припускаючи, що E <= V^2, log E <= log V^2 = 2 log V. Тому E log E є домінуючим.
            // У більшості випадків E log E є більш точним, якщо граф щільний.
            // Якщо граф розріджений (E близько V), то E log V.
            // Оскільки SortedSet використовується для сортування, складність сортування ребер буде O(E log E).
            return $"O(E log E) або O(E log V), де V = {V}, E = {E}";
        }

        // ДОДАНО: Реалізація методу GetTheoreticalComplexityFormula для алгоритму Крускала
        public string GetTheoreticalComplexityFormula()
        {
            // Теоретична складність алгоритму Крускала, де V - кількість вершин, E - кількість ребер.
            // Основний внесок робить сортування ребер, що займає O(E log E).
            // Операції Find та Union у Disjoint Set Union (DSU) мають майже константну амортизовану складність O(α(V)),
            // де α(V) - обернена функція Аккермана, яка росте надзвичайно повільно.
            // Тому загальна складність дорівнює O(E log E) або, оскільки E <= V^2, O(E log V).
            return "O(E log E) або O(E log V)";
        }
    }
}