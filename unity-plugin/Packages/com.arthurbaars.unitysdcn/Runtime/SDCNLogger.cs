using System;
using UnityEngine;

namespace UnitySDCN {
    internal static class SDCNLogger {
        /**
        * Log a message to the Unity console
        * 
        * @param message The message to log
        * @param verbosity The verbosity level of the message
        */
        internal static void Log(Type origin, string message, SDCNVerbosity verbosity = SDCNVerbosity.None) {
            if (SDCNManager.Instance?.LogVerbosity >= verbosity)
                Debug.Log($"{origin}: {message}");
        }

        /**
        * Log a warning to the Unity console
        * 
        * @param message The warning message to log
        * @param verbosity The verbosity level of the message
        */
        internal static void Warning(Type origin, string message, SDCNVerbosity verbosity = SDCNVerbosity.None) {
            if (SDCNManager.Instance?.LogVerbosity >= SDCNVerbosity.None)
                Debug.LogWarning($"{origin}: {message}");
        }

        /**
        * Log an error to the Unity console
        * 
        * @param message The error message to log
        * @param verbosity The verbosity level of the message
        */
        internal static void Error(Type origin, string message, SDCNVerbosity verbosity = SDCNVerbosity.None) {
            if (SDCNManager.Instance?.LogVerbosity >= SDCNVerbosity.None)
                Debug.LogError($"{origin}: {message}");
        }
    }

    [Serializable]
    // Level of verbosity for logging messages
    public enum SDCNVerbosity {
        None = 0,
        Minimal,
        Verbose
    };
}