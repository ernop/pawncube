namespace PawnCube
{
    /// <summary>
    /// basically you'll have these classes which are responsible for looking for one thing. you'll call them with various params like whether
    /// they should evaluate all the boards or just some looking for a result.  they'll return some kind of summary result which is this thing.
    /// </summary>
    public class BooleanEvaluationResult
    {
        public bool Result
        {
            get
            {
                return Examples.Count() > 0;
            }
        }
        /// <summary>
        /// basically unused since there's no real legal situation where you can compute this except in the outermost layer.
        /// </summary>
        public string Details { get; }
        public int NumberOfBoardsEvaluated { get; set; } = 0;
        public IEnumerable<BooleanExample> Examples { get; }
        public BooleanEvaluationResult(string details, IEnumerable<BooleanExample> examples)
        {
            Details = details;
            Examples = examples;
        }
    }
}