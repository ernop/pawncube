using Chess;

using PawnCube;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

using static PawnCube.Statics;

namespace PawnCube
{

    //will call internal overridden class CheckForPattern at every position, returning yes if any of them return true.
    //this andall other similar classes are super inefficient and have many ways to shortcircuit earlier if we wanted them.
    public abstract class PatternMatcherChecker : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public abstract string Name { get; }
        public abstract bool CheckForPattern(ChessBoard board, short xx, short yy, out string details);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            for (var ii = 0; ii < board.ExecutedMoves.Count; ii++)
            {
                board.Next();

                //we check every position since the move you just did may NOT be at the anchor point of the pattern descriptor vector in the class
                //AND we skip the rest of the game once its found once, otherwise we keep detecting it over and over for this game.

                //we have to be crazy about breaking out here. since we recheck the entire board rather than just the piece that moved
                //(since we don't have all versions of all the patterns, but instead just check the base set relative to starting position.
                var done = false;
                for (short xx = 0; xx < 8; xx++)
                {
                    done = false;
                    for (short yy = 0; yy < 8; yy++)
                    {
                        //checking starting pos xx,yy for the pattern.
                        if (CheckForPattern(board, xx, yy, out string details))
                        {
                            var det = $"Pattern shows up: {details}";
                            yield return new BooleanExample(board, det, ii);
                            done = true;
                            break;
                        }
                    }
                    if (done) { break; }
                }
                if (done) { break; }
            }
        }
    }
}

public class TripledPawnEvaluator : PatternMatcherChecker
{
    public override string Name => nameof(TripledPawnEvaluator);

    public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy, out string details)
    {
        details = "";
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
        details = $"at: {xx + 1},{yy + 1} {xx + 1 + up[0].Item1},{yy + 1 + up[0].Item2} {xx + 1 + up[1].Item1},{yy + 1 + up[1].Item2}";
        return true;
    }
}

public class QuadrupledPawnEvaluator : PatternMatcherChecker
{
    public override string Name => nameof(QuadrupledPawnEvaluator);

    public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy, out string details)
    {
        details = "";
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

        details = $"at:  {xx + 1},{yy + 1} {xx + 1 + up[0].Item1},{yy + 1 + up[0].Item2} {xx + 1 + up[1].Item1},{yy + 1 + up[1].Item2} {xx + 1 + up[2].Item1},{yy + 1 + up[2].Item2}";
        //we got through a whole vector without dying.
        return true;
    }
}

public class QueenInACornerEvaluator : PatternMatcherChecker
{
    public override string Name => nameof(QueenInACornerEvaluator);

    public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy, out string details)
    {
        details = "";
        var positions = new List<Tuple<short, short>>() { new Tuple<short, short>(0, 0), new Tuple<short, short>(7, 0), new Tuple<short, short>(0, 7), new Tuple<short, short>(7, 7) };
        foreach (var pos in positions)
        {
            var p = testBoard[pos.Item1, pos.Item2];
            if (p != null && p.Type == PieceType.Queen)
            {
                details = $"at: {xx + 1},{yy + 1}";
                return true;
            }
        }

        return false;
    }
}

public class KingInACornerEvaluator : PatternMatcherChecker
{
    public override string Name => nameof(KingInACornerEvaluator);

    public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy, out string details)
    {
        details = "";
        var positions = new List<Tuple<short, short>>() { new Tuple<short, short>(0, 0), new Tuple<short, short>(7, 0), new Tuple<short, short>(0, 7), new Tuple<short, short>(7, 7) };

        //for this type of single spot pattern it doesn't really make sense to check every square.
        //i could just check the one. so this is duplicated effort.

        foreach (var pos in positions)
        {
            var p = testBoard[pos.Item1, pos.Item2];
            if (p != null && p.Type == PieceType.King)
            {
                details = $"at: {xx + 1},{yy + 1}";
                return true;
            }
        }
        return false;
    }
}

public class Connect5Evaluator : PatternMatcherChecker
{
    public override string Name => nameof(Connect5Evaluator);

    public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy, out string details)
    {
        details = "";
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
            foreach (var pos in vector)
            {
                details += $" {xx + pos.Item1},{yy + pos.Item2}";
            }

            return true;
        }
        return false;
    }
}

public class Connect3Evaluator : PatternMatcherChecker
{
    public override string Name => nameof(Connect3Evaluator);

    public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy, out string details)
    {
        details = "";
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

            details = $"{xx + 1},{yy + 1}";
            //we got through a whole vector without dying.
            foreach (var pos in vector)
            {
                details += $" {xx + pos.Item1},{yy + pos.Item2}";
            }
            return true;
        }
        return false;
    }
}

public class Connect4Evaluator : PatternMatcherChecker
{
    public override string Name => nameof(Connect4Evaluator);

    public override bool CheckForPattern(ChessBoard testBoard, short xx, short yy, out string details)
    {
        details = "";
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

            details = $"{new Position(xx,yy).ToString()}";
            //we got through a whole vector without dying.
            foreach (Tuple<short,short> pos in vector)
            {
                var position = new Position((short)(xx + pos.Item1), (short)(yy + pos.Item2));
                details += $" {position.ToString()}";
            }
            return true;
        }
        return false;
    }
}

