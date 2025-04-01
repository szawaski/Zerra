// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

using System;
using System.Threading.Tasks;

/// <summary>
/// Functionality to wait execution of a method with a timeout.
/// Note that the method will continue to run and needs to be delt with after a timeout event.
/// </summary>
public static class MethodWait
{
    /// <summary>
    /// Waits for a method to complete or throws a <see cref="TimeoutException"/>.
    /// Note that the method will continue to run and needs to be delt with after a timeout event.
    /// </summary>
    /// <param name="it">The method to wait upon.</param>
    /// <param name="timeout">The time to wait before throwing a <see cref="TimeoutException"/>.</param>
    public static void Wait(this Action it, TimeSpan timeout)
    {
        var task = Task.Run(it);
        var succeed = false;
        try
        {
            succeed = task.Wait(timeout);
        }
        catch (Exception ex)
        {
            if (ex.InnerException is not null)
                throw ex.InnerException;
            throw;
        }

        if (!succeed)
            throw new TimeoutException();
    }

    /// <summary>
    /// Waits for a method to complete or throws a <see cref="TimeoutException"/>.
    /// Note that the method will continue to run and needs to be delt with after a timeout event.
    /// </summary>
    /// <typeparam name="T">The return type of the method.</typeparam>
    /// <param name="it">The method to wait upon.</param>
    /// <param name="timeout">The time to wait before throwing a <see cref="TimeoutException"/>.</param>
    /// <returns>The result of the method</returns>
    public static T Wait<T>(this Func<T> it, TimeSpan timeout)
    {
        var task = Task.Run(it);
        var succeed = false;
        try
        {
            succeed = task.Wait(timeout);
        }
        catch (Exception ex)
        {
            if (ex.InnerException is not null)
                throw ex.InnerException;
            throw;
        }

        if (!succeed)
            throw new TimeoutException();

        return task.Result;
    }
}
