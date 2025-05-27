using GraphSolver.Entities;
using GraphSolver.GraphComponents;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GraphSolver.Algorithms
{
    public class PrimAlgorithm : ISpanningTreeAlgorithm // Вже успадковує IComplexityAnalyzable через ISpanningTreeAlgorithm
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

            var visited = new HashSet<Vertex>();

            var priorityQueue = new SortedSet<Edge>(Comparer<Edge>.Create((a, b) =>
            {
                int weightComparison = a.Weight.CompareTo(b.Weight);
                if (weightComparison != 0) return weightComparison;

                int sourceComparison = a.Source.Id.CompareTo(b.Source.Id);
                if (sourceComparison != 0) return sourceComparison;

                return a.Destination.Id.CompareTo(b.Destination.Id);
            }));

            var startVertex = graph.Vertices[0];
            visited.Add(startVertex);

            foreach (var edge in graph.Edges.Where(e => e.Source.Equals(startVertex) || e.Destination.Equals(startVertex)))
                priorityQueue.Add(edge);

            while (priorityQueue.Count > 0 && visited.Count < graph.Vertices.Count)
            {
                var cheapestEdge = priorityQueue.Min;

                if (visited.Contains(cheapestEdge.Source) && visited.Contains(cheapestEdge.Destination))
                {
                    priorityQueue.Remove(cheapestEdge); 
                    continue; 
                }


                if (priorityQueue.Count == 0 && visited.Count < graph.Vertices.Count)
                {
                    break;
                }

                priorityQueue.Remove(cheapestEdge); 

                Vertex unvisitedVertex = visited.Contains(cheapestEdge.Source) ? cheapestEdge.Destination : cheapestEdge.Source;

                if (!visited.Contains(unvisitedVertex))
                {
                    mst.Add(cheapestEdge);
                    totalWeight += cheapestEdge.Weight;
                    visited.Add(unvisitedVertex);

                    foreach (var newEdge in graph.Edges.Where(e =>
                        (e.Source.Equals(unvisitedVertex) || e.Destination.Equals(unvisitedVertex)) &&
                        (!visited.Contains(e.Source) || !visited.Contains(e.Destination)))) // Тільки якщо одна з вершин ще не відвідана
                    {
                        priorityQueue.Add(newEdge);
                    }
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

            if (V < 10)
            {
                return "O(V²)";
            }
            else
            {
                return "O((V + E) log V)";
            }
        }
    }
}