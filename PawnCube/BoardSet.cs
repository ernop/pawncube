using Chess;

namespace PawnCube
{
    internal static partial class Statics
    {
        /// <summary>
        /// For loading.
        /// </summary>
        public class BoardSet
        {
            public string Path { get; set; }
            public List<ChessBoard> Boards { get; set; }
            public BoardSet(string path, List<ChessBoard> boards)
            {
                Path = path;
                Boards = boards;
            }
        }
    }

}
