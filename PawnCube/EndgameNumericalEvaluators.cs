using Chess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PawnCube
{
    public class TenPercentForeEachWinEvaluator : NumericalPerBoardEvaluator
    {
        public override string Name => nameof(TenPercentForeEachWinEvaluator);

        public override int Aggregate(IEnumerable<int> results, out string det)
        {
            det = $"Total: {results.Sum()} wins.";
            return results.Sum() * 10;
        }

        public override int InnerEvaluate(ChessBoard board)
        {
            if (board.EndGame == null)
            {
                throw new Exception("null endgame although game ended?");
            }
            if (board.EndGame.EndgameType == EndgameType.Timeout
                || board.EndGame.EndgameType == EndgameType.Checkmate
                 || board.EndGame.EndgameType == EndgameType.Resigned)
            {
                return 1;
            }
            return 0;
        }
    }

    public class TenPercentForeEachBlackWinEvaluator : NumericalPerBoardEvaluator
    {
        public override string Name => nameof(TenPercentForeEachBlackWinEvaluator);

        public override int InnerEvaluate(ChessBoard board)
        {
            if (board.EndGame.EndgameType == EndgameType.Timeout
                || board.EndGame.EndgameType == EndgameType.Checkmate
                 || board.EndGame.EndgameType == EndgameType.Resigned)
            {
                if (board.ExecutedMoves.Last().Piece.Color == PieceColor.Black)
                {
                    return 1;
                }
            }
            return 0;
        }

        public override int Aggregate(IEnumerable<int> results, out string det)
        {
            var tot = results.Sum();
            det = $"Total: {tot} b wins.";
            return tot * 10;
        }
    }

    /// <summary>
    /// This one is a bit tricky since it introduces a new requirement - that the games be evaluated in the right order.
    /// The other new thing about it is that it uses state within the aggregator.
    /// </summary>
    public class BEverAheadOnPointsEvaluator : NumericalPerBoardEvaluator
    {
        public override string Name => nameof(BEverAheadOnPointsEvaluator);

        public override int Aggregate(IEnumerable<int> results, out string det)
        {
            //calculate w's raw total score, semi useful at least.
            double wRunningTotal = 0;
            var gameNumber = 1;
            foreach (var el in results)
            {
                if (el == 0)
                {
                    wRunningTotal += 0.5;
                }
                else if (el == 1)
                {
                    wRunningTotal += 1;
                }
                else if (el == -1)
                {
                    wRunningTotal += 0;
                }

                if (wRunningTotal - 0.5 * gameNumber < 0)
                {
                    det = $"B got the lead in game {gameNumber} in {results.Count()} with {gameNumber - wRunningTotal} and the final in standard notation with W positive was: {results.Sum()}.";
                    return 100;
                }
                gameNumber++;

            }
            det = $"B never got the lead in {results.Count()} games, and the final was: {results.Sum()}.";
            return 0;
        }

        public override int InnerEvaluate(ChessBoard board)
        {
            if (board.EndGame.WonSide == PieceColor.Black)
            {
                return -1;

            }
            if (board.EndGame.WonSide == PieceColor.White)
            {
                return 1;

            }
            return 0;
        }
    }

    public class OnePercentPerUnmovedPieceEvaluator : INumericalEvaluator
    {
        //1% for every piece on its starting square in the final position of every game
        public string Name => nameof(OnePercentPerUnmovedPieceEvaluator);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var unmovedCount = 0;
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                var unmoved = new Dictionary<Position, bool>();
                for (short xx = 0; xx < 8; xx++)
                {
                    foreach (short yy in new List<short>() { 0, 1, 6, 7 })
                    {
                        var pos = new Position(xx, yy);
                        unmoved[pos] = true;
                    }
                }

                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    unmoved[move.OriginalPosition] = false;
                    unmoved[move.NewPosition] = false;
                    board.Next();
                }

                var thisUnmovedCount = 0;
                foreach (var k in unmoved.Keys)
                {
                    if (unmoved[k])
                    {
                        thisUnmovedCount++;
                    }
                }
                unmovedCount += thisUnmovedCount;
            }
            var raw = 1 * unmovedCount;
            var det = $"Total of {unmovedCount} unmoved pieces.";
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }
}
