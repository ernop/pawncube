using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

using Chess;

using PawnCube;

using static PawnCube.Statics;

var boards = LoadBoards();
var evaluators = new AllEvaluators();
var booleanEvaluators = evaluators.GetBoolean();
var numericalEvaluators = evaluators.GetNumerical();

Console.WriteLine($"Evaluating {boards.Count} games for {booleanEvaluators.Count} boolean evaluators, {numericalEvaluators.Count} numerical evaluators");
Console.WriteLine($"\r\nBoolean evaluators: {booleanEvaluators.Count}");

foreach (var be in booleanEvaluators.OrderBy(el => el.Name))
{
    var res = be.Evaluate(true, boards);

    var overallStats = "";

    var mr = res.Result ? "100%" : "0%";
    Console.WriteLine($"\t{be.Name,-40}\tManifold: {mr,4}\t                {res.Details,-50}");
    var ii = 0;
    foreach (var qq in res.Examples.Take(Statics.NumberOfExamplesToShow))
    {
        ii++;
        var genericDetails = MakeGenericDetails(qq);
        qq.Board.MoveIndex = qq.ExampleMoveIndex;
        Console.WriteLine($"Example:{ii,-3}\t{genericDetails}\t{qq.Details,-60}\t{getResult(qq.Board)}\r\n{qq.Board.ToAscii()}\r\n");
    }
}

Console.WriteLine($"\r\nNumerical evaluators: {numericalEvaluators.Count}");
foreach (var ne in numericalEvaluators.OrderBy(el => el.Name))
{
    var res = ne.Evaluate(boards);
    Console.WriteLine($"\t{ne.Name,-40}\tManifold: {res.ManifoldResult(),3}%\t(raw: {res.RawResult,3})\t{res.Details,-40}");
    var ii = 0;
    foreach (var qq in res.Examples.Take(Statics.NumberOfExamplesToShow))
    {
        ii++;
        qq.Board.MoveIndex = qq.ExampleMoveIndex;

        Console.WriteLine($"\t{ii,-3}\t{MakeGenericDetails(qq)}\t{qq.Details,-60}\t{getResult(qq.Board)}\r\n{qq.Board.ToAscii()}\r\n");
    }
}

if (boards.Count < 5)
{
    foreach (var b in boards)
    {
        b.Last();
        Console.WriteLine(b.ToAscii());
    }
}

static string getResult(ChessBoard board)
{
    var winarar = "";
    if (board.EndGame.WonSide != null)
    {
        winarar = board.EndGame.WonSide == PieceColor.Black ? "Black - " : "White - ";
    }

    var result = $"{winarar}{board.EndGame.EndgameType}";
    return result;
}

static string MakeGenericDetails(IChessBoardExample be)
{
    var ply = be.ExampleMoveIndex;
    var extraDots = ply % 2 == 0 ? "" : " ..";
    var normalMoveNumber = Statics.MakeNormalMoveNumberDescriptor(ply);

    var res = $"{normalMoveNumber}:{extraDots}{be.Board.ExecutedMoves[ply]}\t{Statics.DescribeChessBoard(be.Board)}";
    return res;
}