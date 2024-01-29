using Chess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PawnCube
{
    public class FullComplimentOfMinorPiecesSurvivesEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(FullComplimentOfMinorPiecesSurvivesEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();
            board.Last();
            var originalKnights = 0;
            var originalBishops = 0;

            for (short xx = 0; xx < 8; xx++)
            {
                for (short yy = 0; yy < 8; yy++)
                {
                    var p = board[new Position(xx, yy)];
                    if (p == null)
                    {
                        continue;
                    }
                    if (p.Type == PieceType.Knight)
                    {
                        if (p.Id > 0)
                        {
                            originalKnights++;
                        }
                        else
                        {
                        }

                    }
                    if (p.Type == PieceType.Bishop)
                    {
                        if (p.Id > 0)
                        {
                            originalBishops++;
                        }
                        else
                        {
                        }
                    }
                }
            }

            if (originalBishops == 4 && originalKnights == 4)
            {
                yield return new BooleanExample(board, "All minor pieces survived, wow.", board.ExecutedMoves.Count - 1);
            }
        }
    }

    public class MoreKnightsSurviveThanBishopsEvaluator : INumericalEvaluator
    {
        public string Name => nameof(MoreKnightsSurviveThanBishopsEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var originalKnights = 0;
            var originalBishops = 0;
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                board.Last();

                for (short xx = 0; xx < 8; xx++)
                {
                    for (short yy = 0; yy < 8; yy++)
                    {
                        var p = board[new Position(xx, yy)];
                        if (p == null)
                        {
                            continue;
                        }
                        if (p.Type == PieceType.Knight)
                        {
                            if (p.Id > 0)
                            {
                                originalKnights++;
                            }
                        }
                        if (p.Type == PieceType.Bishop)
                        {
                            if (p.Id > 0)
                            {
                                originalBishops++;
                            }
                        }
                    }
                }
            }

            var det = $"Total surviving knights;{originalKnights}, total surviving bishops: {originalBishops}";
            var raw = (originalKnights - originalBishops > 0) ? 100 : 0;
            return new NumericalEvaluationResult(raw, det, null);
        }
    }

}
