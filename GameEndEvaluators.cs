﻿using Chess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static PawnCube.Statics;

namespace PawnCube
{

    internal class OnePercentForEachMoveInLongestGameEvaluator : INumericalEvaluator
    {
        public string Name => nameof(OnePercentForEachMoveInLongestGameEvaluator);

        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var max = 0;
            var det = "";
            foreach (var board in boards)
            {
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

    internal class HalfPercentForEachMoveInLongestGameEvaluator : INumericalEvaluator
    {
        public string Name => nameof(HalfPercentForEachMoveInLongestGameEvaluator);

        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            var max = 0;
            var det = "";
            foreach (var board in boards)
            {
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

    public class FourteenPieceGameEndEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(FourteenPieceGameEndEvaluator);
        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
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
                        Console.WriteLine(Statics.DescribeChessBoard(board));
                        testBoard.Move(move);
                    }

                }
                //have played out the whole game now.
                //coords are zero indexed.
                var count = 0;
                for (short xx = 0; xx < 8; xx++)
                {
                    for (short yy = 0; yy < 8; yy++)
                    {
                        var p = testBoard[xx, yy];
                        if (p != null)
                        {
                            count++;
                        }
                    }
                }
                if (count == 14)
                {
                    var det = $"game ended by {board.EndGame.EndgameType} with 14 pieces";
                    var exa = new BooleanExample(testBoard, det);
                    examples.Add(exa);
                    if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                    {
                        return new BooleanEvaluationResult("", examples);
                    }
                }
            }
            return new BooleanEvaluationResult("", examples);
        }
    }

    //definitely not bug-free right now.
    public class SurvivingQueen5PercentEachEvaluator : INumericalEvaluator
    {
        public string Name => nameof(SurvivingQueen5PercentEachEvaluator);
        public NumericalEvaluationResult Evaluate(List<ChessBoard> boards)
        {
            //wow it's a huge pain to actually track the individual queen from the beginning of the game?
            var origQueensKilled = 0;
            var queensSeen = 0;
            foreach (var board in boards)
            {
                queensSeen += 2;
                var testBoard = CopyBoardBase(board);
                var wOrigQueenPos = new Position("d1");
                var bOrigQueenPos = new Position("d8");
                var wOrigQueenDead = false;
                var bOrigQueenDead = false;
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
                        Console.WriteLine(Statics.DescribeChessBoard(board));
                        testBoard.Move(move);
                    }


                    //Console.WriteLine(testBoard.ToAscii());
                    var color = ii%2==0 ? "White" : "Black";
                    //Console.WriteLine($"{color} {move.San}");

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
                //Console.WriteLine(testBoard.ToAscii());
            }

            var survivingOriginalQueens = queensSeen - origQueensKilled;
            var raw = 5 * survivingOriginalQueens;
            var det = $"{boards.Count} games saw {queensSeen} queens; {origQueensKilled} original queens were killed, leaving {survivingOriginalQueens}.";
            return new NumericalEvaluationResult(raw, det);
        }
    }


    public class AnyTimeOutEvaluator : IBooleanEvaluator
    {
        public string Name => nameof(AnyTimeOutEvaluator);
        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            foreach (var board in boards)
            {
                var testBoard = CopyBoardBase(board);
                if (board.EndGame.EndgameType == EndgameType.Timeout)
                {
                    foreach (var m in board.ExecutedMoves)
                    {
                        testBoard.Move(m);
                    }
                    var det = "Timeout";
                    var exa = new BooleanExample(testBoard, det);
                    examples.Add(exa);

                    if (!doAll || examples.Count >= Statics.GlobalExampleMax)
                    {
                        return new BooleanEvaluationResult("", examples);
                    }
                }
            }
            return new BooleanEvaluationResult( "", examples);
        }
    }
}
