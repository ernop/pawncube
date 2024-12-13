using Chess;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using static PawnCube.Statics;


namespace PawnCube
{
    public class o1Comments
    {
        public string summary = @"General Patterns of Potential Bugs:

    Piece Identity vs. Piece Type:
    Some evaluators check conditions by only comparing piece types or positions without ensuring they track the same piece identity. For instance, if they want to confirm a specific piece moved repeatedly, they should consistently use piece IDs. Using just piece type and final positions can cause false positives if another piece of the same type occupies a certain square.

    Promotion Handling:
    Evaluators that deal with promoted pieces must carefully update their reference color or identity at the exact time the promotion occurs. Applying promotion logic retroactively or prematurely can produce incorrect results.

    Move/Position Iteration and Board State Management:
    Some evaluators repeatedly call board.Next() without ever reverting to a previous state or handling boundaries. This can cause confusion or errors when indexing moves or analyzing positions before or after certain moves. They must ensure that at each iteration, the board is in the correct state they expect.

    Multiple Yields and Early Breaks:
    Some evaluators yield conditions multiple times or fail to break after yielding, possibly leading to multiple results when only one might be intended.

Specific Evaluators:

    SamePieceMovesEightTimesInARowEvaluator:
        Potential Issue: It attempts to verify if the same piece moved eight times in a row by looking at the original and new positions. However, it does not verify that the piece has the same piece ID, only that it’s the same piece type and that moves “line up” with previous positions. This could allow a different but identical-type piece to create a false positive scenario.
        Also: It does not check for indexing issues if it looks back 2 moves at a time when ii is small. For a small ii, ii - checkedCount*2 - 2 could become negative, causing an index out of range error.

    BishopManualUndoEvaluator:
    This evaluator looks for a bishop returning to a square it occupied two moves ago. It checks only piece type (PieceType.Bishop) and not piece ID. Another bishop could have moved into that square, triggering a false positive. Ideally, it should verify that the exact same piece (by ID) returned, not just any bishop.

    HomecomingPiecesTenPercentEachEvaluator:
    This evaluator tries to assign piece IDs to starting positions (ranks 0,1,6,7) to track which pieces return home. However, the code assumes that piece IDs assigned by the chess library align exactly with the order of pieces on these ranks. This assumption may not hold if the engine assigns IDs differently. The logic also relies on id increments that may not match actual IDs, risking incorrect mappings and thus subtle bugs.

    NoCapturesBeforeMoveXXEvaluator (10,20,30):
    These evaluators check if no captures occur before a certain move number. Once they detect that no captures occurred through the threshold, they yield a result. However, they do so inside a loop that continues to run, potentially yielding multiple times if there are more moves after the threshold. They should break after yielding once.

    BothKingsWrongSideEvaluator:
    At first glance, the logic is a bit confusing but on careful inspection it appears correct. It sets bad = true if white king is still on its original side (pos.Y < 4 for White means not crossed the midpoint) or black king is on its original side (pos.Y >= 4 for Black means not crossed down). If after checking both kings bad remains false, it yields. This logic, while correct, is not very intuitive and can easily confuse a reader. It’s not strictly a bug, but a place for improvement in clarity. A future maintainer might mistake this for a bug.

    QueenEnPriseThreeMovesEvaluator:
    This evaluator seems incomplete or at least overly complicated:
        It tries to track if a queen is attackable for three consecutive moves, but the code snippet given doesn’t fully revert the board to check each position properly or ensure timing is correct.
        It writes to console (suggesting debugging) and has logic that might not be fully tested.
        The logic uses board.Moves() at each iteration without ensuring the board is in the correct state. If board.Next() is repeatedly called, by the time it checks moves, it might not refer to the intended position in the timeline.

    FullRankEvaluator:
    The logic that checks if a rank is full of pieces and also counts how many are in their original positions is somewhat arbitrary and complex. It may not be a bug per se, but the conditions (piecesInOriginalPosition > 6 || (totalPieces - piecesInOriginalPosition) < 2) might not be what the author intended. It’s unclear what the exact goal is and whether the condition correctly implements that goal. Without clearer documentation, it’s hard to confirm correctness.";
    }

    public class BoardTiltEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(BoardTiltEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();

            for (int ii = 0; ii <= board.ExecutedMoves.Count; ii++)
            {
                if (ii > 0)
                {
                    board.Next();
                }

                var pieces = GetAllPiecesPositions(board);

                // Count how many pieces are in each half
                int topHalfCount = pieces.Count(p => p.Y >= 4);
                int bottomHalfCount = pieces.Count(p => p.Y <= 3);
                int leftHalfCount = pieces.Count(p => p.X <= 3);
                int rightHalfCount = pieces.Count(p => p.X >= 4);

                // Check horizontal halves
                // top half >=10 and bottom half=0, or bottom half>=10 and top half=0
                if ((topHalfCount >= 10 && bottomHalfCount == 0) || (bottomHalfCount >= 10 && topHalfCount == 0))
                {
                    var detail = $"Board tilt horizontally at move {ii}. One horizontal half has ≥10 pieces, the other is empty.";
                    yield return new BooleanExample(board, detail, Math.Max(0, ii - 1));
                    yield break;
                }

                // Check vertical halves
                // left half >=10 and right half=0, or right half>=10 and left half=0
                if ((leftHalfCount >= 10 && rightHalfCount == 0) || (rightHalfCount >= 10 && leftHalfCount == 0))
                {
                    var detail = $"Board tilt vertically at move {ii}. One vertical half has ≥10 pieces, the other is empty.";
                    yield return new BooleanExample(board, detail, Math.Max(0, ii - 1));
                    yield break;
                }
            }
        }

