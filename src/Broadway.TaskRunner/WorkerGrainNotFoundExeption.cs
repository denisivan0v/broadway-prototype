using System;

namespace NuClear.Broadway.TaskRunner
{
    public sealed class WorkerGrainNotFoundExeption : Exception
    {
        public WorkerGrainNotFoundExeption(string taskId, string taskType)
            : base($"Worker for task '{taskId}' of type '{taskType}' not found.")
        {
        }
    }
}