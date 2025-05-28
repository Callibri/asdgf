using GraphSolver.Entities;
using GraphSolver.GraphComponents;
using System;
using System.Collections.Generic;

namespace GraphSolver.Algorithms
{
    public interface ISpanningTreeAlgorithm : IComplexityAnalyzable 
    {
        (List<Edge> spanningTree, double totalWeight, TimeSpan timeTaken) FindMST(Graph graph);
    }
}