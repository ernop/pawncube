using PawnCube;

namespace PawnCube
{
    public class NumericalEvaluationResult
    {
        public int ManifoldResult()
        {
            if (RawResult > 100)
            {
                return 100;
            }
            if (RawResult < 0)
            {
                return 0;
            }
            return RawResult;
        }
        public int RawResult { get; }
        public string Details { get; }
        public int NumberOfBoardsEvaluated { get; set; } = 0;
        public IEnumerable<NumericalExample> Examples { get; }
        public NumericalEvaluationResult(int res, string details, IEnumerable<NumericalExample> examples)
        {
            RawResult = res;
            Details = details;

            //this can be null/empty - in lots of cases it doesn't really make sense.
            Examples = examples ?? new List<NumericalExample>();
        }
    }
}