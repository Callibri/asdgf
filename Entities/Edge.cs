using System;

namespace GraphSolver.Entities
{
    public class Edge
    {
        public Vertex Source { get; set; }
        public Vertex Destination { get; set; }
        public double Weight { get; set; }
        public bool IsInSpanningTree { get; set; }

        public Edge(Vertex source, Vertex destination, double weight)
        {
            Source = source;
            Destination = destination;
            Weight = weight;
            IsInSpanningTree = false;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Edge other)
            {
                return (Source.Equals(other.Source) && Destination.Equals(other.Destination)) ||
                       (Source.Equals(other.Destination) && Destination.Equals(other.Source));
            }
            return false;
        }

        public override int GetHashCode()
        {

            int id1 = Math.Min(Source.Id, Destination.Id);
            int id2 = Math.Max(Source.Id, Destination.Id);
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + id1.GetHashCode();
                hash = hash * 23 + id2.GetHashCode();
                return hash;
            }
        }
    }
}