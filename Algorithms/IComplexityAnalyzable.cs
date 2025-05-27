// File: IComplexityAnalyzable.cs
using GraphSolver.GraphComponents;

namespace GraphSolver.Algorithms
{
    public interface IComplexityAnalyzable
    {
        /// <summary>
        /// ������� ������� ��������� ��������� ��������� ��� �������� �����.
        /// </summary>
        /// <param name="graph">����, ��� ����� ������������ ���������.</param>
        /// <returns>�����, �� ������ ������� ��������� (���������, "O(V + E log V)").</returns>
        string GetPracticalComplexityFormula(Graph graph);
    }
}