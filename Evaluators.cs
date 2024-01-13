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
    public class CapturedBishopsFiveCapturedPawnsMinusOneEvaluator : INumericalEvaluator
    {
        public string Name => nameof(CapturedBishopsFiveCapturedPawnsMinusOneEvaluator);

        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var capturedBishops = 0;
            var capturedPawns = 0;

            foreach (var board in boards)
            {
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

            var det = $"Captured pawns:{capturedPawns} Captured Bishops{capturedBishops}";
            return new NumericalEvaluationResult(raw, det);
        }
    }

    public class CheckmateWithAPawnEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(CheckmateWithAPawnEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                //todo tomorrow: convert this to full iteration over a precalculated set of full board and available moves.
                //it's not good to recalculate the entire thing.
                //that way this will turn out to be like:
                //foreach (var bp in board.all_positions) { 

                //}

                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (move.IsMate && move.Piece.Type == PieceType.Pawn)
                    {
                        if (!move.IsCheck)
                        {
                            //yess you can be "mated" without being in check. (i.e. stalemate!!?)
                            //dumb library.
                            Console.WriteLine("Error: mate without check detected. HMMMM");
                            continue;
                        }
                        if (move.Parameter != null && move.Parameter.ShortStr.Substring(0, 1) == "=")
                        {
                            //this system counts promotions as pawn moves.
                            //note: this means they also count promotion mates as NOT by the promoted piece!
                            Console.WriteLine("Error: pawn mate where the pawn promoted first detected. HMM LIBRARY>?");
                            continue;
                        }
                        var det = $"";
                        var exa = new BooleanExample(testBoard, det);
                        examples.Add(exa);
                        if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                        {
                            return new BooleanEvaluationResult("", examples);
                        }
                    }

                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    

    public class NoCapturesBeforeMove30Evaluator : IBooleanEvaluator
    {
        public string Name => nameof(NoCapturesBeforeMove30Evaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var lim = 60;
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (ii <= lim && move.CapturedPiece != null)
                    {
                        break;
                    }
                    if (ii > lim)
                    {
                        var endCapture = "Game ended with no piece ever taken.";
                        var innerCopy = CopyBoardBase(testBoard);
                        for (var jj = 0; jj < board.ExecutedMoves.Count; jj++)
                        {
                            var mm = board.ExecutedMoves[jj];
                            if (mm.CapturedPiece != null)
                            {
                                endCapture = $"The first capture was: {Statics.MakeNormalMoveNumberDescriptor(jj)} {mm} taking a {innerCopy[mm.NewPosition]}";
                                break;
                            }
                            innerCopy.Move(mm);
                        }
                        var det = $"Game with no captures through move {lim / 2}; {endCapture}";
                        var exa = new BooleanExample(testBoard, det);
                        examples.Add(exa);
                        if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                        {
                            return new BooleanEvaluationResult("", examples);
                        }
                        break;
                    }

                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class NoCapturesBeforeMove20Evaluator : IBooleanEvaluator
    {
        public string Name => nameof(NoCapturesBeforeMove20Evaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var lim = 40;
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (ii <= lim && move.CapturedPiece != null)
                    {
                        break;
                    }
                    if (ii > lim)
                    {
                        var endCapture = "Game ended with no piece ever taken.";
                        var innerCopy = CopyBoardBase(testBoard);
                        for (var jj = 0; jj < board.ExecutedMoves.Count; jj++)
                        {
                            var mm = board.ExecutedMoves[jj];
                            if (mm.CapturedPiece != null)
                            {
                                endCapture = $"The first capture was: {Statics.MakeNormalMoveNumberDescriptor(jj)} {mm} taking a {innerCopy[mm.NewPosition]}";
                                break;
                            }
                            innerCopy.Move(mm);
                        }
                        var det = $"Game with no captures through move {lim / 2}; {endCapture}";
                        var exa = new BooleanExample(testBoard, det);
                        examples.Add(exa);
                        if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                        {
                            return new BooleanEvaluationResult("", examples);
                        }
                        break;
                    }

                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class NoCapturesBeforeMove10Evaluator : IBooleanEvaluator
    {
        public string Name => nameof(NoCapturesBeforeMove10Evaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var lim = 20;
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (ii <= lim && move.CapturedPiece != null)
                    {
                        break;
                    }
                    if (ii > lim)
                    {
                        var endCapture = "Game ended with no piece ever taken.";
                        var innerCopy = CopyBoardBase(testBoard);
                        for (var jj = 0; jj < board.ExecutedMoves.Count; jj++)
                        {
                            var mm = board.ExecutedMoves[jj];
                            if (mm.CapturedPiece != null)
                            {
                                endCapture = $"The first capture was: {Statics.MakeNormalMoveNumberDescriptor(jj)} {mm} taking a {innerCopy[mm.NewPosition]}";
                                break;
                            }
                            innerCopy.Move(mm);
                        }
                        var det = $"Game with no captures through move {lim/2}; {endCapture}";
                        var exa = new BooleanExample(testBoard, det);
                        examples.Add(exa);
                        if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                        {
                            return new BooleanEvaluationResult("", examples);
                        }
                        break;
                    }

                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class TenPercentForeEachWinEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TenPercentForeEachWinEvaluator);

        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var winCount = 0;
            foreach (var board in boards)
            {

                if (board.EndGame.EndgameType == EndgameType.Timeout
                    || board.EndGame.EndgameType == EndgameType.Checkmate
                     || board.EndGame.EndgameType == EndgameType.Resigned)
                {
                    winCount++;
                }
            }

            var raw = 10 * winCount;
            var det = $"Total of {winCount} wins out of {boards.Count} games.";
            return new NumericalEvaluationResult(raw, det);
        }
    }


    public class NonQueenPromotionEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(NonQueenPromotionEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (move.Parameter != null)
                    {
                        if (move.Parameter.ShortStr.Substring(0, 1) == "=")
                        {
                            if (move.Parameter.ShortStr != "=Q")
                            {
                                var det = $"Promoted to {move.Parameter.ShortStr}";
                                var exa = new BooleanExample(testBoard, det);
                                examples.Add(exa);
                                if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                                {
                                    return new BooleanEvaluationResult("", examples);
                                }
                            }
                        }
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class EverQvsRREndgameEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(EverQvsRREndgameEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var wq = 1;
                var bq = 1;
                var wr = 2;
                var br = 2;
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
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
                        var exa = new BooleanExample(testBoard, det);
                        examples.Add(exa);
                        if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                        {
                            return new BooleanEvaluationResult("", examples);
                        }
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class BishopMovesSevenSquaresEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(BishopMovesSevenSquaresEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (move.Piece.Type == PieceType.Bishop)
                    {
                        var gap = Math.Abs(move.OriginalPosition.X - move.NewPosition.X);
                        if (gap >= 7)
                        {
                            var det = $"Moved 7 squares to: {move.San}";
                            var exa = new BooleanExample(testBoard, det);
                            examples.Add(exa);
                            if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                            {
                                return new BooleanEvaluationResult("", examples);
                            }
                        }
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class BishopMovesSixSquaresEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(BishopMovesSixSquaresEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)

            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (move.Piece.Type == PieceType.Bishop)
                    {
                        var gap = Math.Abs(move.OriginalPosition.X - move.NewPosition.X);
                        if (gap >= 6)
                        {
                            var det = $"6 square move{move.San} from {move.OriginalPosition}";
                            var exa = new BooleanExample(testBoard, det);
                            examples.Add(exa);
                            if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                            {
                                return new BooleanEvaluationResult("", examples);
                            }
                        }
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }


    public class TwentyPercentPerEnPassantCaptureEvaluator : INumericalEvaluator
    {
        public string Name => nameof(TwentyPercentPerEnPassantCaptureEvaluator);

        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var epCount = 0;
            foreach (var board in boards)
            {
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    if (move.Parameter != null)
                    {
                        var ss = move.Parameter.ShortStr;
                        if (ss == "e.p.")
                        {
                            epCount++;
                        }
                    }
                }
            }
            var raw = 20 * epCount;
            var det = $"total of {epCount} en passant captures in {boards.Count} games.";
            return new NumericalEvaluationResult(raw, det);
        }
    }

    public class CastleAfterMove40Evaluator : IBooleanEvaluator
    {
        public string Name => nameof(CastleAfterMove40Evaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    var normalMoveNumber = ii / 2 + 1;
                    if (normalMoveNumber <= 40) { continue; }
                    var color = ii % 2 == 0 ? "White" : "Black";

                    if (move.Parameter != null)
                    {
                        var ss = move.Parameter.ShortStr;
                        if (ss == "O-O" || ss == "O-O-O")
                        {
                            var det = $"";
                            var exa = new BooleanExample(testBoard, det);
                            examples.Add(exa);
                            if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                            {
                                return new BooleanEvaluationResult("", examples);
                            }
                        }
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }



    public class CastleAfterMove20Evaluator : IBooleanEvaluator
    {
        public string Name => nameof(CastleAfterMove20Evaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    var normalMoveNumber = ii / 2 + 1;
                    var color = ii % 2 == 0 ? "White" : "Black";
                    if (normalMoveNumber <= 20) { continue; }
                    if (move.Parameter != null)
                    {
                        var ss = move.Parameter.ShortStr;
                        if (ss == "O-O" || ss == "O-O-O")
                        {
                            var det = $"";
                            var exa = new BooleanExample(testBoard, det);
                            examples.Add(exa);
                            if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                            {
                                return new BooleanEvaluationResult("", examples);
                            }
                        }
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class EnPassantRefusedEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(EnPassantRefusedEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);

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
                    var candidateMoves = testBoard.Moves().Where(el => el.Parameter != null).Where(el => el.Parameter.ShortStr == "e.p.");

                    if (candidateMoves.Any())
                    {
                        var joined = string.Join(',', candidateMoves.Select(el => el.San));
                        var det = $"e.p. refused. could have done: {joined} but actually did: {nextMove}";
                        var exa = new BooleanExample(testBoard, det);
                        examples.Add(exa);
                        if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                        {
                            return new BooleanEvaluationResult("", examples);
                        }
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class EnPassantEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(EnPassantEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];

                    if (move.Parameter != null)
                    {
                        if (move.Parameter.ShortStr == "e.p.")
                        {
                            var det = $"Player subsequently En Passants with {move}";
                            var exa = new BooleanExample(testBoard, det);
                            examples.Add(exa);
                            if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                            {
                                return new BooleanEvaluationResult("", examples);
                            }
                        }
                    }
                    testBoard.Move(move);
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class PromoteAndCheckEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(PromoteAndCheckEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (move.Parameter != null)
                    {
                        if (move.Parameter.ShortStr.Substring(0, 1) == "=")
                        {
                            if (move.IsCheck)
                            {
                                var det = $"Promoted: {move.Parameter.ShortStr} and Check.";
                                var exa = new BooleanExample(testBoard, det);
                                examples.Add(exa);
                                if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                                {
                                    return new BooleanEvaluationResult(det, examples);
                                }
                            }

                        }
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class AnyPawnPromotionEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(AnyPawnPromotionEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                if (board.ExecutedMoves.Count == 0) { continue; }
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (move.Parameter != null)
                    {
                        var l = move.Parameter.ShortStr;
                        if (l == "=" || l == "=Q" || l == "=R" || l == "=B" || l == "=N")
                        {
                            var det = $"Promotion: {move.Parameter.ShortStr}";
                            var exa = new BooleanExample(testBoard, det);
                            examples.Add(exa);
                            if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                            {
                                return new BooleanEvaluationResult("", examples);
                            }
                        }
                    }
                }

            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class TwoPawnPromotionsInOneGameEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(TwoPawnPromotionsInOneGameEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var pawnPromotionsThisGame = 0;
                if (board.ExecutedMoves.Count == 0) { continue; }
                var promotionPoints = new List<int>();
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
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
                    var det = $"Game with >=2 promotions: there were {pawnPromotionsThisGame}";
                    var exa = new BooleanExample(testBoard, det);
                    examples.Add(exa);
                    return new BooleanEvaluationResult("", examples);
                }


            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class SamePieceMovesEightTimesInARowEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(SamePieceMovesEightTimesInARowEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
                    if (ii < 14) { continue; }
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
                            var exa = new BooleanExample(testBoard, det);
                            examples.Add(exa);
                            if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                            {
                                return new BooleanEvaluationResult("", examples);
                            }

                        }
                    }
                }

            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class AnyOppositeSideCastlingGameEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(AnyOppositeSideCastlingGameEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var hasShort = false;
                var hasLong = false;
                var testBoard = CopyBoardBase(board);
                if (board.ExecutedMoves.Count == 0) { continue; }
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    testBoard.Move(move);
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
                        var exa = new BooleanExample(testBoard, det);
                        examples.Add(exa);
                        if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                        {
                            return new BooleanEvaluationResult("", examples);
                        }
                        continue;
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    public class BishopManualUndoEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(BishopManualUndoEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    testBoard.Move(board.ExecutedMoves[ii]);
                    if (ii <= 2) { continue; }
                    var move = board.ExecutedMoves[ii];
                    var priorMove = board.ExecutedMoves[ii - 2];
                    if (priorMove.CapturedPiece != null) { continue; }
                    if (move.Piece.Type == PieceType.Bishop && priorMove.Piece.Type == PieceType.Bishop)
                    {
                        if (move.NewPosition == priorMove.OriginalPosition)
                        {
                            var det = $"Bishop moved back to: {priorMove.OriginalPosition} from {priorMove.NewPosition}";
                            var exa = new BooleanExample(testBoard, det);
                            examples.Add(exa);
                            if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                            {
                                return new BooleanEvaluationResult("", examples);
                            }
                        }
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }
    internal class RookTakesAQueenEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(RookTakesAQueenEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var m = board.ExecutedMoves[ii];
                    testBoard.Move(m);
                    if (m.CapturedPiece != null && m.CapturedPiece.Type == PieceType.Queen && m.Piece.Type == PieceType.Rook)
                    {
                        var det = $"";
                        var exa = new BooleanExample(testBoard, det);
                        examples.Add(exa);
                        if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                        {
                            return new BooleanEvaluationResult("", examples);
                        }
                    }
                }
            }

            return new BooleanEvaluationResult("", examples);
        }
    }

    internal class PawnTakesAQueenEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(PawnTakesAQueenEvaluator);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var m = board.ExecutedMoves[ii];
                    testBoard.Move(m);
                    if (m.CapturedPiece != null && m.CapturedPiece.Type == PieceType.Queen && m.Piece.Type == PieceType.Pawn)
                    {
                        var det = $"";
                        var exa = new BooleanExample(testBoard, det);
                        examples.Add(exa);
                        if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                        {
                            return new BooleanEvaluationResult("", examples);
                        }
                    }
                }
            }

            return new BooleanEvaluationResult("", examples);
        }
    }
}