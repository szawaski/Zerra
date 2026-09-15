$(function () {

    const $orders = $("#orders");
    const $lines = $("#order-lines");
    let products = [];
    let available = {};
    let shipments = {};

    const statusBadges = {
        Placed: "info",
        Shipped: "success",
        Cancelled: "neutral"
    };

    //the order form needs data from all three services
    async function loadFormData() {
        const [customers, catalog, levels] = await Promise.all([
            Store.query(IOrdersQueryHandler.GetCustomers),
            Store.query(ICatalogQueryHandler.GetProducts),
            Store.query(IInventoryQueryHandler.GetStockLevels)
        ]);

        const $customer = $("#customer");
        const selectedCustomer = $customer.val();
        $customer.empty();
        for (const customer of customers)
            $customer.append($("<option>").val(customer.ID).text(customer.Name + " · " + customer.Email));
        if (selectedCustomer)
            $customer.val(selectedCustomer);

        products = catalog;
        available = {};
        for (const level of levels)
            available[level.ProductID] = level.Available;

        //refresh the product choices without losing what's selected
        $lines.find(".order-line").each(function () {
            const $select = $(this).find("select");
            const selected = $select.val();
            fillProductOptions($select);
            $select.val(selected);
        });
        if ($lines.children().length === 0)
            addLine();
        updateTotal();
    }

    function fillProductOptions($select) {
        $select.empty();
        for (const product of products) {
            const stock = available[product.ID] || 0;
            let text = product.Name + " · " + Store.money(product.Price);
            if (!product.IsActive)
                text += " · discontinued";
            else
                text += stock === 0 ? " · out of stock" : " · " + stock + " available";
            //out of stock and discontinued products stay selectable so the services' rules can be tried
            $select.append($("<option>").val(product.ID).text(text));
        }
    }

    function addLine() {
        const $select = $("<select>");
        fillProductOptions($select);
        const $quantity = $("<input type='number' min='1' max='100' value='1'>");
        const $remove = $("<button type='button'>").addClass("secondary small").attr("title", "Remove").text("✕");
        const $line = $("<div>").addClass("order-line").append($select, $quantity, $remove).appendTo($lines);

        //start each new line on a product not already in the order
        const used = $lines.find("select").not($select).map(function () { return $(this).val(); }).get();
        const next = products.find(function (x) { return x.IsActive && used.indexOf(x.ID) < 0; });
        if (next)
            $select.val(next.ID);

        $select.on("change", updateTotal);
        $quantity.on("input", updateTotal);
        $remove.on("click", function () {
            $line.remove();
            updateTotal();
        });
        updateTotal();
    }

    function readItems() {
        return $lines.find(".order-line").map(function () {
            return {
                ProductID: $(this).find("select").val(),
                Quantity: parseInt($(this).find("input").val(), 10) || 0
            };
        }).get();
    }

    function updateTotal() {
        let total = 0;
        for (const item of readItems()) {
            const product = products.find(function (x) { return x.ID === item.ProductID; });
            if (product)
                total += product.Price * item.Quantity;
        }
        $("#order-total").text(Store.money(total));
        $("#place-order").prop("disabled", $lines.children().length === 0);
    }

    async function loadOrders() {
        //Shipping is a separate service the gateway forwards to over HTTP, its data is joined onto the orders here in the browser
        const [orders, shipmentList] = await Promise.all([
            Store.query(IOrdersQueryHandler.GetOrders),
            Store.query(IShippingQueryHandler.GetShipments)
        ]);

        shipments = {};
        for (const shipment of shipmentList)
            shipments[shipment.OrderID] = shipment;

        $orders.empty();
        if (orders.length === 0) {
            $orders.append(Store.messageRow(6, "No orders yet."));
            return;
        }
        for (const order of orders)
            $orders.append(orderRow(order));
    }

    function orderRow(order) {
        const $row = $("<tr>");
        $row.append($("<td>").append(
            $("<div>").append($("<b>").text(order.OrderNumber)),
            $("<div>").addClass("subtle").text(order.CustomerName + " · " + Store.dateTime(order.PlacedOn))
        ));
        const $items = $("<td>").appendTo($row);
        for (const line of order.Lines || [])
            $items.append($("<div>").text(line.Quantity + " × " + line.ProductName).attr("title", Store.money(line.UnitPrice) + " each"));
        $row.append($("<td>").addClass("num").text(Store.money(order.Total)));
        $row.append($("<td>").append(Store.badge(order.Status, statusBadges[order.Status] || "neutral")));

        const shipment = shipments[order.ID];
        $row.append($("<td>").append(shipment
            ? [$("<div>").text(shipment.Carrier), $("<div>").addClass("subtle").append($("<code>").text(shipment.TrackingNumber), " · " + shipment.Status)]
            : $("<span>").addClass("subtle").text("—")));

        const $actions = $("<td>").addClass("actions").appendTo($row);
        if (order.Status === "Placed") {
            $("<button>").addClass("small").text("Ship").appendTo($actions)
                .on("click", function () { changeOrder(this, new ShipOrderCommand({ OrderID: order.ID }), order.OrderNumber + " shipped"); });
            $("<button>").addClass("danger small").text("Cancel").css("margin-left", "6px").appendTo($actions)
                .on("click", function () { changeOrder(this, new CancelOrderCommand({ OrderID: order.ID }), order.OrderNumber + " cancelled"); });
        }
        return $row;
    }

    function changeOrder(button, command, message) {
        Store.busy(button, Store.send(command))
            .then(function () {
                Store.toast(message + ", Inventory will settle the stock from the event", "success");
                return Promise.all([loadOrders(), loadFormData()]);
            })
            .catch(function () { });
    }

    $("#add-line").on("click", addLine);

    $("#new-order").on("submit", function (e) {
        e.preventDefault();
        const command = new PlaceOrderCommand({
            CustomerID: $("#customer").val(),
            Items: readItems()
        });
        //PlaceOrderCommand has a result, DispatchAwait resolves with the PlaceOrderResult
        Store.busy("#place-order", Store.send(command))
            .then(function (result) {
                Store.toast("Order " + result.OrderNumber + " placed for " + Store.money(result.Total), "success");
                $lines.empty();
                return Promise.all([loadOrders(), loadFormData()]);
            })
            .catch(function () { });
    });

    $("#refresh-orders").on("click", function () {
        Store.busy(this, Promise.all([loadOrders(), loadFormData()])).catch(function () { });
    });

    $orders.append(Store.messageRow(6, "Loading…"));
    loadOrders().catch(function () {
        $orders.empty().append(Store.messageRow(6, "Couldn't load orders. Is the Orders service running?"));
    });
    loadFormData().catch(function () { });
});
