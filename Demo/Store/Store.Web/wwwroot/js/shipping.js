$(function () {

    const $shipments = $("#shipments");

    const statusBadges = {
        InTransit: "info",
        Delivered: "success"
    };

    async function loadShipments() {
        const shipments = await Store.query(IShippingQueryHandler.GetShipments);
        $shipments.empty();
        if (shipments.length === 0) {
            $shipments.append(Store.messageRow(5, "No shipments yet. Ship an order to see one appear here."));
            return;
        }
        for (const shipment of shipments)
            $shipments.append(shipmentRow(shipment));
    }

    function shipmentRow(shipment) {
        const $row = $("<tr>");
        $row.append($("<td>").append($("<div>").text(shipment.OrderNumber), $("<div>").addClass("subtle").text(Store.dateTime(shipment.ShippedOn))));
        $row.append($("<td>").text(shipment.Carrier));
        $row.append($("<td>").append($("<code>").text(shipment.TrackingNumber)));
        $row.append($("<td>").append(Store.badge(shipment.Status, statusBadges[shipment.Status] || "neutral")));

        const $actions = $("<td>").addClass("actions").appendTo($row);
        if (shipment.Status === "InTransit") {
            $("<button>").addClass("secondary small").text("Mark delivered").appendTo($actions).on("click", function () {
                Store.busy(this, Store.send(new MarkDeliveredCommand({ OrderID: shipment.OrderID })))
                    .then(function () {
                        Store.toast("Order " + shipment.OrderNumber + " marked delivered", "success");
                        return loadShipments();
                    })
                    .catch(function () { });
            });
        } else {
            $actions.append($("<span>").addClass("subtle").text(Store.dateTime(shipment.DeliveredOn)));
        }
        return $row;
    }

    //events are handled by the Shipping service a moment after an order ships, polling shows them arrive
    setInterval(function () {
        if ($("#auto-refresh").is(":checked") && document.visibilityState === "visible")
            loadShipments().catch(function () { $("#auto-refresh").prop("checked", false); });
    }, 4000);

    $shipments.append(Store.messageRow(5, "Loading…"));
    loadShipments().catch(function () {
        $shipments.empty().append(Store.messageRow(5, "Couldn't load shipments. Is the Shipping service running?"));
    });
});
