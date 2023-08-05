using System;

namespace NamedPipeWrapper
{
    public static class Utility
    {
        public static void DebugLog(string message)
        {
#if UNITY_ENGINE
            Debug.Log(message);
#else 
            Console.WriteLine(message);
#endif
        }
    }
}