$(function () {

    const $products = $("#products");
    const $filter = $("#category-filter");

    async function loadCategories() {
        const categories = await Store.query(ICatalogQueryHandler.GetCategories);
        for (const category of categories) {
            $filter.append($("<option>").val(category.ID).text(category.Name));
            $("#new-category").append($("<option>").val(category.ID).text(category.Name));
        }
    }

    async function loadProducts() {
        const categoryID = $filter.val();
        //the filter uses a query with an argument, the service does the filtering
        const products = categoryID
            ? await Store.query(ICatalogQueryHandler.GetProductsByCategory, categoryID)
            : await Store.query(ICatalogQueryHandler.GetProducts);

        $products.empty();
        if (products.length === 0) {
            $products.append(Store.messageRow(6, "No products in this category."));
            return;
        }
        for (const product of products)
            $products.append(productRow(product));
    }

    //failures are already shown by BusFail in BusRoutes.js
    function reload() {
        return loadProducts().catch(function () { });
    }

    function productRow(product) {
        const $row = $("<tr>").toggleClass("muted-row", !product.IsActive);
        $row.append($("<td>").append($("<code>").text(product.Sku)));
        $row.append($("<td>").append($("<div>").text(product.Name), $("<div>").addClass("subtle").text(product.Description || "")));
        $row.append($("<td>").text(product.CategoryName));
        const $price = $("<td>").addClass("num").text(Store.money(product.Price)).appendTo($row);
        $row.append($("<td>").append(product.IsActive ? Store.badge("Active", "success") : Store.badge("Discontinued", "neutral")));

        const $actions = $("<td>").addClass("actions").appendTo($row);
        if (product.IsActive) {
            $("<button>").addClass("secondary small").text("Edit price").appendTo($actions)
                .on("click", function () { editPrice(product, $price, $actions); });
            $("<button>").addClass("danger small").text("Discontinue").css("margin-left", "6px").appendTo($actions)
                .on("click", function () { discontinue(product, this); });
        }
        return $row;
    }

    function editPrice(product, $price, $actions) {
        const $input = $("<input type='number' step='0.01'>").addClass("qty-input").css("width", "96px").val(product.Price);
        const $save = $("<button>").addClass("small").text("Save");
        const $cancel = $("<button>").addClass("secondary small").text("Cancel");
        $price.empty().append($input);
        $actions.children().hide();
        $actions.append($("<span>").addClass("inline").append($save, $cancel));
        $input.trigger("focus").trigger("select");

        $cancel.on("click", reload);
        $save.on("click", function () {
            const command = new ChangeProductPriceCommand({ ProductID: product.ID, Price: parseFloat($input.val()) });
            Store.busy(this, Store.send(command))
                .then(function () {
                    Store.toast(product.Name + " is now " + Store.money(command.Price), "success");
                    return loadProducts();
                })
                .catch(function () { });
        });
        $input.on("keydown", function (e) {
            if (e.key === "Enter") $save.trigger("click");
            if (e.key === "Escape") reload();
        });
    }

    function discontinue(product, button) {
        if (!confirm("Discontinue " + product.Name + "? It can no longer be ordered."))
            return;
        Store.busy(button, Store.send(new DiscontinueProductCommand({ ProductID: product.ID })))
            .then(function () {
                Store.toast(product.Name + " discontinued", "success");
                return loadProducts();
            })
            .catch(function () { });
    }

    $("#add-product").on("submit", function (e) {
        e.preventDefault();
        const command = new AddProductCommand({
            CategoryID: $("#new-category").val(),
            Sku: $("#new-sku").val(),
            Name: $("#new-name").val(),
            Description: $("#new-description").val(),
            Price: parseFloat($("#new-price").val()) || 0
        });
        //AddProductCommand has a result, DispatchAwait resolves with it
        Store.busy("#add-product-button", Store.send(command))
            .then(function (result) {
                Store.toast("Added " + command.Name + " as product " + result.ProductID, "success");
                $("#new-sku, #new-name, #new-description, #new-price").val("");
                return loadProducts();
            })
            .catch(function () { });
    });

    $filter.on("change", reload);

    $products.append(Store.messageRow(6, "Loading…"));
    Promise.all([loadCategories(), loadProducts()]).catch(function () {
        $products.empty().append(Store.messageRow(6, "Couldn't load the catalog. Is the Catalog service running?"));
    });
});
