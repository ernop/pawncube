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

    public class AnEnPassantCaptureEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(AnEnPassantCaptureEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                if (move.Parameter != null)
                {
                    var ss = move.Parameter.ShortStr;
                    if (ss == "e.p.")
                    {
                        yield return new BooleanExample(board, "", ii);
                    }
                }
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

    public class FirstCaptureRIsQueen : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(FirstCaptureRIsQueen);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                if (move.CapturedPiece != null)
                {
                    if (move.Piece.Type == PieceType.Queen)
                    {
                        var det = $"First capture done by queen.";
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
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                //var move = board.ExecutedMoves[ii];
                board.Next();
                for (short yy = 2; yy < 6; yy++)
                {
                    var bad = false;

                    for (short xx = 0; xx < 8; xx++)
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
            //we check if the current move just allowed EP AND the next move exists and is not EP
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];
                board.Next();

                //now we check if this move enabled an en passant capture by the opponent

                //if this was last move of the game, skip.
                if (board.ExecutedMoves.Count == ii + 1) { continue; }

                var nextMove = board.ExecutedMoves[ii + 1];
                //if they actually did ep, skip this.
                if (nextMove.Parameter != null && nextMove.Parameter.ShortStr == "e.p.") { continue; }

                //okay now we know they didn't EP next move. But did they have the option to?
                //well, the only time they have the option is when i just moved a pawn 2 spaces.
                //so let's at least check those cases:

                //if it wasn't a pawn or wasn't ending up on the rank or didn't start on the right rank, skip
                if (move.Piece.Type != PieceType.Pawn) { continue; }
                if (move.NewPosition.Y != 3 && move.NewPosition.Y != 4) { continue; }
                if (move.OriginalPosition.Y != 1 && move.NewPosition.Y != 6) { continue; }

                //okay so now we look to see if they COULD have done EP this move. Since we know they didn't, if EP is in this list, it's a 
                //case of refusal.
                var candidateMoves = board.Moves().Where(el => el.Parameter != null).Where(el => el.Parameter.ShortStr == "e.p.");

                if (candidateMoves.Any())
                {
                    var joined = string.Join(',', candidateMoves.Select(el => el.San));
                    var det = $"e.p. refused. could have done: {joined} but actually did: {nextMove}";
                    yield return new BooleanExample(board, det, ii);
                }
            }
        }
    }

    public class EnPassantCaptureHappensEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(EnPassantCaptureHappensEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                var move = board.ExecutedMoves[ii];

                if (move.Parameter != null && move.Parameter.ShortStr == "e.p.")
                {
                    var det = $"Player subsequently En Passants with {move}";
                    yield return new BooleanExample(board, det, ii - 1);
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
                        //Console.WriteLine(board.ToAscii());
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
                        new Position((short)(kingPos.X+1), (short)(kingPos.Y+1))
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
                        // Console.WriteLine($"White queen captured by {move.Piece.Color} pawn at {move.NewPosition}");
                    }
                    else if (move.CapturedPiece.Id == blackQueenId)
                    {
                        blackQueenTakenByPawn = true;
                        // Console.WriteLine($"Black queen captured by {move.Piece.Color} pawn at {move.NewPosition}");
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
}