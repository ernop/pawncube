using Chess;

namespace PawnCube
{
    public interface INumericalEvaluator
    {
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards);
        public string Name { get; }
    }
}