    // Diagnostic for the launch crash of 2026-09-13: every Java-to-C#
    // callback died with UnsatisfiedLinkError on
    // com.unity3d.player.ReflectionHelper.nativeProxyInvoke, the bridge Unity
    // registers for AndroidJavaProxy. Cause: the Android JNI built-in module
    // (com.unity.modules.androidjni) was not in Packages/manifest.json. The
    // precompiled AdMob DLLs still compiled and their outgoing Java calls
    // still worked, but the module's native side, which registers that
    // bridge, was not in the player. This fires a Runnable proxy of our own
    // from the Android main looper once a second and logs each arrival, so
    // a development build shows "[jni]" ticks in logcat when the bridge is
    // present. Development builds only (GameApp).
    static class JniProbe
    {
        sealed class Tick : AndroidJavaProxy
        {
            int _n;
            readonly DateTime _t0 = DateTime.UtcNow;
            public Tick() : base("java.lang.Runnable") { }

            // Called on the Android main thread. Nothing here may throw: an
            // exception leaving a proxy is a java.lang.Error on that thread.
            public void run()
            {
                try
                {
                    _n++;
                    Debug.Log($"[jni] proxy callback #{_n} ok at {(DateTime.UtcNow - _t0).TotalSeconds:F1}s");
                    Post(1000);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[jni] probe callback failed: " + e);
                }
            }
        }

        static Tick _tick;
        static AndroidJavaObject _handler;

        public static void Start()
        {
            if (_tick != null) return;
            try
            {
                _tick = new Tick();
                using (var looper = new AndroidJavaClass("android.os.Looper"))
                using (var main = looper.CallStatic<AndroidJavaObject>("getMainLooper"))
                {
                    _handler = new AndroidJavaObject("android.os.Handler", main);
                }
                Debug.Log("[jni] probe armed");
                Post(500);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[jni] probe failed to arm: " + e);
            }
        }

        static void Post(long ms) => _handler.Call<bool>("postDelayed", _tick, ms);
    }
}
#endif
