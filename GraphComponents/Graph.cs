using GraphSolver.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphSolver.GraphComponents
{
    public class Graph
    {
        public List<Vertex> Vertices { get; private set; }
        public HashSet<Edge> Edges { get; private set; }
        private Dictionary<Vertex, List<Edge>> AdjacencyList { get; set; }

        public Graph()
        {
            Vertices = new List<Vertex>();
            Edges = new HashSet<Edge>();
            AdjacencyList = new Dictionary<Vertex, List<Edge>>();
        }

        public void AddVertex(Vertex vertex)
        {
            if (!Vertices.Any(v => v.Id == vertex.Id))
            {
                Vertices.Add(vertex);
                AdjacencyList[vertex] = new List<Edge>();
            }
        }

        public void RemoveVertex(Vertex vertexToRemove)
        {
            if (vertexToRemove == null) return;
            Vertices.Remove(vertexToRemove);

            var edgesToRemove = Edges.Where(e => e.Source.Equals(vertexToRemove) || e.Destination.Equals(vertexToRemove)).ToList();
            foreach (var edge in edgesToRemove)
            {
                Edges.Remove(edge);
                if (AdjacencyList.ContainsKey(edge.Source))
                    AdjacencyList[edge.Source].Remove(edge);
                if (AdjacencyList.ContainsKey(edge.Destination))
                    AdjacencyList[edge.Destination].Remove(edge);
            }
            AdjacencyList.Remove(vertexToRemove);
        }

        public void AddEdge(Vertex source, Vertex destination, double weight)
        {
            if (!Vertices.Contains(source) || !Vertices.Contains(destination))
            {
                throw new ArgumentException("Source or destination vertex not found in graph.");
            }

            if (Edges.Contains(new Edge(source, destination, 0)))
            {
                throw new ArgumentException($"Ребро між вершинами {source.Id} та {destination.Id} вже існує.");
            }

            var newEdge = new Edge(source, destination, weight);
            Edges.Add(newEdge);
            AdjacencyList[source].Add(newEdge);
            AdjacencyList[destination].Add(newEdge);
        }

        public bool RemoveEdge(Edge edgeToRemove)
        {
            if (edgeToRemove == null) return false;

            bool removedFromEdges = Edges.Remove(edgeToRemove);
            if (removedFromEdges)
            {
                if (AdjacencyList.ContainsKey(edgeToRemove.Source))
                    AdjacencyList[edgeToRemove.Source].Remove(edgeToRemove);
                if (AdjacencyList.ContainsKey(edgeToRemove.Destination))
                    AdjacencyList[edgeToRemove.Destination].Remove(edgeToRemove);
            }
            return removedFromEdges;
        }

        public void Clear()
        {
            Vertices.Clear();
            Edges.Clear();
            AdjacencyList.Clear(); 
        }

        public void ClearEdges()
        {
            Edges.Clear();
            foreach (var vertex in Vertices)
            {
                AdjacencyList[vertex].Clear();
            }
        }

        public bool IsConnected()
        {
            if (Vertices.Count == 0) return true;
            if (Vertices.Count == 1) return true;

            HashSet<Vertex> visited = new HashSet<Vertex>();
            Queue<Vertex> queue = new Queue<Vertex>();

            Vertex startVertex = Vertices.First();
            queue.Enqueue(startVertex);
            visited.Add(startVertex);

            while (queue.Count > 0)
            {
                Vertex current = queue.Dequeue();


                if (AdjacencyList.TryGetValue(current, out List<Edge>? currentEdges))
                {
                    foreach (var edge in currentEdges)
                    {
                        Vertex neighbor = edge.Source.Equals(current) ? edge.Destination : edge.Source;
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
            return visited.Count == Vertices.Count;
        }
    }
}
