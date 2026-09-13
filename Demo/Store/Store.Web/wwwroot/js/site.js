//Helpers shared by the Store pages

const Store = {

    //Wraps a generated query function in a promise, e.g. Store.query(ICatalogQueryHandler.GetProductsByCategory, categoryID)
    query: function (queryFunction, ...args) {
        return new Promise(function (resolve, reject) {
            queryFunction(...args, resolve, reject);
        });
    },

    //Dispatches a command and waits for the service to finish handling it, resolving with the result for commands that have one
    send: function (command) {
        return new Promise(function (resolve, reject) {
            Bus.DispatchAwait(command, resolve, reject);
        });
    },

    _currency: new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }),
    money: function (value) {
        return Store._currency.format(value);
    },

    dateTime: function (value) {
        if (!(value instanceof Date))
            return "";
        return value.toLocaleString(undefined, { month: "short", day: "numeric", hour: "numeric", minute: "2-digit" });
    },

    time: function (value) {
        if (!(value instanceof Date))
            return "";
        return value.toLocaleTimeString(undefined, { hour: "numeric", minute: "2-digit", second: "2-digit" });
    },

    badge: function (text, kind) {
        return $("<span>").addClass("badge badge-" + kind).text(text);
    },

    //A single table row spanning every column, for loading and empty states
    messageRow: function (columns, text) {
        return $("<tr>").addClass("message-row").append($("<td>").attr("colspan", columns).text(text));
    },

    //Disables a button while the promise runs so a command isn't sent twice
    busy: function (button, promise) {
        const $button = $(button).prop("disabled", true);
        return promise.finally(function () { $button.prop("disabled", false); });
    },

    toast: function (message, kind) {
        let $container = $("#toasts");
        if ($container.length === 0)
            $container = $("<div id='toasts' role='status' aria-live='polite'>").appendTo(document.body);
        const $toast = $("<div>").addClass("toast toast-" + (kind || "info")).text(message).appendTo($container);
        setTimeout(function () {
            $toast.addClass("toast-hide");
            setTimeout(function () { $toast.remove(); }, 300);
        }, kind === "error" ? 7000 : 3500);
    }
};
