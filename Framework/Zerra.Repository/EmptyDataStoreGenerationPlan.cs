using System;
using System.Collections.Generic;

namespace Zerra.Repository
{
    public class EmptyDataStoreGenerationPlan : IDataStoreGenerationPlan
    {
        public ICollection<string> Plan => Array.Empty<string>();

        public void Execute() { }
    }
}
