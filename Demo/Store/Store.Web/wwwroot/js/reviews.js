$(function () {

    const $reviews = $("#reviews");
    const $customer = $("#review-customer");
    const $product = $("#review-product");

    async function loadOptions() {
        const [customers, products] = await Promise.all([
            Store.query(IOrdersQueryHandler.GetCustomers),
            Store.query(ICatalogQueryHandler.GetProducts)
        ]);
        for (const customer of customers)
            $customer.append($("<option>").val(customer.ID).text(customer.Name));
        for (const product of products.filter(function (p) { return p.IsActive; }))
            $product.append($("<option>").val(product.ID).text(product.Name));
    }

    async function loadReviews() {
        const reviews = await Store.query(IReviewsQueryHandler.GetRecentReviews, 25);
        $reviews.empty();
        if (reviews.length === 0) {
            $reviews.append(Store.messageRow(4, "No reviews yet."));
            return;
        }
        for (const review of reviews)
            $reviews.append(reviewRow(review));
    }

    function reviewRow(review) {
        const $row = $("<tr>");
        $row.append($("<td>").text(review.ProductName));
        $row.append($("<td>").append($("<span>").addClass("stars").text(Store.stars(review.Rating))));
        $row.append($("<td>").append($("<div>").text(review.Comment || "")));
        const $customerCell = $("<td>").appendTo($row);
        $customerCell.append($("<div>").text(review.CustomerName));
        $customerCell.append($("<div>").append(
            review.VerifiedPurchase ? Store.badge("Verified purchase", "success") : Store.badge("Not verified", "neutral"),
            $("<span>").addClass("subtle").text(" " + Store.dateTime(review.CreatedOn))
        ));
        return $row;
    }

    $("#refresh-reviews").on("click", function () {
        Store.busy(this, loadReviews()).catch(function () { });
    });

    $("#new-review").on("submit", function (e) {
        e.preventDefault();
        const command = new SubmitReviewCommand({
            CustomerID: $customer.val(),
            ProductID: $product.val(),
            Rating: parseInt($("#review-rating").val(), 10),
            Comment: $("#review-comment").val()
        });
        Store.busy("#submit-review", Store.send(command))
            .then(function (result) {
                Store.toast(result.VerifiedPurchase ? "Review submitted as a verified purchase" : "Review submitted", "success");
                $("#review-comment").val("");
                return loadReviews();
            })
            .catch(function () { });
    });

    $reviews.append(Store.messageRow(4, "Loading…"));
    Promise.all([loadOptions(), loadReviews()]).catch(function () {
        $reviews.empty().append(Store.messageRow(4, "Couldn't load reviews. Are the Catalog, Orders, and Reviews services running?"));
    });
});
