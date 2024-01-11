using Chess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace PawnCube
{

    //will call internal overridden class CheckForPattern at every position, returning yes if any of them return true.
    public abstract class PatternMatcherChecker : IBooleanEvaluator
    {
        public abstract string Name { get; }
        public abstract bool CheckForPattern(ChessBoard board, short xx, short yy);
        public BooleanEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            foreach (var board in boards)
            {
                var testBoard = new ChessBoard();// { AutoEndgameRules = AutoEndgameRules.All };
                for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
                {
                    var move = board.ExecutedMoves[ii];
                    try
                    {
                        testBoard.Move(move);
                        
                    }
                    catch (Chess.ChessGameEndedException ex)
                    {
                        //okay sometimes the pgn goes beyond the official 3fold end of the game.
                        break;

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(testBoard.ToAscii());
                        Statics.DescribeChessBoard(testBoard);
                        testBoard.Move(move);
                    }

                    for (short xx = 0; xx < 8; xx++)
                    {
                        for (short yy = 0; yy < 8; yy++)
                        {
                            //checking starting pos xx,yy for the pattern.
                            if (CheckForPattern(testBoard, xx, yy))
                            {
                                return new BooleanEvaluationResult(true, $"Pattern shows up at move {Statics.MakeNormalMoveNumberDescriptor(ii)}, position: xx={xx + 1}, yy={yy+1} in game: {Statics.DescribeChessBoard(board)}\r\n{testBoard.ToAscii()}");
                            }
                        }
                    }
                }
            }
            return new BooleanEvaluationResult(false, "");
        }
    }

    public class TripledPawnEvaluator : PatternMatcherChecker
    {
        public override string Name => nameof(TripledPawnEvaluator);

        public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy)
        {
            var up = new List<Tuple<short, short>>() { new Tuple<short, short>(0, 1), new Tuple<short, short>(0, 2) };


            var p = testBoard[xx, yy];
            if (p == null || p.Type != PieceType.Pawn) { return false; }

            var testColor = p.Color;
            foreach (var pos in up)
            {
                var target = new Tuple<short, short>((short)(xx + pos.Item1), (short)(yy + pos.Item2));
                if (!Statics.IsInBounds(target))
                {
                    return false;
                }
                var targetp = testBoard[target.Item1, target.Item2];
                if (targetp == null)
                {
                    return false;
                }
                if (targetp.Color != testColor)
                {
                    return false;
                }
                if (targetp.Type != PieceType.Pawn)
                {
                    return false;
                }
            }

            //we got through a whole vector without dying.
            return true;
        }
    }

    public class QuadrupledPawnEvaluator : PatternMatcherChecker
    {
        public override string Name => nameof(QuadrupledPawnEvaluator);

        public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy)
        {
            var up = new List<Tuple<short, short>>() { new Tuple<short, short>(0, 1), new Tuple<short, short>(0, 2), new Tuple<short, short>(0, 3), };


            var p = testBoard[xx, yy];
            if (p == null || p.Type != PieceType.Pawn) { return false; }

            var testColor = p.Color;
            foreach (var pos in up)
            {
                var target = new Tuple<short, short>((short)(xx + pos.Item1), (short)(yy + pos.Item2));
                if (!Statics.IsInBounds(target))
                {
                    return false;
                }
                var targetp = testBoard[target.Item1, target.Item2];
                if (targetp == null)
                {
                    return false;
                }
                if (targetp.Color != testColor)
                {
                    return false;
                }
                if (targetp.Type != PieceType.Pawn)
                {
                    return false;
                }
            }

            //we got through a whole vector without dying.
            return true;
        }
    }

    public class QueenInACornerEvaluator : PatternMatcherChecker
    {
        public override string Name => nameof(QueenInACornerEvaluator);

        public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy)
        {
            var positions = new List<Tuple<short, short>>() { new Tuple<short, short>(0, 0), new Tuple<short, short>(7, 0), new Tuple<short, short>(0, 7), new Tuple<short, short>(7, 7) };
            foreach (var pos in positions)
            {
                var p = testBoard[pos.Item1, pos.Item2];
                if (p != null && p.Type == PieceType.Queen)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class KingInACornerEvaluator : PatternMatcherChecker
    {
        public override string Name => nameof(KingInACornerEvaluator);

        public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy)
        {
            var positions = new List<Tuple<short, short>>() { new Tuple<short, short>(0, 0), new Tuple<short, short>(7, 0), new Tuple<short, short>(0, 7), new Tuple<short, short>(7, 7) };
            foreach (var pos in positions)
            {
                var p = testBoard[pos.Item1, pos.Item2];
                if (p != null && p.Type == PieceType.King)
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class Connect5Evaluator : PatternMatcherChecker
    {
        public override string Name => nameof(Connect5Evaluator);

        public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy)
        {
            var upright = new List<Tuple<short, short>>() { new Tuple<short, short>(1, 1), new Tuple<short, short>(2, 2), new Tuple<short, short>(3, 3), new Tuple<short, short>(4, 4) };
            var right = new List<Tuple<short, short>>() { new Tuple<short, short>(1, 0), new Tuple<short, short>(2, 0), new Tuple<short, short>(3, 0), new Tuple<short, short>(4, 0) };
            var up = new List<Tuple<short, short>>() { new Tuple<short, short>(0, 1), new Tuple<short, short>(0, 2), new Tuple<short, short>(0, 3), new Tuple<short, short>(0, 4) };

            var vectors = new List<List<Tuple<short, short>>>() { upright, right, up };

            var p = testBoard[xx, yy];
            if (p == null || p.Type != PieceType.Pawn) { return false; }

            var testColor = p.Color;
            foreach (var vector in vectors)
            {
                //exclude starting position.
                if (yy == 1 && testColor == PieceColor.White && vector == right)
                {
                    continue;
                }
                if (yy == 6 && testColor == PieceColor.Black && vector == right)
                {
                    continue;
                }
                var bad = false;
                foreach (var pos in vector)
                {
                    var target = new Tuple<short, short>((short)(xx + pos.Item1), (short)(yy + pos.Item2));
                    if (!Statics.IsInBounds(target))
                    {
                        bad = true;
                        break;
                    }
                    var targetp = testBoard[target.Item1, target.Item2];
                    if (targetp == null)
                    {
                        bad = true;
                        break;
                    }
                    if (targetp.Color != testColor)
                    {
                        bad = true;
                        break;
                    }
                    if (targetp.Type != PieceType.Pawn)
                    {
                        bad = true;
                        break;
                    }
                }
                if (bad) //the whole vector broke.
                {
                    break;
                }


                //we got through a whole vector without dying.
                return true;
            }
            return false;
        }
    }

    public class Connect3Evaluator : PatternMatcherChecker
    {
        public override string Name => nameof(Connect3Evaluator);

        public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy)
        {
            var upright = new List<Tuple<short, short>>() { new Tuple<short, short>(1, 1), new Tuple<short, short>(2, 2), };
            var right = new List<Tuple<short, short>>() { new Tuple<short, short>(1, 0), new Tuple<short, short>(2, 0), };
            var up = new List<Tuple<short, short>>() { new Tuple<short, short>(0, 1), new Tuple<short, short>(0, 2), };

            var vectors = new List<List<Tuple<short, short>>>() { upright, right, up };

            var p = testBoard[xx, yy];
            if (p == null || p.Type != PieceType.Pawn) { return false; }

            var testColor = p.Color;
            foreach (var vector in vectors)
            {
                //exclude starting position.
                if (yy == 1 && testColor == PieceColor.White && vector == right)
                {
                    continue;
                }
                if (yy == 6 && testColor == PieceColor.Black && vector == right)
                {
                    continue;
                }
                var bad = false;
                foreach (var pos in vector)
                {
                    var target = new Tuple<short, short>((short)(xx + pos.Item1), (short)(yy + pos.Item2));
                    if (!Statics.IsInBounds(target))
                    {
                        bad = true;
                        break;
                    }
                    var targetp = testBoard[target.Item1, target.Item2];
                    if (targetp == null)
                    {
                        bad = true;
                        break;
                    }
                    if (targetp.Color != testColor)
                    {
                        bad = true;
                        break;
                    }
                    if (targetp.Type != PieceType.Pawn)
                    {
                        bad = true;
                        break;
                    }
                }
                if (bad) //the whole vector broke.
                {
                    break;
                }


                //we got through a whole vector without dying.
                return true;
            }
            return false;
        }
    }

    public class Connect4Evaluator : PatternMatcherChecker
    {
        public override string Name => nameof(Connect4Evaluator);

        public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy)
        {
            var upright = new List<Tuple<short, short>>() { new Tuple<short, short>(1, 1), new Tuple<short, short>(2, 2), new Tuple<short, short>(3, 3), };
            var right = new List<Tuple<short, short>>() { new Tuple<short, short>(1, 0), new Tuple<short, short>(2, 0), new Tuple<short, short>(3, 0), };
            var up = new List<Tuple<short, short>>() { new Tuple<short, short>(0, 1), new Tuple<short, short>(0, 2), new Tuple<short, short>(0, 3), };

            var vectors = new List<List<Tuple<short, short>>>() { upright, right, up };

            var p = testBoard[xx, yy];
            if (p == null || p.Type != PieceType.Pawn) { return false; }

            var testColor = p.Color;
            foreach (var vector in vectors)
            {
                //exclude starting position.
                if (yy == 1 && testColor == PieceColor.White && vector == right)
                {
                    continue;
                }
                if (yy == 6 && testColor == PieceColor.Black && vector == right)
                {
                    continue;
                }
                var bad = false;
                foreach (var pos in vector)
                {
                    var target = new Tuple<short, short>((short)(xx + pos.Item1), (short)(yy + pos.Item2));
                    if (!Statics.IsInBounds(target))
                    {
                        bad = true;
                        break;
                    }
                    var targetp = testBoard[target.Item1, target.Item2];
                    if (targetp == null)
                    {
                        bad = true;
                        break;
                    }
                    if (targetp.Color != testColor)
                    {
                        bad = true;
                        break;
                    }
                    if (targetp.Type != PieceType.Pawn)
                    {
                        bad = true;
                        break;
                    }
                }
                if (bad) //the whole vector broke.
                {
                    break;
                }


                //we got through a whole vector without dying.
                return true;
            }
            return false;
        }
    }
}
