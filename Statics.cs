﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PawnCube
{
    internal static class Statics
    {

        public static bool IsInBounds(Tuple<short, short> inp)
        {
            if (inp.Item1 < 0 || inp.Item1 > 7 || inp.Item2 < 0 || inp.Item2 > 7) { return false; }
            return true;
        }


        public static string MakeNormalMoveNumberDescriptor(int n)
        {
            var s = n / 2.0;
            if (s % 1 == 0) { return $"Move {Math.Floor(s) + 1}, W"; }
            return $"Move {Math.Floor(s) + 1}, B";
        }


        static string items = @"Daniel Naroditsky is one of the live commentators
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