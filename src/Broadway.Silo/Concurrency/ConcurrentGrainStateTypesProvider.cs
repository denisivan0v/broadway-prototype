using System;
using System.Collections.Generic;

using Orleans.EventSourcing.LogStorage;
using Orleans.Persistence.Cassandra.Concurrency;

namespace NuClear.Broadway.Silo.Concurrency
{
    public class ConcurrentGrainStateTypesProvider : IConcurrentGrainStateTypesProvider
    {
        public static ConcurrentGrainStateTypesProvider Instance => new ConcurrentGrainStateTypesProvider();

        private ConcurrentGrainStateTypesProvider()
        {
        }

        public IReadOnlyCollection<Type> GetGrainStateTypes()
            => new[]
                {
                    typeof(LogStateWithMetaData<object>)
                };
    }
}