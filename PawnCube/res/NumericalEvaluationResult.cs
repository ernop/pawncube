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
        public IEnumerable<BooleanExample> Examples { get; }
        public NumericalEvaluationResult(int res, string details)
        {
            RawResult = res;
            Details = details;
        }
    }
}