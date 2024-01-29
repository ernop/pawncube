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
            var results = new List<NumericalExample>();
            foreach (var board in boards)
            {
                var exa = InnerEvaluate(board);
                results.Add(exa);
            }
            return Aggregate(results);
        }

        public abstract NumericalExample InnerEvaluate(ChessBoard board);
        public abstract NumericalEvaluationResult Aggregate(IEnumerable<NumericalExample> examples);
    }
}
