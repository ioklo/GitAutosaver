using System;
using System.Runtime.Serialization;

namespace GitAutosaver
{
    [Serializable]
    internal class GitRepoException : Exception
    {
        public GitRepoException()
        {
        }

        public GitRepoException(string? message) : base(message)
        {
        }

        public GitRepoException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected GitRepoException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}