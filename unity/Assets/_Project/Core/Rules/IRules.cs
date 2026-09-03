namespace GridInfect.Core
{
    // The mechanics as a session sees them. Only actions call these; the
    // classic rules (Rules, frozen, proven by the 128 vectors) and RulesV2
    // (docs/RULES_V2.md) are the two implementations.
    public interface IRules
    {
        bool CanPlace(LevelSession s, int pieceIndex, int i, int j);
        void SetPiece(LevelSession s, int pieceIndex, int i, int j);
        void Resolve(LevelSession s);
        void ClearPiece(LevelSession s, int pieceIndex);
        void FullReset(LevelSession s);
    }

    public sealed class RulesV1 : IRules
    {
        public static readonly RulesV1 Instance = new RulesV1();

        public bool CanPlace(LevelSession s, int pieceIndex, int i, int j) => Rules.CanPlace(s, pieceIndex, i, j);
        public void SetPiece(LevelSession s, int pieceIndex, int i, int j) => Rules.SetPiece(s, pieceIndex, i, j);
        public void Resolve(LevelSession s) => Rules.Resolve(s);
        public void ClearPiece(LevelSession s, int pieceIndex) => Rules.ClearPiece(s, pieceIndex);
        public void FullReset(LevelSession s) => Rules.FullReset(s);
    }
}
