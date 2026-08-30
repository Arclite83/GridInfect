using System;

namespace Bloodhound.Engine
{
    /// <summary>
    /// PCG-XSH-RR 32-bit generator (O'Neill, pcg-random.org), fixed constants.
    ///
    /// This is the engine's only randomness source. It is a mutable struct on
    /// purpose: the RNG state is part of game state, so any consumer that wants
    /// deterministic replay passes the seed through an action input and draws
    /// from its own stream. Draw order is contract: changing the number or
    /// order of draws in any consumer is a versioned change.
    /// </summary>
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

        /// <summary>
        /// Uniform-ish draw in [0, bound) via modulo. Modulo bias is accepted
        /// and documented: this port defines its own deterministic contract
        /// (the original used libc rand() % n, which was never reproducible
        /// cross-platform anyway). Golden tests lock the sequences.
        /// </summary>
        public int Next(int bound)
        {
            if (bound <= 0) throw new ArgumentOutOfRangeException(nameof(bound), "bound must be positive");
            return (int)(NextUInt() % (uint)bound);
        }
    }
}
