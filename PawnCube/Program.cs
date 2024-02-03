using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

using Chess;
using static PawnCube.Statics;

using PawnCube;

using static PawnCube.Statics;

var boardSets = LoadBoardSets();

var evaluators = new AllEvaluators();
var booleanEvaluators = evaluators.GetBoolean();
var numericalEvaluators = evaluators.GetNumerical();

foreach (var boardSet in boardSets)
{

    Console.WriteLine($"=================\r\nEvaluating {boardSet.Path} which has: {boardSet.Boards.Count} games for {booleanEvaluators.Count} boolean evaluators, {numericalEvaluators.Count} numerical evaluators");
    Console.WriteLine($"\r\nBoolean evaluators: {booleanEvaluators.Count}");

    foreach (var be in booleanEvaluators.OrderBy(el => el.Name))
    {
        var res = be.Evaluate(true, boardSet.Boards);

        var overallStats = "";

        var mr = res.Result ? "100%" : "0%";
        Console.WriteLine($"\t{be.Name,-40}\tManifold: {mr,4}\t                {res.Details,-50}");
        var ii = 0;
        foreach (var example in res.Examples.Take(Statics.NumberOfExamplesToShow))
        {
            ii++;
            var genericDetails = MakeGenericDetails(example);
            example.Board.MoveIndex = example.ExampleMoveIndex;
            Console.WriteLine($"Example:{ii,-3}\t{genericDetails}\t{example.Details,-60}\t{getResult(example.Board)}\r\n{example.Board.ToAscii()}\r\n");
        }
    }

    Console.WriteLine($"\r\nNumerical evaluators: {numericalEvaluators.Count}");
    foreach (var ne in numericalEvaluators.OrderBy(el => el.Name))
    {
        var res = ne.Evaluate(boardSet.Boards);
        Console.WriteLine($"\t{ne.Name,-40}\tManifold: {res.ManifoldResult(),3}%\t(raw: {res.RawResult,3})\t{res.Details,-40}");
        var ii = 0;
        foreach (var example in res.Examples.Take(Statics.NumberOfExamplesToShow))
        {
            ii++;
            example.Board.MoveIndex = example.ExampleMoveIndex;

            Console.WriteLine($"\t{ii,-3}\t{MakeGenericDetails(example)}\t{example.Details,-60}\t{getResult(example.Board)}\r\n{example.Board.ToAscii()}\r\n");
        }
    }

    if (boardSet.Boards.Count < 5)
    {
        foreach (var b in boardSet.Boards)
        {
            b.Last();
            Console.WriteLine(b.ToAscii());
        }
    }
}

