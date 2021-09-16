var parseUrlRoute = function (rootName) {
    var urlSplit = window.location.href.split("?");
    var urlRoute = urlSplit[0].split("/");
    if (urlRoute[urlRoute.length - 1] === "")
        urlRoute.pop();

    var results = [];
    var urlArg = urlRoute.pop();
    while (urlArg.toLowerCase() !== rootName.toLowerCase()) {
        results.push(urlArg);
        if (urlRoute.length == 0)
            break;
        urlArg = urlRoute.pop();
    }

    results.reverse();

    return results;
}

var parseUrlArgs = function () {
    var urlSplit = window.location.href.split("?");
    var urlArgs = urlSplit.length > 1 ? urlSplit.slice(1, urlSplit.length).join("") : "";
    var urlArgSplit = urlArgs.split("&");

    var result = {};
    for (var i = 0; i < urlArgSplit.length; i++) {
        var keyvalue = urlArgSplit[i].split("=");
        if (keyvalue.length !== 2)
            break;
        result[keyvalue[0]] = keyvalue[1];
    }

    return result;
}

var parseUrlHash = function () {
    var urlSplit = window.location.href.split("#");
    var urlArgs = urlSplit.length > 1 ? urlSplit.slice(1, urlSplit.length).join("") : "";
    var urlArgSplit = urlArgs.split("&");

    var result = {};
    for (var i = 0; i < urlArgSplit.length; i++) {
        var keyvalue = urlArgSplit[i].split("=");
        if (keyvalue.length !== 2)
            break;
        result[keyvalue[0]] = keyvalue[1];
    }

    return result;
}