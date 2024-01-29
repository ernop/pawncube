using Chess;

using System;
using System.Collections.Generic;
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

        public override int Aggregate(IEnumerable<int> results, out string det)
        {
            var tot = results.Sum();
            det = $"Total {tot}, where {results.Where(el=>el<0).Count()} times B was ahead; {results.Where(el => el == 0).Count()} times +0, {results.Where(el => el > 0).Count()} times W ahead on board.";
            return tot * 10;
        }

        public override int InnerEvaluate(ChessBoard board)
        {
            board.Last();
            return(GetMaterialDifference(board));
        }
    }
}