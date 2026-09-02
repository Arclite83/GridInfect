using System;

namespace GridInfect.Core.Solving
{
    // A set of board cells. The board has 66 cells, two more than a ulong
    // holds, so this is two words with the bit operators the solver needs.
    public readonly struct CellMask : IEquatable<CellMask>
    {
        public readonly ulong Lo;   // cells 0..63
        public readonly ulong Hi;   // cells 64..65

        public static readonly CellMask None = new CellMask(0, 0);

        public CellMask(ulong lo, ulong hi)
        {
            Lo = lo;
            Hi = hi;
        }

        public static CellMask Bit(int loc) => loc < 64 ? new CellMask(1ul << loc, 0) : new CellMask(0, 1ul << (loc - 64));

        public bool Has(int loc) => loc < 64 ? (Lo >> loc & 1) != 0 : (Hi >> (loc - 64) & 1) != 0;
        public bool IsEmpty => Lo == 0 && Hi == 0;
        public bool Contains(CellMask other) => (other.Lo & ~Lo) == 0 && (other.Hi & ~Hi) == 0;
        public bool Intersects(CellMask other) => (Lo & other.Lo) != 0 || (Hi & other.Hi) != 0;

        public int Count
        {
            get
            {
                int n = 0;
                ulong m = Lo;
                while (m != 0) { m &= m - 1; n++; }
                m = Hi;
                while (m != 0) { m &= m - 1; n++; }
                return n;
            }
        }

        public static CellMask operator |(CellMask a, CellMask b) => new CellMask(a.Lo | b.Lo, a.Hi | b.Hi);
        public static CellMask operator &(CellMask a, CellMask b) => new CellMask(a.Lo & b.Lo, a.Hi & b.Hi);
        public static CellMask operator ~(CellMask a) => new CellMask(~a.Lo, ~a.Hi & 3ul);
        public static bool operator ==(CellMask a, CellMask b) => a.Lo == b.Lo && a.Hi == b.Hi;
        public static bool operator !=(CellMask a, CellMask b) => !(a == b);

        public bool Equals(CellMask other) => this == other;
        public override bool Equals(object obj) => obj is CellMask m && m == this;
        public override int GetHashCode() => (Lo ^ (Hi * 0x9E3779B97F4A7C15ul)).GetHashCode();
        public override string ToString() => $"{Hi:X}:{Lo:X16}";
    }
}
