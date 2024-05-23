// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection
{
    public sealed class ConstructorDetail<T> : ConstructorDetail
    {
        private bool creatorLoaded = false;
        private Func<object?[]?, T>? creator = null;
        public Func<object?[]?, T> Creator
        {
            get
            {
                LoadCreator();
                return this.creator ?? throw new NotSupportedException($"{nameof(ConstructorDetail)} {Name} does not have a {nameof(Creator)}"); ;
            }
        }
        public bool HasCreator
        {
            get
            {
                LoadCreator();
                return this.creator != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCreator()
        {
            if (!creatorLoaded)
            {
                lock (locker)
                {
                    if (!creatorLoaded)
                    {
                        if (ConstructorInfo.DeclaringType != null && !ConstructorInfo.DeclaringType.IsAbstract && !ConstructorInfo.DeclaringType.IsGenericTypeDefinition)
                        {
                            this.creator = AccessorGenerator.GenerateCreator<T>(ConstructorInfo);
                        }
                        creatorLoaded = true;
                    }
                }
            }
        }

        public override Delegate? CreatorTyped => Creator;

        internal ConstructorDetail(ConstructorInfo constructor, object locker) : base(constructor, locker) { }
    }
}
