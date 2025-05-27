// File: Vertex.cs
namespace GraphSolver.Entities
{
    public class Vertex
    {
        public int Id { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public Vertex(int id, double x, double y)
        {
            Id = id;
            X = x;
            Y = y;
        }

        public override bool Equals(object? obj)
        {
            if (obj is Vertex other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}