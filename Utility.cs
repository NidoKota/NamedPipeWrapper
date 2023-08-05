using System;

#if UNITY_EDITOR
using UnityEngine;
#endif 

namespace NamedPipeWrapper
{
    public static class Utility
    {
        public static void DebugLog(string message)
        {
#if UNITY_EDITOR
            Debug.Log(message);
#else 
            Console.WriteLine(message);
#endif
        }
    }
}