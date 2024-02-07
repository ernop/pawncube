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

        /// <summary>
        /// Convert the board into a numerical example or null if it doesn't match. That example's .Value will be used later on (possibly)
        /// within the aggregate function which will be fed all the examples.
        /// </summary>
        public abstract NumericalExample InnerEvaluate(ChessBoard board);
        public abstract NumericalEvaluationResult Aggregate(IEnumerable<NumericalExample> examples);
    }
}
