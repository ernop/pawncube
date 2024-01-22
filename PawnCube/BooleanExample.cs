using Chess;

namespace PawnCube
{
    /// <summary>
    /// boolean searchers will create one of these, with a useful custom description specific to it
    /// for every time it's found.
    /// (things like move number, board layout can be generically added later)
    /// </summary>
    public class BooleanExample
    {
        public BooleanExample(ChessBoard board, string det, int moveNumber)
        {
            Board = board;
            Details = det;
            ExampleMoveIndex = moveNumber;
        }

        /// <summary>
        /// this is the original!
        /// </summary>
        public ChessBoard Board { get; set; }

        //move number or whatever explaining the example.
        public string Details { get; set; }
        public int ExampleMoveIndex { get; set; }
    }
}