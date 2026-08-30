using System;

namespace Bloodhound.Engine
{
    // PCG-XSH-RR (pcg-random.org). RNG state is game state: seeds arrive via
    // action inputs and consumers own their draw order — changing it is a versioned change.
    public struct Pcg32
    {
        const ulong Multiplier = 6364136223846793005ul;

        ulong _state;
        ulong _inc;

        public Pcg32(ulong seed, ulong sequence = 54)
        {
            _state = 0;
            _inc = (sequence << 1) | 1;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = old * Multiplier + _inc;
            uint xorshifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }

        public int Next(int bound)
        {
            if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound), "bound must be positive");
            return (int)(NextUInt() % (uint)bound);
        }
    }
}
