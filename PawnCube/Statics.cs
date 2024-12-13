using Chess;

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PawnCube
{
    internal static class Statics
    {
        public static int NumberOfExamplesToCollect = int.MaxValue;
        public static int NumberOfExamplesToShow = 1;

        /// <summary>
        /// The point of all this is to judge these two prediction markets
        /// </summary>
        static string RelatedMarket1 = "https://manifold.markets/Ernie/rosen-score-of-weird-thing-that-wil";
        static string RelatedMarket2 = "https://manifold.markets/Ernie/what-will-happen-during-ding-lirens";
        internal static Regex numberMatcher = new Regex(@"[\d]{1,1000}\.");

        public static IEnumerable<BoardSet> LoadBoardSets()
        {
            var paths = new List<string>();
            //paths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Ding\Ding.pgn"));
            //paths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Ding\ding-liren-tata-steel-2024.pgn"));
            paths.Add(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Ding\ding-gukesh-2024-championship.pgn"));
            var maxGamesToProcess = 20000;
            var ct = 0;
            foreach (var path in paths)
            {

                var boards = new List<ChessBoard>();
                var pgnStrings = Statics.SplitPgns(path);
                Console.WriteLine($"Loading PGNStrings. {pgnStrings.Count()}");

                foreach (var pgnStr in pgnStrings)
                {
                    boards.Add(Statics.Pgn2Board(pgnStr.Trim()));
                    ct++;
                    if (ct >= maxGamesToProcess)
                    {
                        break;
                    }
                }
                var boardSet = new BoardSet(path, boards);

                yield return boardSet;
            }
        }

        /// <summary>
        /// okay, so some PGNs have moves in them from beyond the extent of the actual game.
        /// i.e. at least this engine thinks the game is already ended but the PGN has move moves.
        /// That's fine, but the problem is, it throws then.
        /// So, let's just clean that up here by cutting those all off
        /// So that internally, there will be a guarantee that ChessBoard c does not have that problem, ever.
        /// </summary>
        public static ChessBoard Pgn2Board(string pgnStr)
        {
            var usep = pgnStr;
            var parts = usep.Split("\r\n\r\n");
            var moveParts = parts[1].Replace("\r\n", " ");
            while (true)
            {
                if (moveParts.Contains("  "))
                {
                    moveParts = moveParts.Replace("  ", " ");
                }
                else
                {
                    break;
                }
            }
            var moveParts2 = moveParts.Split(" ");
            ////the result is magically the last item in the space-split list of moves. ugh.
            var result = moveParts2[moveParts2.Count() - 1].Trim();

            var moves = parts[1].Replace(result, "").Replace("  ", "");
            var joined = string.Join('\n', moves).Replace("\r\n", " ");
            joined = numberMatcher.Replace(joined, "").Trim();

            //will this work - telling the loader that just the first move happened, then the game ended, but actually adding on moves after?
            //on the theory that MOVES work but loading from pgn including EP doesnt work?
            var fakeString = parts[0] + "\r\n\r\n";
            var board = ChessBoard.LoadFromPgn(fakeString);
            if (joined.ToLower().Contains("e.p."))
            {
                Console.WriteLine(pgnStr);
                Console.WriteLine(joined);
            }

            foreach (var m in joined.Split(' '))
            {
                if (string.IsNullOrEmpty(m)) { continue; }
                //if the EP is from the b file, is it confused by bishop vs b pawn doing EP? both error cases were from that file.
                try
                {
                    board.Move(m);
                }
                catch (Chess.ChessGameEndedException ex)
                {
                    Console.WriteLine(ex.Message);
                    var qq = 4;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    var mm = board.Moves();
                    //the bug in this case is that it fails to generate the ep move from the prior position.
                    var ae = 3;
                }
            }

            if (board.EndGame == null)
            {

                if (result == "1-0")
                {
                    board!.Resign(PieceColor.Black);
                }
                else if (result == "0-1")
                {
                    board!.Resign(PieceColor.White);
                }
                else if (result == "1/2-1/2")
                {
                    board!.Draw();
                }
                else
                {
                    throw new Exception("E");
                }
            }

            board.GoToStartingPosition();

            return board;
        }

        public static IEnumerable<string> SplitPgns(string fp)
        {

            var lines = System.IO.File.ReadAllText(fp);
            var parts = lines.Split("[Event ");
            foreach (var p in parts)
            {
                if (string.IsNullOrEmpty(p)) { continue; }
                yield return "[Event " + p;
            }
        }

        public static string DescribeChessBoard(ChessBoard board)
        {

            var h = board.Headers;
            var res = $"{h.GetValueOrDefault("White")}-{h.GetValueOrDefault("Black")} {h.GetValueOrDefault("Date")}";
            return res;
        }

        public static bool IsInBounds(Tuple<short, short> inp)
        {
            if (inp.Item1 < 0 || inp.Item1 > 7 || inp.Item2 < 0 || inp.Item2 > 7) { return false; }
            return true;
        }

        public static string MakeNormalMoveNumberDescriptor(int plies)
        {
            if (plies == 0)
            {
                return "Initial Position";
            }
            var s = plies / 2.0;
            if (plies % 2 == 1)
            {
                return $"B{Math.Floor(s) + 1}";
            }
            return $"W{Math.Floor(s) + 1}";
        }

        public static List<Piece> GetAllPieces(ChessBoard board)
        {
            var res = new List<Piece>();
            for (short xx = 0; xx < 8; xx++)
            {
                for (short yy = 0; yy < 8; yy++)
                {
                    var p = board[xx, yy];
                    if (p == null) { continue; }
                    res.Add(p);
                }
            }
            return res;
        }

        public static List<Tuple<Piece,Position>> GetAllPiecesAndPositions(ChessBoard board)
        {
            var res = new List<Tuple<Piece,Position>>();
            for (short xx = 0; xx < 8; xx++)
            {
                for (short yy = 0; yy < 8; yy++)
                {
                    var piece = board[xx, yy];
                    if (piece == null) { continue; }
                    res.Add(new Tuple<Piece,Position>(piece, new Position(xx,yy)));
                }
            }
            return res;
        }

        public static List<Piece> GetAllCaptures(ChessBoard board)
        {
            var res = new List<Piece>();
            res.AddRange(board.CapturedBlack);
            res.AddRange(board.CapturedWhite);
            return res;
        }

        /// <summary>
        /// +x for w, -x means b leads on board.
        /// </summary>
        public static int GetMaterialDifference(ChessBoard board)
        {
            var count = 0;
            
            for (short xx = 0; xx < 8; xx++)
            {
                for (short yy = 0; yy < 8; yy++)
                {
                    var piece = board[new Position(xx,yy)];
                    var mult = 1;
                    if (piece == null) { continue; }
                    if (piece.Color == PieceColor.Black) { mult = -1; }
                    if (piece.Type == PieceType.Pawn) { mult *= 1; }
                    if (piece.Type == PieceType.Knight) { mult *= 3; }
                    if (piece.Type == PieceType.Bishop) { mult *= 3; }
                    if (piece.Type == PieceType.Rook) { mult *= 5; }
                    if (piece.Type == PieceType.Queen) { mult *= 9; }
                    count += mult;
                }
            }
            return count;
        }

        public static string getResult(ChessBoard board)
        {
            var winarar = "";
            if (board.EndGame.WonSide != null)
            {
                winarar = board.EndGame.WonSide == PieceColor.Black ? "Black - " : "White - ";
            }

            var result = $"{winarar}{board.EndGame.EndgameType}";
            return result;
        }

        public static string MakeGenericDetails(IChessBoardExample be)
        {
            var ply = be.ExampleMoveIndex;
            var extraDots = ply % 2 == 0 ? "" : " ..";
            var normalMoveNumber = Statics.MakeNormalMoveNumberDescriptor(ply);

            var res = $"{normalMoveNumber}:{extraDots}{be.Board.ExecutedMoves[ply]}\t{Statics.DescribeChessBoard(be.Board)}";
            return res;
        }

        public static void DisplayWholeGame(ChessBoard board)
        {
            var moves = board.ExecutedMoves;
            board.GoToStartingPosition();
            Console.WriteLine(Statics.DescribeChessBoard(board));
            var ii = 0;
            foreach (var m in moves)
            {
                ii++;
                board.Next();
                Console.WriteLine($"{board.ToAscii()}\r\nMoveNumber:{ii}, {m}");
            }
        }

        public static string items = @"Daniel Naroditsky is one of the live commentators
1% for each move in the longest game
King in a corner
We get to a bishop and knight endgame
10% every time a pawn makes the first move in a game, -10% every time a knight does
10% for each resignation
Rook takes a Queen
Pawn takes a Queen
40% for every black win, -10% for every white win
Opposite side Castling game
A pawn promotes
Bishop manually undoes move (a bishop moves somewhere then immediately goes back to where it started))
Queen in a corner
+10% every short castle, -5% every long castle
10% for each draw
7% for each draw
20% for each extra pawn held by the side with the greatest pawn advantage, in any position in any game
5% for each queen which survives to the end of a game
10% * n where n is the side length of the largest n x n empty square that appears in any final position in any game
10% for each point of material advantage in any game's final position
1% for every piece on its starting square in the final position of every game
5% for every dead bishop, -1% for every dead pawn, final positions
B is ever ahead on points (add up all points from B and W in these games, is B >0?)
-TBD
10% for every point of advantage W has in all the ending positions (including negative, when B is ahead)
A King and a knight are in the center 4 squares
At any point in a game, 2 bishops and 0 knights on one side oppose 2 knights and 0 bishops
- TBD
+1% every surviving pawn, -2% every surviving knight, bishop, or rook
Tripled pawns
En passant refused
5% for every pawn who jumps two steps in his first move, -5% for every pawn who takes one step or captures in his first move
3% for every knight move that's more vertical, -4% for every one that's more horizontal
10% for each nonpawn which never moves in a game
If game were immediately switched to self-capture chess, one side would have mate in 1
20% for each win in the longest consecutive win streak by one player
0.5% for each move in the longest game
Two pawn promotions in one game
Same piece moves eight times in a row for one side
10% for each win
There is ever Q vs RR (just covering those pieces, ignoring other material)
20% for every decisive game, -10% for ever non-decisive game.
Chess speaks for itself said by any player or commentator in an official interview or broadcast
20% for each En Passant capture
Castle after move 40
No captures before move 30
More knights survive than bishops, judging by the final position of each game
A game ends with a full complement of knights and bishops
A full rank (at least two non original pieces)
10% for each black win
Bishop in a corner
A full file
First piece captured in a game is a rook
First piece captured of any kind in a game is a queen
A queen en prise for three moves in a row
Someone wins while down >=4 pts of material on board
Both kings are on the wrong side of the board (the half they didn't start in)
All pawns lost and castling not possible. This means we can forget which side is which, now.
Someone voluntarily or accidentally doesn't play a game resulting in a loss, but no honor violation
Game where there are 7 or more pieces and all pieces are on the same color square
Board Tilt - any half of the board has at least 10 pieces while the other half is empty. h/v only, no diagonal
Knight in a corner
The first piece to capture anything in a game is a queen
A side has two queens at once
At least three decisive games in a row
10% for each white win
A King reaches the opponents home edge
2 knights on the rim (of any color) at the same time
100% - (1% for each move in the shortest game)
One side has 4 or more pawns than the other in a game
Double check
5% for every pawn promoted
A player is fined by FIDE during the match
There will be a 3x3 square full of pieces
A player is more than 1m late to the board after a game starts
Ding ties for lead at the end of 14
No castling game
Pawn Cube
20% for every stalemate
10% for every queen taken by a king
A pawn promotes to a non-queen and checks
A single piece captures at least 19 points of material in a game
Castle with Check
More than two pawn promotions in a game
A game ends with exactly fourteen pieces on board
Shenanigans resulting in a game result being determined off the board (judges ruling, etc.) due to misbehavior
Connect 5
Any win by time out
Quadrupled Pawns
Connect 6
Someone has a mate in <=3 but loses that game
All 4 corners occupied by non-rooks at one time
Center Pawn Cube
10% for each checkmate played out on board
10% for every piece which ends a game in its starting square after having moved, over all games
A pawn promotes and also double check
En Passant capture
A bishop moves seven or more squares at once
Checkmate with a Pawn
A pawn promotion to a non-queen
En passant capture and double check
A pawn promotes to a non-queen and checkmates";
    }

}
