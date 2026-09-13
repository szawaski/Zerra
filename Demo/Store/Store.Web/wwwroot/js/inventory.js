$(function () {

    const $stock = $("#stock");
    const $movements = $("#movements");
    let products = {};

    const movementBadges = {
        Restocked: "success",
        Reserved: "info",
        Released: "neutral",
        Shipped: "warning"
    };

    async function loadStock() {
        //two services queried in parallel, the page composes the view
        const [catalog, levels] = await Promise.all([
            Store.query(ICatalogQueryHandler.GetProducts),
            Store.query(IInventoryQueryHandler.GetStockLevels)
        ]);

        products = {};
        for (const product of catalog)
            products[product.ID] = product;

        const levelsByProduct = {};
        for (const level of levels)
            levelsByProduct[level.ProductID] = level;

        //rows are rebuilt only when the product list changes, otherwise the numbers update in place so a quantity being typed isn't lost
        const productKey = catalog.map(function (x) { return x.ID + x.IsActive; }).join();
        if ($stock.data("productKey") !== productKey) {
            $stock.empty().data("productKey", productKey);
            for (const product of catalog)
                $stock.append(stockRow(product));
        }

        for (const product of catalog) {
            //a new product has no stock record until it's restocked
            const level = levelsByProduct[product.ID] || { OnHand: 0, Reserved: 0, Available: 0 };
            updateStockRow($stock.children("[data-product='" + product.ID + "']"), level);
        }
    }

    function updateStockRow($row, level) {
        $row.find(".on-hand").text(level.OnHand);
        $row.find(".reserved").text(level.Reserved);

        let badge;
        if (level.Available === 0)
            badge = Store.badge("Out", "danger");
        else if (level.Available <= 3)
            badge = Store.badge(level.Available + " low", "warning");
        else
            badge = Store.badge(level.Available, "success");
        $row.find(".available").empty().append(badge);
    }

    function stockRow(product) {
        const $row = $("<tr>").attr("data-product", product.ID).toggleClass("muted-row", !product.IsActive);
        $row.append($("<td>").append($("<div>").text(product.Name), $("<div>").addClass("subtle").text(product.Sku + (product.IsActive ? "" : " · discontinued"))));
        $row.append($("<td>").addClass("num on-hand"));
        $row.append($("<td>").addClass("num reserved"));
        $row.append($("<td>").addClass("num available"));

        const $quantity = $("<input type='number' min='1' value='10'>").addClass("qty-input");
        const $button = $("<button>").addClass("secondary small").text("Add");
        $button.on("click", function () {
            const command = new RestockProductCommand({ ProductID: product.ID, Quantity: parseInt($quantity.val(), 10) || 0 });
            Store.busy(this, Store.send(command))
                .then(function () {
                    Store.toast("Restocked " + command.Quantity + " " + product.Name, "success");
                    return Promise.all([loadStock(), loadMovements()]);
                })
                .catch(function () { });
        });
        $row.append($("<td>").addClass("num").append($("<span>").addClass("inline").append($quantity, $button)));
        return $row;
    }

    async function loadMovements() {
        const movements = await Store.query(IInventoryQueryHandler.GetRecentMovements, 25);
        $movements.empty();
        if (movements.length === 0) {
            $movements.append(Store.messageRow(3, "No stock movements yet."));
            return;
        }
        for (const movement of movements) {
            const product = products[movement.ProductID];
            const $row = $("<tr>");
            $row.append($("<td>").addClass("subtle").text(Store.time(movement.OccurredOn)));
            $row.append($("<td>").append(
                $("<div>").text(product ? product.Name : movement.ProductID),
                $("<div>").append(Store.badge(movement.Kind, movementBadges[movement.Kind] || "neutral"), $("<span>").addClass("subtle").text(movement.OrderNumber ? " " + movement.OrderNumber : ""))
            ));
            const sign = movement.Kind === "Restocked" ? "+" : movement.Kind === "Shipped" ? "−" : "";
            $row.append($("<td>").addClass("num").text(sign + movement.Quantity));
            $movements.append($row);
        }
    }

    $("#refresh-stock").on("click", function () {
        Store.busy(this, Promise.all([loadStock(), loadMovements()])).catch(function () { });
    });

    //events are handled by the Inventory service a moment after the order changes, polling shows them arrive
    setInterval(function () {
        if ($("#auto-refresh").is(":checked") && document.visibilityState === "visible")
            Promise.all([loadStock(), loadMovements()]).catch(function () { $("#auto-refresh").prop("checked", false); });
    }, 4000);

    $stock.append(Store.messageRow(5, "Loading…"));
    $movements.append(Store.messageRow(3, "Loading…"));
    loadStock()
        .then(loadMovements)
        .catch(function () {
            $stock.empty().removeData("productKey").append(Store.messageRow(5, "Couldn't load stock. Are the Catalog and Inventory services running?"));
            $movements.empty();
        });
});
