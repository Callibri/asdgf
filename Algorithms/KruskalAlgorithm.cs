using GraphSolver.Entities;
using GraphSolver.GraphComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GraphSolver.Algorithms
{
    public class KruskalAlgorithm : ISpanningTreeAlgorithm
    {
        public (HashSet<Edge> spanningTree, double totalWeight, TimeSpan timeTaken) FindMST(Graph graph)
        {
            var stopwatch = Stopwatch.StartNew();
            var mst = new HashSet<Edge>();
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

        public string GetPracticalComplexityFormula(Graph graph)
        {
            int V = graph.Vertices.Count;
            int E = graph.Edges.Count;

            if (V == 0) return "O(0) - порожній граф";

            bool areVerticesAlmostOnSameHeight = false;
            if (V > 1)
            {
                var yCoordinates = graph.Vertices.Select(v => v.Y).ToList();
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
                return $"O(E log* V) (E={E}, V={V}) - вершини майже на одній висоті";
            }
            else
            {
                return $"O(E log E) (E={E}, V={V})";
            }
        }


        public string GetTheoreticalComplexityFormula()
        {
            return "O(E log E)";
        }
    }
}
