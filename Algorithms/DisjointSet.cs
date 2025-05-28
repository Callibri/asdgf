using GraphSolver.Entities;
using System.Collections.Generic;
using System.Linq;

namespace GraphSolver.Algorithms
{
    public class DisjointSet
    {
        private readonly Dictionary<Vertex, Vertex> parent = new Dictionary<Vertex, Vertex>();
        private readonly Dictionary<Vertex, int> rank = new Dictionary<Vertex, int>();

        public DisjointSet(IEnumerable<Vertex> vertices)
        {
            foreach (var v in vertices)
            {
                parent[v] = v;
                rank[v] = 0;
            }
        }

        public Vertex Find(Vertex v)
        {
            if (!parent[v].Equals(v))
                parent[v] = Find(parent[v]);
            return parent[v];
        }

        public void Union(Vertex x, Vertex y)
        {
            var xRoot = Find(x);
            var yRoot = Find(y);

            if (xRoot.Equals(yRoot)) return;

            if (rank[xRoot] < rank[yRoot])
                parent[xRoot] = yRoot;
            else if (rank[xRoot] > rank[yRoot])
                parent[yRoot] = xRoot;
            else
            {
                parent[yRoot] = xRoot;
                rank[xRoot]++;
            }
        }
    }
}