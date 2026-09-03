using UnityEngine;

namespace GridInfect.Services
{
    // R-602: the cadence values live in an asset, not code. Create one under
    // Resources as "AdCadence"; AdGate falls back to the defaults without it.
    [CreateAssetMenu(fileName = "AdCadence", menuName = "Grid Infect/Ad Cadence")]
    public sealed class AdCadenceConfig : ScriptableObject
    {
        public AdCadence Cadence = new AdCadence();
    }
}
