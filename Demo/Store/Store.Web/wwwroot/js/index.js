$(function () {

    //each service reports the data store and the messaging routes it picked at startup: the real database or the in-memory fallback,
    //and a message broker or the direct connection
    const services = {
        catalog: ICatalogQueryHandler,
        inventory: IInventoryQueryHandler,
        orders: IOrdersQueryHandler,
        shipping: IShippingQueryHandler,
        reviews: IReviewsQueryHandler
    };

    for (const name in services) {
        const handler = services[name];
        const $card = $("[data-service='" + name + "']");
        const started = performance.now();

        handler.GetDataStoreName(function (storeName) {
            $card.find(".status-dot").addClass("up");
            $card.find(".store").text(storeName + " (" + Math.round(performance.now() - started) + " ms)");
        }, function () {
            $card.find(".status-dot").addClass("down");
            $card.find(".store").text("Not reachable, is the service running?");
        });

        handler.GetMessagingName(function (messaging) {
            $card.find(".messaging").text(messaging);
        }, function () {
            $card.find(".messaging").text("Not reachable");
        });
    }
});
