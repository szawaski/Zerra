// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.CQRS
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public interface IBusInternal
    {
#pragma warning disable IDE1006 // Naming Styles
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        TReturn _CallMethod<TReturn>(Type interfaceType, string methodName, object[] arguments, string source);
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task _CallMethodTask(Type interfaceType, string methodName, object[] arguments, string source);
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task<TReturn> _CallMethodTaskGeneric<TReturn>(Type interfaceType, string methodName, object[] arguments, string source);

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task _DispatchCommandInternalAsync(ICommand command, Type commandType, bool requireAffirmation, string source, CancellationToken cancellationToken);
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task<TResult> _DispatchCommandWithResultInternalAsync<TResult>(ICommand<TResult> command, Type commandType, string source, CancellationToken cancellationToken);
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        Task _DispatchEventInternalAsync(IEvent @event, Type eventType, string source, CancellationToken cancellationToken);
#pragma warning restore IDE1006 // Naming Styles
    }
}
