// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Identity
{
    public abstract class Binding<T>
        where T : class
    {
        public abstract BindingType BindingType { get; }
        public BindingDirection BindingDirection { get; protected set; }

        protected T Document = null;

        public virtual T GetDocument()
        {
            return this.Document;
        }

        public abstract IdentityHttpResponse GetResponse(string url = null);
        public abstract string GetContent();
    }
}
