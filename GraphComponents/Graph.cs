using GraphSolver.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GraphSolver.GraphComponents
{
    public class Graph
    {
        public List<Vertex> Vertices { get; private set; }
        public List<Edge> Edges { get; private set; }

        public Graph()
        {
            Vertices = new List<Vertex>();
            Edges = new List<Edge>();
        }

        public void AddVertex(Vertex vertex)
        {
            if (!Vertices.Any(v => v.Id == vertex.Id))
            {
                Vertices.Add(vertex);
            }
        }

        public void RemoveVertex(Vertex vertexToRemove)
        {
            if (vertexToRemove == null) return;
            Vertices.Remove(vertexToRemove);
            Edges.RemoveAll(e => e.Source.Equals(vertexToRemove) || e.Destination.Equals(vertexToRemove));
        }

        public void AddEdge(Vertex source, Vertex destination, double weight)
        {
            if (!Vertices.Contains(source) || !Vertices.Contains(destination))
            {
                throw new ArgumentException("Source or destination vertex not found in graph.");
            }
            if (Edges.Any(e => (e.Source.Equals(source) && e.Destination.Equals(destination)) ||
                               (e.Source.Equals(destination) && e.Destination.Equals(source))))
            {
                throw new ArgumentException($"Ребро між вершинами {source.Id} та {destination.Id} вже існує.");
            }
            Edges.Add(new Edge(source, destination, weight));
        }

        public bool RemoveEdge(Edge edgeToRemove)
        {
            if (edgeToRemove == null) return false;
            return Edges.Remove(edgeToRemove);
        }

        public void Clear()
        {
            Vertices.Clear();
            Edges.Clear();
        }

        public void ClearEdges()
        {
            Edges.Clear();
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
                var neighbors = Edges
                    .Where(e => e.Source.Equals(current) || e.Destination.Equals(current))
                    .Select(e => e.Source.Equals(current) ? e.Destination : e.Source) 
                    .ToList();

                foreach (var neighbor in neighbors)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
            return visited.Count == Vertices.Count;
        }
    }
}