using Chess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static PawnCube.Statics;

namespace PawnCube
{

    public class AnyPawnPromotionEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(AnyPawnPromotionEvaluator);

        public BooleanEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            foreach (var board in boards)
            {
                if (board.ExecutedMoves.Count == 0) { continue; }
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    if (move.Parameter != null)
                    {
                        var l = move.Parameter.ShortStr;
                        if (l == "=" || l == "=q" || l == "=r" || l == "=b" || l == "=n")
                        {
                            var det = $"Promotion: move {Statics.MakeNormalMoveNumberDescriptor(ii)} of {Statics.DescribeChessBoard(board)}";
                            return new BooleanEvaluationResult(true, det);
                        }
                    }                    
                }

            }
            return new BooleanEvaluationResult(false, "");
        }
    }

    public class TwoPawnPromotionsInOneGameEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(TwoPawnPromotionsInOneGameEvaluator);

        public BooleanEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            foreach (var board in boards)
            {
                var pawnPromotionsThisGame = 0;
                if (board.ExecutedMoves.Count == 0) { continue; }
                var promotionPoints = new List<int>();
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    if (move.Parameter != null)
                    {
                        var l = move.Parameter.ShortStr;
                        if (l == "=" || l == "=q" || l == "=r" || l == "=b" || l == "=n")
                        {
                            pawnPromotionsThisGame++;
                            promotionPoints.Add(ii);
                        }
                    }
                }
                if (pawnPromotionsThisGame >= 2)
                {
                    var joined = string.Join(',', promotionPoints.Select(MakeNormalMoveNumberDescriptor));
                    var det = $"Game with >=2 promotions: there were {pawnPromotionsThisGame} in {joined} of {Statics.DescribeChessBoard(board)}";
                    return new BooleanEvaluationResult(true, det);
                }
                

            }
            return new BooleanEvaluationResult(false, "");
        }
    }

    public class AnyOppositeSideCastlingGameEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(AnyOppositeSideCastlingGameEvaluator);

        public BooleanEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            foreach (var board in boards)
            {
                var hasShort = false;
                var hasLong = false;

                if (board.ExecutedMoves.Count == 0) { continue; }
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    if (move.Parameter != null)
                    {
                        var l = move.Parameter.ShortStr;
                        if (l == "O-O")
                        {
                            hasShort = true;
                        }
                        else if (l == "O-O-O")
                        {
                            hasLong = true;
                        }
                    }
                }
                if (hasShort && hasLong)
                {
                    var det = $"Opposite Side Castled in: {Statics.DescribeChessBoard(board)}";
                    Console.WriteLine(board.ToAscii());
                    return new BooleanEvaluationResult(true, det);
                }

            }
            return new BooleanEvaluationResult(false, "");
        }
    }


    internal class RookTakesAQueenEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(RookTakesAQueenEvaluator);

        public BooleanEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            foreach (var board in boards)
            {
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var m = board.ExecutedMoves[ii];
                    if (m.CapturedPiece != null && m.CapturedPiece.Type == PieceType.Queen && m.Piece.Type == PieceType.Rook)
                    {
                        var res = new BooleanEvaluationResult(true, $"move {MakeNormalMoveNumberDescriptor(ii)} of {Statics.DescribeChessBoard(board)}");
                        return res;
                    }
                }
            }

            var fres = new BooleanEvaluationResult(false, "false");
            return fres;
        }

    }

    internal class PawnTakesAQueenEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(PawnTakesAQueenEvaluator);

        public BooleanEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            foreach (var board in boards)
            {
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var m = board.ExecutedMoves[ii];
                    if (m.CapturedPiece != null && m.CapturedPiece.Type == PieceType.Queen && m.Piece.Type == PieceType.Pawn)
                    {
                        var res = new BooleanEvaluationResult(true, $"move {MakeNormalMoveNumberDescriptor(ii)} of {Statics.DescribeChessBoard(board)}");
                        return res;
                    }
                }
            }

            var fres = new BooleanEvaluationResult(false, "false");
            return fres;
        }

    }

}