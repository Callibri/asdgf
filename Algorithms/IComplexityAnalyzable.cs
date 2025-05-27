// File: IComplexityAnalyzable.cs
using GraphSolver.GraphComponents;

namespace GraphSolver.Algorithms
{
    public interface IComplexityAnalyzable
    {
        /// <summary>
        /// Повертає формулу практичної складності алгоритму для заданого графа.
        /// </summary>
        /// <param name="graph">Граф, для якого обчислюється складність.</param>
        /// <returns>Рядок, що містить формулу складності (наприклад, "O(V + E log V)").</returns>
        string GetPracticalComplexityFormula(Graph graph);
    }
}