namespace PawnCube
{
    public class BooleanEvaluationResult
    {
        public bool Result
        {
            get
            {
                return Examples.Count() > 0;
            }
        }

        public string Details { get; }
        public IEnumerable<BooleanExample> Examples { get; }
        public BooleanEvaluationResult(string details, IEnumerable<BooleanExample> examples)
        {
            Details = details;
            Examples = examples;
        }
    }
}