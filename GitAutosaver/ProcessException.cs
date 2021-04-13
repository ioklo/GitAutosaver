using System;

namespace GitAutosaver
{
    class ProcessException : Exception
    {
        public enum Reason
        {
            CloneFailed,
            WorkingOnAutosaveBranch
        }

        Reason reason;

        public ProcessException(Reason reason)
        {
            this.reason = reason;
        }
    }
}
