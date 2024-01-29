using Chess;

namespace PawnCube
{
    /// <summary>
    /// boolean searchers will create one of these, with a useful custom description specific to it
    /// for every time it's found.
    /// (things like move number, board layout can be generically added later)
    /// </summary>
    public class BooleanExample: IChessBoardExample
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

    /// <summary>
    /// These are slightly weird since your summary is produced somewhat dynamically.
    /// However, there are still lots of cases where htis is useful.
    /// For example: 10% for the game with the most surviving pawns.
    /// It's numerical because it's a calculation, but "examples" are sensible since specific games
    /// will determine the entire result.
    /// </summary>
    public class NumericalExample : IChessBoardExample
    {
        public NumericalExample(ChessBoard board, string det, int moveNumber, int value)
        {
            Board = board;
            Details = det;
            ExampleMoveIndex = moveNumber;
            Value = value;
        }

        /// <summary>
        /// this is the original!
        /// </summary>
        public ChessBoard Board { get; set; }
        public int Value { get; set; }

        //move number or whatever explaining the example.
        public string Details { get; set; }
        public int ExampleMoveIndex { get; set; }
    }

    public interface IChessBoardExample {         
        ChessBoard Board { get; }
        string Details { get; }
        int ExampleMoveIndex { get; }
    }
}