using Chess;

namespace PawnCube
{
    public interface IBooleanEvaluator
    {
        /// <summary>
        /// Individual classes subclass the abstract class to implement their custom way of checking if a board is good or not.
        /// </summary>
        public IEnumerable<BooleanExample> RunOne(ChessBoard board);

        /// <summary>
        /// This is for calling into the subclass's parent class which generically helps it iterate over all boards, up to the query specified here.
        /// i.e. sometimes looking for just one example, and sometimes looking through all the games you have.
        /// </summary>
        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards);
        public string Name { get; }
    }
}