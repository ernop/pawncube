using Chess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static PawnCube.Statics;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PawnCube
{

    public class Pawn10VsKnightMinus10FirstMoveEvaluator : INumericalEvaluator
    {
        public string Name => nameof(Pawn10VsKnightMinus10FirstMoveEvaluator);
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var knights = 0;
            var pawns = 0;
            foreach (var board in boards)
            {
                var m = board.ExecutedMoves[0];
                if (m.Piece.Type == PieceType.Pawn)
                {
                    pawns++;
                }
                else if (m.Piece.Type == PieceType.Knight)
                {
                    knights++;
                }
            }
            var raw = 10 * pawns + -10 * knights;
            var det = $"Total of {pawns} pawn first moves, {knights} knight first moves, so result is: {raw}";
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }

    public class PawnMoveTypesFiveForTwoJumpMinusFiveForOtherFirstMove : INumericalEvaluator
    {
        //1% for every piece on its starting square in the final position of every game
        public string Name => nameof(OnePercentPerUnmovedPieceEvaluator);
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var twoSpaceJumpCount = 0;
            var oneSpaceJumpCount = 0;
            var firstMoveCaptures = 0;

            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count(); ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (move.Piece.Type == PieceType.Pawn)
                    {
                        //pawn is on his starting square
                        if ((move.Piece.Color == PieceColor.White && move.OriginalPosition.Y == 1) ||
                            (move.Piece.Color == PieceColor.Black && move.OriginalPosition.Y == 6))
                        {
                            var gap = Math.Abs(move.OriginalPosition.Y - move.NewPosition.Y);
                            if (gap == 2)
                            {
                                twoSpaceJumpCount++;
                            }
                            else if (gap == 1)
                            {
                                if (move.CapturedPiece == null)
                                {
                                    oneSpaceJumpCount++;
                                }
                                else
                                {
                                    firstMoveCaptures++;
                                }
                            }
                            else
                            {
                                var a = 4;
                            }
                        }
                    }
                }
            }

            var raw = 5 * twoSpaceJumpCount * -5 * (oneSpaceJumpCount + firstMoveCaptures);
            var det = $"Total of {twoSpaceJumpCount} two space jumps, {oneSpaceJumpCount} one space jumps, and {firstMoveCaptures} first move captures.";
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }

    public class OnePercentPerUnmovedPieceEvaluator : INumericalEvaluator
    {
        //1% for every piece on its starting square in the final position of every game
        public string Name => nameof(OnePercentPerUnmovedPieceEvaluator);
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var unmovedCount = 0;
            foreach (var board in boards)
            {
                var unmoved = new Dictionary<Position, bool>();
                for (short xx = 0; xx < 8; xx++)
                {
                    foreach (short yy in new List<short>() { 0, 1, 6, 7 })
                    {
                        var pos = new Position(xx, yy);
                        unmoved[pos] = true;
                    }
                }

                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count(); ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    unmoved[move.OriginalPosition] = false;
                    unmoved[move.NewPosition] = false;
                    testBoard.Move(move);
                }

                var thisUnmovedCount = 0;
                foreach (var k in unmoved.Keys)
                {
                    if (unmoved[k])
                    {
                        thisUnmovedCount++;
                    }
                }
                //Console.WriteLine($"game with {thisUnmovedCount} unmoved\r\n{testBoard.ToAscii()}");
                unmovedCount += thisUnmovedCount;
            }
            var raw = 1 * unmovedCount;
            var det = $"Total of {unmovedCount} unmoved pieces.";
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }

    public class TenPercentPerResignationEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TenPercentPerResignationEvaluator);
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var resignations = 0;
            foreach (var board in boards)
            {
                var e = board.EndGame;
                if (e.EndgameType == EndgameType.Resigned)
                {
                    resignations++;
                }
            }
            var raw = 10 * resignations;
            var det = $"Total of {resignations} resignations";
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }

    public class TwentyPercentForDecisiveMinusTenForOtherwiseEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TwentyPercentForDecisiveMinusTenForOtherwiseEvaluator);
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var indecisiveCount = 0;
            var decisiveCount = 0;
            foreach (var board in boards)
            {
                var e = board.EndGame;
                //Checkmate,
                //Resigned,
                //Timeout,
                //Stalemate,aa
                //DrawDeclared,aa
                //InsufficientMaterial,aa
                //FiftyMoveRule,aa
                //Repetition,aa
                if (e.EndgameType == EndgameType.DrawDeclared
                     || e.EndgameType == EndgameType.Stalemate
                     || e.EndgameType == EndgameType.InsufficientMaterial
                     || e.EndgameType == EndgameType.Repetition
                     || e.EndgameType == EndgameType.FiftyMoveRule)
                {
                    indecisiveCount++;
                }
                else
                {
                    decisiveCount++;
                }
            }
            var raw = 20 * decisiveCount + -10 * indecisiveCount;
            var det = $"Decisive: {decisiveCount}, indecisive: {indecisiveCount}";
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }

    public class TenPercentForEachDrawEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TenPercentForEachDrawEvaluator);
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var drawcount = 0;
            foreach (var board in boards)
            {
                var e = board.EndGame;
                if (e.EndgameType == EndgameType.DrawDeclared || e.EndgameType == EndgameType.Stalemate
                     || e.EndgameType == EndgameType.InsufficientMaterial
                      || e.EndgameType == EndgameType.Repetition
                       || e.EndgameType == EndgameType.FiftyMoveRule)
                {
                    drawcount++;
                }
            }
            var raw = 10 * drawcount;
            var det = $"Total draws: {drawcount}";
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }

    public class SevenPercentForEachDrawEvaluator : INumericalEvaluator
    {
        public string Name => nameof(SevenPercentForEachDrawEvaluator);
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var drawcount = 0;
            foreach (var board in boards)
            {
                var e = board.EndGame;
                if (e.EndgameType == EndgameType.DrawDeclared || e.EndgameType == EndgameType.Stalemate
                     || e.EndgameType == EndgameType.InsufficientMaterial
                      || e.EndgameType == EndgameType.Repetition
                       || e.EndgameType == EndgameType.FiftyMoveRule)
                {
                    drawcount++;
                }
            }
            var raw = 7 * drawcount;
            var det = $"Total draws: {drawcount}";
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }



    public class KnightDirectionNumerical3PercentVerticalMinus4PercentHorizontalEvaluator : INumericalEvaluator
    {
        public string Name => nameof(KnightDirectionNumerical3PercentVerticalMinus4PercentHorizontalEvaluator);
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var horizontalCount = 0;
            var verticalCount = 0;
            var pergame = new List<string>();
            foreach (var board in boards)
            {
                var gameh = 0;
                var gamev = 0;
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    if (move.Piece.Type == PieceType.Knight)
                    {
                        var oldPos = move.OriginalPosition;
                        var newPos = move.NewPosition;
                        var absX = Math.Abs(oldPos.X - newPos.X);
                        if (absX == 1)
                        {
                            verticalCount++;
                            gamev++;
                        }
                        else if (absX == 2)
                        {
                            horizontalCount++;
                            gameh++;
                        }
                        else { throw new Exception("huh"); }
                    }
                }
                pergame.Add($"h:{gameh}v:{gamev}");
            }
            var raw = -4 * horizontalCount + 3 * verticalCount;
            //var joined = string.Join(',', pergame).Replace(",", ", ");

            var det = $"Total horizontal knight moves in games: {horizontalCount}, total vertical: {verticalCount}";
            return new NumericalEvaluationResult(raw, det);
        }
    }

    public class ShortCastleTenPercentVsLongCastleMinusFivePercentEvaluator : INumericalEvaluator
    {
        public string Name => nameof(ShortCastleTenPercentVsLongCastleMinusFivePercentEvaluator);

        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var shortCount = 0;
            var longCount = 0;
            foreach (var board in boards)
            {

                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    if (move.Parameter != null)
                    {
                        var l = move.Parameter.ShortStr;
                        if (l == "O-O")
                        {
                            shortCount++;
                        }
                        else if (l == "O-O-O")
                        {
                            longCount++;
                        }
                    }
                }
            }

            var raw = 10 * shortCount + -5 * longCount;
            var det = $"";
            return new NumericalEvaluationResult(raw, det);
        }
    }


    public class Black40VsWhiteMinus10WinEvalutor : INumericalEvaluator
    {
        public string Name => nameof(Black40VsWhiteMinus10WinEvalutor);
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var blackWins = 0;
            var whitewins = 0;
            foreach (var board in boards)
            {
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
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }
}
