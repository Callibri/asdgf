using GraphSolver.GraphComponents;

namespace GraphSolver.Algorithms
{
    public interface IComplexityAnalyzable
    {
        string GetPracticalComplexityFormula(Graph graph);
    }
}