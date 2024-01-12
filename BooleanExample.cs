using Chess;

namespace PawnCube
{
    public class BooleanExample
    {
        public BooleanExample(ChessBoard testBoard, string det)
        {
            //I should actually make a copy of this testboard so the original isn't touched.
            var copy = new ChessBoard();
            foreach (var m in testBoard.ExecutedMoves)
            {
                copy.Move(m);

            }
            foreach (var h in testBoard.Headers)
            {
                copy.AddHeader(h.Key, h.Value);
            }

            Board = copy;
            Details = det;
        }

        /// <summary>
        /// in the demonstrated position
        /// </summary>
        public ChessBoard Board { get; set; }

        //move number or whatever explaining the example.
        public string Details { get; set; }
    }
}