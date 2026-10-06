using Zerra.CQRS;

namespace Store.Web.Domain.Reviews
{
    public sealed class SubmitReviewCommand : ICommand<SubmitReviewResult>
    {
        public required Guid CustomerID { get; set; }
        public required Guid ProductID { get; set; }
        public required int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
