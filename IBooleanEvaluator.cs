using Chess;

namespace PawnCube
{
    public interface IBooleanEvaluator
    {
        public BooleanEvaluationResult Evaluate(List<ChessBoard> boards);
        public string Name { get; }
    }
}