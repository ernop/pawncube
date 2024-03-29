﻿using Chess;

using PawnCube;

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
    public class KingTakesQueenFivePercentEachEvaluator : INumericalEvaluator
    {
        public string Name => nameof(KingTakesQueenFivePercentEachEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var totalKingsTakingQueens = 0;
            var examples = new List<NumericalExample>();
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {

                    var move = board.ExecutedMoves[ii];
                    var po = move.OriginalPosition;
                    var exiPiece = board[po];
                    if (exiPiece == null)
                    {
                        board.Next();
                        continue;
                    }
                    if (exiPiece.Type != PieceType.King)
                    {
                        board.Next();
                        continue;
                    }
                    if (move.CapturedPiece != null && move.CapturedPiece.Type == PieceType.Queen)
                    {
                        totalKingsTakingQueens++;
                        examples.Add(new NumericalExample(board, $"King takes queen at move {ii}", ii, 5));
                    }
                    board.Next();
                }
            }
            var raw = totalKingsTakingQueens * 5;
            var det = $"Total of {totalKingsTakingQueens} kings taking queens";
            return new NumericalEvaluationResult(raw, det, examples);
        }
    }

    public class PieceOnStartingSquareOnePercentEachEvaluator : INumericalEvaluator
    {
        public string Name => nameof(PieceOnStartingSquareOnePercentEachEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var totalPiecesOnStartingSquare = 0;
            var examples = new List<NumericalExample>();
            foreach (var board in boards)
            {
                var thisBoardPiecesOnStartingSquare = 0;
                board.GoToStartingPosition();
                var pieces_original = Statics.GetAllPiecesAndPositions(board);

                board.Last();
                var pieces_end = Statics.GetAllPiecesAndPositions(board);
                foreach (var el in pieces_end)
                {
                    var piece = el.Item1;
                    var pos = el.Item2;
                    foreach (var orig in pieces_original)
                    {
                        if (orig.Item1.Id == piece.Id && orig.Item2 == pos)
                        {
                            totalPiecesOnStartingSquare++;
                            thisBoardPiecesOnStartingSquare++;
                            break;
                        }
                    }
                }
                if (thisBoardPiecesOnStartingSquare > 0)
                {
                    examples.Add(new NumericalExample(board, $"This board has: {thisBoardPiecesOnStartingSquare} pieces on starting square at the end", board.ExecutedMoves.Count() - 1, thisBoardPiecesOnStartingSquare));
                }
            }
            var raw = totalPiecesOnStartingSquare;
            var det = $"Total of {totalPiecesOnStartingSquare} pieces on starting square";
            return new NumericalEvaluationResult(raw, det, examples);
        }
    }

    ///That is, only pick the game which had the most of these guys.    
    public class NonpawnNonmoversWorthTenPercentEachInTheirBiggestGame : INumericalEvaluator
    {
        public string Name => nameof(NonpawnNonmoversWorthTenPercentEachInTheirBiggestGame);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {

            var mostNonPawnNonMovers = 0;
            ChessBoard bestGame = null;
            var examples = new List<NumericalExample>();
            foreach (var board in boards)
            {
                var pieceIds = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 25, 26, 27, 28, 29, 30, 31, 32 };
                var moveNo = 0;
                foreach (var move in board.ExecutedMoves)
                {
                    var color = moveNo % 2 == 0 ? PieceColor.White : PieceColor.Black;
                    if (move.Parameter == null)
                    {
                        var ii = pieceIds.IndexOf(move.Piece.Id);
                        if (ii >= 0)
                        {
                            pieceIds.RemoveAt(ii);
                        }
                    }
                    else
                    {
                        if (move.Parameter.ShortStr == "O-O")
                        {
                            if (color == PieceColor.White)
                            {
                                var pp = pieceIds.Remove(5);
                                var nn = pieceIds.Remove(8);
                                if (!pp || !nn)
                                {
                                    throw new Exception("huh");
                                }
                            }
                            else
                            {
                                var rr = pieceIds.Remove(29);
                                var qq = pieceIds.Remove(32);
                                if (!rr || !qq)
                                {
                                    throw new Exception("huh");
                                }
                            }
                        }
                        else if (move.Parameter.ShortStr == "O-O-O")
                        {
                            if (color == PieceColor.White)
                            {
                                var pp = pieceIds.Remove(5);
                                var nn = pieceIds.Remove(1);
                                if (!pp || !nn)
                                {
                                    throw new Exception("huh");
                                }
                            }
                            else
                            {
                                var rr = pieceIds.Remove(25);
                                var qq = pieceIds.Remove(29);
                                if (!rr || !qq)
                                {
                                    throw new Exception("huh");
                                }
                            }
                        }

                    }
                    moveNo++;
                }
                var thisNonPawnNonMovers = pieceIds.Count();
                if (thisNonPawnNonMovers>mostNonPawnNonMovers)
                {
                    mostNonPawnNonMovers = thisNonPawnNonMovers;
                    bestGame = board;
                    
                }
            }

            examples.Add(new NumericalExample(bestGame, $"This board has: {mostNonPawnNonMovers} non-pawn non-movers", bestGame.ExecutedMoves.Count() - 1, mostNonPawnNonMovers * 10));
            var raw = mostNonPawnNonMovers * 10;
            var det = $"Total of {mostNonPawnNonMovers} non-pawns didn't move in the game which had the biggest value of that number.";
            return new NumericalEvaluationResult(raw, det, examples);
        }
    }

    public class PawnPromotionFivePercentEachEvaluator : INumericalEvaluator
    {
        public string Name => nameof(PawnPromotionFivePercentEachEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var totalPromotions = 0;
            var examples = new List<NumericalExample>();
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                var promotionsThisGame = 0;
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    board.Next();
                    if (move.Parameter != null)
                    {
                        var l = move.Parameter.ShortStr;
                        if (l == "=" || l == "=Q" || l == "=R" || l == "=B" || l == "=N")
                        {
                            promotionsThisGame++;
                        }
                    }
                }
                totalPromotions += promotionsThisGame;
                if (promotionsThisGame > 0)
                {
                    examples.Add(new NumericalExample(board, $"Game with {promotionsThisGame} promotions", 0, promotionsThisGame * 5));
                }
            }
            var raw = totalPromotions * 5;
            var det = $"Total of {totalPromotions} promotions";
            return new NumericalEvaluationResult(raw, det, examples);
        }
    }

    public class TotalPawnAdvantageSeen20PercentPerPawnEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TotalPawnAdvantageSeen20PercentPerPawnEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var highestPawnAdvantage = 0;
            ChessBoard bestGame = null;
            var bestMoveNumber = 0;
            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var pieces = GetAllPieces(board);
                    var bPawnCount = 0;
                    var wPawnCount = 0;
                    foreach (var piece in pieces)
                    {
                        if (piece.Type == PieceType.Pawn)
                        {
                            if (piece.Color == PieceColor.White)
                            {
                                wPawnCount++;
                            }
                            else
                            {
                                bPawnCount++;
                            }
                        }
                    }
                    var gap = Math.Abs(wPawnCount - bPawnCount);
                    if (gap > highestPawnAdvantage)
                    {
                        highestPawnAdvantage = gap;
                        bestGame = board;
                        bestMoveNumber = ii;
                    }
                    board.Next();
                }
            }

            if (bestGame != null)
            {
                var color = bestMoveNumber % 2 == 0 ? "white" : "black";
                var det = $"Highest pawn advantage seen was {highestPawnAdvantage} pawns, for {color}, in game {Statics.DescribeChessBoard(bestGame)} at move {bestMoveNumber}";
                var examples = new List<NumericalExample>() { new NumericalExample(bestGame, det, bestMoveNumber, highestPawnAdvantage) };
                return new NumericalEvaluationResult(highestPawnAdvantage, det, examples);
            }
            else
            {
                return new NumericalEvaluationResult(0, "No pawn advantage seen", new List<NumericalExample>());
            }
        }
    }

    public class Pawn10VsKnightMinus10FirstMoveEvaluator : INumericalEvaluator
    {
        public string Name => nameof(Pawn10VsKnightMinus10FirstMoveEvaluator);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var knightCount = 0;
            var pawnCount = 0;
            foreach (var board in boards)
            {
                var m = board.ExecutedMoves[0];
                if (m.Piece.Type == PieceType.Pawn)
                {
                    pawnCount++;
                }
                else if (m.Piece.Type == PieceType.Knight)
                {
                    knightCount++;
                }
            }
            var raw = 10 * pawnCount + -10 * knightCount;
            var det = $"Total of {pawnCount} pawn first moves, {knightCount} knight first moves, so result is: {raw}";
            var res = new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
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
                        }
                    }
                }
            }

            var raw = 5 * twoSpaceJumpCount + -5 * oneSpaceJumpCount + -5 * firstMoveCaptures;
            var det = $"Total of {twoSpaceJumpCount} two space jumps, {oneSpaceJumpCount} one space jumps, and {firstMoveCaptures} first move captures.";
            var res = new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
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
            var res = new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
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
            var res = new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
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
            var res = new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
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
            var res = new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
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
            return new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
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
                shortCount += board.ExecutedMoves.Where(el => el.Parameter != null && el.Parameter.ShortStr == "O-O").Count();
                longCount += board.ExecutedMoves.Where(el => el.Parameter != null && el.Parameter.ShortStr == "O-O-O").Count();
            }

            var raw = 10 * shortCount + -5 * longCount;
            var det = $"Total short castles: {shortCount}, total long castles: {longCount}";
            return new NumericalEvaluationResult(raw, det, null);
        }
    }

    public class CapturedBishopsFiveCapturedPawnsMinusOneEvaluator : NumericalPerBoardEvaluator
    {
        public override string Name => nameof(CapturedBishopsFiveCapturedPawnsMinusOneEvaluator);

        public override NumericalEvaluationResult Aggregate(IEnumerable<NumericalExample> examples)
        {
            var val = examples.Select(el => el.Value).Sum();
            return new NumericalEvaluationResult(val, "", null);
        }

        public override NumericalExample InnerEvaluate(ChessBoard board)
        {
            board.Last();
            var total = 0;
            foreach (var piece in GetAllCaptures(board))
            {
                if (piece.Type == PieceType.Bishop)
                {
                    total += 5;
                }
                if (piece.Type == PieceType.Pawn)
                {
                    total += -1;
                }
            }
            return new NumericalExample(board, "", 0, total);
        }
    }

    public class SurvivingPawnsWorthOneSurvivingKnightBishopRookWorthNegativeTwoEvaluator : INumericalEvaluator
    {
        public string Name => nameof(SurvivingPawnsWorthOneSurvivingKnightBishopRookWorthNegativeTwoEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            int p = 0, b = 0, k = 0, r = 0;
            var most = 0;
            var least = 0;
            ChessBoard mostBoard = null;
            ChessBoard leastBoard = null;
            var mostDet = "";
            var leastDet = "";

            var examples = new List<NumericalExample>();
            foreach (var board in boards)
            {
                board.Last();
                var pieces = Statics.GetAllPieces(board).Where(el => el.Id > 0);
                var tp = pieces.Where(el => el.Type == PieceType.Pawn).Count();
                var tb = pieces.Where(el => el.Type == PieceType.Bishop).Count();
                var tk = pieces.Where(el => el.Type == PieceType.Knight).Count();
                var tr = pieces.Where(el => el.Type == PieceType.Rook).Count();

                p += tp;
                b += tb;
                k += tk;
                r += tr;

                var can = tp + tk * -2 + tb * -2 + tr * -2;
                if (can > most)
                {
                    most = can;
                    mostBoard = board;
                    mostDet = $"Best Pawn example: Total of {tp} pawns, {tk} knights, {tb} bishops, {tr} rooks survived in this one, total points: {can}.";
                }
                if (can < least)
                {
                    least = can;
                    leastBoard = board;
                    leastDet = $"Best non-pawn example: Total of {tp} pawns, {tk} knights, {tb} bishops, {tr} rooks survived in this one, total points: {can}.";
                }
            }

            if (mostBoard != null)
            {
                examples.Add(new NumericalExample(mostBoard, mostDet, mostBoard.ExecutedMoves.Count() - 1, most));
            }
            if (leastBoard != null)
            {
                examples.Add(new NumericalExample(leastBoard, leastDet, leastBoard.ExecutedMoves.Count() - 1, least));
            }

            var raw = p + k * -2 + b * -2 + r * -2;
            var det = $"Total of {p} pawns, {k} knights, {b} bishops, {r} rooks survived.";
            return new NumericalEvaluationResult(raw, det, examples);
        }
    }

    //definitely not bug-free right now.
    public class SurvivingQueen5PercentEachEvaluator : INumericalEvaluator
    {
        public string Name => nameof(SurvivingQueen5PercentEachEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var totalQueensSurvived = 0;
            foreach (var board in boards)
            {
                board.Last();
                var pieces = Statics.GetAllPieces(board).Where(el => el.Id > 0);
                var tq = pieces.Where(el => el.Type == PieceType.Queen).Count();
                totalQueensSurvived += tq;

            }
            var raw = totalQueensSurvived * 5;
            var det = $"Total of {totalQueensSurvived} queens survived.";
            return new NumericalEvaluationResult(raw, det, new List<NumericalExample>());
        }
    }

    internal class HalfPercentForEachMoveInLongestGameEvaluator : INumericalEvaluator
    {
        public string Name => nameof(HalfPercentForEachMoveInLongestGameEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var longest = boards.OrderByDescending(el => el.ExecutedMoves.Count).First();
            var chessMoves = Math.Ceiling(longest.ExecutedMoves.Count / 2.0);
            var raw = (int)Math.Floor(chessMoves * 0.5);
            var exa = new List<NumericalExample>() { new NumericalExample(longest, "", longest.ExecutedMoves.Count() - 1, raw) };
            var det = $"{Statics.DescribeChessBoard(longest)} had {longest.ExecutedMoves.Count()} plies which rolls up to {chessMoves} moves including partials, worth {raw}%";
            return new NumericalEvaluationResult(raw, det, exa);
        }
    }

    public class OnePercentForEachMoveInLongestGameEvaluator : INumericalEvaluator
    {
        public string Name => nameof(OnePercentForEachMoveInLongestGameEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var longest = boards.OrderByDescending(el => el.ExecutedMoves.Count).First();
            var chessMoves = Math.Ceiling(longest.ExecutedMoves.Count / 2.0);
            var raw = (int)Math.Floor(chessMoves);
            var exa = new List<NumericalExample>() { new NumericalExample(longest, "", longest.ExecutedMoves.Count() - 1, raw) };
            var det = $"{Statics.DescribeChessBoard(longest)} had {longest.ExecutedMoves.Count()} plies which rolls up to {chessMoves} moves including partials, worth {raw}%";
            return new NumericalEvaluationResult(raw, det, exa);
        }
    }
}
