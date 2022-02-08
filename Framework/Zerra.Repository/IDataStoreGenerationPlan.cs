using System.Collections.Generic;

namespace Zerra.Repository
{
    public interface IDataStoreGenerationPlan
    {
        ICollection<string> Plan { get; }
        void Execute();
    }
}
