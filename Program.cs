using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

using Chess;

using PawnCube;

static IEnumerable<string> SplitPgns(string fp)
{

    var lines = System.IO.File.ReadAllText(fp);
    var parts = lines.Split("[Event");
    foreach (var p in parts)
    {
        if (string.IsNullOrEmpty(p)) { continue; }
        yield return "[Event" + p;
    }

}

var ct = 0;

var boards = new List<ChessBoard>();

var based = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\Ding\Ding.pgn");

var maxGamesToProcess = 20000;

//okay so this library will fail to remove pawns captured EP if you load them via LoadFromPgn, but if you load the 
//filed issue for this here: https://github.com/Geras1mleo/Chess/issues/19
static void BrokenLoad()
{
    //board state is correct

    var pgn = "1.e4 e6 2.d4 d5 3.Nc3 Nf6 4.Bg5 dxe4 5.Nxe4 Be7 6.Bxf6 gxf6 7.Nf3 f5 8.Nc3 a6\r\n9.Qd2 b5 10.O-O-O b4 11.Na4 Bb7 12.Bc4 Bd5 13.Qe2 Nc6 14.Rhe1 Na5 15.Bxd5 Qxd5 16.b3 Nc6 17.c4";
    var cb = ChessBoard.LoadFromPgn(pgn);
    Console.WriteLine("PGN\r\n" + cb.ToAscii());

    //takes EP okay but doesn't remove the pawn.
    var pgn2 = "1.e4 e6 2.d4 d5 3.Nc3 Nf6 4.Bg5 dxe4 5.Nxe4 Be7 6.Bxf6 gxf6 7.Nf3 f5 8.Nc3 a6\r\n9.Qd2 b5 10.O-O-O b4 11.Na4 Bb7 12.Bc4 Bd5 13.Qe2 Nc6 14.Rhe1 Na5 15.Bxd5 Qxd5 16.b3 Nc6 17.c4 bxc3";
    var cb2 = ChessBoard.LoadFromPgn(pgn2);
    Console.WriteLine("PGN2\r\n" + cb2.ToAscii());

    //you can see later on taht the pawn still being their breaks the full game processing.
    var pgn3 = "1.e4 e6 2.d4 d5 3.Nc3 Nf6 4.Bg5 dxe4 5.Nxe4 Be7 6.Bxf6 gxf6 7.Nf3 f5 8.Nc3 a6\r\n9.Qd2 b5 10.O-O-O b4 11.Na4 Bb7 12.Bc4 Bd5 13.Qe2 Nc6 14.Rhe1 Na5 15.Bxd5 Qxd5\r\n16.b3 Nc6 17.c4 bxc3 18.Nxc3 Qa5 19.Qc4 Nb4 20.Kb1 O-O 21.Ne5 Rad8 22.g4 f4\r\n23.Nd3 Nxd3 24.Rxd3 Bf6 25.Red1 Bg7 26.Qc5 Qxc5 27.dxc5 Rxd3 28.Rxd3 f5 29.gxf5 Bxc3\r\n30.Rxc3 Rxf5 31.Kc2 Rg5 32.Kd3 Rg2 33.Ke2 Rxh2 34.Rd3 e5 35.Rd5 e4 36.Rg5+ Kh8\r\n37.Rg4 e3 38.Rxf4 exf2 39.Rf7 c6 40.a4 Kg8 41.Rc7 Rh3 42.Rxc6 Rxb3 43.Rxa6 Rb2+\r\n44.Kf1 Kf7 45.a5 Ke8 46.Ra8+ Kd7 47.a6 Ra2 48.a7 Kc7  1/2-1/2";
    var cb3 = ChessBoard.LoadFromPgn(pgn3);
    Console.WriteLine("PGN3" + cb3.ToAscii());

    var mm = cb2.Moves();

    //ALSO, if you *play through* moves one by one, that works too.
    var testBoard = new ChessBoard();
    foreach (var mmm in cb2.ExecutedMoves)
    {
        testBoard.Move(mmm);
    }
    Console.WriteLine(testBoard.ToAscii());

    foreach (var m in mm) { Console.WriteLine(m.San); }
    //okay, so we do detect en passant moves.
    //they should as ep in the prior board position's "moves" array.    
    try
    {
        var cbi = ChessBoard.LoadFromPgn(pgn2);
        Console.WriteLine(cb.ToAscii());
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to load game, skipping. Exception: {ex}\r\nPGN={pgn2}");
    }
}

var testBrokenEPPGNLoad = true;
testBrokenEPPGNLoad = false;
if (testBrokenEPPGNLoad)
{
    BrokenLoad();
}

