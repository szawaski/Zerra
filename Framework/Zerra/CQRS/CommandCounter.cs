namespace Zerra.CQRS
{
    /// <summary>
    /// A counter to track and limit the commands processed by this service.
    /// Short lived services can be told to terminate after processing a number of commands.
    /// </summary>
    public sealed class CommandCounter
    {
        private readonly object locker = new();

        private int started = 0;
        private int completed = 0;

        private readonly int? receiveCountBeforeExit;
        private readonly Action? processExit;

        internal CommandCounter()
        {
            this.receiveCountBeforeExit = null;
            this.processExit = null;
        }
        internal CommandCounter(int? receiveCountBeforeExit, Action processExit)
        {
            if (receiveCountBeforeExit.HasValue && receiveCountBeforeExit.Value < 1) throw new ArgumentException("cannot be less than 1", nameof(receiveCountBeforeExit));

            this.receiveCountBeforeExit = receiveCountBeforeExit;
            this.processExit = processExit;
        }

        /// <summary>
        /// The number of commands to process before the service terminates.
        /// </summary>
        public int? ReceiveCountBeforeExit => receiveCountBeforeExit;

        /// <summary>
        /// Called by a <see cref="ICommandHandler{T}"/>, increment the count and return if a command should be received and handled.
        /// </summary>
        /// <returns>True if the service should continue to receive and handle a command.</returns>
        public bool BeginReceive()
        {
            if (!receiveCountBeforeExit.HasValue)
                return true;

            lock (locker)
            {
                if (started == receiveCountBeforeExit.Value)
                    return false; //do not receive any more
                started++;
            }
            return true;
        }

        /// <summary>
        /// Called by a <see cref="ICommandHandler{T}"/> when a command handling count needs cancelled because there was no command recieved.
        /// </summary>
        /// <param name="throttle">A throttler used by the <see cref="ICommandHandler{T}"/> passed here so it can release at the approriate time.</param>
        public void CancelReceive(SemaphoreSlim throttle)
        {
            if (!receiveCountBeforeExit.HasValue)
            {
                _ = throttle.Release();
                return;
            }

            lock (locker)
            {
                started--;
                _ = throttle.Release();
            }
        }

        /// <summary>
        /// Called by a <see cref="ICommandHandler{T}"/> when a command has completed receiving and handling.
        /// </summary>
        /// <param name="throttle">A throttler used by the <see cref="ICommandHandler{T}"/> passed here so it can release at the approriate time.</param>
        public void CompleteReceive(SemaphoreSlim throttle)
        {
            if (!receiveCountBeforeExit.HasValue)
            {
                _ = throttle.Release();
                return;
            }

            lock (locker)
            {
                completed++;
                if (completed == receiveCountBeforeExit.Value)
                    processExit?.Invoke();
                else if (throttle.CurrentCount < receiveCountBeforeExit.Value - started)
                    _ = throttle.Release(); //do not release more than needed to reach maxReceive
            }
        }
    }
}