        private IEnumerable<Position> GetAllPiecesPositions(ChessBoard board)
        {
            for (short x = 0; x < 8; x++)
            {
                for (short y = 0; y < 8; y++)
                {
                    var piece = board[new Position(x, y)];
                    if (piece != null) yield return new Position(x, y);
                }
            }
        }
    }

    public class KingReachesOpponentsHomeEdgeEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(KingReachesOpponentsHomeEdgeEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();

            // After each move, check king positions
            for (int ii = 0; ii <= board.ExecutedMoves.Count; ii++)
            {
                if (ii > 0)
                {
                    board.Next();
                }

                var pieces = GetAllPieces(board);
                var whiteKing = pieces.FirstOrDefault(p => p.Type == PieceType.King && p.Color == PieceColor.White);
                var blackKing = pieces.FirstOrDefault(p => p.Type == PieceType.King && p.Color == PieceColor.Black);

                if (whiteKing != null)
                {
                    var wPos = FindPiece(board, whiteKing.Id);
                    if (wPos.Y == 7)
                    {
                        var details = $"White King has reached Black's home edge at move {ii}.";
                        yield return new BooleanExample(board, details, Math.Max(0, ii - 1));
                        yield break;
                    }
                }

                if (blackKing != null)
                {
                    var bPos = FindPiece(board, blackKing.Id);
                    if (bPos.Y == 0)
                    {
                        var details = $"Black King has reached White's home edge at move {ii}.";
                        yield return new BooleanExample(board, details, Math.Max(0, ii - 1));
                        yield break;
                    }
                }
            }
        }

        private Position FindPiece(ChessBoard board, int pieceId)
        {
            for (short x = 0; x < 8; x++)
            {
                for (short y = 0; y < 8; y++)
                {
                    var p = board[new Position(x, y)];
                    if (p != null && p.Id == pieceId)
                        return new Position(x, y);
                }
            }
            throw new Exception("Piece not found");
        }

        private IEnumerable<Piece> GetAllPieces(ChessBoard board)
        {
            for (short x = 0; x < 8; x++)
            {
                for (short y = 0; y < 8; y++)
                {
                    var p = board[new Position(x, y)];
                    if (p != null) yield return p;
                }
            }
        }
    }

    public class BothKingsWrongSideEvaluator2 : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(BothKingsWrongSideEvaluator2);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();

            // After each move (and including the initial state), check kings' positions
            for (int ii = 0; ii <= board.ExecutedMoves.Count; ii++)
            {
                // If ii > 0, advance to the next move position
                if (ii > 0)
                {
                    board.Next();
                }

                // Find both kings
                var pieces = GetAllPieces(board);
                var whiteKing = pieces.FirstOrDefault(p => p.Color == PieceColor.White && p.Type == PieceType.King);
                var blackKing = pieces.FirstOrDefault(p => p.Color == PieceColor.Black && p.Type == PieceType.King);

                if (whiteKing == null || blackKing == null)
                {
                    // If for some reason a king is missing (extremely unusual in normal chess),
                    // we just continue.
                    continue;
                }

                // Check positions
                var whiteKingPos = FindPiece(board, whiteKing.Id);
                var blackKingPos = FindPiece(board, blackKing.Id);

                // White king wrong side: y >= 4
                // Black king wrong side: y <= 3
                if (whiteKingPos.Y >= 4 && blackKingPos.Y <= 3)
                {
                    var details = $"At move {ii}, both kings on wrong side: White king at {whiteKingPos}, Black king at {blackKingPos}.";
                    yield return new BooleanExample(board, details, Math.Max(0, ii - 1));
                    yield break;
                }
            }
        }

        private Position FindPiece(ChessBoard board, int pieceId)
        {
            for (short x = 0; x < 8; x++)
            {
                for (short y = 0; y < 8; y++)
                {
                    var p = board[new Position(x, y)];
                    if (p != null && p.Id == pieceId)
                        return new Position(x, y);
                }
            }
            throw new Exception("Piece not found");
        }

        private IEnumerable<Piece> GetAllPieces(ChessBoard board)
        {
            for (short x = 0; x < 8; x++)
            {
                for (short y = 0; y < 8; y++)
                {
                    var p = board[new Position(x, y)];
                    if (p != null) yield return p;
                }
            }
        }
    }

    public class FirstCaptureIsQueenEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(FirstCaptureIsQueenEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();

            for (int i = 0; i < board.ExecutedMoves.Count; i++)
            {
                var move = board.ExecutedMoves[i];
                board.Next();

                if (move.CapturedPiece != null)
                {
                    // This is the first capture encountered
                    if (move.Piece.Type == PieceType.Queen)
                    {
                        var details = $"The first capture of the game was made by a queen at move {i}.";
                        yield return new BooleanExample(board, details, i);
                    }
                    break; // Stop after finding the first capture
                }
            }
        }
    }

    public class SamePieceMovesEightTimesInARowEvaluator2 : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(SamePieceMovesEightTimesInARowEvaluator2);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();

            // We'll track moves by White and Black separately
            // White moves are even indices, Black moves are odd indices
            var whiteMoves = board.ExecutedMoves.Where((m, idx) => idx % 2 == 0).ToList();
            var blackMoves = board.ExecutedMoves.Where((m, idx) => idx % 2 == 1).ToList();

            // Function to check one color's moves
            IEnumerable<BooleanExample> CheckColorMoves(List<Move> moves, PieceColor color, ChessBoard brd)
            {
                if (moves.Count < 8) yield break; // Need at least 8 moves by that side

                int consecutiveCount = 1;
                int lastPieceId = moves[0].Piece.Id;

                for (int i = 1; i < moves.Count; i++)
                {
                    var currentPieceId = moves[i].Piece.Id;
                    if (currentPieceId == lastPieceId)
                    {
                        consecutiveCount++;
                        if (consecutiveCount >= 8)
                        {
                            // Found a scenario where the same piece moved 8 times in a row for this color
                            // Determine the global move index for this scenario
                            // If these are white moves, their indices in ExecutedMoves are even; 
                            // If black moves, indices are odd.
                            // We can find any representative global move index:
                            // For example, the last move that confirmed the streak.
                            int globalMoveIndex = -1;
                            int countSoFar = 0;
                            for (int globalIdx = 0; globalIdx < brd.ExecutedMoves.Count; globalIdx++)
                            {
                                if (brd.ExecutedMoves[globalIdx].Piece.Color == color)
                                {
                                    countSoFar++;
                                    if (countSoFar == i + 1) // i+1 because i is 0-based index in color moves
                                    {
                                        globalMoveIndex = globalIdx;
                                        break;
                                    }
                                }
                            }

                            brd.GoToStartingPosition();
                            for (int step = 0; step <= globalMoveIndex; step++)
                                brd.Next();

                            var sideStr = color == PieceColor.White ? "White" : "Black";
                            var details = $"{sideStr}'s same piece (ID={lastPieceId}) moved 8 times in a row.";
                            yield return new BooleanExample(brd, details, globalMoveIndex);
                            yield break;
                        }
                    }
                    else
                    {
                        // Reset count
                        consecutiveCount = 1;
                        lastPieceId = currentPieceId;
                    }
                }
            }

            foreach (var example in CheckColorMoves(whiteMoves, PieceColor.White, board))
            {
                yield return example;
            }

            foreach (var example in CheckColorMoves(blackMoves, PieceColor.Black, board))
            {
                yield return example;
            }
        }
    }

    /// <summary>
    /// "2 Bishops vs 2 Knights" condition:
    /// At any point, one color has exactly 2 bishops and 0 knights,
    /// and the opposing color has exactly 2 knights and 0 bishops.
    /// Other pieces may be present.
    /// </summary>
    public class TwoBishopsVsTwoKnightsEvaluator2 : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(TwoBishopsVsTwoKnightsEvaluator2);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();
            for (int ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();

                var pieces = GetAllPieces(board);

                var whiteBishops = pieces.Count(p => p.Type == PieceType.Bishop && p.Color == PieceColor.White);
                var whiteKnights = pieces.Count(p => p.Type == PieceType.Knight && p.Color == PieceColor.White);

                var blackBishops = pieces.Count(p => p.Type == PieceType.Bishop && p.Color == PieceColor.Black);
                var blackKnights = pieces.Count(p => p.Type == PieceType.Knight && p.Color == PieceColor.Black);

                // Check condition: one side has 2 bishops & 0 knights, the other side has 2 knights & 0 bishops
                bool whiteHas2BishopsNoKnights = (whiteBishops == 2 && whiteKnights == 0);
                bool blackHas2BishopsNoKnights = (blackBishops == 2 && blackKnights == 0);
                bool whiteHas2KnightsNoBishops = (whiteKnights == 2 && whiteBishops == 0);
                bool blackHas2KnightsNoBishops = (blackKnights == 2 && blackBishops == 0);

                if ((whiteHas2BishopsNoKnights && blackHas2KnightsNoBishops) ||
                    (blackHas2BishopsNoKnights && whiteHas2KnightsNoBishops))
                {
                    var det = "Position reached: One side has exactly 2 bishops and 0 knights, while the other side has exactly 2 knights and 0 bishops.";
                    yield return new BooleanExample(board, det, ii);
                    yield break;
                }
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
                    {
                        yield return piece;
                    }
                }
            }
        }
    }

    public class SinglePieceCaptures19PointsOfMaterialEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(SinglePieceCaptures19PointsOfMaterialEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            // Dictionary to track total captured material by each piece (keyed by Piece.Id)
            var pieceIdToCapturedValue = new Dictionary<int, int>();

            board.GoToStartingPosition();
            for (int moveIndex = 0; moveIndex < board.ExecutedMoves.Count; moveIndex++)
            {
                var move = board.ExecutedMoves[moveIndex];
                board.Next();

                if (move.CapturedPiece != null)
                {
                    int value = GetPieceValue(move.CapturedPiece.Type);

                    if (!pieceIdToCapturedValue.ContainsKey(move.Piece.Id))
                    {
                        pieceIdToCapturedValue[move.Piece.Id] = 0;
                    }

                    pieceIdToCapturedValue[move.Piece.Id] += value;

                    if (pieceIdToCapturedValue[move.Piece.Id] >= 19)
                    {
                        var det = $"A single piece (ID={move.Piece.Id}, {move.Piece}) has captured at least 19 points of material.";
                        yield return new BooleanExample(board, det, moveIndex);
                        yield break;
                    }
                }
            }
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
    }

    /// <summary>
    /// "Center Pawn Cube" - The four center squares (3,3), (4,3), (3,4), (4,4)
    /// all occupied by pawns of the same color.
    /// </summary>
    public class CenterPawnCubeEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(CenterPawnCubeEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();
            for (int moveIndex = 0; moveIndex < board.ExecutedMoves.Count; moveIndex++)
            {
                board.Next();

                var p1 = board[new Position(3, 3)];
                var p2 = board[new Position(4, 3)];
                var p3 = board[new Position(3, 4)];
                var p4 = board[new Position(4, 4)];

                if (p1 != null && p2 != null && p3 != null && p4 != null &&
                    p1.Type == PieceType.Pawn && p2.Type == PieceType.Pawn &&
                    p3.Type == PieceType.Pawn && p4.Type == PieceType.Pawn &&
                    p1.Color == p2.Color && p2.Color == p3.Color && p3.Color == p4.Color)
                {
                    var det = $"Center Pawn Cube detected at center squares.";
                    yield return new BooleanExample(board, det, moveIndex);
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// "Pawn Cube" - Detect a 2x2 square of 4 pawns all of the same color.
    /// </summary>
    public class PawnCubeEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(PawnCubeEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();
            for (int moveIndex = 0; moveIndex < board.ExecutedMoves.Count; moveIndex++)
            {
                board.Next();

                // Check all 2x2 squares in an 8x8 board.
                // The top-left corner of a 2x2 block can start at most from (6,6) since we need (x+1,y+1).
                for (short x = 0; x < 7; x++)
                {
                    for (short y = 0; y < 7; y++)
                    {
                        var p1 = board[new Position(x, y)];
                        var p2 = board[new Position((short)(x + 1), y)];
                        var p3 = board[new Position(x, (short)(y + 1))];
                        var p4 = board[new Position((short)(x + 1), (short)(y + 1))];

                        // Check if all are non-null, pawns, and the same color
                        if (p1 != null && p2 != null && p3 != null && p4 != null &&
                            p1.Type == PieceType.Pawn && p2.Type == PieceType.Pawn &&
                            p3.Type == PieceType.Pawn && p4.Type == PieceType.Pawn &&
                            p1.Color == p2.Color && p2.Color == p3.Color && p3.Color == p4.Color)
                        {
                            var det = $"Pawn Cube detected at positions: {(x, y)}, {(x + 1, y)}, {(x, y + 1)}, {(x + 1, y + 1)}.";
                            yield return new BooleanExample(board, det, moveIndex);
                            yield break;
                        }
                    }
                }
            }
        }
    }


    /// <summary>
    /// "Social Distancing" - All pieces are at least 2 squares apart from each other (manhattan distance >= 2)
    /// Requires at least 6 pieces on the board.
    ///
    /// Manhattan distance between two positions (x1,y1) and (x2,y2) is |x1-x2| + |y1-y2|.
    /// 
    /// If at any point all pieces on the board (≥6) have pairwise distances ≥2, we yield a result.
    /// </summary>
    public class SocialDistancingEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(SocialDistancingEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();
            for (int ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();

                var piecesPositions = GetAllPiecesAndPositions(board).ToList();
                if (piecesPositions.Count < 6)
                {
                    continue;
                }

                bool allDistanced = true;
                for (int i = 0; i < piecesPositions.Count && allDistanced; i++)
                {
                    for (int j = i + 1; j < piecesPositions.Count && allDistanced; j++)
                    {
                        var posA = piecesPositions[i].Item2;
                        var posB = piecesPositions[j].Item2;
                        int distance = Math.Abs(posA.X - posB.X) + Math.Abs(posA.Y - posB.Y);
                        if (distance < 2)
                        {
                            // Found a pair too close
                            allDistanced = false;
                        }
                    }
                }

                if (allDistanced)
                {
                    var det = $"Social Distancing: All {piecesPositions.Count} pieces are at least 2 squares apart.";
                    yield return new BooleanExample(board, det, ii);
                    // After yielding once, we can break or continue. Usually we break since we found an occurrence.
                    break;
                }
            }
        }
    }

    public class LargestNxNSquareInAnyPositionInAnyGame : INumericalEvaluator
    {
        public string Name => nameof(LargestNxNSquareInAnyPositionInAnyGame);
        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            //okay, optimizations are reasonable. if we've already found width 4, don't start in such a way that only 4 edge length or smaller things can be found, for example.

            //find largest square.
            // this will be hyper inefficient.
            short bestBoardWidth = 1;
            ChessBoard bestBoard = null;
            foreach (var board in boards)
            {
                board.Last();
                //Console.WriteLine(board.ToAscii());
                //okay stop trying to optimize this. just look at all the ++ directional squares of edge length w.
                for (short width = (short)(bestBoardWidth + 1); width < 8; width++)
                {
                    //okay since we're ++ checking, no need to ever start on the + side of the current best seen box
                    var gotThisWidth = false;

                    //these are just the starting points; so the most positive starting point we should do
                    //butts right up against the edge. if width is N, then that means we start at 8-width.
                    //For example: if we're checking width edges of 2 long, we should check up to 6,6, since such a
                    //square would go from say 6,2 to 8,4.
                    for (short xx = 0; xx <= 8 - width; xx++)
                    {
                        for (short yy = 0; yy <= 8 - width; yy++)
                        {
                            //Console.WriteLine($"BestSeen is: {bestBoardWidth}. Checking {xx},{yy} for width {width}");
                            var res = CheckBoxPositive(board, xx, yy, width);
                            if (res)
                            {
                                //Console.WriteLine($"Square size {width} fits at {xx},{yy}");
                                gotThisWidth = true;
                                bestBoardWidth = width;
                                bestBoard = board;
                                break;
                            }
                            //else
                            //{
                            //    Console.WriteLine($"Square size {width} NOT fit {xx},{yy}");
                            //}

                        }
                        if (gotThisWidth) { break; }
                    }
                    //if you got through all that and still didn't get this width, then just bail, too.
                    if (!gotThisWidth) { break; }
                }
            }
            var raw = 10 * bestBoardWidth;
            return new NumericalEvaluationResult(raw, $"Largest NxN square in any position in any position of this game was: {bestBoardWidth}.", new List<NumericalExample>() { new NumericalExample(bestBoard, $"Largest NxN square in any position in any position of this game was: {bestBoardWidth}.", bestBoard.ExecutedMoves.Count - 1, bestBoardWidth) });

        }

        private static bool CheckBoxPositive(ChessBoard board, short xx, short yy, short w)
        {
            for (short xg = 0; xg < w; xg++)
            {
                for (short yg = 0; yg < w; yg++)
                {
                    if ((xx + xg) > 7 || (yy + yg) > 7)
                    {
                        throw new Exception("Should not be.");
                    }
                    var p = board[new Position((short)(xx + xg), (short)(yy + yg))];
                    if (p != null) { return false; }
                }
            }
            return true;
        }
    }

    /// <summary>
    /// This is actually really rare.
    /// </summary>
    public class AllFourCornersOccupiedByNonRooksEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(AllFourCornersOccupiedByNonRooksEvaluator);
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();
            var corners = new List<Position>() { new Position(0, 0), new Position(0, 7), new Position(7, 0), new Position(7, 7) };
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();
                var bad = false;
                var sawCt = 0;
                foreach (var pos in corners)
                {
                    var p = board[pos];
                    if (p == null)
                    {
                        bad = true;
                        break;
                    }
                    if (p.Type == PieceType.Rook)
                    {
                        bad = true;
                        break;
                    }
                    sawCt++;
                }
                if (bad)
                {
                    continue;
                }
                var det = $"All four corners occupied by non-rooks.";
                yield return new BooleanExample(board, det, ii);
                break;
            }
        }
    }

    public class SinglePieceCaptures19PointsOfMaterialInAGame : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(SinglePieceCaptures19PointsOfMaterialInAGame);
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            var pieceId2totalMaterialCaptured = new Dictionary<int, int>();
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];

                if (move.CapturedPiece != null)
                {
                    var capturer = move.Piece.Id;
                    if (!pieceId2totalMaterialCaptured.ContainsKey(capturer))
                    {
                        pieceId2totalMaterialCaptured[capturer] = 0;
                    }
                    var cap = move.CapturedPiece;
                    var val = 0;
                    if (cap.Type == PieceType.Pawn) { val = 1; }
                    else if (cap.Type == PieceType.Knight) { val = 3; }
                    else if (cap.Type == PieceType.Bishop) { val = 3; }
                    else if (cap.Type == PieceType.Rook) { val = 5; }
                    else if (cap.Type == PieceType.Queen) { val = 9; }
                    else
                    {
                        throw new Exception("BDS");
                    }
                    pieceId2totalMaterialCaptured[capturer] += val;
                    foreach (var k in pieceId2totalMaterialCaptured.Keys)
                    {

                        if (pieceId2totalMaterialCaptured[k] >= 19)
                        {
                            var det = $"At this move, a certain piece actually captured 19 points of material (or more, :{pieceId2totalMaterialCaptured[k]}";
                            yield return new BooleanExample(board, det, ii);
                        }
                    }
                }
            }

        }
    }

    public class TwoBishopsVsTwoKnightsEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(TwoBishopsVsTwoKnightsEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();
                var pieces = Statics.GetAllPieces(board);

                var bishops = pieces.Where(el => el.Type == PieceType.Bishop);
                if (bishops.Count() != 2)
                {
                    continue;
                }
                var knights = pieces.Where(el => el.Type == PieceType.Knight);
                if (knights.Count() != 2)
                {
                    continue;
                }
                if (bishops.First().Color != bishops.Skip(1).First().Color)
                {
                    continue;
                }
                if (knights.First().Color != knights.Skip(1).First().Color)
                {
                    continue;
                }
                if (knights.First().Color == bishops.First().Color)
                {
                    continue;
                }

                var det = $"Bishops Vs Knight war initiated.";
                yield return new BooleanExample(board, det, ii);
                break;
            }
        }
    }

    public class FullFileEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(FullFileEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();
                for (short xx = 0; xx < 8; xx++)
                {
                    var bad = false;
                    for (short yy = 0; yy < 8; yy++)
                    {
                        if (board[new Position(xx, yy)] == null)
                        {
                            bad = true;
                            break;
                        }
                    }
                    if (bad)
                    {
                        continue;
                    }
                    var det = $"";
                    yield return new BooleanExample(board, det, ii);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// definitely not debugged.
    /// </summary>
    public class HomecomingPiecesTenPercentEachEvaluator : INumericalEvaluator
    {
        public string Name => nameof(HomecomingPiecesTenPercentEachEvaluator);

        public NumericalEvaluationResult Evaluate(IEnumerable<ChessBoard> boards)
        {
            //we have to calculate the pieces
            //which are on their home squares,
            //minus the pieces who moved. Then give 10% for each.
            var homecomingPieces = 0;
            var bestPerboard = 0;
            ChessBoard bestBoard = null;

            foreach (var board in boards)
            {
                board.GoToStartingPosition();
                board.Last();
                var thisHomecomingCount = 0;

                var moved = new Dictionary<int, bool>();
                for (var ii = 0; ii < 32; ii++)
                {
                    moved[ii] = false;
                }
                foreach (var move in board.ExecutedMoves)
                {
                    var pid = move.Piece.Id;
                    if (pid <= 0)
                    {
                        continue;
                        //good, we correctly no longer manage this piece as having moved.
                    }
                    moved[pid] = true;
                }

                //okay now go through and count the pieces that are home and moved.
                var id = 0;
                foreach (var yy in new List<short>() { 0, 1, 6, 7 })
                {
                    for (short xx = 0; xx < 8; xx++)
                    {
                        id++;
                        if (!moved[id - 1])
                        {
                            continue;
                        }
                        var piece = board[new Position(xx, yy)];
                        if (piece != null)
                        {
                            if (piece != null)
                            {
                                if (piece.Id == id - 1)
                                {
                                    homecomingPieces++;
                                    thisHomecomingCount++;
                                }
                            }
                        }
                    }
                }
                if (thisHomecomingCount > bestPerboard)
                {
                    bestPerboard = thisHomecomingCount;
                    bestBoard = board;
                }
            }

            var raw = homecomingPieces * 10;
            var det = $"Total homecoming pieces: {homecomingPieces}.";
            var examples = new List<NumericalExample>();
            if (bestBoard != null)
            {
                examples.Add(new NumericalExample(bestBoard, $"This game had {bestPerboard} pieces return to their starting squares at end.", bestBoard.ExecutedMoves.Count() - 1, bestPerboard));
            }

            return new NumericalEvaluationResult(raw, det, examples);
        }
    }

    public class KnightAndKingInCenterFourSquaresEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(KnightAndKingInCenterFourSquaresEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();
            var centerSquares = new List<Position>() { new Position(3, 3), new Position(3, 4), new Position(4, 3), new Position(4, 4) };
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();

                var hasKnight = false;
                var hasKing = false;
                foreach (var pos in centerSquares)
                {
                    var p = board[pos];
                    if (p == null) { continue; }
                    if (p.Type == PieceType.Knight)
                    {
                        hasKnight = true;
                    }
                    if (p.Type == PieceType.King)
                    {
                        hasKing = true;
                    }
                }

                if (hasKing && hasKnight)
                {
                    var det = $"Knight and king in center.";
                    yield return new BooleanExample(board, det, ii);
                    break;
                }
            }
        }
    }

    public class BishopVsKnightEndgameReachedEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(BishopVsKnightEndgameReachedEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();
                var pieces = Statics.GetAllPieces(board);

                var bishops = pieces.Where(el => el.Type == PieceType.Bishop);
                if (bishops.Count() != 1)
                {
                    continue;
                }
                var knights = pieces.Where(el => el.Type == PieceType.Knight);
                if (knights.Count() != 1)
                {
                    continue;
                }
                if (bishops.First().Color == knights.First().Color)
                {
                    continue;
                }
                if (pieces.Where(el => el.Type == PieceType.Rook).Count() > 0)
                {
                    continue;
                }
                if (pieces.Where(el => el.Type == PieceType.Queen).Count() > 0)
                {
                    continue;
                }
                if (pieces.Where(el => el.Type == PieceType.Pawn).Count() == 0)
                {
                    continue;
                }

                var det = $"Bishop vs Knight endgame reached.";
                yield return new BooleanExample(board, det, ii);
                break;
            }
        }
    }


    public class FirstCaptureIsRook : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(FirstCaptureIsRook);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                if (move.CapturedPiece != null)
                {
                    if (move.CapturedPiece.Type == PieceType.Rook)
                    {
                        var det = $"";
                        yield return new BooleanExample(board, det, ii);
                    }
                    break;
                }
            }

        }
    }

    public class Down4PtsOrMoreMaterialButWins : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(Down4PtsOrMoreMaterialButWins);
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.Last();
            var mat = Statics.GetMaterialDifference(board);
            if ((mat >= 4 && board.EndGame.WonSide == PieceColor.Black) || (mat <= -4 && board.EndGame.WonSide == PieceColor.White))
            {
                yield return new BooleanExample(board, $"Down material by {mat} but wins.", board.ExecutedMoves.Count() - 1);
            }
        }
    }

    public class FirstCaptureIsQueen : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(FirstCaptureIsQueen);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                if (move.CapturedPiece != null)
                {
                    if (move.CapturedPiece.Type == PieceType.Queen)
                    {
                        var det = $"First piece captured was a queen.";
                        yield return new BooleanExample(board, det, ii);
                    }
                    break;
                }
            }

        }
    }


    public class KingReachesOpponentsFarSideOfBoardEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(KingReachesOpponentsFarSideOfBoardEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                if (move.Piece.Type == PieceType.King)
                {
                    if ((move.NewPosition.Y == 7 && ii % 2 == 0) || (move.NewPosition.Y == 0 && ii % 2 == 1))
                    {
                        var det = $"King reaches opposite side.";
                        yield return new BooleanExample(board, det, ii);
                        break;
                    }

                }
            }

        }
    }

    public class FullRankEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(FullRankEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            // Get starting position piece locations
            board.GoToStartingPosition();
            var originalPositions = new HashSet<string>();
            for (short x = 0; x < 8; x++)
            {
                for (short y = 0; y < 8; y++)
                {
                    var piece = board[new Position(x, y)];
                    if (piece != null)
                    {
                        originalPositions.Add($"{piece.Id}:{x},{y}");
                    }
                }
            }

            // Check each position in the game
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();

                // Check each rank
                for (short yy = 0; yy < 8; yy++)
                {
                    var piecesInOriginalPosition = 0;
                    var totalPieces = 0;
                    var bad = false;

                    // Check if rank is full
                    for (short xx = 0; xx < 8; xx++)
                    {
                        var piece = board[new Position(xx, yy)];
                        if (piece == null)
                        {
                            bad = true;
                            break;
                        }

                        totalPieces++;
                        // Check if piece is in its original position
                        if (originalPositions.Contains($"{piece.Id}:{xx},{yy}"))
                        {
                            piecesInOriginalPosition++;
                        }
                    }

                    // Skip if rank isn't full or too many pieces are in original positions
                    if (bad || totalPieces < 8 || piecesInOriginalPosition > 6 || (totalPieces - piecesInOriginalPosition) < 2)
                    {
                        continue;
                    }

                    var det = $"Rank {yy + 1} is full with {totalPieces - piecesInOriginalPosition} pieces moved from original squares";
                    yield return new BooleanExample(board, det, ii);
                    break;
                }
            }
        }
    }

    public class ThanosSnapEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(ThanosSnapEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();
                var pieces = GetAllPieces(board);

                var whitePieces = pieces.Where(p => p.Color == PieceColor.White);
                var blackPieces = pieces.Where(p => p.Color == PieceColor.Black);

                // Count each piece type for both sides
                var whitePawns = whitePieces.Count(p => p.Type == PieceType.Pawn);
                var whiteKnights = whitePieces.Count(p => p.Type == PieceType.Knight);
                var whiteBishops = whitePieces.Count(p => p.Type == PieceType.Bishop);
                var whiteRooks = whitePieces.Count(p => p.Type == PieceType.Rook);
                var whiteHasQueen = whitePieces.Any(p => p.Type == PieceType.Queen);

                var blackPawns = blackPieces.Count(p => p.Type == PieceType.Pawn);
                var blackKnights = blackPieces.Count(p => p.Type == PieceType.Knight);
                var blackBishops = blackPieces.Count(p => p.Type == PieceType.Bishop);
                var blackRooks = blackPieces.Count(p => p.Type == PieceType.Rook);
                var blackHasQueen = blackPieces.Any(p => p.Type == PieceType.Queen);
                //Console.WriteLine($"Pawns: {whitePawns},{blackPawns}, bishops:{whiteBishops},{blackBishops}, rooks:{whiteRooks},{blackRooks}, knights:{whiteKnights},{blackKnights}, queens:{whiteHasQueen},{blackHasQueen}");
                // Check if both sides have exactly half their army
                if (whitePawns == 4 && whiteKnights == 1 && whiteBishops == 1 && whiteRooks == 1 &&
                    blackPawns == 4 && blackKnights == 1 && blackBishops == 1 && blackRooks == 1 &&
                    whiteHasQueen == blackHasQueen)
                {
                    var queenStatus = whiteHasQueen ? "both sides kept their queens" : "neither side has a queen";
                    var det = $"Perfectly balanced: both sides have exactly half their army (4 pawns, 1 knight, 1 bishop, 1 rook), and {queenStatus}.";
                    yield return new BooleanExample(board, det, ii);
                    break;
                }
            }
        }
    }

    public class CheckmateWithAPawnEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(CheckmateWithAPawnEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {

            //todo tomorrow: convert this to full iteration over a precalculated set of full board and available moves.
            //it's not good to recalculate the entire thing.
            //that way this will turn out to be like:
            //foreach (var bp in board.all_positions) { 

            //}

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.IsMate && move.Piece.Type == PieceType.Pawn)
                {
                    if (!move.IsCheck)
                    {
                        //yess you can be "mated" without being in check. (i.e. stalemate!!?)
                        //dumb library.
                        //Console.WriteLine("Error: mate without check detected. HMMMM");
                        continue;
                    }
                    if (move.Parameter != null && move.Parameter.ShortStr.Substring(0, 1) == "=")
                    {
                        //this system counts promotions as pawn moves.
                        //note: this means they also count promotion mates as NOT by the promoted piece!
                        //Console.WriteLine("Error: pawn mate where the pawn promoted first detected. HMM LIBRARY>?");
                        continue;
                    }
                    var det = $"";
                    yield return new BooleanExample(board, det, ii);
                }
            }
        }
    }

    public class NoCapturesBeforeMove30Evaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(NoCapturesBeforeMove30Evaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            var limitPlies = 60;

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (ii <= limitPlies && move.CapturedPiece != null)
                {
                    break;
                }
                if (ii > limitPlies)
                {
                    var det = $"Game with no captures through move {Statics.MakeNormalMoveNumberDescriptor(limitPlies)};";
                    yield return new BooleanExample(board, det, ii);
                    break;
                }
            }
        }
    }

    public class NoCapturesBeforeMove20Evaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(NoCapturesBeforeMove20Evaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            var limitPlies = 40;

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (ii <= limitPlies && move.CapturedPiece != null)
                {
                    break;
                }
                if (ii > limitPlies)
                {
                    var det = $"Game with no captures through move {Statics.MakeNormalMoveNumberDescriptor(limitPlies)};";
                    yield return new BooleanExample(board, det, ii);
                    break;
                }
            }
        }
    }

    public class NoCapturesBeforeMove10Evaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(NoCapturesBeforeMove10Evaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            var limitPlies = 20;

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (ii <= limitPlies && move.CapturedPiece != null)
                {
                    break;
                }
                if (ii > limitPlies)
                {
                    var det = $"Game with no captures through move {Statics.MakeNormalMoveNumberDescriptor(limitPlies)};";
                    yield return new BooleanExample(board, det, ii);
                    break;
                }
            }
        }
    }
    public class NonQueenPromotionEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(NonQueenPromotionEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)

        {

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.Parameter != null)
                {
                    if (move.Parameter.ShortStr.Substring(0, 1) == "=")
                    {
                        if (move.Parameter.ShortStr != "=Q")
                        {
                            var det = $"Promoted to {move.Parameter.ShortStr}";
                            yield return new BooleanExample(board, det, ii);
                        }
                    }
                }
            }
        }
    }

    public class EverQvsRREndgameEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(EverQvsRREndgameEvaluator);

        /// <summary>
        /// okay there has got to be a better way.
        /// </summary>
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {

            var wq = 1;
            var bq = 1;
            var wr = 2;
            var br = 2;

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.Parameter != null)
                {
                    var s = move.Parameter.ShortStr;
                    if (s == "=Q")
                    {
                        if (ii % 2 == 0)
                        {
                            wq++;
                        }
                        else
                        {
                            bq++;
                        }
                    }
                    if (s == "=R")
                    {
                        if (ii % 2 == 0)
                        {
                            wr++;
                        }
                        else
                        {
                            br++;
                        }
                    }
                }

                if (move.CapturedPiece != null)
                {
                    if (move.CapturedPiece.Type == PieceType.Rook)
                    {
                        if (ii % 2 == 0)
                        {
                            wr--;
                        }
                        else
                        {
                            br--;
                        }
                    }
                    if (move.CapturedPiece.Type == PieceType.Queen)
                    {
                        if (ii % 2 == 0)
                        {
                            wq--;
                        }
                        else
                        {
                            bq--;
                        }
                    }
                }

                if ((wr == 2 && wq == 0 && bq == 1 && br == 0) || (br == 2 && bq == 0 && wq == 1 && wr == 0))
                {
                    var det = $"has RR vs Q";
                    yield return new BooleanExample(board, det, ii);
                    break;
                }
            }
        }
    }

    public class BishopMovesSevenSquaresEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(BishopMovesSevenSquaresEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.Piece.Type == PieceType.Bishop)
                {
                    var gap = Math.Abs(move.OriginalPosition.X - move.NewPosition.X);
                    if (gap >= 7)
                    {
                        var det = $"Moved 7 squares via {move.San}, from {move.OriginalPosition} to {move.NewPosition}";
                        yield return new BooleanExample(board, det, ii);
                        break;
                    }
                }
            }
        }
    }

    public class BishopMovesSixSquaresEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(BishopMovesSixSquaresEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.Piece.Type == PieceType.Bishop)
                {
                    var gap = Math.Abs(move.OriginalPosition.X - move.NewPosition.X);
                    if (gap >= 6)
                    {
                        var det = $"Moved 6 squares via {move.San}, from {move.OriginalPosition} to {move.NewPosition}";
                        yield return new BooleanExample(board, det, ii);
                        break;
                    }
                }
            }
        }
    }

    public class CastleAfterMove40Evaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(CastleAfterMove40Evaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                var normalMoveNumber = ii / 2 + 1;
                if (normalMoveNumber <= 40)
                {
                    continue;
                }
                if (move.Parameter != null)
                {
                    var ss = move.Parameter.ShortStr;
                    if (ss == "O-O" || ss == "O-O-O")
                    {
                        var det = $"";
                        yield return new BooleanExample(board, det, ii);
                    }
                }
            }
        }
    }

    public class CastleWithCheckEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(CastleWithCheckEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.Parameter != null)
                {
                    var ss = move.Parameter.ShortStr;
                    if (ss == "O-O" || ss == "O-O-O")
                    {
                        var det = $"";
                        if (move.IsCheck)
                        {
                            yield return new BooleanExample(board, det, ii);
                        }
                    }
                }
            }
        }
    }
    public class CastleAfterMove20Evaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(CastleAfterMove20Evaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                var normalMoveNumber = ii / 2 + 1;
                if (normalMoveNumber <= 20)
                {
                    continue;
                }
                if (move.Parameter != null)
                {
                    var ss = move.Parameter.ShortStr;
                    if (ss == "O-O" || ss == "O-O-O")
                    {
                        var det = $"";
                        yield return new BooleanExample(board, det, ii);
                    }
                }
            }
        }
    }

    public class EnPassantRefusedEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(EnPassantRefusedEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count - 1; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();

                // Check if this was a pawn moving two squares
                if (move.Piece.Type != PieceType.Pawn ||
                    Math.Abs(move.OriginalPosition.Y - move.NewPosition.Y) != 2)
                {
                    continue;
                }

                // Check for enemy pawns in position to capture en passant
                var capturingColor = move.Piece.Color == PieceColor.White ? PieceColor.Black : PieceColor.White;
                var possibleCapturers = new List<Position>();

                // Check both adjacent files
                if (move.NewPosition.X > 0)
                {
                    possibleCapturers.Add(new Position((short)(move.NewPosition.X - 1), move.NewPosition.Y));
                }
                if (move.NewPosition.X < 7)
                {
                    possibleCapturers.Add(new Position((short)(move.NewPosition.X + 1), move.NewPosition.Y));
                }

                // Look for enemy pawns in those positions
                foreach (var pos in possibleCapturers)
                {
                    var piece = board[pos];
                    if (piece?.Type == PieceType.Pawn && piece.Color == capturingColor)
                    {
                        // There was a pawn that could have captured en passant
                        // Check if the next move was NOT en passant
                        var nextMove = board.ExecutedMoves[ii + 1];
                        if (nextMove.Parameter?.ShortStr != "e.p.")
                        {
                            var det = $"En passant refused: {piece.Color} pawn at {pos} could have captured {move.Piece.Color} pawn via e.p. but played {nextMove.San} instead";
                            yield return new BooleanExample(board, det, ii + 1);
                        }
                        break;
                    }
                }
            }
        }
    }

    public class EnPassantDoneInGameEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(EnPassantDoneInGameEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 1; ii < board.ExecutedMoves.Count; ii++) // Start at 1 to check previous move
            {
                var move = board.ExecutedMoves[ii];
                var previousMove = board.ExecutedMoves[ii - 1];
                board.Next();

                // Skip if not a pawn capture
                if (move.Piece.Type != PieceType.Pawn || move.CapturedPiece == null)
                {
                    continue;
                }

                // Check if this was a diagonal pawn capture
                if (move.OriginalPosition.X != move.NewPosition.X && // Moved diagonally
                    move.CapturedPiece.Type == PieceType.Pawn &&    // Captured a pawn
                    Math.Abs(move.OriginalPosition.Y - move.NewPosition.Y) == 1) // Moved one rank
                {
                    // Verify previous move was a pawn moving two squares
                    if (previousMove.Piece.Type == PieceType.Pawn &&
                        Math.Abs(previousMove.OriginalPosition.Y - previousMove.NewPosition.Y) == 2 &&
                        previousMove.NewPosition.X == move.NewPosition.X) // The captured pawn should be on same file as destination
                    {
                        // For white's en passant (moving up)
                        if (move.Piece.Color == PieceColor.White &&
                            move.NewPosition.Y == previousMove.NewPosition.Y + 1 &&
                            previousMove.NewPosition.Y == 4)
                        {
                            var det = $"En passant capture performed via {move.San}";
                            yield return new BooleanExample(board, det, ii);
                            break;
                        }
                        // For black's en passant (moving down)
                        else if (move.Piece.Color == PieceColor.Black &&
                                move.NewPosition.Y == previousMove.NewPosition.Y - 1 &&
                                previousMove.NewPosition.Y == 3)
                        {
                            var det = $"En passant capture performed via {move.San}";
                            yield return new BooleanExample(board, det, ii);
                            break;
                        }
                    }
                }
            }
        }
    }

    public class PromoteAndCheckEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(PromoteAndCheckEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.Parameter != null)
                {
                    if (move.Parameter.ShortStr.Substring(0, 1) == "=")
                    {
                        if (move.IsCheck)
                        {
                            var det = $"Promoted: {move.Parameter.ShortStr} and Check.";
                            yield return new BooleanExample(board, det, ii);
                        }
                    }
                }
            }
        }
    }

    public class AnyPawnPromotionEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(AnyPawnPromotionEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.Parameter != null)
                {
                    var l = move.Parameter.ShortStr;
                    if (l == "=" || l == "=Q" || l == "=R" || l == "=B" || l == "=N")
                    {
                        var det = $"Promotion: {move.Parameter.ShortStr}";
                        yield return new BooleanExample(board, det, ii);
                        break;
                    }
                }
            }
        }
    }

    public class PawnPromotionAndMateEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(PawnPromotionAndMateEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.Parameter != null)
                {
                    var l = move.Parameter.ShortStr;
                    if (l == "=" || l == "=Q" || l == "=R" || l == "=B" || l == "=N")
                    {
                        if (move.IsMate && move.IsCheck)
                        {
                            //Also we have to check that this new piece attacks the king?
                            var det = $"Pawn promoted with mate (not necessarily the pawn actually mating)";
                            yield return new BooleanExample(board, det, ii);
                            break;
                        }
                    }
                }
            }
        }
    }

    public class PawnUnderpromotionAndMateEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(PawnPromotionAndMateEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.Parameter != null)
                {
                    var l = move.Parameter.ShortStr;
                    if (l == "=R" || l == "=B" || l == "=N")
                    {
                        if (move.IsMate && move.IsCheck)
                        {
                            //Also we have to check that this new piece attacks the king?
                            var det = $"Pawn underpromoted with mate (not necessarily the pawn actually mating)";
                            yield return new BooleanExample(board, det, ii);
                            break;
                        }
                    }
                }
            }
        }
    }

    public class TwoPawnPromotionsInOneGameEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(TwoPawnPromotionsInOneGameEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            var pawnPromotionsThisGame = 0;

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.Parameter != null)
                {
                    var l = move.Parameter.ShortStr;
                    if (l == "=" || l == "=Q" || l == "=R" || l == "=B" || l == "=N")
                    {
                        pawnPromotionsThisGame++;
                    }
                }

                if (pawnPromotionsThisGame >= 2)
                {
                    var det = $"Game with >=2 promotions: there were {pawnPromotionsThisGame}";
                    yield return new BooleanExample(board, det, ii);
                }
            }
        }
    }

    public class SamePieceMovesEightTimesInARowEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(SamePieceMovesEightTimesInARowEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (ii < 14)
                {
                    continue;
                }
                var checkedCount = 0;
                while (true)
                {
                    var move1 = board.ExecutedMoves[ii - checkedCount * 2];
                    var move2 = board.ExecutedMoves[ii - checkedCount * 2 - 2];
                    if (move1.Piece.Type != move2.Piece.Type) { break; }
                    if (move1.OriginalPosition != move2.NewPosition) { break; }
                    checkedCount++;
                    //so, we know the piece moved here 2 plys ago, is the same piece
                    if (checkedCount == 7) //7 comparisons succeeding means 8 in a row.
                    {
                        var joined = string.Join(", ", board.ExecutedMoves.Select(el => el.San).Take(ii));
                        var det = $"{move.Piece} moved 8 times in a row: {joined}";
                        yield return new BooleanExample(board, det, ii);
                        break;
                    }
                }
            }
        }
    }

    public class AnyOppositeSideCastlingGameEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(AnyOppositeSideCastlingGameEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            var hasShort = false;
            var hasLong = false;

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();

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

                if (hasShort && hasLong)
                {
                    var det = $"Opposite Side Castled";
                    yield return new BooleanExample(board, det, ii);
                    break;
                }
            }
        }
    }

    public class BishopManualUndoEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(BishopManualUndoEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (ii <= 2)
                {
                    continue;
                }
                var priorMove = board.ExecutedMoves[ii - 2];
                if (priorMove.CapturedPiece != null)
                {
                    continue;
                }
                //also exclude like 1) move B, 2) opponent moves piece to where you just left 3) you return and kill it.  Just doesn't seem like a repeat at all.
                //we're looking for direct, nearly provably undos (which still might be good, if zugzwang)
                if (move.CapturedPiece != null)
                {
                    continue;
                }
                if (move.Piece.Type == PieceType.Bishop && priorMove.Piece.Type == PieceType.Bishop)
                {
                    if (move.NewPosition == priorMove.OriginalPosition)
                    {
                        var det = $"Bishop moved back to: {priorMove.OriginalPosition} from {priorMove.NewPosition}";
                        yield return new BooleanExample(board, det, ii);
                    }
                }
            }
        }
    }

    public class RookTakesAQueenEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(RookTakesAQueenEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.CapturedPiece != null && move.CapturedPiece.Type == PieceType.Queen && move.Piece.Type == PieceType.Rook)
                {
                    var det = $"";
                    yield return new BooleanExample(board, det, ii);
                }
            }
        }
    }

    public class PawnTakesAQueenEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(PawnTakesAQueenEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();
                if (move.CapturedPiece != null && move.CapturedPiece.Type == PieceType.Queen && move.Piece.Type == PieceType.Pawn)
                {
                    var det = $"";
                    yield return new BooleanExample(board, det, ii);
                }
            }
        }
    }

    public class BothKingsWrongSideEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(BothKingsWrongSideEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();
                var bad = false;
                foreach (var el in GetAllPiecesAndPositions(board).Where(el => el.Item1.Type == PieceType.King))
                {
                    var piece = el.Item1;
                    var pos = el.Item2;
                    if (piece.Color == PieceColor.White && pos.Y < 4)
                    {
                        bad = true;
                    }
                    if (piece.Color == PieceColor.Black && pos.Y >= 4)
                    {
                        bad = true;
                    }

                }
                if (bad)
                {
                    continue;
                }

                yield return new BooleanExample(board, "Both kings on wrong side.", ii);
                break;
            }
        }
    }

    public class QueenInACornerEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(QueenInACornerEvaluator);
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();
                var bad = false;
                foreach (var el in GetAllPiecesAndPositions(board).Where(el => el.Item1.Type == PieceType.Queen))
                {
                    var piece = el.Item1;
                    var pos = el.Item2;
                    if ((pos.Y == 0 || pos.Y == 7) && (pos.X == 0 || pos.X == 7))
                    {
                        yield return new BooleanExample(board, "Queen in a corner", ii);
                        bad = true;
                        break;
                    }
                }
                if (bad) { break; }
            }
        }
    }

    public class KingInACornerEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(KingInACornerEvaluator);
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var bad = false;
                board.Next();
                foreach (var el in GetAllPiecesAndPositions(board).Where(el => el.Item1.Type == PieceType.King))
                {
                    var piece = el.Item1;
                    var pos = el.Item2;
                    if ((pos.Y == 0 || pos.Y == 7) && (pos.X == 0 || pos.X == 7))
                    {
                        yield return new BooleanExample(board, "King in a corner", ii);
                        bad = true;
                        break;
                    }
                }
                if (bad) { break; }
            }
        }
    }

    public class BishopInACornerEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(BishopInACornerEvaluator);
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var bad = false;
                board.Next();
                foreach (var el in GetAllPiecesAndPositions(board).Where(el => el.Item1.Type == PieceType.Bishop))
                {
                    var piece = el.Item1;
                    var pos = el.Item2;
                    if ((pos.Y == 0 || pos.Y == 7) && (pos.X == 0 || pos.X == 7))
                    {
                        yield return new BooleanExample(board, "Bishop in corner", ii);
                        bad = true;
                        break;
                    }
                }
                if (bad) { break; }
            }
        }
    }

    public class KnightInACornerEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(KnightInACornerEvaluator);
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var bad = false;
                board.Next();
                foreach (var el in GetAllPiecesAndPositions(board).Where(el => el.Item1.Type == PieceType.Knight))
                {
                    var piece = el.Item1;
                    var pos = el.Item2;
                    if ((pos.Y == 0 || pos.Y == 7) && (pos.X == 0 || pos.X == 7))
                    {
                        yield return new BooleanExample(board, "Knight in corner", ii);
                        bad = true;
                        break;
                    }
                }
                if (bad) { break; }
            }
        }
    }
    public class AllPiecesSameColorSquareWithAtLeast7Evaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(AllPiecesSameColorSquareWithAtLeast7Evaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();
            for (int ii = 0; ii <= board.ExecutedMoves.Count; ii++)
            {
                if (ii > 0)
                {
                    board.Next();
                }

                var piecesAndPositions = GetAllPiecesAndPositions(board).ToList();
                if (piecesAndPositions.Count < 7) continue; // Need at least 7 pieces

                // Determine the required color based on the first piece's square
                var firstPos = piecesAndPositions[0].Item2;
                bool firstColor = ((firstPos.X + firstPos.Y) % 2 == 0);

                bool allSameColor = true;
                foreach (var pp in piecesAndPositions)
                {
                    var pos = pp.Item2;
                    var squareColor = ((pos.X + pos.Y) % 2 == 0);
                    if (squareColor != firstColor)
                    {
                        allSameColor = false;
                        break;
                    }
                }

                if (allSameColor)
                {
                    var colorStr = firstColor ? "light" : "dark";
                    var details = $"All {piecesAndPositions.Count} pieces are on {colorStr} squares at move {ii}.";
                    yield return new BooleanExample(board, details, Math.Max(0, ii - 1));
                    yield break;
                }
            }
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

    public class ThreeByThreeFullEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(ThreeByThreeFullEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();

            for (int ii = 0; ii <= board.ExecutedMoves.Count; ii++)
            {
                if (ii > 0)
                {
                    board.Next();
                }

                // Check all possible 3x3 squares.
                // 3x3 squares can start at (x,y) from 0 to 5 (0-based) because:
                // top-left corner of a 3x3 block at max is (5,5)
                // so that (5+2=7 and 5+2=7 for max indexes)
                for (int startX = 0; startX <= 5; startX++)
                {
                    for (int startY = 0; startY <= 5; startY++)
                    {
                        if (Is3x3Full(board, startX, startY))
                        {
                            var details = $"A 3x3 square full of pieces found at move {ii}, top-left corner at ({startX},{startY}).";
                            yield return new BooleanExample(board, details, Math.Max(0, ii - 1));
                            yield break;
                        }
                    }
                }
            }
        }

        private bool Is3x3Full(ChessBoard board, int startX, int startY)
        {
            for (int x = startX; x < startX + 3; x++)
            {
                for (int y = startY; y < startY + 3; y++)
                {
                    if (board[new Position((short)x, (short)y)] == null)
                        return false;
                }
            }
            return true;
        }
    }


    public class FourPawnsMoreThanOpponentEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(FourPawnsMoreThanOpponentEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();

            for (int ii = 0; ii <= board.ExecutedMoves.Count; ii++)
            {
                if (ii > 0)
                {
                    board.Next();
                }

                var pieces = GetAllPieces(board);
                int whitePawns = pieces.Count(p => p.Type == PieceType.Pawn && p.Color == PieceColor.White);
                int blackPawns = pieces.Count(p => p.Type == PieceType.Pawn && p.Color == PieceColor.Black);

                int diff = whitePawns - blackPawns;
                if (Math.Abs(diff) >= 4)
                {
                    var sideAhead = diff > 0 ? "White" : "Black";
                    var details = $"{sideAhead} is ahead by {Math.Abs(diff)} pawns at move {ii}.";
                    yield return new BooleanExample(board, details, Math.Max(0, ii - 1));
                    yield break;
                }
            }
        }

        private IEnumerable<Piece> GetAllPieces(ChessBoard board)
        {
            for (short x = 0; x < 8; x++)
            {
                for (short y = 0; y < 8; y++)
                {
                    var p = board[new Position(x, y)];
                    if (p != null) yield return p;
                }
            }
        }
    }

    public class TwoKnightsOnSideEdgeEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(TwoKnightsOnSideEdgeEvaluator);
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();
                var ct = 0;
                var bad = false;
                var allPieces = GetAllPiecesAndPositions(board);
                foreach (var el in allPieces.Where(el => el.Item1.Type == PieceType.Knight))
                {
                    var piece = el.Item1;
                    var pos = el.Item2;
                    if (pos.X == 0 || pos.X == 7)
                    {
                        ct++;
                    }
                    if (ct >= 2)
                    {
                        yield return new BooleanExample(board, "Two knights are on a side edge.", ii);
                        bad = true;
                        break;
                    }
                }
                if (bad) { break; }
            }
        }
    }

    public class AllPiecesOnSameBoardColorWithAtLeastSevenTotal : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(AllPiecesOnSameBoardColorWithAtLeastSevenTotal);
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();
                var bad = false;
                var allPieces = GetAllPiecesAndPositions(board);
                if (allPieces.Count() < 7) { continue; }
                var requiredSquareColor = (allPieces.First().Item2.X + allPieces.First().Item2.Y) % 2;
                foreach (var el in allPieces)
                {
                    var mySquareColor = (el.Item2.X + el.Item2.Y) % 2;
                    if (mySquareColor != requiredSquareColor)
                    {
                        bad = true;
                        break;
                    }
                }
                if (bad) { continue; }
                yield return new BooleanExample(board, "All pieces were on the same color.", ii);
                break;
            }
        }
    }

    public class RingOfFireEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(RingOfFireEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();

                // Find kings that aren't on edges
                var kings = GetAllPiecesAndPositions(board)
                    .Where(el => el.Item1.Type == PieceType.King)
                    .Where(el => el.Item2.X > 0 && el.Item2.X < 7 && el.Item2.Y > 0 && el.Item2.Y < 7);

                foreach (var kingData in kings)
                {
                    var kingPos = kingData.Item2;
                    var surroundingSquares = new List<Position>
                    {
                        new Position((short)(kingPos.X-1), (short)(kingPos.Y-1)),
                        new Position((short)(kingPos.X-1), (short)(kingPos.Y)),
                        new Position((short)(kingPos.X-1), (short)(kingPos.Y+1)),
                        new Position((short)(kingPos.X), (short)(kingPos.Y-1)),
                        new Position((short)(kingPos.X), (short)(kingPos.Y+1)),
                        new Position((short)(kingPos.X+1), (short)(kingPos.Y-1)),
                        new Position((short)(kingPos.X+1), (short)(kingPos.Y)),
                        new Position((short)(kingPos.X+1), (short)(kingPos.Y+1)),
                        kingPos // Include king's own square
                    };

                    // Check if all surrounding squares are occupied
                    var allOccupied = surroundingSquares.All(pos => board[pos] != null);

                    if (allOccupied)
                    {
                        var color = kingData.Item1.Color;
                        var det = $"{color} king surrounded by a ring of pieces at {kingPos}";
                        yield return new BooleanExample(board, det, ii);
                        break;
                    }
                }
            }
        }
    }

    public class NakedKingEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(NakedKingEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();

                // Find kings that aren't on edges
                var kings = GetAllPiecesAndPositions(board)
                    .Where(el => el.Item1.Type == PieceType.King)
                    .Where(el => el.Item2.X > 0 && el.Item2.X < 7 && el.Item2.Y > 0 && el.Item2.Y < 7);

                foreach (var kingData in kings)
                {
                    var kingPos = kingData.Item2;
                    var surroundingSquares = new List<Position>
                    {
                        new Position((short)(kingPos.X-1), (short)(kingPos.Y-1)),
                        new Position((short)(kingPos.X-1), (short)(kingPos.Y)),
                        new Position((short)(kingPos.X-1), (short)(kingPos.Y+1)),
                        new Position((short)(kingPos.X), (short)(kingPos.Y-1)),
                        new Position((short)(kingPos.X), (short)(kingPos.Y+1)),
                        new Position((short)(kingPos.X+1), (short)(kingPos.Y-1)),
                        new Position((short)(kingPos.X+1), (short)(kingPos.Y)),
                        new Position((short)(kingPos.X+1), (short)(kingPos.Y+1))
                    };

                    // Check if all surrounding squares are occupied
                    var noneOccupied = !surroundingSquares.Any(pos => board[pos] != null);

                    if (noneOccupied)
                    {
                        var color = kingData.Item1.Color;
                        var det = $"{color} king is completely alone at {kingPos}";
                        yield return new BooleanExample(board, det, ii);
                        break;
                    }
                }
            }
        }
    }

    public class ChainReactionCastlingEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(ChainReactionCastlingEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 1; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                var previousMove = board.ExecutedMoves[ii - 1];
                board.Next();

                // Check if both current and previous moves are castling
                if (move.Parameter != null && previousMove.Parameter != null)
                {
                    var currentCastle = move.Parameter.ShortStr;
                    var previousCastle = previousMove.Parameter.ShortStr;

                    if ((currentCastle == "O-O" || currentCastle == "O-O-O") &&
                        (previousCastle == "O-O" || previousCastle == "O-O-O"))
                    {
                        var det = $"Chain reaction castling: {previousCastle} followed by {currentCastle}";
                        yield return new BooleanExample(board, det, ii);
                        break;
                    }
                }
            }
        }
    }

    public class RomeoAndJulietEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(RomeoAndJulietEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            // Go to start position and find original queen IDs
            board.GoToStartingPosition();
            var pieces = GetAllPiecesAndPositions(board);
            var whiteQueenId = pieces.First(p => p.Item1.Type == PieceType.Queen && p.Item1.Color == PieceColor.White).Item1.Id;
            var blackQueenId = pieces.First(p => p.Item1.Type == PieceType.Queen && p.Item1.Color == PieceColor.Black).Item1.Id;

            bool whiteQueenTakenByPawn = false;
            bool blackQueenTakenByPawn = false;

            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                if (move.CapturedPiece != null &&
                    move.CapturedPiece.Type == PieceType.Queen &&
                    move.Piece.Type == PieceType.Pawn)
                {
                    if (move.CapturedPiece.Id == whiteQueenId)
                    {
                        whiteQueenTakenByPawn = true;
                    }
                    else if (move.CapturedPiece.Id == blackQueenId)
                    {
                        blackQueenTakenByPawn = true;
                    }

                    if (whiteQueenTakenByPawn && blackQueenTakenByPawn)
                    {
                        var det = "Romeo & Juliet: Both original queens were captured by pawns";
                        yield return new BooleanExample(board, det, ii);
                        break;
                    }
                }
                board.Next();
            }
        }
    }

    public class FortKnoxEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(FortKnoxEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();

                var pieces = GetAllPiecesAndPositions(board);
                var whitePieces = pieces.Where(p => p.Item1.Color == PieceColor.White).ToList();
                var blackPieces = pieces.Where(p => p.Item1.Color == PieceColor.Black).ToList();

                // Check white pieces
                if (whitePieces.Count >= 4)
                {
                    var whiteKing = whitePieces.FirstOrDefault(p => p.Item1.Type == PieceType.King);
                    if (whiteKing != null && IsInFortKnox(board, whiteKing.Item2, whitePieces))
                    {
                        yield return new BooleanExample(board, "White has formed Fort Knox: all pieces within king's attack range", ii);
                        break;
                    }
                }

                // Check black pieces
                if (blackPieces.Count >= 4)
                {
                    var blackKing = blackPieces.FirstOrDefault(p => p.Item1.Type == PieceType.King);
                    if (blackKing != null && IsInFortKnox(board, blackKing.Item2, blackPieces))
                    {
                        yield return new BooleanExample(board, "Black has formed Fort Knox: all pieces within king's attack range", ii);
                        break;
                    }
                }
            }
        }

        private bool IsInFortKnox(ChessBoard board, Position kingPos, List<Tuple<Piece, Position>> pieces)
        {
            // Get all squares the king can attack (including diagonals)
            var kingAttackSquares = new List<Position>
            {
                new Position((short)(kingPos.X-1), (short)(kingPos.Y-1)),
                new Position((short)(kingPos.X-1), (short)(kingPos.Y)),
                new Position((short)(kingPos.X-1), (short)(kingPos.Y+1)),
                new Position((short)(kingPos.X), (short)(kingPos.Y-1)),
                new Position((short)(kingPos.X), (short)(kingPos.Y+1)),
                new Position((short)(kingPos.X+1), (short)(kingPos.Y-1)),
                new Position((short)(kingPos.X+1), (short)(kingPos.Y)),
                new Position((short)(kingPos.X+1), (short)(kingPos.Y+1)),
                kingPos // Include king's own square
            };

            // Filter out invalid positions (off board)
            kingAttackSquares = kingAttackSquares
                .Where(pos => pos.X >= 0 && pos.X < 8 && pos.Y >= 0 && pos.Y < 8)
                .ToList();

            // Check if all pieces are within king's attack range
            return pieces.All(piece => kingAttackSquares.Any(pos => pos.X == piece.Item2.X && pos.Y == piece.Item2.Y));
        }
    }

    /// <summary>
    /// "Identity Crisis" - 
    /// Condition: At some point in the game, all pieces currently on the board (≥ 8 pieces)
    /// must be on squares of the opposite color from their reference square.
    /// Reference square for a piece:
    ///   - For originally placed pieces: their reference color is the color of the square they started on.
    ///   - For promoted pieces: their reference color is the color of the promotion square.
    /// Opposite color means: if reference was a light (white) square, they must stand on a dark (black) square, and vice versa.
    /// A square's color is determined in the usual chessboard pattern:
    ///   - If (x + y) is even, it's usually a light square.
    ///   - If (x + y) is odd, it's usually a dark square.
    ///
    /// We only yield a result for the first time it occurs, if at all.
    /// </summary>
    //public class IdentityCrisisEvaluator2 : AbstractBooleanEvaluator, IBooleanEvaluator
    //{
    //    public string Name => nameof(IdentityCrisisEvaluator2);

    //    public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
    //    {
    //        // Step 1: Go to starting position and record each piece's initial square color.
    //        board.GoToStartingPosition();
    //        var pieceReferenceColor = new Dictionary<int, bool>();
    //        // true = light square (even sum), false = dark square (odd sum).
    //        // This will store the reference color for original pieces.

    //        foreach (var pieceAndPos in GetAllPiecesAndPositions(board))
    //        {
    //            var piece = pieceAndPos.Item1;
    //            var pos = pieceAndPos.Item2;
    //            pieceReferenceColor[piece.Id] = ((pos.X + pos.Y) % 2 == 0);
    //        }

    //        // Step 2: As we proceed through the game, we must handle promotions.
    //        // When a pawn promotes, the promoted piece effectively "restarts" its identity reference color
    //        // to the color of the promotion square. The old pawn ID is retained (the library presumably reuses the same ID).
    //        // We'll update that piece's reference color at promotion.

    //        // To track this properly, we'll play through the moves, updating when promotions occur, and after each move,
    //        // check if the "Identity Crisis" condition is met.

    //        board.GoToStartingPosition();
    //        for (var moveIndex = 0; moveIndex < board.ExecutedMoves.Count; moveIndex++)
    //        {
    //            board.Next();
    //            var move = board.ExecutedMoves[moveIndex];

    //            // Check if a promotion occurred. 
    //            // In this system, a promotion move has move.Parameter.ShortStr starting with "=".
    //            if (move.Parameter?.ShortStr.StartsWith("=") == true)
    //            {
    //                // The piece that just moved (a pawn that promoted) now becomes another piece (queen/rook/bishop/knight).
    //                // The reference color for this piece is now the color of the promotion square.
    //                bool promotionSquareColor = ((move.NewPosition.X + move.NewPosition.Y) % 2 == 0);
    //                pieceReferenceColor[move.Piece.Id] = promotionSquareColor;
    //            }

    //            // Now check the board for the condition:
    //            // 1. At least 8 pieces on the board.
    //            // 2. All pieces currently on the board are on the opposite color from their reference color.
    //            var currentPieces = GetAllPiecesAndPositions(board).ToList();
    //            if (currentPieces.Count < 8)
    //                continue;

    //            bool allOpposite = true;
    //            foreach (var pAndPos in currentPieces)
    //            {
    //                var p = pAndPos.Item1;
    //                var pos = pAndPos.Item2;
    //                bool currentSquareColor = ((pos.X + pos.Y) % 2 == 0);
    //                bool referenceColor = pieceReferenceColor[p.Id];

    //                // They must be on the opposite color
    //                if (currentSquareColor == referenceColor)
    //                {
    //                    allOpposite = false;
    //                    break;
    //                }
    //            }

    //            if (allOpposite)
    //            {
    //                var det = $"Identity Crisis: All {currentPieces.Count} pieces are on opposite-colored squares from their reference squares.";
    //                yield return new BooleanExample(board, det, moveIndex);
    //                break; // Found an occurrence, we can stop.
    //            }
    //        }

    //    }
    //}

    /// <summary>
    /// "A queen en prise for three moves in a row" 
    /// A single queen is capturable by the opponent on their upcoming move for 3 consecutive positions.
    /// </summary>
    public class QueenEnPriseThreeMovesEvaluator2 : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(QueenEnPriseThreeMovesEvaluator2);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();

            // Track consecutive en-prise counts per queen ID
            var consecutiveCounts = new Dictionary<int, int>();

            for (int moveIndex = 0; moveIndex < board.ExecutedMoves.Count; moveIndex++)
            {
                board.Next();

                // Determine which side is to move next
                // If moveIndex is even: White just moved, Black to move next.
                // If moveIndex is odd: Black just moved, White to move next.
                PieceColor sideToMove = (moveIndex % 2 == 0) ? PieceColor.Black : PieceColor.White;

                var pieces = GetAllPiecesAndPositions(board).ToList();
                var queens = pieces.Where(x => x.Item1.Type == PieceType.Queen).ToList();

                // Get all legal moves for sideToMove
                var moves = board.Moves();

                // Identify which queens are en prise (attackable) right now
                var attackedQueens = new HashSet<int>();
                foreach (var m in moves)
                {
                    if (m.CapturedPiece != null && m.CapturedPiece.Type == PieceType.Queen)
                    {
                        attackedQueens.Add(m.CapturedPiece.Id);
                    }
                }

                // Update the consecutive counts
                // For queens attacked now, increment or set to 1
                // For queens not attacked, reset their count to 0
                var queenIds = queens.Select(q => q.Item1.Id).ToHashSet();
                foreach (var qid in queenIds)
                {
                    if (attackedQueens.Contains(qid))
                    {
                        if (!consecutiveCounts.ContainsKey(qid))
                            consecutiveCounts[qid] = 0;
                        consecutiveCounts[qid] += 1;
                    }
                    else
                    {
                        // Not attacked, reset count
                        consecutiveCounts[qid] = 0;
                    }
                }

                // Check if any queen reached 3 consecutive en-prise counts
                foreach (var kvp in consecutiveCounts)
                {
                    if (kvp.Value >= 3)
                    {
                        var det = $"Queen (ID={kvp.Key}) has been en prise for 3 consecutive moves.";
                        yield return new BooleanExample(board, det, moveIndex);
                        yield break;
                    }
                }
            }
        }
    }

    public class EnPassantAcceptedEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(EnPassantAcceptedEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();

            int? lastDoublePushMoveIndex = null;

            for (int i = 0; i < board.ExecutedMoves.Count; i++)
            {
                board.Next();
                var move = board.ExecutedMoves[i];

                // If the previous move was a double-step pawn move and we are now at the next move:
                if (lastDoublePushMoveIndex.HasValue && i == lastDoublePushMoveIndex.Value + 1)
                {
                    // Go to the position immediately after the double-step move
                    board.GoToStartingPosition();
                    for (int k = 0; k <= lastDoublePushMoveIndex.Value; k++)
                        board.Next();

                    var legalMoves = board.Moves();
                    var enPassantMoves = legalMoves.Where(m => IsEnPassantCapture(board, m)).ToList();

                    // Now revert to the current move's position:
                    board.GoToStartingPosition();
                    for (int x = 0; x <= i; x++)
                        board.Next();

                    // If en passant was possible and the chosen move is indeed one of the en passant moves:
                    if (enPassantMoves.Count > 0 && enPassantMoves.Any(m => MovesAreSame(m, move)))
                    {
                        var det = "En passant accepted. After a double-step pawn advance, the opponent executed the en passant capture.";
                        yield return new BooleanExample(board, det, i);
                        yield break;
                    }

                    lastDoublePushMoveIndex = null;
                }

                // Check if current move is a two-square pawn advance
                if (move.Piece.Type == PieceType.Pawn)
                {
                    int distance = Math.Abs(move.NewPosition.Y - move.OriginalPosition.Y);
                    if (distance == 2)
                        lastDoublePushMoveIndex = i;
                    else
                        lastDoublePushMoveIndex = null;
                }
                else
                {
                    lastDoublePushMoveIndex = null;
                }
            }
        }

        private bool IsEnPassantCapture(ChessBoard board, Move move)
        {
            if (move.Piece.Type != PieceType.Pawn)
                return false;

            // Pawn captures must move diagonally by one file, one rank
            if (Math.Abs(move.NewPosition.X - move.OriginalPosition.X) == 1 &&
                Math.Abs(move.NewPosition.Y - move.OriginalPosition.Y) == 1)
            {
                // Undo one move to see the position before this move was made
                board.Previous();

                // Check if the destination square was empty before making this move
                var preDestPiece = board[move.NewPosition];
                board.Next();
                if (preDestPiece != null)
                {
                    // If there was a piece at the destination before, it's a normal capture, not en passant.
                    return false;
                }

                // Check the captured piece - must be a pawn for en passant
                if (move.CapturedPiece == null || move.CapturedPiece.Type != PieceType.Pawn)
                    return false;

                // Now apply the additional standard rules:
                // White en passant capture: White pawn moves from y=4 to y=5
                // Black en passant capture: Black pawn moves from y=3 to y=2
                if (move.Piece.Color == PieceColor.White)
                {
                    // White capturing upwards: original y must be 4, new y must be 5
                    if (move.OriginalPosition.Y == 4 && move.NewPosition.Y == 5)
                        return true;
                }
                else
                {
                    // Black capturing downwards: original y must be 3, new y must be 2
                    if (move.OriginalPosition.Y == 3 && move.NewPosition.Y == 2)
                        return true;
                }
            }

            return false;
        }

        private bool MovesAreSame(Move a, Move b)
        {
            if (a.Piece.Id != b.Piece.Id) return false;
            if (a.OriginalPosition != b.OriginalPosition) return false;
            if (a.NewPosition != b.NewPosition) return false;

            if ((a.Parameter == null && b.Parameter != null) || (a.Parameter != null && b.Parameter == null))
                return false;
            if (a.Parameter != null && b.Parameter != null && a.Parameter.ShortStr != b.Parameter.ShortStr)
                return false;

            return true;
        }
    }

    public class EnPassantRefusedEvaluator2 : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(EnPassantRefusedEvaluator2);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.GoToStartingPosition();

            int? lastDoublePushMoveIndex = null;

            for (int i = 0; i < board.ExecutedMoves.Count; i++)
            {
                board.Next();
                var move = board.ExecutedMoves[i];

                // If the previous move was a double-step pawn move and now we have the next move:
                if (lastDoublePushMoveIndex.HasValue && i == lastDoublePushMoveIndex.Value + 1)
                {
                    // Go to the position immediately after the double-step move
                    board.GoToStartingPosition();
                    for (int k = 0; k <= lastDoublePushMoveIndex.Value; k++)
                        board.Next();

                    var legalMoves = board.Moves();
                    var enPassantMoves = legalMoves.Where(m => IsEnPassantCapture(board, m)).ToList();

                    // Now revert to the current move's position:
                    board.GoToStartingPosition();
                    for (int x = 0; x <= i; x++)
                        board.Next();

                    // If en passant was possible but not chosen:
                    if (enPassantMoves.Count() > 0 && !enPassantMoves.Any(m => MovesAreSame(m, move)))
                    {
                        var det = "En passant refused. After a double-step pawn advance, an en passant capture was available but not taken.";
                        yield return new BooleanExample(board, det, i);
                        yield break;
                    }

                    lastDoublePushMoveIndex = null;
                }

                // Check if current move is a two-square pawn advance
                if (move.Piece.Type == PieceType.Pawn)
                {
                    int distance = Math.Abs(move.NewPosition.Y - move.OriginalPosition.Y);
                    if (distance == 2)
                        lastDoublePushMoveIndex = i;
                    else
                        lastDoublePushMoveIndex = null;
                }
                else
                {
                    lastDoublePushMoveIndex = null;
                }
            }
        }

        private bool IsEnPassantCapture(ChessBoard board, Move move)
        {
            if (move.Piece.Type != PieceType.Pawn)
                return false;

            // Pawn captures must move diagonally by one file, one rank
            if (Math.Abs(move.NewPosition.X - move.OriginalPosition.X) == 1 &&
                Math.Abs(move.NewPosition.Y - move.OriginalPosition.Y) == 1)
            {
                // Undo one move to see the position before this move was made
                board.Previous();

                // Check if the destination square was empty before making this move
                var preDestPiece = board[move.NewPosition];
                board.Next();
                if (preDestPiece != null)
                {
                    // If there was a piece at the destination before, it's a normal capture, not en passant.
                    // Note: this is impossible anyway since the pawn couldn't have moved through it.
                    return false;
                }

                // Check the captured piece - must be a pawn for en passant
                if (move.CapturedPiece == null || move.CapturedPiece.Type != PieceType.Pawn)
                    return false;

                // Now apply the additional standard rules:
                // White en passant capture: White pawn moves from y=4 to y=5
                // Black en passant capture: Black pawn moves from y=3 to y=2
                if (move.Piece.Color == PieceColor.White)
                {
                    // White capturing upwards: original y must be 4, new y must be 5
                    if (move.OriginalPosition.Y == 4 && move.NewPosition.Y == 5)
                        return true;
                }
                else
                {
                    // Black capturing downwards: original y must be 3, new y must be 2
                    if (move.OriginalPosition.Y == 3 && move.NewPosition.Y == 2)
                        return true;
                }
            }

            return false;
        }

        private bool MovesAreSame(Move a, Move b)
        {
            if (a.Piece.Id != b.Piece.Id) return false;
            if (a.OriginalPosition != b.OriginalPosition) return false;
            if (a.NewPosition != b.NewPosition) return false;

            if ((a.Parameter == null && b.Parameter != null) || (a.Parameter != null && b.Parameter == null))
                return false;
            if (a.Parameter != null && b.Parameter != null && a.Parameter.ShortStr != b.Parameter.ShortStr)
                return false;

            return true;
        }
    }


    public class QueenEnPriseThreeMovesEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(QueenEnPriseThreeMovesEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            // Track queen attackability through the game
            var isQueenIdAttackedOnMoveN = new Dictionary<int, List<bool>>();

            // First pass: collect data about queen attacks
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();
                var pieces = GetAllPiecesAndPositions(board);
                var queens = pieces.Where(p => p.Item1.Type == PieceType.Queen);

                foreach (var queenData in queens)
                {
                    var queen = queenData.Item1;
                    if (!isQueenIdAttackedOnMoveN.ContainsKey(queen.Id))
                    {
                        isQueenIdAttackedOnMoveN[queen.Id] = new List<bool> { false };
                    }

                    var moves = board.Moves();
                    var captureMove = moves.FirstOrDefault(m => m.CapturedPiece?.Id == queen.Id);
                    if (captureMove != null)
                    {
                        isQueenIdAttackedOnMoveN[queen.Id].Add(true);
                    }
                    else
                    {
                        isQueenIdAttackedOnMoveN[queen.Id].Add(false);
                    }
                }
            }

            // Second pass: analyze the data for sequences of 3 or more

            foreach (var queenId in isQueenIdAttackedOnMoveN.Keys)
            {
                var attackData = isQueenIdAttackedOnMoveN[queenId];
                board.GoToStartingPosition();
                //for (var i = 0; i < attackData.Count; i++)
                //{
                //    Console.WriteLine($"Move {i/2 + 1}{(i%2 == 0 ? "w" : "b")}: {(attackData[i] ? "Attackable" : "Safe")}");

                //}
                var moveno = 0;

                for (var i = 0; i < attackData.Count - 2; i++)
                {
                    moveno++;
                    if (attackData[i] && attackData[i + 1] && attackData[i + 2])
                    {
                        //Console.WriteLine(board.ToAscii());
                        var det = $"Queen {queenId} was attacked 3 times in a row";
                        yield return new BooleanExample(board, det, moveno);
                        yield break;
                    }
                }
            }
        }
    }
}