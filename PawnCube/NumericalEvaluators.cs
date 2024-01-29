using Chess;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using static PawnCube.Statics;

using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PawnCube
{

    public class Pawn10VsKnightMinus10FirstMoveEvaluator : INumericalEvaluator
    {
        public string Name => nameof(Pawn10VsKnightMinus10FirstMoveEvaluator);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
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
        public string Name => nameof(PawnMoveTypesFiveForTwoJumpMinusFiveForOtherFirstMove);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var twoSpaceJumpCount = 0;
            var oneSpaceJumpCount = 0;
            var firstMoveCaptures = 0;

            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    board.Next();
                    if (move.Piece.Type == PieceType.Pawn)
                    {
                        //pawn is on his starting square
                        if ((move.Piece.Color == PieceColor.White && move.OriginalPosition.Y == 1) ||
                            (move.Piece.Color == PieceColor.Black && move.OriginalPosition.Y == 6))
                        {
                            var gap = Math.Abs(move.OriginalPosition.Y - move.NewPosition.Y);
                            if (gap == 2)
                            {
                                Console.WriteLine(move);
                                Console.WriteLine(board.ToAscii());
                                twoSpaceJumpCount++;
                            }
                            else if (gap == 1)
                            {
                                if (move.CapturedPiece == null)
                                {
                                    //Console.WriteLine(move);
                                    //Console.WriteLine(board.ToAscii());
                                    oneSpaceJumpCount++;
                                }
                                else
                                {
                                    //Console.WriteLine(move);
                                    //Console.WriteLine(board.ToAscii());
                                    firstMoveCaptures++;
                                }
                            }
                        }
                    }
                }
            }

            var raw = 5 * twoSpaceJumpCount + -5 * oneSpaceJumpCount + -5 * firstMoveCaptures;
            var det = $"Total of {twoSpaceJumpCount} two space jumps, {oneSpaceJumpCount} one space jumps, and {firstMoveCaptures} first move captures.";
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }

    public class TenPercentPerResignationEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TenPercentPerResignationEvaluator);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var resignations = 0;
            foreach (var board in boards.Where(el => el.EndGame.EndgameType == EndgameType.Resigned))
            {
                board.GoToStartingPosition();
                resignations++;
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
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var indecisiveCount = 0;
            var decisiveCount = 0;
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
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
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var drawcount = 0;
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                var e = board.EndGame;
                if (e.EndgameType == EndgameType.DrawDeclared
                    || e.EndgameType == EndgameType.Stalemate
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
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var drawcount = 0;
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
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
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var horizontalCount = 0;
            var verticalCount = 0;
            var pergame = new List<string>();
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
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

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var shortCount = 0;
            var longCount = 0;
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
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
            var det = $"Short castles: {shortCount}, long castles: {longCount}";
            return new NumericalEvaluationResult(raw, det);
        }
    }

    public class CapturedBishopsFiveCapturedPawnsMinusOneEvaluator : INumericalEvaluator
    {
        public string Name => nameof(CapturedBishopsFiveCapturedPawnsMinusOneEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var capturedBishops = 0;
            var capturedPawns = 0;

            foreach (var board in boards)
            {
                board.Last();
                var all = new List<Piece>();
                all.AddRange(board.CapturedWhite);
                all.AddRange(board.CapturedBlack);

                foreach (var p in all)
                {
                    if (p.Type == PieceType.Bishop)
                    {
                        capturedBishops++;
                    }
                    else if (p.Type == PieceType.Pawn)
                    {
                        capturedPawns++;
                    }
                }

            }
            var raw = 5 * capturedBishops + -1 * capturedPawns;

            var det = $"Captured pawns:{capturedPawns} Captured Bishops:{capturedBishops}";
            return new NumericalEvaluationResult(raw, det);
        }
    }

    public class SurvivingPawnsWorthOneSurvivingKnightBishopRookWorthNegativeTwoEvaluator : NumericalPerBoardEvaluator
    {
        public override string Name => nameof(SurvivingPawnsWorthOneSurvivingKnightBishopRookWorthNegativeTwoEvaluator);

        public override int Aggregate(IEnumerable<int> results, out string det)
        {
            det = "";
            return results.Sum();
        }

        public override int InnerEvaluate(ChessBoard board)
        {

            var all = new List<Piece>();
            all.AddRange(board.CapturedWhite);
            all.AddRange(board.CapturedBlack);
            var promotionCount = 0;
            //aand of course, if a piece is promoted, then we kill a pawn.
            foreach (var move in board.ExecutedMoves)
            {
                if (move.Parameter != null && move.Parameter.ShortStr[0] == '=')
                {
                    promotionCount++;
                }
            }

            var nonReal = all.Where(el => el.Id <= 0);
            if (nonReal.Count() > 0)
            {
                Console.WriteLine(board.ToAscii());
                foreach (var m in board.ExecutedMoves) { Console.Write(m);Console.Write(' '); }
                var a = 3;
            }

            all = all.Where(el => el.Id > 0).ToList();

            var survivingPawns = 16- all.Where(el => el.Type == PieceType.Pawn).Count() -1;
            var survivingBishops = 4 - all.Where(el => el.Type == PieceType.Bishop).Count();
            var survivingKnights= 4 - all.Where(el => el.Type == PieceType.Knight).Count();
            var survivingRooks = 4 - all.Where(el => el.Type == PieceType.Rook).Count();

            //Console.WriteLine(board.ToAscii());

            var raw = survivingPawns + survivingKnights * -2 + survivingBishops * -2 + survivingRooks * -2;

            return raw;

        }
    }

    //definitely not bug-free right now.
    public class SurvivingQueen5PercentEachEvaluator : INumericalEvaluator
    {
        public string Name => nameof(SurvivingQueen5PercentEachEvaluator);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            //wow it's a huge pain to actually track the individual queen from the beginning of the game?
            var origQueensKilled = 0;
            var queensSeen = 0;
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                queensSeen += 2;
                //var testBoard = CopyBoardBase(board);
                var wOrigQueenPos = new Position("d1");
                var bOrigQueenPos = new Position("d8");
                var wOrigQueenDead = false;
                var bOrigQueenDead = false;
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    board.Next();

                    var color = ii % 2 == 0 ? "White" : "Black";

                    //tracking moving of the original queen.
                    if (move.Piece.Type == PieceType.Queen && !wOrigQueenDead && move.OriginalPosition == wOrigQueenPos && move.Piece.Color == PieceColor.White)
                    {
                        wOrigQueenPos = move.NewPosition;
                        continue;
                    }
                    else if (move.Piece.Type == PieceType.Queen && !bOrigQueenDead && move.OriginalPosition == bOrigQueenPos && move.Piece.Color == PieceColor.Black)
                    {
                        bOrigQueenPos = move.NewPosition;
                        continue;
                    }

                    //if someone killed the original queen.
                    if (!wOrigQueenDead && move.NewPosition == wOrigQueenPos)
                    {
                        wOrigQueenDead = true;
                    }
                    if (!bOrigQueenDead && move.NewPosition == bOrigQueenPos)
                    {
                        bOrigQueenDead = true;
                    }
                }
                if (wOrigQueenDead)
                {
                    origQueensKilled++;
                }
                if (bOrigQueenDead)
                {
                    origQueensKilled++;
                }
            }

            var survivingOriginalQueens = queensSeen - origQueensKilled;
            var raw = 5 * survivingOriginalQueens;
            var det = $"{boards.Count()} games saw {queensSeen} queens; {origQueensKilled} original queens were killed, leaving {survivingOriginalQueens}.";
            return new NumericalEvaluationResult(raw, det);
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
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }

    internal class HalfPercentForEachMoveInLongestGameEvaluator : INumericalEvaluator
    {
        public string Name => nameof(HalfPercentForEachMoveInLongestGameEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var max = 0;
            var det = "";
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                if (board.ExecutedMoves.Count > max)
                {
                    max = board.ExecutedMoves.Count;
                    det = $"Longest game is {Statics.DescribeChessBoard(board)}, with {board.ExecutedMoves.Count} moves.";
                }
            }

            var raw = max / 2;
            var res = new NumericalEvaluationResult(raw, det);
            return res;
        }
    }
    public class OnePercentForEachMoveInLongestGameEvaluator : INumericalEvaluator
    {
        public string Name => nameof(OnePercentForEachMoveInLongestGameEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var max = 0;
            var det = "";
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                if (board.ExecutedMoves.Count > max)
                {
                    max = board.ExecutedMoves.Count;
                    det = $"Longest game is {Statics.DescribeChessBoard(board)}, with {board.ExecutedMoves.Count} moves.";
                }
            }

            var res = new NumericalEvaluationResult(max, det);
            return res;
        }
    }

}