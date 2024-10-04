// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Zerra.Reflection.Runtime
{
    internal sealed class ConstructorDetailRuntime<T> : ConstructorDetail<T>
    {
        public override bool IsGenerated => false;

        public override ConstructorInfo ConstructorInfo { get; }
        public override string Name => ConstructorInfo.Name;

        private ParameterDetail[]? parameterInfos = null;
        public override IReadOnlyList<ParameterDetail> ParameterDetails
        {
            get
            {
                if (this.parameterInfos == null)
                {
                    lock (locker)
                    {
                        var parameters = ConstructorInfo.GetParameters();
                        this.parameterInfos ??= parameters.Select(x => new ParameterDetailRuntime(x, locker)).ToArray();
                    }
                }
                return this.parameterInfos;
            }
        }

        private Attribute[]? attributes = null;
        public override IReadOnlyList<Attribute> Attributes
        {
            get
            {
                if (this.attributes == null)
                {
                    lock (locker)
                    {
                        this.attributes ??= ConstructorInfo.GetCustomAttributes().ToArray();
                    }
                }
                return this.attributes;
            }
        }

        private bool creatorBoxedLoaded = false;
        private Func<object>? creatorBoxed = null;
        public override Func<object> CreatorBoxed
        {
            get
            {
                if (!creatorBoxedLoaded)
                    LoadCreatorBoxed();
                return this.creatorBoxed ?? throw new NotSupportedException($"{nameof(ConstructorDetail)} {Name} does not have a {nameof(CreatorBoxed)}"); ;
            }
        }
        public override bool HasCreatorBoxed
        {
            get
            {
                if (!creatorBoxedLoaded)
                    LoadCreatorBoxed();
                return this.creatorBoxed != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCreatorBoxed()
        {
            lock (locker)
            {
                if (!creatorBoxedLoaded)
                {
                    if (ConstructorInfo.DeclaringType != null && !ConstructorInfo.DeclaringType.IsAbstract && !ConstructorInfo.DeclaringType.IsGenericTypeDefinition)
                    {
                        this.creatorBoxed = AccessorGenerator.GenerateCreatorNoArgs(ConstructorInfo);
                    }
                    creatorBoxedLoaded = true;
                }
            }
        }

        private bool creatorWithArgsBoxedLoaded = false;
        private Func<object?[]?, object>? creatorWithArgsBoxed = null;
        public override Func<object?[]?, object> CreatorWithArgsBoxed
        {
            get
            {
                if (!creatorWithArgsBoxedLoaded)
                    LoadCreatorWithArgsBoxed();
                return this.creatorWithArgsBoxed ?? throw new NotSupportedException($"{nameof(ConstructorDetail)} {Name} does not have a {nameof(CreatorWithArgsBoxed)}"); ;
            }
        }
        public override bool HasCreatorWithArgsBoxed
        {
            get
            {
                if (!creatorWithArgsBoxedLoaded)
                    LoadCreatorWithArgsBoxed();
                return this.creatorWithArgsBoxed != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCreatorWithArgsBoxed()
        {
            lock (locker)
            {
                if (!creatorWithArgsBoxedLoaded)
                {
                    if (ConstructorInfo.DeclaringType != null && !ConstructorInfo.DeclaringType.IsAbstract && !ConstructorInfo.DeclaringType.IsGenericTypeDefinition)
                    {
                        this.creatorWithArgsBoxed = AccessorGenerator.GenerateCreator(ConstructorInfo);
                    }
                    creatorWithArgsBoxedLoaded = true;
                }
            }
        }

        private bool creatorLoaded = false;
        private Func<T>? creator = null;
        public override Func<T> Creator
        {
            get
            {
                LoadCreator();
                return this.creator ?? throw new NotSupportedException($"{nameof(ConstructorDetail)} {Name} does not have a {nameof(Creator)}"); ;
            }
        }
        public override bool HasCreator
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
                            this.creator = AccessorGenerator.GenerateCreatorNoArgs<T>(ConstructorInfo);
                        }
                        creatorLoaded = true;
                    }
                }
            }
        }

        public override Delegate? CreatorTyped => Creator;

        private bool creatorWithArgsLoaded = false;
        private Func<object?[]?, T>? creatorWithArgs = null;
        public override Func<object?[]?, T> CreatorWithArgs
        {
            get
            {
                LoadCreatorWithArgs();
                return this.creatorWithArgs ?? throw new NotSupportedException($"{nameof(ConstructorDetail)} {Name} does not have a {nameof(CreatorWithArgs)}"); ;
            }
        }
        public override bool HasCreatorWithArgs
        {
            get
            {
                LoadCreatorWithArgs();
                return this.creatorWithArgs != null;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void LoadCreatorWithArgs()
        {
            if (!creatorWithArgsLoaded)
            {
                lock (locker)
                {
                    if (!creatorWithArgsLoaded)
                    {
                        if (ConstructorInfo.DeclaringType != null && !ConstructorInfo.DeclaringType.IsAbstract && !ConstructorInfo.DeclaringType.IsGenericTypeDefinition)
                        {
                            this.creatorWithArgs = AccessorGenerator.GenerateCreator<T>(ConstructorInfo);
                        }
                        creatorWithArgsLoaded = true;
                    }
                }
            }
        }

        public override Delegate? CreatorWithArgsTyped => CreatorWithArgs;

        private readonly object locker;
        internal ConstructorDetailRuntime(ConstructorInfo constructor, object locker)
        {
            this.locker = locker;
            this.ConstructorInfo = constructor;
        }

        private static readonly Type typeDetailT = typeof(ConstructorDetailRuntime<>);
        internal static ConstructorDetail New(Type type, ConstructorInfo constructor, object locker)
        {
            var typeDetailGeneric = typeDetailT.MakeGenericType(type);
            var obj = typeDetailGeneric.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0].Invoke(new object[] { constructor, locker });
            return (ConstructorDetail)obj;
        }

        internal override void SetConstructorInfo(ConstructorInfo constructorInfo) => throw new NotSupportedException();
    }
}
