using UnityEngine;

namespace SleepyStudios.LoopScroll.Tests
{
    public sealed class AwakeInitializedCell : MonoBehaviour
    {
        public bool Initialized { get; private set; }
        private void Awake() { Initialized = true; }
    }
}
