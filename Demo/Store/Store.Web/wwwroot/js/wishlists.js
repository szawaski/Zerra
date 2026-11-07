$(function () {

    const $customer = $("#customer");
    const $product = $("#product");
    const $items = $("#items");
    const $mostWished = $("#most-wished");

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

    async function loadWishlist() {
        const customerID = $customer.val();
        if (!customerID)
            return;
        renderWishlist(await Store.query(IWishlistsQueryHandler.GetWishlist, customerID));
    }

    async function loadMostWished() {
        renderMostWished(await Store.query(IWishlistsQueryHandler.GetMostWished, 5));
    }

    function renderWishlist(items) {
        $items.empty();
        if (items.length === 0)
            $items.append(Store.messageRow(5, "Nothing saved yet."));
        for (const item of items) {
            const $row = $("<tr>").appendTo($items);
            const $name = $("<td>").text(item.ProductName).appendTo($row);
            if (!item.IsActive)
                $name.append(" ", Store.badge("Discontinued", "warning"));
            $row.append($("<td>").addClass("num").text(item.IsActive ? Store.money(item.Price) : "—"));
            $row.append($("<td>").addClass("subtle").text(item.Note || ""));
            $row.append($("<td>").addClass("subtle").text(Store.dateTime(item.AddedOn)));
            $("<button>").addClass("secondary small").attr("title", "Remove").text("✕")
                .on("click", function () {
                    send(this, new RemoveFromWishlistCommand({ CustomerID: $customer.val(), ProductID: item.ProductID }), item.ProductName + " removed");
                })
                .appendTo($("<td>").addClass("actions").appendTo($row));
        }
    }

    function renderMostWished(products) {
        $mostWished.empty();
        if (products.length === 0)
            $mostWished.append(Store.messageRow(2, "No product is on a wishlist yet."));
        for (const product of products) {
            const $row = $("<tr>").appendTo($mostWished);
            $row.append($("<td>").text(product.ProductName));
            $row.append($("<td>").addClass("num").text(product.Customers));
        }
    }

    function send(button, command, message) {
        return Store.busy(button, Store.send(command))
            .then(function () {
                Store.toast(message, "success");
                return Promise.all([loadWishlist(), loadMostWished()]);
            })
            .catch(function () { });
    }

    $customer.on("change", function () {
        loadWishlist().catch(function () { });
    });

    $("#add-item").on("submit", function (e) {
        e.preventDefault();
        const command = new AddToWishlistCommand({
            CustomerID: $customer.val(),
            ProductID: $product.val(),
            Note: $("#note").val()
        });
        send("#add", command, $product.find("option:selected").text().split(" · ")[0] + " saved").then(function () {
            $("#note").val("");
        });
    });

    $("#refresh").on("click", function () {
        Store.busy(this, Promise.all([loadWishlist(), loadMostWished()])).catch(function () { });
    });

    $items.append(Store.messageRow(5, "Loading…"));
    $mostWished.append(Store.messageRow(2, "Loading…"));
    loadOptions()
        .then(function () {
            return Promise.all([loadWishlist(), loadMostWished()]);
        })
        .catch(function () {
            $items.empty().append(Store.messageRow(5, "Couldn't load the wishlist. Are the Catalog, Orders, and Wishlists services running?"));
            $mostWished.empty();
        });
});
