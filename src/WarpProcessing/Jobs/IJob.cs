﻿using System.ComponentModel;

namespace Warp9.Jobs
{
    public interface IJob
    {
        public string Title { get; }
        public string StatusText { get; }

        public int NumItems { get; }
        public int NumItemsDone { get; }
        public int NumItemsFailed { get; }
        public bool IsCompleted { get; }
        public bool IsFatallyFailed { get; }

        public IJobContext? Context { get; }

        public bool TryExecuteNext();

        public IJobContext DetachContext();
    }
}
