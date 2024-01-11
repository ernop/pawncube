namespace PawnCube
{
    public class BooleanEvaluationResult
    {
        public bool Result { get; }
        public string Details { get; }
        public BooleanEvaluationResult(bool res, string details)
        {
            Result = res;
            Details = details;
        }
    }
}