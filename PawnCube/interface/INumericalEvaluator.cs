using Chess;

namespace PawnCube
{
    public interface INumericalEvaluator
    {
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards);
        public string Name { get; }
    }
}