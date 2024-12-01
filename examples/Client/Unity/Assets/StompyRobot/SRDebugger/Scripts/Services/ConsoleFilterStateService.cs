using System;
using UnityEngine;

namespace SRDebugger.Services
{
    public sealed class ConsoleFilterStateService
    {
        public event ConsoleStateChangedEventHandler FilterStateChange;

        private bool[] _states;

        public ConsoleFilterStateService()
        {
            _states = new bool[Enum.GetValues(typeof(LogType)).Length];
            for (var i = 0; i < _states.Length; i++)
            {
                _states[i] = true;
            }
        }

        /// <summary>
        /// Set whether log messages with <paramref name="logType"/> severity
        /// should be displayed in the SRDebugger console.
        /// </summary>
        /// <param name="logType">Type of message (only Error/Warning/Log are used. <see cref="LogType.Exception"/> and <see cref="LogType.Assert"/> will redirect to <see cref="LogType.Error"/></param>
        /// <param name="enabled">True to display the log type, false to hide.</param>
        public void SetState(LogType type, bool newState)
        {
            type = GetType(type);
            if (_states[(int)type] == newState)
            {
                return;
            }

            _states[(int)type] = newState;
            FilterStateChange?.Invoke(type, newState);
        }

        public bool GetState(LogType type)
        {
            type = GetType(type);
            return _states[(int)type];
        }

        private static LogType GetType(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    return LogType.Error;
                default:
                    return type;
            }
        }
    }
}