﻿using Chess;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PawnCube
{

    public class FourteenPieceGameEndEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(FourteenPieceGameEndEvaluator);

        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            board.Last();
            if (Statics.GetAllPieces(board).Count() == 14)
            {
                var det = $"game ended by {board.EndGame.EndgameType} with 14 pieces";
                yield return new BooleanExample(board, det, board.ExecutedMoves.Count - 1);
            }
        }
    }

    public class AnyTimeOutEvaluator : AbstractBooleanEvaluator, IBooleanEvaluator
    {
        public string Name => nameof(AnyTimeOutEvaluator);
        public override IEnumerable<BooleanExample> RunOne(ChessBoard board)
        {
            if (board.EndGame != null && board.EndGame.EndgameType == EndgameType.Timeout)
            {
                board.Last();
                var det = "Timeout";
                yield return new BooleanExample(board, det, board.ExecutedMoves.Count - 1);
            }
        }
    }
}
