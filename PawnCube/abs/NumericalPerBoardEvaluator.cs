using Chess;

namespace PawnCube
{
    /// <summary>
    /// a class which just feeds in the boards and the thing returns the differential result?
    /// </summary>
    public abstract class NumericalPerBoardEvaluator : INumericalEvaluator
    {
        public abstract string Name { get; }
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var results = new List<int>() { };
            foreach (var board in boards)
            {
                results.Add(this.InnerEvaluate(board));
            }
            var raw = this.Aggregate(results, out var det);

            //var det = $"Total of {results.Where(el => el != 0).Count()} nonzero returns of {boards.Count()} games resulting in manifold result: {raw}";
            return new NumericalEvaluationResult(raw, det);
        }

        public abstract int InnerEvaluate(ChessBoard board);
        public abstract int Aggregate(IEnumerable<int> results, out string det);
    }
}
