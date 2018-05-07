using System;
using System.Collections.Generic;

using NuClear.Broadway.Interfaces;

using Orleans.Persistence.Cassandra.Concurrency;

namespace NuClear.Broadway.Silo.Concurrency
{
    public class ConcurrentGrainStateTypesProvider : IConcurrentGrainStateTypesProvider
    {
        public IReadOnlyCollection<Type> GetGrainStateTypes()
            => new[]
                {
                    typeof(Campaign)
                };
    }
}