using GraphSolver.Entities;
using GraphSolver.GraphComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GraphSolver.Algorithms
{
    // Успадкування IComplexityAnalyzable вже відбувається через ISpanningTreeAlgorithm,
    // тому явне додавання тут не потрібне, але не зашкодить.
    public class BoruvkaAlgorithm : ISpanningTreeAlgorithm, IComplexityAnalyzable
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

            var ds = new DisjointSet(graph.Vertices);
            int numComponents = graph.Vertices.Count;

            while (numComponents > 1)
            {
                var cheapestEdge = new Dictionary<Vertex, Edge>();

                foreach (var edge in graph.Edges)
                {
                    var root1 = ds.Find(edge.Source);
                    var root2 = ds.Find(edge.Destination);

                    if (!root1.Equals(root2))
                    {
                        if (!cheapestEdge.ContainsKey(root1) || edge.Weight < cheapestEdge[root1].Weight)
                        {
                            cheapestEdge[root1] = edge;
                        }
                        if (!cheapestEdge.ContainsKey(root2) || edge.Weight < cheapestEdge[root2].Weight)
                        {
                            cheapestEdge[root2] = edge;
                        }
                    }
                }

                if (cheapestEdge.Count == 0 && numComponents > 1)
                {
                    break;
                }

                foreach (var entry in cheapestEdge.ToList())
                {
                    var edge = entry.Value;
                    var root1 = ds.Find(edge.Source);
                    var root2 = ds.Find(edge.Destination);

                    if (!root1.Equals(root2))
                    {
                        mst.Add(edge);
                        totalWeight += edge.Weight;
                        ds.Union(edge.Source, edge.Destination);
                        numComponents--;
                    }
                }
            }

            stopwatch.Stop();
            return (mst, totalWeight, stopwatch.Elapsed);
        }

        /// <summary>
        /// Повертає формулу практичної складності алгоритму Борувки для заданого графа.
        /// </summary>
        /// <param name="graph">Граф, для якого обчислюється складність.</param>
        /// <returns>Рядок, що містить формулу складності (наприклад, "O(E log V)").</returns>
        public string GetPracticalComplexityFormula(Graph graph)
        {
            int V = graph.Vertices.Count; // Кількість вершин
            int E = graph.Edges.Count;    // Кількість ребер

            if (V == 0) return "O(0) - порожній граф";

            // Теоретична складність алгоритму Борувки: O(E log V) або O(E * log(log V))
            // Залежить від ефективності реалізації DSU.
            // З використанням DSU з оптимізаціями (стиснення шляхів та об'єднання за рангом/розміром)
            // складність становить O(E * α(V)), де α - обернена функція Акермана, що є дуже повільно зростаючою функцією
            // і практично дорівнює константі. Тому часто спрощується до O(E log V) або навіть O(E) для практичних цілей.
            // Ми використаємо O(E log V) як загальноприйняту "практичну" складність для цієї реалізації.
            return $"O(E log V) (E={E}, V={V})";
        }
    }
}