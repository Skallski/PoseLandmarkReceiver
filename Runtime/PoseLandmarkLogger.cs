namespace PoseLandmarkReceiver
{
    internal static class PoseLandmarkLogger
    {
        private const string MSG_HEADER = "<color=blue>[POSE LANDMARK RECEIVER]</color>";
        
        internal static void Log(string msg)
        {
            UnityEngine.Debug.Log($"{MSG_HEADER} {msg}");
        }
        
        internal static void LogWarning(string msg)
        {
            UnityEngine.Debug.LogWarning($"{MSG_HEADER} {msg}");
        }

        internal static void LogError(string msg)
        {
            UnityEngine.Debug.LogError($"{MSG_HEADER} {msg}");
        }
    }
}