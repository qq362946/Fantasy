using UnityEngine;

namespace SRDebugger.Services
{
    public delegate void ConsoleStateChangedEventHandler(LogType logType, bool newState);

    public interface IConsoleFilterState
    {
        event ConsoleStateChangedEventHandler FilterStateChange;

        /// <summary>
        /// Set whether log messages with <paramref name="logType"/> severity
        /// should be displayed in the SRDebugger console.
        /// </summary>
        /// <param name="logType">Type of message (only Error/Warning/Log are used. <see cref="LogType.Exception"/> and <see cref="LogType.Assert"/> will redirect to <see cref="LogType.Error"/></param>
        /// <param name="enabled">True to display the log type, false to hide.</param>
        void SetConsoleFilterState(LogType logType, bool enabled);

        /// <summary>
        /// Get whether log messages with <paramref name="logType"/> severity are
        /// being displayed in the SRDebugger console.
        /// </summary>
        /// <param name="logType">Type of message (only Error/Warning/Log are used. <see cref="LogType.Exception"/> and <see cref="LogType.Assert"/> will redirect to <see cref="LogType.Error"/></param>
        /// <returns>True if the log type is displayed.</returns>
        bool GetConsoleFilterState(LogType logType);
    }
}