using System;
using System.Runtime.InteropServices;

using Chess;

using PawnCube;

//if (false)
//{

//    // See https://aka.ms/new-console-template for more information
//    Console.WriteLine("Hello, World!");
//    var board = new ChessBoard() { AutoEndgameRules = AutoEndgameRules.All };

//    while (!board.IsEndGame)
//    {
//        var moves = board.Moves();
//        board.Move(moves[Random.Shared.Next(moves.Length)]);
//        Console.WriteLine(moves.Length);
//        Console.WriteLine(moves);
//        Console.WriteLine(board.ToAscii());
//        Console.WriteLine(board.ToPgn());
//    }

//    Console.WriteLine(board.ToAscii());
//    Console.WriteLine(board.ToPgn());

//}

var ii = 1;

var boards = new List<ChessBoard>();
while (true)
{
    var pgn = $"d:/proj/pawncube/PawnCube/{ii}.pgn";
    if (!System.IO.File.Exists(pgn))
    {
        Console.WriteLine($"not exi. {pgn}");
        break;
    }
    //Console.WriteLine(pgn);
    var lines = System.IO.File.ReadAllText(pgn);
    try
    {
        var cb = ChessBoard.LoadFromPgn(lines);
        cb.AddHeader("FilePath", pgn);
        boards.Add(cb);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to load game: {pgn}, skipping. Exception: {ex}");
    }
    ii++;
}


var booleanEvaluators = new List<IBooleanEvaluator>() { new RookTakesAQueenEvaluator(), new KingInACornerEvaluator(), new PawnTakesAQueenEvaluator(), new AnyOppositeSideCastlingGameEvaluator(), new AnyPawnPromotionEvaluator(), new QueenInACornerEvaluator(), new AnyTimeOutEvaluator(), new FourteenPieceGameEndEvaluator(), new Connect5Evaluator(), new Connect3Evaluator(), new TripledPawnEvaluator(), new Connect4Evaluator(), new QuadrupledPawnEvaluator(),
    new TwoPawnPromotionsInOneGameEvaluator()
};
var numericalEvaluators = new List<INumericalEvaluator>() { new OnePercentForEachMoveInLongestGameEvaluator(), new Pawn10VsKnightMinus10FirstMoveEvaluator(), new TenPercentPerResignationEvaluator(), new Black40VsWhiteMinus10WinEvalutor(),
new SevenPercentForEachDrawEvaluator(), new TenPercentForEachDrawEvaluator(), new ShortCastleTenPercentVsLongCastleMinusFivePercentEvaluator(), new SurvivingQueen5PercentEachEvaluator(),
 new HalfPercentForEachMoveInLongestGameEvaluator(), new KnightDirectionNumerical3PercentVerticalMinus4PercentHorizontalEvaluator(),
};

Console.WriteLine($"Evaluating {boards.Count} games for {booleanEvaluators.Count} boolean evaluators, {numericalEvaluators.Count} numerical evaluators");

Console.WriteLine($"\r\nBoolean evaluators: {booleanEvaluators.Count}");
foreach (var be in booleanEvaluators.OrderBy(el => el.Name))
{
    var res = be.Evaluate(boards);
    var mr = res.Result ? "100%" : "0%";
    Console.WriteLine($"\t{be.Name,-40}\tManifold: {mr,4}\t                {res.Details,-50}");
}

Console.WriteLine($"\r\nNumerical evaluators: {numericalEvaluators.Count}");
foreach (var ne in numericalEvaluators.OrderBy(el => el.Name))
{
    var res = ne.Evaluate(boards);
    Console.WriteLine($"\t{ne.Name,-40}\tManifold: {res.ManifoldResult(),3}%\t(raw: {res.RawResult,3})\t{res.Details,-40}");
}

