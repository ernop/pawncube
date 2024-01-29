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
            if (bestBoard!=null)
            {
                examples.Add(new NumericalExample(bestBoard, $"This game had {bestPerboard} pieces return to their starting squares at end.", bestBoard.ExecutedMoves.Count() - 1, bestPerboard));
            }
            
            return new NumericalEvaluationResult(raw, det, examples);
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
                        var joined = string.Join(',', board.ExecutedMoves.Select(el => el.San).Take(ii));
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

    internal class RookTakesAQueenEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
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

    internal class PawnTakesAQueenEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
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
}