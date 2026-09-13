$(function () {

    //each service reports the data store it picked at startup, the real database or the in-memory fallback
    const services = {
        catalog: ICatalogQueryHandler.GetDataStoreName,
        inventory: IInventoryQueryHandler.GetDataStoreName,
        orders: IOrdersQueryHandler.GetDataStoreName
    };

    for (const name in services) {
        const $card = $("[data-service='" + name + "']");
        const started = performance.now();

        services[name](function (storeName) {
            $card.find(".status-dot").addClass("up");
            $card.find(".store").text(storeName + " (" + Math.round(performance.now() - started) + " ms)");
        }, function () {
            $card.find(".status-dot").addClass("down");
            $card.find(".store").text("Not reachable, is the service running?");
        });
    }
});
