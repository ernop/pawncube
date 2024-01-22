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
        Console.WriteLine($"\t{ii,-3}\t{genericDetails}\t{qq.Details,-60}\r\n{qq.Board.ToAscii()}\r\n");
    }
}

Console.WriteLine($"\r\nNumerical evaluators: {numericalEvaluators.Count}");
foreach (var ne in numericalEvaluators.OrderBy(el => el.Name))
{
    var res = ne.Evaluate(boards);
    Console.WriteLine($"\t{ne.Name,-40}\tManifold: {res.ManifoldResult(),3}%\t(raw: {res.RawResult,3})\t{res.Details,-40}");
}

if (boards.Count < 5)
{
    foreach (var b in boards)
    {
        b.Last();
        Console.WriteLine(b.ToAscii());
    }
}

static string MakeGenericDetails(BooleanExample be)
{
    var ply = be.ExampleMoveIndex;
    var NormalMoveNumber = Statics.MakeNormalMoveNumberDescriptor(ply);

    var res = $"{NormalMoveNumber}:{be.Board.ExecutedMoves[ply]} of {Statics.DescribeChessBoard(be.Board)}";
    return res;
}