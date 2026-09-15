using Store.Reviews.Domain.Commands;
using Store.Reviews.Domain.Models;
using Zerra.CQRS;

namespace Store.Reviews.Domain
{
    public interface IReviewsCommandHandler :
        ICommandHandler<SubmitReviewCommand, SubmitReviewResult>
    {
    }
}
