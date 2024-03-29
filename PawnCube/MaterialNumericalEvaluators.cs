﻿using Chess;

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
    /// max(abs(advantage any player had at any end game))
    /// </summary>
    public class TenPercentForEachPointOfMaterialAdvantageInAnyFinalPositionEvaluator : NumericalPerBoardEvaluator
    {
        public override string Name => nameof(TenPercentForEachPointOfMaterialAdvantageInAnyFinalPositionEvaluator);

        public override NumericalEvaluationResult Aggregate(IEnumerable<NumericalExample> examples)
        {
            var most = examples.OrderByDescending(el => Math.Abs(el.Value)).First();
            var det = $"Most extreme game end advantage had by anyone was: {most.Details}. ";
            var raw = most.Value * 10;
            return new NumericalEvaluationResult(raw, det, new List<NumericalExample>() { most});
        }

        public override NumericalExample InnerEvaluate(ChessBoard board)
        {
            board.Last();
            var materialDifferenceOnBoard = GetMaterialDifference(board);
            return new NumericalExample(board, $"Advantage: {materialDifferenceOnBoard}", board.ExecutedMoves.Count()-1, materialDifferenceOnBoard);
        }
    }

    /// <summary>
    /// note that this uses + and - material advantages, so it's a measure of how far ahead W is.
    /// </summary>
    public class OverallWhiteGameEndPointAdvantageTenPercentPerEvaluator: NumericalPerBoardEvaluator
    {
        public override string Name => nameof(OverallWhiteGameEndPointAdvantageTenPercentPerEvaluator);

        public override NumericalEvaluationResult Aggregate(IEnumerable<NumericalExample> examples)
        {
            var tot = examples.Select(el => el.Value).Sum();
            var det = $"Total w point advantage in all games: {tot}. At the end of the game, B ahead: {examples.Select(el => el.Value).Where(el => el < 0).Count()} times." +
                $"W ahead: {examples.Select(el => el.Value).Where(el => el == 0).Count()} times." +
                $"Same: {examples.Select(el => el.Value).Where(el => el > 0).Count()} times.";
            var raw = tot * 10;
            return new NumericalEvaluationResult(raw, det, examples.Where(el => el.Value != 0));
        }

        public override NumericalExample InnerEvaluate(ChessBoard board)
        {
            board.Last();
            var d = GetMaterialDifference(board);
            return new NumericalExample(board, $"Advantage: {d}", board.ExecutedMoves.Count() - 1, d);
        }
    }

    public class BiggestPawnMaterialLeadInAnyGameTwentyPercentPerPawnLeadOnBoardEvaluator : NumericalPerBoardEvaluator
    {
        public override string Name => nameof(BiggestPawnMaterialLeadInAnyGameTwentyPercentPerPawnLeadOnBoardEvaluator);

        public override NumericalEvaluationResult Aggregate(IEnumerable<NumericalExample> examples)
        {
            var materialLead = 0;
            ChessBoard bestGame = null;
            NumericalExample bestExample = null;
            foreach (var example in examples)
            {
                if (example.Value > materialLead)
                {
                    materialLead = example.Value;
                    bestGame = example.Board;
                    bestExample = example;
                }
            }
            var det = $"The biggest on-board pawn lead was:{materialLead}";
            var raw = materialLead * 20;
            return new NumericalEvaluationResult(raw, det, new List<NumericalExample>() { bestExample });
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