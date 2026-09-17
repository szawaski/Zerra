$(function () {

    const $customer = $("#customer");
    const $product = $("#product");
    const $items = $("#items");
    const $history = $("#history");

    const eventBadges = {
        CartItemAddedEvent: ["Item added", "info"],
        CartItemRemovedEvent: ["Item removed", "neutral"],
        CartEmptiedEvent: ["Emptied", "warning"],
        CartCheckedOutEvent: ["Checked out", "success"]
    };

    async function loadOptions() {
        const [customers, products] = await Promise.all([
            Store.query(IOrdersQueryHandler.GetCustomers),
            Store.query(ICatalogQueryHandler.GetProducts)
        ]);
        for (const customer of customers)
            $customer.append($("<option>").val(customer.ID).text(customer.Name));
        //discontinued products stay selectable so the rule can be tried
        for (const product of products)
            $product.append($("<option>").val(product.ID).text(product.Name + " · " + Store.money(product.Price) + (product.IsActive ? "" : " · discontinued")));
    }

    //the cart and its stream are two queries, both replay the same events in the Carts service
    async function loadCart() {
        const customerID = $customer.val();
        if (!customerID)
            return;
        const [cart, history] = await Promise.all([
            Store.query(ICartsQueryHandler.GetCart, customerID),
            Store.query(ICartsQueryHandler.GetCartHistory, customerID)
        ]);
        renderCart(cart);
        renderHistory(history);
    }

    function renderCart(cart) {
        $items.empty();
        if (cart.Items.length === 0)
            $items.append(Store.messageRow(5, cart.LastOrderNumber ? "Empty, last checked out as " + cart.LastOrderNumber + "." : "The cart is empty."));
        for (const item of cart.Items) {
            const $row = $("<tr>").appendTo($items);
            $row.append($("<td>").text(item.ProductName));
            $row.append($("<td>").addClass("num").text(Store.money(item.UnitPrice)));
            $row.append($("<td>").addClass("num").text(item.Quantity));
            $row.append($("<td>").addClass("num").text(Store.money(item.Total)));
            $("<button>").addClass("secondary small").attr("title", "Remove").text("✕")
                .on("click", function () {
                    send(this, new RemoveFromCartCommand({ CustomerID: cart.CustomerID, ProductID: item.ProductID }), item.ProductName + " removed");
                })
                .appendTo($("<td>").addClass("actions").appendTo($row));
        }

        const summary = cart.ItemCount + (cart.ItemCount === 1 ? " item" : " items");
        $("#cart-summary").text(cart.LastEventNumber == null ? summary : summary + " · event " + cart.LastEventNumber + " · " + Store.dateTime(cart.UpdatedOn));
        $("#cart-total").text(Store.money(cart.Total));
        $("#empty-cart, #checkout").prop("disabled", cart.Items.length === 0);
    }

    function renderHistory(history) {
        $history.empty();
        if (history.length === 0) {
            $history.append(Store.messageRow(4, "No events yet, the stream is created by the first one."));
            return;
        }
        for (const entry of history) {
            const badge = eventBadges[entry.EventName] || [entry.EventName, "neutral"];
            const $row = $("<tr>").appendTo($history);
            $row.append($("<td>").addClass("num").text(entry.EventNumber));
            $row.append($("<td>").append(
                Store.badge(badge[0], badge[1]),
                $("<span>").addClass("subtle").text(" " + Store.dateTime(entry.OccurredOn))
            ));
            $row.append($("<td>").addClass("num").text(entry.ItemCount));
            $row.append($("<td>").addClass("num").text(Store.money(entry.Total)));
        }
    }

    function send(button, command, message) {
        return Store.busy(button, Store.send(command))
            .then(function (result) {
                Store.toast(typeof message === "function" ? message(result) : message, "success");
                return loadCart();
            })
            .catch(function () { });
    }

    $customer.on("change", function () {
        loadCart().catch(function () { });
    });

    $("#add-item").on("submit", function (e) {
        e.preventDefault();
        const command = new AddToCartCommand({
            CustomerID: $customer.val(),
            ProductID: $product.val(),
            Quantity: parseInt($("#quantity").val(), 10) || 0
        });
        send("#add", command, $product.find("option:selected").text().split(" · ")[0] + " added");
    });

    $("#empty-cart").on("click", function () {
        send(this, new EmptyCartCommand({ CustomerID: $customer.val() }), "Cart emptied");
    });

    //CheckoutCartCommand has a result, DispatchAwait resolves with the CheckoutCartResult
    $("#checkout").on("click", function () {
        send(this, new CheckoutCartCommand({ CustomerID: $customer.val() }), function (result) {
            return "Order " + result.OrderNumber + " placed for " + Store.money(result.Total);
        });
    });

    $("#refresh").on("click", function () {
        Store.busy(this, loadCart()).catch(function () { });
    });

    $items.append(Store.messageRow(5, "Loading…"));
    loadOptions()
        .then(function () {
            //Grace's cart is seeded, so start there
            if ($customer.find("option[value='9d3b6a10-2f4c-4e8a-b5d1-6c0e2a7f0002']").length)
                $customer.val("9d3b6a10-2f4c-4e8a-b5d1-6c0e2a7f0002");
            return loadCart();
        })
        .catch(function () {
            $items.empty().append(Store.messageRow(5, "Couldn't load the cart. Are the Catalog, Orders, and Carts services running?"));
        });
});
