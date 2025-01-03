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
    public class TenPercentForNonpawnNonmoversEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TenPercentForNonpawnNonmoversEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            int totalRaw = 0;
            var examples = new List<NumericalExample>();

            foreach (var board in boards)
            {
                // Go to starting position to identify initial non-pawn pieces
                board.GoToStartingPosition();

                // Get all non-pawn pieces at start
                var nonPawnPieceIds = new HashSet<int>();
                foreach (var piecePos in GetAllPiecesAndPositions(board))
                {
                    var piece = piecePos.Item1;
                    if (piece.Type != PieceType.Pawn)
                    {
                        nonPawnPieceIds.Add(piece.Id);
                    }
                }

                // Now iterate through moves and remove pieces that move
                for (int i = 0; i < board.ExecutedMoves.Count; i++)
                {
                    var move = board.ExecutedMoves[i];
                    // If this piece moves, it's no longer a non-mover
                    if (nonPawnPieceIds.Contains(move.Piece.Id))
                    {
                        nonPawnPieceIds.Remove(move.Piece.Id);
                    }
                    board.Next();
                }

                // Now at final position, nonPawnPieceIds contains IDs of pieces that never moved
                if (nonPawnPieceIds.Count > 0)
                {
                    int increment = nonPawnPieceIds.Count * 10;
                    totalRaw += increment;

                    var details = $"{nonPawnPieceIds.Count} non-pawn pieces never moved, contributing {increment}% for this board.";
                    var lastMoveIndex = Math.Max(0, board.ExecutedMoves.Count - 1);
                    examples.Add(new NumericalExample(board, details, lastMoveIndex, increment));
                }
            }

            var finalDetails = $"Total raw score: {totalRaw}%. 10% for each non-pawn piece that never moved in all games considered.";
            return new NumericalEvaluationResult(totalRaw, finalDetails, examples);
        }

        private IEnumerable<Tuple<Piece, Position>> GetAllPiecesAndPositions(ChessBoard board)
        {
            for (short x = 0; x < 8; x++)
            {
                for (short y = 0; y < 8; y++)
                {
                    var piece = board[new Position(x, y)];
                    if (piece != null)
                    {
                        yield return Tuple.Create(piece, new Position(x, y));
                    }
                }
            }
        }
    }

    public class TenPercentForPiecesReturningToStartAfterMovingEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TenPercentForPiecesReturningToStartAfterMovingEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            int totalRaw = 0;
            var examples = new List<NumericalExample>();

            foreach (var board in boards)
            {
                // Start at initial position
                board.GoToStartingPosition();

                // Record original positions for all pieces by their Id
                var originalPositions = new Dictionary<int, Position>();
                foreach (var piecePos in GetAllPiecesAndPositions(board))
                {
                    originalPositions[piecePos.Item1.Id] = piecePos.Item2;
                }

                // Track which pieces have moved
                var movedPieces = new HashSet<int>();

                // Advance to the end of the game
                for (int i = 0; i < board.ExecutedMoves.Count; i++)
                {
                    var move = board.ExecutedMoves[i];
                    movedPieces.Add(move.Piece.Id);
                    board.Next();
                }

                // Now at final position
                var finalPositions = GetAllPiecesAndPositions(board);

                int boardCount = 0;
                foreach (var piecePos in finalPositions)
                {
                    var piece = piecePos.Item1;
                    var pos = piecePos.Item2;
                    if (originalPositions.TryGetValue(piece.Id, out var startPos))
                    {
                        // Check if piece is on its original square at the end
                        // and has moved during the game
                        if (pos == startPos && movedPieces.Contains(piece.Id))
                        {
                            boardCount++;
                        }
                    }
                }

                if (boardCount > 0)
                {
                    int increment = boardCount * 10;
                    totalRaw += increment;
                    var details = $"{boardCount} pieces returned to their starting squares after having moved, contributing {increment}% for this board.";
                    var lastMoveIndex = Math.Max(0, board.ExecutedMoves.Count - 1);
                    examples.Add(new NumericalExample(board, details, lastMoveIndex, increment));
                }
            }

            var finalDetails = $"Total raw score: {totalRaw}%. 10% for each piece that ended on its starting square after moving at least once.";
            return new NumericalEvaluationResult(totalRaw, finalDetails, examples);
        }

        private IEnumerable<Tuple<Piece, Position>> GetAllPiecesAndPositions(ChessBoard board)
        {
            for (short x = 0; x < 8; x++)
            {
                for (short y = 0; y < 8; y++)
                {
                    var piece = board[new Position(x, y)];
                    if (piece != null)
                    {
                        yield return Tuple.Create(piece, new Position(x, y));
                    }
                }
            }
        }
    }

    public class KnightDirection3PercentVerticalMinus4PercentHorizontalEvaluator : INumericalEvaluator
    {
        public string Name => nameof(KnightDirection3PercentVerticalMinus4PercentHorizontalEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var examples = new List<NumericalExample>();
            int totalRaw = 0;

            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                int boardVerticalCount = 0;
                int boardHorizontalCount = 0;

                for (int i = 0; i < board.ExecutedMoves.Count; i++)
                {
                    var move = board.ExecutedMoves[i];
                    board.Next();

                    if (move.Piece.Type == PieceType.Knight)
                    {
                        var oldPos = move.OriginalPosition;
                        var newPos = move.NewPosition;
                        var dx = Math.Abs(oldPos.X - newPos.X);
                        var dy = Math.Abs(oldPos.Y - newPos.Y);

                        // Knight moves in an L-shape: (dx, dy) is either (1, 2) or (2, 1)
                        // If dx = 1 and dy = 2, the move is more vertical
                        // If dx = 2 and dy = 1, the move is more horizontal
                        if ((dx == 1 && dy == 2))
                        {
                            // vertical move +3%
                            boardVerticalCount++;
                        }
                        else if ((dx == 2 && dy == 1))
                        {
                            // horizontal move -4%
                            boardHorizontalCount++;
                        }
                        else
                        {
                            // Any other pattern would be unexpected for a knight
                            throw new Exception("Unexpected knight move pattern.");
                        }
                    }
                }

                if (boardVerticalCount > 0 || boardHorizontalCount > 0)
                {
                    var boardIncrement = boardVerticalCount * 3 + boardHorizontalCount * (-4);
                    totalRaw += boardIncrement;

                    var detail = $"Board: {Statics.DescribeChessBoard(board)}, vertical knight moves: {boardVerticalCount} (+3% each), horizontal knight moves: {boardHorizontalCount} (-4% each), net: {boardIncrement}%";
                    var lastMoveIndex = Math.Max(0, board.ExecutedMoves.Count - 1);
                    examples.Add(new NumericalExample(board, detail, lastMoveIndex, boardIncrement));
                }
            }

            var finalDetails = $"Total raw score: {totalRaw}%. Accumulated from +3% per vertical knight move and -4% per horizontal knight move across all boards.";
            return new NumericalEvaluationResult(totalRaw, finalDetails, examples);
        }
    }

    public class TenPercentPerPointOfFinalMaterialAdvantageEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TenPercentPerPointOfFinalMaterialAdvantageEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var examples = new List<NumericalExample>();
            int totalRaw = 0;

            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                board.Last();

                var pieces = GetAllPieces(board);

                int whiteMaterial = 0;
                int blackMaterial = 0;

                foreach (var piece in pieces)
                {
                    if (piece.Type != PieceType.King) { 
                    int value = GetPieceValue(piece.Type);

                        if (piece.Color == PieceColor.White)
                            whiteMaterial += value;
                        else
                            blackMaterial += value;
                    }
                }

                int difference = Math.Abs(whiteMaterial - blackMaterial);
                if (difference != 0)
                {
                    int increment = difference * 10;
                    totalRaw += increment;

                    var sideAhead = difference > 0 ? "White" : "Black";
                    var details = $"{sideAhead} is ahead by {difference} points of material in the final position, contributing {increment}%.";

                    // Use last move index as example reference
                    var moveIndex = board.ExecutedMoves.Count - 1;
                    if (moveIndex < 0) moveIndex = 0; // In case no moves are made
                    examples.Add(new NumericalExample(board, details, moveIndex, increment));
                }
            }

            var det = $"Total raw score: {totalRaw}%. Calculated by summing 10% per material point advantage in each final position.";
            return new NumericalEvaluationResult(totalRaw, det, examples);
        }


        private int GetPieceValue(PieceType type)
        {
            // Switch on the integer 'Value' property of PieceType.
            switch (type.Value)
            {
                case 1: // Pawn
                    return 1;
                case 2: // Rook
                    return 5;
                case 3: // Knight
                    return 3;
                case 4: // Bishop
                    return 3;
                case 5: // Queen
                    return 9;
                case 6: // King
                        // Normally not captured, so handle as an error
                    throw new Exception("Unexpected piece type captured (King).");
                default:
                    throw new Exception($"Unexpected piece type value: {type.Value}");
            }
        }

        private IEnumerable<Piece> GetAllPieces(ChessBoard board)
        {
            for (short x = 0; x < 8; x++)
            {
                for (short y = 0; y < 8; y++)
                {
                    var piece = board[new Position(x, y)];
                    if (piece != null)
                        yield return piece;
                }
            }
        }
    }

    /// <summary>
    /// 5% for every pawn who jumps two steps in his first move, -5% for every pawn who takes one step or captures in his first move
    /// </summary>
    public class PawnMoveTypesFiveForTwoJumpMinusFiveForOtherFirstMove2 : INumericalEvaluator
    {
        public string Name => nameof(PawnMoveTypesFiveForTwoJumpMinusFiveForOtherFirstMove);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var twoSpaceJumpCountTotal = 0;
            var oneSpaceJumpCountTotal = 0;
            var firstMoveCapturesTotal = 0;

            var examples = new List<NumericalExample>();

            foreach (var board in boards)
            {
                board.GoToStartingPosition();

                // For each pawn, we only care about its first move.
                // We'll track which pawns have moved and what their first move was.
                var pawnFirstMoveDone = new HashSet<int>();

                int boardTwoSteps = 0;
                int boardOneSteps = 0;
                int boardCaptures = 0;

                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    board.Next();

                    if (move.Piece.Type == PieceType.Pawn && !pawnFirstMoveDone.Contains(move.Piece.Id))
                    {
                        // Pawn is making its first move
                        pawnFirstMoveDone.Add(move.Piece.Id);

                        // Check if it's from the initial rank (White pawns start at y=1, Black at y=6)
                        bool isStartingRank = (move.Piece.Color == PieceColor.White && move.OriginalPosition.Y == 1) ||
                                              (move.Piece.Color == PieceColor.Black && move.OriginalPosition.Y == 6);

                        if (isStartingRank)
                        {
                            var gap = Math.Abs(move.OriginalPosition.Y - move.NewPosition.Y);
                            if (gap == 2)
                            {
                                // Two-step jump
                                twoSpaceJumpCountTotal++;
                                boardTwoSteps++;
                            }
                            else if (gap == 1)
                            {
                                if (move.CapturedPiece == null)
                                {
                                    // One-step advance
                                    oneSpaceJumpCountTotal++;
                                    boardOneSteps++;
                                }
                                else
                                {
                                    // Capture on first move
                                    firstMoveCapturesTotal++;
                                    boardCaptures++;
                                }
                            }
                        }
                    }
                }

                // If this board had any relevant moves, record an example
                if (boardTwoSteps > 0 || boardOneSteps > 0 || boardCaptures > 0)
                {
                    // Calculate the board increment
                    // +5 for each two-step jump, -5 for each one-step and -5 for each capture
                    var boardIncrement = 5 * boardTwoSteps - 5 * boardOneSteps - 5 * boardCaptures;
                    var details = $"Board had {boardTwoSteps} two-step first pawn moves, {boardOneSteps} one-step first pawn moves, and {boardCaptures} first-move captures, contributing {boardIncrement}%.";

                    // Use last executed move index as a reference or -1 if no moves
                    var moveIndex = Math.Max(0, board.ExecutedMoves.Count - 1);
                    examples.Add(new NumericalExample(board, details, moveIndex, boardIncrement));
                }
            }

            // Compute final raw result
            var raw = 5 * twoSpaceJumpCountTotal - 5 * oneSpaceJumpCountTotal - 5 * firstMoveCapturesTotal;

            var det = $"Across all boards: {twoSpaceJumpCountTotal} two-step jumps, {oneSpaceJumpCountTotal} one-step moves, and {firstMoveCapturesTotal} first-move captures. Result: {raw}%";
            return new NumericalEvaluationResult(raw, det, examples);
        }
    }

    public class KingTakesQueenTenPercentEachEvaluator : INumericalEvaluator
    {
        public string Name => nameof(KingTakesQueenTenPercentEachEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var totalKingsTakingQueens = 0;
            var examples = new List<NumericalExample>();
            var allQueenCaptures = new List<string>();

            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                var gameName = Statics.DescribeChessBoard(board);

                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];

                    // Track all queen captures
                    if (move.CapturedPiece != null && move.CapturedPiece.Type == PieceType.Queen)
                    {
                        var captureColor = move.Piece.Color == PieceColor.White ? "White" : "Black";
                        allQueenCaptures.Add(
                            $"Game {gameName}: {captureColor} {move.Piece.Type} takes queen at move {ii}"
                        );
                    }

                    // Track king-specific captures for scoring
                    if (move.Piece.Type == PieceType.King &&
                        move.CapturedPiece != null &&
                        move.CapturedPiece.Type == PieceType.Queen)
                    {
                        totalKingsTakingQueens++;
                        var kingColor = move.Piece.Color == PieceColor.White ? "White" : "Black";
                        examples.Add(new NumericalExample(board,
                            $"{kingColor} king takes queen at move {ii} in game {gameName}",
                            ii,
                            10));
                    }
                    board.Next();
                }
            }

            // Print all queen captures
            Console.WriteLine("\nAll Queen Captures:");
            foreach (var capture in allQueenCaptures)
            {
                Console.WriteLine(capture);
            }

            // Print king-specific captures
            Console.WriteLine("\nKing Takes Queen Examples:");
            foreach (var example in examples)
            {
                Console.WriteLine(example.Details);
            }
            Console.WriteLine();

            var raw = totalKingsTakingQueens * 10;
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
                if (thisNonPawnNonMovers > mostNonPawnNonMovers)
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

    public class PieceTourEvaluator : INumericalEvaluator
    {
        public string Name => nameof(PieceTourEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var visited = new bool[8, 8];
            var visitCount = new int[8, 8];
            var unvisitedCount = 64;
            var examples = new List<NumericalExample>();

            foreach (var board in boards)
            {
                board.GoToStartingPosition();

                // Mark initial positions
                for (short x = 0; x < 8; x++)
                {
                    for (short y = 0; y < 8; y++)
                    {
                        var piece = board[new Position(x, y)];
                        if (piece != null)
                        {
                            visitCount[x, y]++;
                            if (!visited[x, y])
                            {
                                visited[x, y] = true;
                                unvisitedCount--;
                            }
                        }
                    }
                }

                // Track moves
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    board.Next();

                    var newX = move.NewPosition.X;
                    var newY = move.NewPosition.Y;

                    visitCount[newX, newY]++;

                    if (!visited[newX, newY])
                    {
                        visited[newX, newY] = true;
                        unvisitedCount--;

                        if (unvisitedCount == 0)
                        {
                            examples.Add(new NumericalExample(board,
                                "This move completed the tour of all squares",
                                ii,
                                100));
                        }
                    }
                }
            }

            // Print visitation counts
            Console.WriteLine("\nSquare visitation counts:");
            for (int y = 7; y >= 0; y--)
            {
                Console.Write($"{y + 1} ");
                for (int x = 0; x < 8; x++)
                {
                    Console.Write($"{visitCount[x, y],5}");
                }
                Console.WriteLine();
            }
            Console.Write("   ");
            for (int x = 0; x < 8; x++)
            {
                Console.Write($"    {(char)('a' + x)}");
            }
            Console.WriteLine("\n");

            var raw = unvisitedCount == 0 ? 100 : 0;
            var unvisitedSquares = new List<string>();

            if (unvisitedCount > 0)
            {
                for (short x = 0; x < 8; x++)
                {
                    for (short y = 0; y < 8; y++)
                    {
                        if (!visited[x, y])
                        {
                            unvisitedSquares.Add($"{(char)('a' + x)}{y + 1}");
                        }
                    }
                }
            }

            var det = unvisitedCount == 0
                ? "All squares were visited by at least one piece across all games"
                : $"Squares never visited: {string.Join(", ", unvisitedSquares)}";

            return new NumericalEvaluationResult(raw, det, examples);
        }
    }

    public class KnightCoverageEvaluator : INumericalEvaluator
    {
        public string Name => nameof(KnightCoverageEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            // Track both if visited and how many times visited
            var visited = new bool[8, 8];
            var visitCount = new int[8, 8];
            var unvisitedCount = 64;
            var examples = new List<NumericalExample>();

            foreach (var board in boards)
            {
                board.GoToStartingPosition();

                // Mark initial positions
                for (short x = 0; x < 8; x++)
                {
                    for (short y = 0; y < 8; y++)
                    {
                        var piece = board[new Position(x, y)];
                        if (piece != null && piece.Type == PieceType.Knight)
                        {
                            visitCount[x, y]++;
                            if (!visited[x, y])
                            {
                                visited[x, y] = true;
                                unvisitedCount--;
                            }
                        }
                    }
                }

                // Track moves
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    board.Next();

                    if (move.Piece.Type == PieceType.Knight)
                    {
                        var newX = move.NewPosition.X;
                        var newY = move.NewPosition.Y;

                        visitCount[newX, newY]++;

                        if (!visited[newX, newY])
                        {
                            visited[newX, newY] = true;
                            unvisitedCount--;

                            if (unvisitedCount == 0)
                            {
                                examples.Add(new NumericalExample(board,
                                    "This move completed the knight coverage of all squares",
                                    ii,
                                    100));
                            }
                        }
                    }
                }
            }

            // Print visitation counts
            Console.WriteLine("\nSquare visitation counts:");
            for (int y = 7; y >= 0; y--)
            {
                Console.Write($"{y + 1} ");
                for (int x = 0; x < 8; x++)
                {
                    Console.Write($"{visitCount[x, y],5}");
                }
                Console.WriteLine();
            }
            Console.Write("   ");
            for (int x = 0; x < 8; x++)
            {
                Console.Write($"    {(char)('a' + x)}");
            }
            Console.WriteLine("\n");

            var raw = unvisitedCount == 0 ? 100 : 0;
            var unvisitedSquares = new List<string>();

            if (unvisitedCount > 0)
            {
                for (short x = 0; x < 8; x++)
                {
                    for (short y = 0; y < 8; y++)
                    {
                        if (!visited[x, y])
                        {
                            unvisitedSquares.Add($"{(char)('a' + x)}{y + 1}");
                        }
                    }
                }
            }

            var det = unvisitedCount == 0
                ? "All squares were visited by at least one knight across all games"
                : $"Squares never visited by knights: {string.Join(", ", unvisitedSquares)}";

            return new NumericalEvaluationResult(raw, det, examples);
        }
    }

    public class TwentyPercentPerEnPassantDoneNumericalEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TwentyPercentPerEnPassantDoneNumericalEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var totalEnPassants = 0;
            var examples = new List<NumericalExample>();

            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                for (var ii = 1; ii < board.ExecutedMoves.Count; ii++) // Start at 1 to check previous move
                {
                    var currentMove = board.ExecutedMoves[ii];
                    var previousMove = board.ExecutedMoves[ii - 1];

                    // Check if current move is a pawn moving diagonally
                    if (currentMove.Piece.Type == PieceType.Pawn &&
                        Math.Abs(currentMove.OriginalPosition.X - currentMove.NewPosition.X) == 1)
                    {
                        // Check if previous move was a pawn moving two squares
                        if (previousMove.Piece.Type == PieceType.Pawn &&
                            Math.Abs(previousMove.OriginalPosition.Y - previousMove.NewPosition.Y) == 2 &&
                            previousMove.NewPosition.X == currentMove.NewPosition.X)
                        {
                            // Verify the capture occurred on the square the opponent's pawn passed through
                            int expectedY = (previousMove.OriginalPosition.Y + previousMove.NewPosition.Y) / 2;
                            if (currentMove.NewPosition.Y == expectedY)
                            {
                                totalEnPassants++;
                                examples.Add(new NumericalExample(board,
                                    $"En passant capture at move {ii}",
                                    ii,
                                    20));
                            }
                        }
                    }
                    board.Next();
                }
            }

            var raw = totalEnPassants * 20;
            var det = $"Total of {totalEnPassants} en passant captures";
            return new NumericalEvaluationResult(raw, det, examples);
        }
    }

    public class TwentyPercentPerExtraPawnMaxAdvantageEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TwentyPercentPerExtraPawnMaxAdvantageEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            var maxPawnAdvantage = 0;
            NumericalExample bestExample = null;
            var det = "";

            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var pieces = GetAllPieces(board);
                    var whitePawns = pieces.Count(p => p.Type == PieceType.Pawn && p.Color == PieceColor.White);
                    var blackPawns = pieces.Count(p => p.Type == PieceType.Pawn && p.Color == PieceColor.Black);

                    var advantage = Math.Abs(whitePawns - blackPawns);
                    if (advantage > maxPawnAdvantage)
                    {
                        maxPawnAdvantage = advantage;
                        var leadingColor = whitePawns > blackPawns ? "White" : "Black";
                        det = $"Greatest pawn advantage was {advantage} pawns ({leadingColor} ahead) at move {ii}";
                        bestExample = new NumericalExample(board, det, ii, advantage * 20);
                    }
                    board.Next();
                }
            }

            var raw = maxPawnAdvantage * 20;
            var examples = bestExample != null ? new List<NumericalExample> { bestExample } : new List<NumericalExample>();
            //var det = bestExample?.Description ?? "No pawn advantage found";

            return new NumericalEvaluationResult(raw, det, examples);
        }
    }
}
