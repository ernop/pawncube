using Chess;

using PawnCube;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace PawnCube
{
    public class TenPercentForeEachWinEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TenPercentForeEachWinEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var winCount = 0;
            foreach (var board in boards)
            {
                if (board.EndGame == null)
                {
                    throw new Exception("null endgame although game ended?");
                }
                if (board.EndGame.WonSide != null)
                {
                    winCount++;
                }
            }
            return new NumericalEvaluationResult(winCount * 10, $"Total: {winCount} wins.", new List<NumericalExample>());
        }
    }

    public class LongestWinstreakByPlayerTwentyPercentEachEvaluator : INumericalEvaluator
    {
        public string Name => nameof(LongestWinstreakByPlayerTwentyPercentEachEvaluator);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var runningTotal = 0;
            var maxSeen = 0;
            var lastWinner = "";

            foreach (var board in boards)
            {
                if (board.EndGame == null)
                {
                    throw new Exception("null endgame although game ended?");
                }
                if (board.EndGame.WonSide == null)
                {
                    runningTotal = 0;
                }
                else
                {
                    var winner = "";
                    if (board.EndGame.WonSide == PieceColor.White)
                    {
                        winner = board.Headers["White"];
                    }
                    else if (board.EndGame.WonSide == PieceColor.Black)
                    {
                        winner = board.Headers["Black"];
                    }
                    else
                    {
                        throw new Exception("no winner?");
                    }

                    if (lastWinner == winner)
                    {
                        runningTotal += 1;
                    }
                    else
                    {
                        runningTotal = 1;
                    }
                    maxSeen = Math.Max(maxSeen, runningTotal);
                    lastWinner = winner;
                }
            }

            var raw = maxSeen * 20;
            var det = $"Total longest streak was {maxSeen}.";
            var res = new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
            return res;
        }
    }

    public class TenPercentForeEachBlackWinEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TenPercentForeEachBlackWinEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var bwins = boards.Where(board => board.EndGame.EndgameType == EndgameType.Timeout || board.EndGame.EndgameType == EndgameType.Checkmate || board.EndGame.EndgameType == EndgameType.Resigned)
                .Where(el => el.EndGame.WonSide == PieceColor.Black);
            var raw = bwins.Count() * 10;
            var det = $"Total: {bwins.Count()} b wins.";
            return new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
        }
    }

    /// <summary>
    /// This one is a bit tricky since it introduces a new requirement - that the games be evaluated in the right order.
    /// The other new thing about it is that it uses state within the aggregator.
    /// </summary>
    public class BlackPlayerEverAheadOnPointsInGameSeriesEvaluator : INumericalEvaluator
    {
        public string Name => nameof(BlackPlayerEverAheadOnPointsInGameSeriesEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var bRunningTotal = 0.0;
            var allDrawTotal = 0.0;
            foreach (var board in boards)
            {
                allDrawTotal += 0.5;
                if (board.EndGame != null && board.EndGame.WonSide == PieceColor.Black)
                {
                    bRunningTotal += 1;
                }
                else if (board.EndGame != null && board.EndGame.WonSide == null)
                {
                    bRunningTotal += 0.5;
                }
                else
                {
                    var a = 4;
                }
                if (bRunningTotal > allDrawTotal)
                {
                    var exa = new NumericalExample(board, "", board.ExecutedMoves.Count()-1, 100);
                    return new NumericalEvaluationResult(100, "Black was ahead at some point.", new List<NumericalExample>() { exa });
                }
            }
            return new NumericalEvaluationResult(0, "Black was never ahead", null);
        }
    }

    public class Black40VsWhiteMinus10WinEvalutor : INumericalEvaluator
    {
        public string Name => nameof(Black40VsWhiteMinus10WinEvalutor);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var blackWins = 0;
            var whitewins = 0;
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                var e = board.EndGame;
                if (e.EndgameType == EndgameType.Resigned)
                {
                    if (e.WonSide == PieceColor.White)
                    {
                        whitewins++;
                    }
                    if (e.WonSide == PieceColor.Black)
                    {
                        blackWins++;
                    }
                }
            }
            var raw = 40 * blackWins + -10 * whitewins;
            var det = $"Total of {blackWins} black wins, {whitewins} white wins";
            var res = new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
            return res;
        }
    }

    public class OnePercentPerUnmovedPieceEvaluator : INumericalEvaluator
    {
        //1% for every piece on its starting square in the final position of every game
        public string Name => nameof(OnePercentPerUnmovedPieceEvaluator);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var unmovedCount = 0;
            var mostUnmovedCount = 0;
            ChessBoard mostUnmovedGame = null;
            NumericalExample bestExample = null;

            foreach (var board in boards)
            {
                //a piece must both not have moved and not be killed.
                board.Last();
                var survivingPieceIds = Statics.GetAllPieces(board).Where(el => el.Id > 0 && el.Id < 32).Select(el=>el.Id);
                var movedIds = board.ExecutedMoves.Select(el => el.Piece.Id).Distinct();
                var deadIds = Statics.GetAllCaptures(board).Select(el => el.Id).ToList();
                var guys = survivingPieceIds.Where(el => !deadIds.Contains(el)).Where(el => !movedIds.Contains(el));
                var ct = guys.Count();
                if (ct > mostUnmovedCount)
                {
                    mostUnmovedCount = ct;
                    mostUnmovedGame = board;
                    bestExample = new NumericalExample(board, "", board.ExecutedMoves.Count()-1, ct);
                }
                unmovedCount += guys.Count();
            }
            var raw = 1 * unmovedCount;
            var det = $"In the game with the most unmoved pieces, there were {mostUnmovedCount}, and overall there were {unmovedCount} unmoved pieces;";
            var res = new NumericalEvaluationResult(raw, det, new List<NumericalExample>() { bestExample });
            return res;
        }
    }
}
