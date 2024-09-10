using System;
using System.Collections.Generic;

namespace SRDebugger.Services.Implementation
{
    using SRF.Service;
    using UnityEngine;

    [Service(typeof (IConsoleService))]
    public class StandardConsoleService : IConsoleService, IDisposable
    {
        private readonly bool _collapseEnabled;
        private bool _hasCleared;

        private readonly CircularBuffer<ConsoleEntry> _allConsoleEntries;
        private CircularBuffer<ConsoleEntry> _consoleEntries;
        private readonly object _threadLock = new object();

        private ILogHandler _expectedLogHandler;

        public StandardConsoleService()
        {
            Application.logMessageReceivedThreaded += UnityLogCallback;
            _expectedLogHandler = Debug.unityLogger.logHandler;
            
            SRServiceManager.RegisterService<IConsoleService>(this);
            _collapseEnabled = Settings.Instance.CollapseDuplicateLogEntries;
            _allConsoleEntries = new CircularBuffer<ConsoleEntry>(Settings.Instance.MaximumConsoleEntries);
        }

        public void Dispose()
        {
            Application.logMessageReceivedThreaded -= UnityLogCallback;
            if (_consoleEntries != null)
            {
                _consoleEntries.Clear();
            }

            _allConsoleEntries.Clear();
        }

        public int ErrorCount { get; private set; }
        public int WarningCount { get; private set; }
        public int InfoCount { get; private set; }

        public event ConsoleUpdatedEventHandler Updated;
        public event ConsoleUpdatedEventHandler Error;

        public bool LoggingEnabled
        {
            get { return Debug.unityLogger.logEnabled; }
            set { Debug.unityLogger.logEnabled = value; }
        }

        public bool LogHandlerIsOverriden
        {
            get
            {
                return Debug.unityLogger.logHandler != _expectedLogHandler;
            }
        }

        public IReadOnlyList<ConsoleEntry> Entries
        {
            get
            {
                if (!_hasCleared)
                {
                    return _allConsoleEntries;
                }

                return _consoleEntries;
            }
        }

        public IReadOnlyList<ConsoleEntry> AllEntries
        {
            get { return _allConsoleEntries; }
        }

        public void Clear()
        {
            lock (_threadLock)
            {
                _hasCleared = true;

                if (_consoleEntries == null)
                {
                    _consoleEntries = new CircularBuffer<ConsoleEntry>(Settings.Instance.MaximumConsoleEntries);
                }
                else
                {
                    _consoleEntries.Clear();
                }

                ErrorCount = WarningCount = InfoCount = 0;
            }

            OnUpdated();
        }

        protected void OnEntryAdded(ConsoleEntry entry)
        {
            if (_hasCleared)
            {
                // Decrement counters if adding this entry will push another
                // entry from the buffer.
                if (_consoleEntries.IsFull)
                {
                    AdjustCounter(_consoleEntries.Front().LogType, -1);
                    _consoleEntries.PopFront();
                }

                _consoleEntries.PushBack(entry);
            }
            else
            {
                if (_allConsoleEntries.IsFull)
                {
                    AdjustCounter(_allConsoleEntries.Front().LogType, -1);
                    _allConsoleEntries.PopFront();
                }
            }

            _allConsoleEntries.PushBack(entry);
            OnUpdated();
        }

        protected void OnEntryDuplicated(ConsoleEntry entry)
        {
            entry.Count++;
            OnUpdated();

            // If has cleared, add this entry again for the current list
            if (_hasCleared && _consoleEntries.Count == 0)
            {
                OnEntryAdded(new ConsoleEntry(entry) {Count = 1});
            }
        }

        private void OnUpdated()
        {
            if (Updated != null)
            {
                try
                {
                    Updated(this);
                }
                catch {}
            }
        }

        private void UnityLogCallback(string condition, string stackTrace, LogType type)
        {                
            //if (condition.StartsWith("[SRConsole]"))
            //    return;

            lock (_threadLock)
            {
                var prevMessage = _collapseEnabled && _allConsoleEntries.Count > 0
                    ? _allConsoleEntries[_allConsoleEntries.Count - 1]
                    : null;

                AdjustCounter(type, 1);

                if (prevMessage != null && prevMessage.LogType == type && prevMessage.Message == condition &&
                    prevMessage.StackTrace == stackTrace)
                {
                    OnEntryDuplicated(prevMessage);
                }
                else
                {
                    var newEntry = new ConsoleEntry
                    {
                        LogType = type,
                        StackTrace = stackTrace,
                        Message = condition
                    };

                    OnEntryAdded(newEntry);
                }
            }
        }

        private void AdjustCounter(LogType type, int amount)
        {
            switch (type)
            {
                case LogType.Assert:
                case LogType.Error:
                case LogType.Exception:
                    ErrorCount += amount;

                    if (Error != null)
                    {
                        Error.Invoke(this);
                    }
                    break;

                case LogType.Warning:
                    WarningCount += amount;
                    break;

                case LogType.Log:
                    InfoCount += amount;
                    break;
            }
        }
    }
}
