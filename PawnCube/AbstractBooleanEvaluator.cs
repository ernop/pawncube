using Chess;

using static PawnCube.Statics;

namespace PawnCube
{
    /// <summary>
    /// to store the shared logic where in general for all boolean evaluators, depending on what we ask,
    /// at least track every time they run a test on a bunch of board, just having the single evaluator decide 
    /// where an individual board satisfies the situation or not.
    /// hmm, but how can an outer layer assemble all this information?  well,
    /// these ARE boolean evaluators after all so it's guaranteed that cross-board resolutions are not necessary.
    /// </summary>
    public abstract class AbstractBooleanEvaluator
    {
        public abstract IEnumerable<BooleanExample> RunOne(ChessBoard board);

        public BooleanEvaluationResult Evaluate(bool doAll, List<ChessBoard> boards)
        {
            var examples = new List<BooleanExample>();
            var ber = new BooleanEvaluationResult("", examples);
            var ii = 0;
            foreach (var board in boards)
            {
                ii++;

                ber.NumberOfBoardsEvaluated++;
                //var boardCopy = CopyBoardBase(board);

                //wow, if you don't treat this as a list,
                //it will literally treat this as a lambda to repeatedly execute
                //every time you reference it. Which will cause the internal board objects
                //to be continuously rerun, which would mean tons of redo calculations.
                board.GoToStartingPosition();

                var potentialExamples = RunOne(board).ToList();

                if (potentialExamples == null)
                {
                    continue;
                }
                if (potentialExamples.Count() == 0)
                {
                    continue;
                }

                //it was an example.

                examples.AddRange(potentialExamples);

                if (doAll)
                {
                    
                }
                if (examples.Count >= Statics.NumberOfExamplesToCollect)
                {
                    Console.WriteLine($"\t\thit limit.");
                    return ber;
                }

            }
            return ber;
        }
    }
}
