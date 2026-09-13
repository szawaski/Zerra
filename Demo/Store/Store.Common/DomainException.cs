namespace Store.Common
{
    /// <summary>
    /// A business rule was violated. The message is written for the end user and is shown by the web pages.
    /// </summary>
    public sealed class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }
}
