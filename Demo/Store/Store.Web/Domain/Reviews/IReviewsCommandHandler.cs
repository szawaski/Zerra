using Zerra.CQRS;

namespace Store.Web.Domain.Reviews
{
    public interface IReviewsCommandHandler :
        ICommandHandler<SubmitReviewCommand, SubmitReviewResult>
    {
    }
}
