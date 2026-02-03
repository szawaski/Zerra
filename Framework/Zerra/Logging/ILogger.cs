// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

namespace Zerra.Logging
{
    /// <summary>
    /// Defines the contract for logging operations at various severity levels.
    /// </summary>
    /// <remarks>
    /// This interface provides a simple, structured logging API that supports multiple severity levels
    /// from Trace (most verbose) to Critical (most severe). Implementations should handle message
    /// formatting, filtering, and output routing based on configured log levels.
    /// </remarks>
    public interface ILogger
    {
        /// <summary>
        /// Log an event with a level of Trace.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <remarks>
        /// Trace level is typically used for very detailed diagnostic information, useful primarily
        /// during development and troubleshooting. This is the most verbose logging level.
        /// </remarks>
        void Trace(string message);

        /// <summary>
        /// Log an event with a level of Debug.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <remarks>
        /// Debug level is used for detailed diagnostic information intended for developers.
        /// This level is useful for understanding application flow and state during development.
        /// </remarks>
        void Debug(string message);

        /// <summary>
        /// Log an event with a level of Information.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <remarks>
        /// Information level is used for general informational messages that highlight the progress
        /// of the application. This is typically used in production environments.
        /// </remarks>
        void Info(string message);

        /// <summary>
        /// Log an event with a level of Warning.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <remarks>
        /// Warning level indicates a potentially harmful situation that may require attention.
        /// The application is still functioning, but something unexpected occurred.
        /// </remarks>
        void Warn(string message);

        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="message">The message to log, or null to use only the exception.</param>
        /// <param name="ex">The exception to log, or null to log only the message.</param>
        /// <remarks>
        /// Error level indicates an error event that might still allow the application to continue running.
        /// At least one of <paramref name="message"/> or <paramref name="ex"/> should be provided.
        /// </remarks>
        void Error(string? message = null, Exception? ex = null);

        /// <summary>
        /// Log an event with a level of Error.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <remarks>
        /// Error level indicates an error event that might still allow the application to continue running.
        /// <paramref name="ex"/> should be provided.
        /// </remarks>
        void Error(Exception? ex = null);

        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="message">The message to log, or null to use only the exception.</param>
        /// <param name="ex">The exception to log, or null to log only the message.</param>
        /// <remarks>
        /// Critical level indicates a serious error event that has likely caused or will cause
        /// the application to abort or function improperly. This is the most severe logging level.
        /// At least one of <paramref name="message"/> or <paramref name="ex"/> should be provided.
        /// </remarks>
        void Critical(string? message = null, Exception? ex = null);

        /// <summary>
        /// Log an event with a level of Critical.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <remarks>
        /// Critical level indicates a serious error event that has likely caused or will cause
        /// the application to abort or function improperly. This is the most severe logging level.
        /// <paramref name="ex"/> should be provided.
        /// </remarks>
        void Critical(Exception? ex = null);
    }
}
