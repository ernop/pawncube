using Chess;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static PawnCube.Statics;

namespace PawnCube
{
    /// <summary>
    /// note that this uses + and - material advantages, so it's a measure of how far ahead W is.
    /// </summary>
    public class TenPercentForEachPointOfMaterialAdvantageInFinalPositionEvaluator : NumericalPerBoardEvaluator
    {
        public override string Name => nameof(TenPercentForEachPointOfMaterialAdvantageInFinalPositionEvaluator);

        public override NumericalEvaluationResult Aggregate(IEnumerable<NumericalExample> examples)
        {
            var tot = examples.Select(el => el.Value).Sum();
            var det = $"Total {tot}. At the end of the game, B ahead: {examples.Select(el => el.Value).Where(el => el < 0).Count()} times." +
                $"W ahead: {examples.Select(el => el.Value).Where(el => el == 0).Count()} times." +
                $"Same: {examples.Select(el => el.Value).Where(el => el > 0).Count()} times.";
            return new NumericalEvaluationResult(tot * 10, det, examples.Where(el=>el.Value!=0));
        }

        public override NumericalExample InnerEvaluate(ChessBoard board)
        {
            board.Last();
            var d = GetMaterialDifference(board);
            return new NumericalExample(board, $"Advantage: {d}", board.ExecutedMoves.Count()-1, d);
        }
    }

    public class BiggestPawnMaterialLeadInAnyGameTwentyPercentPerPawnLeadOnBoardEvaluator : NumericalPerBoardEvaluator
    {
        public override string Name => nameof(BiggestPawnMaterialLeadInAnyGameTwentyPercentPerPawnLeadOnBoardEvaluator);

        public override NumericalEvaluationResult Aggregate(IEnumerable<NumericalExample> examples)
        {
            var mx = 0;
            ChessBoard bestGame = null;
            NumericalExample bestExample = null;
            foreach (var example in examples)
            {
                if (example.Value > mx)
                {
                    mx = example.Value;
                    bestGame = example.Board;
                    bestExample = example;
                }
            }
            var det = $"The biggest on-board pawn lead was:{mx}";
            return new NumericalEvaluationResult(mx * 20, det, new List<NumericalExample>() { bestExample });
        }

        public override NumericalExample InnerEvaluate(ChessBoard board)
        {
            board.GoToStartingPosition();
            var maxGap = 0;
            var bestMoveNumber = 0;
            var moveNumber = 0;

            foreach (var move in board.ExecutedMoves)
            {
                board.Next();
                var allp = Statics.GetAllPieces(board);
                var wpawns = allp.Where(p => p.Color == PieceColor.White && p.Type == PieceType.Pawn).Count();
                var bpawns = allp.Where(p => p.Color == PieceColor.Black && p.Type == PieceType.Pawn).Count();
                var theGap = Math.Abs(bpawns - wpawns);
                if (theGap > maxGap)
                {
                    maxGap = Math.Max(maxGap, theGap);
                    bestMoveNumber = moveNumber;
                }
                
                moveNumber++;
            }

            return new NumericalExample(board, $"game {DescribeChessBoard(board)} at move {bestMoveNumber} {board.ExecutedMoves[bestMoveNumber]} one side had a pawn advantage of {maxGap}", bestMoveNumber, maxGap);
        }
    }
}