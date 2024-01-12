using Chess;

namespace PawnCube
{
    public interface IBooleanEvaluator
    {
        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards);
        public string Name { get; }
    }
}