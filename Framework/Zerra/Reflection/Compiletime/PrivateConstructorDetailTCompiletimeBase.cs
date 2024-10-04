using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Zerra.Reflection.Runtime;

namespace Zerra.Reflection.Compiletime
{
    public abstract class PrivateConstructorDetailCompiletimeBase<T> : ConstructorDetail<T>
    {
        protected readonly object locker;
        private readonly Action loadConstructorInfo;
        public PrivateConstructorDetailCompiletimeBase(object locker, Action loadConstructorInfo)
        {
            this.locker = locker;
            this.loadConstructorInfo = loadConstructorInfo;
        }

        public override sealed bool IsGenerated => true;

        private ConstructorInfo? constructorInfo = null;
        public override sealed ConstructorInfo ConstructorInfo
        {
            get
            {
                if (constructorInfo is null)
                {
                    lock (locker)
                    {
                        if (constructorInfo is null)
                        {
                            loadConstructorInfo();
                        }
                    }
                }
                return constructorInfo!;
            }
        }

        protected abstract Func<Attribute[]> CreateAttributes { get; }
        private Attribute[]? attributes = null;
        public override sealed IReadOnlyList<Attribute> Attributes
        {
            get
            {
                if (attributes is null)
                {
                    lock (locker)
                    {
                        attributes ??= CreateAttributes();
                    }
                }
                return attributes;
            }
        }

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

        public override sealed Delegate? CreatorTyped => Creator;
        public override sealed Delegate? CreatorWithArgsTyped => CreatorWithArgs;

        internal override sealed void SetConstructorInfo(ConstructorInfo constructorInfo)
        {
            this.constructorInfo = constructorInfo;
        }

        private bool creatorBoxedLoaded = false;
        private Func<object>? creatorBoxed = null;
        public override sealed Func<object> CreatorBoxed
        {
            get
            {
                if (!creatorBoxedLoaded)
                    LoadCreatorBoxed();
                return this.creatorBoxed ?? throw new NotSupportedException($"{nameof(ConstructorDetail)} {Name} does not have a {nameof(CreatorBoxed)}"); ;
            }
        }
        public override sealed bool HasCreatorBoxed
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
        public override sealed Func<object?[]?, object> CreatorWithArgsBoxed
        {
            get
            {
                if (!creatorWithArgsBoxedLoaded)
                    LoadCreatorWithArgsBoxed();
                return this.creatorWithArgsBoxed ?? throw new NotSupportedException($"{nameof(ConstructorDetail)} {Name} does not have a {nameof(CreatorWithArgsBoxed)}"); ;
            }
        }
        public override sealed bool HasCreatorWithArgsBoxed
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
        public override sealed Func<T> Creator
        {
            get
            {
                LoadCreator();
                return this.creator ?? throw new NotSupportedException($"{nameof(ConstructorDetail)} {Name} does not have a {nameof(Creator)}"); ;
            }
        }
        public override sealed bool HasCreator
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

        private bool creatorWithArgsLoaded = false;
        private Func<object?[]?, T>? creatorWithArgs = null;
        public override sealed Func<object?[]?, T> CreatorWithArgs
        {
            get
            {
                LoadCreatorWithArgs();
                return this.creatorWithArgs ?? throw new NotSupportedException($"{nameof(ConstructorDetail)} {Name} does not have a {nameof(CreatorWithArgs)}"); ;
            }
        }
        public override sealed bool HasCreatorWithArgs
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

        protected void LoadParameterInfo()
        {
            var parameters = ConstructorInfo.GetParameters();
            foreach (var parameterDetail in ParameterDetails)
            {
                var parameter = parameters.FirstOrDefault(x => x.Name == parameterDetail.Name);
                if (parameter == null)
                    throw new InvalidOperationException($"Parameter not found for {parameterDetail.Name}");

                var parameterBase = (ParameterDetailCompiletimeBase)parameterDetail;
                parameterBase.SetParameterInfo(parameter);
            }
        }
    }
}