var rr = new Regex(@"[\d]{1,1000}\.");
var pgnStrings = SplitPgns(based);
foreach (var pgnStr in pgnStrings)
{
    var useBrokenMethod = false;
    if (useBrokenMethod)
    {
        try
        {
            var cb = ChessBoard.LoadFromPgn(pgnStr);
            //okay, so personally loading the pgn myself DOES work and I can draw the board right too??
            boards.Add(cb);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load game, skipping. Exception: {ex}\r\nPGN={pgnStr}");
        }
    }
    else
    {
        //var usep = "[Event \"zt 3.5 Men\"]\r\n[Site \"Beijing CHN\"]\r\n[Date \"2009.04.19\"]\r\n[Round \"3\"]\r\n[White \"Yu Yangyi\"]\r\n[Black \"Ding Liren\"]\r\n[Result \"1/2-1/2\"]\r\n[WhiteElo \"2433\"]\r\n[BlackElo \"2458\"]\r\n[ECO \"C13\"]\r\n\r\n1.e4 e6 2.d4 d5 3.Nc3 Nf6 4.Bg5 dxe4 5.Nxe4 Be7 6.Bxf6 gxf6 7.Nf3 f5 8.Nc3 a6\r\n9.Qd2 b5 10.O-O-O b4 11.Na4 Bb7 12.Bc4 Bd5 13.Qe2 Nc6 14.Rhe1 Na5 15.Bxd5 Qxd5\r\n16.b3 Nc6 17.c4 bxc3 18.Nxc3 Qa5 19.Qc4 Nb4 20.Kb1 O-O 21.Ne5 Rad8 22.g4 f4\r\n23.Nd3 Nxd3 24.Rxd3 Bf6 25.Red1 Bg7 26.Qc5 Qxc5 27.dxc5 Rxd3 28.Rxd3 f5 29.gxf5 Bxc3\r\n30.Rxc3 Rxf5 31.Kc2 Rg5 32.Kd3 Rg2 33.Ke2 Rxh2 34.Rd3 e5 35.Rd5 e4 36.Rg5+ Kh8\r\n37.Rg4 e3 38.Rxf4 exf2 39.Rf7 c6 40.a4 Kg8 41.Rc7 Rh3 42.Rxc6 Rxb3 43.Rxa6 Rb2+\r\n44.Kf1 Kf7 45.a5 Ke8 46.Ra8+ Kd7 47.a6 Ra2 48.a7 Kc7  1/2-1/2";

        var usep = pgnStr;
        //usep = "[Event \"China vs World Team 2018\"]\r\n[Site \"Liaocheng CHN\"]\r\n[Date \"2018.04.09\"]\r\n[Round \"7.4\"]\r\n[White \"Shankland,S\"]\r\n[Black \"Ding Liren\"]\r\n[Result \"1/2-1/2\"]\r\n[WhiteElo \"2671\"]\r\n[BlackElo \"2778\"]\r\n\r\n\r\n1.d4 Nf6 2.c4 e6 3.Nc3 Bb4 4.e3 O-O 5.Ne2 Re8 6.a3 Bf8 7.Ng3 d5 8.Be2 b6\r\n9.cxd5 exd5 10.O-O Bb7 11.b4 Nbd7 12.Qb3 c6 13.Bf3 a5 14.b5 c5 15.Nxd5 a4\r\n16.Qa2 Nxd5 17.Bxd5 Bxd5 18.Qxd5 cxd4 19.exd4 Nf6 20.Qf3 Qxd4 21.Bg5 Qd5\r\n22.Bxf6 Qxf3 23.gxf3 gxf6 24.Ne4 Re6 25.Rfc1 Ra5 26.Rc4 Rxb5 27.Rxa4 Ra5\r\n28.Rxa5 bxa5 29.a4 f5 30.Ng3 f4 31.Nh5 Rg6+ 32.Kh1 Bd6 33.Rc1 f5 34.Rc4 Rh6\r\n35.Rd4 Kf7 36.Nxf4 Be5 37.Rc4 Rh4 38.Nd3 Rxh2+ 39.Kg1 Bd6 40.Rd4 Rh6 41.Rd5 Ke6\r\n42.Rxa5 Rh4 43.Kf1 h5 44.Ra8 Rh1+ 45.Ke2 h4 46.Rh8 h3 47.f4 Ra1 48.Rxh3 Rxa4\r\n49.Rh6+ Ke7 50.Ke3 Re4+ 51.Kf3 Rd4 52.Ne5 Bxe5 53.fxe5 Re4  1/2-1/2";

        //okay i fake the loading and then just manually play through the moves. ha.
        var parts = usep.Split("\r\n\r\n");
        var result = parts[1].Split("  ")[1].Trim();

        var moves = parts[1].Replace(result, "").Replace("  ", "");
        var joined = string.Join('\n', moves).Replace("\r\n", " ");
        joined = rr.Replace(joined, "").Trim();

        //will this work - telling the loader that just the first move happened, then the game ended, but actually adding on moves after?
        //on the theory that MOVES work but loading from pgn including EP doesnt work?
        var fakeString = parts[0] + "\r\n\r\n";
        var b = ChessBoard.LoadFromPgn(fakeString);

        foreach (var m in joined.Split(' '))
        {
            //Console.WriteLine(b.ToAscii());

            //if the EP is from the b file, is it confused by bishop vs b pawn doing EP? both error cases were from that file.
            try
            {
                b.Move(m);
            }
            catch (Exception ex)
            {
                var mm = b.Moves();
                //the bug in this case is that it fails to generate the ep move from the prior position.
                var ae = 3;
            }
        }

        if (b.EndGame == null)
        {

            if (result == "1-0")
            {
                b!.Resign(PieceColor.Black);
            }
            else if (result == "0-1")
            {
                b!.Resign(PieceColor.White);
            }
            else if (result == "1/2-1/2")
            {
                b!.Draw();
            }
            else
            {
                throw new Exception("E");
            }
        }
        boards.Add(b);
        //break;
    }

    ct++;
    if (ct > maxGamesToProcess) { break; }
}


var booleanEvaluators = new List<IBooleanEvaluator>() { new RookTakesAQueenEvaluator(), new KingInACornerEvaluator(), new PawnTakesAQueenEvaluator(), new AnyOppositeSideCastlingGameEvaluator(), new AnyPawnPromotionEvaluator(), new QueenInACornerEvaluator(), new AnyTimeOutEvaluator(), new FourteenPieceGameEndEvaluator(), new Connect5Evaluator(), new Connect3Evaluator(), new TripledPawnEvaluator(), new Connect4Evaluator(), new QuadrupledPawnEvaluator(), new TwoPawnPromotionsInOneGameEvaluator(), new BishopManualUndoEvaluator()
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

