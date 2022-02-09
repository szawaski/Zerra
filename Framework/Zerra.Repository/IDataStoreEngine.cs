// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System.Collections.Generic;
using Zerra.Repository.Reflection;

namespace Zerra.Repository
{
    public interface IDataStoreEngine
    {
        bool ValidateDataSource();
        IDataStoreGenerationPlan BuildStoreGenerationPlan(bool create, bool update, bool delete, ICollection<ModelDetail> modelDetail);
    }
}
