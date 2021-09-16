// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

const Bus = {

    createModel: function (modelTypeName) {
        const model = {};
        const modelType = ModelTypeDictionary[modelTypeName];
        for (const property in modelType) {
            const propertyType = modelType[property];
            let value = null;
            if (propertyType !== "number" && propertyType !== "string" && propertyType !== "boolean" && propertyType !== "Date") {
                const hasMany = propertyType.includes("[]");
                if (hasMany) {
                    value = [];
                } else {
                    try {
                        const subModel = ModelTypeDictionary[propertyType.replace("[]", "")];
                        value = Bus.createModel(subModel);
                    } catch (exception) { const ok = 1; }
                }
            }
            model[property] = value;
        }
        return model;
    },

    _pad: function (value, length) {
        let str = value.toString();
        while (str.length < length)
            str = "0" + str;
        return str;
    },

    _dateToString: function (value) {
        const offset = value.getTimezoneOffset();
        const offsetHours = Math.abs(Math.floor(offset / 60));
        const offsetMinutes = Math.abs(offset - (Math.floor(offset / 60) * 60));

        let d = value.getFullYear() + "-" + Bus._pad(value.getMonth() + 1, 2) + "-" + Bus._pad(value.getDate(), 2);
        d += "T" + Bus._pad(value.getHours(), 2) + ":" + Bus._pad(value.getMinutes(), 2) + ":" + Bus._pad(value.getSeconds(), 2) + "." + Bus._pad(value.getMilliseconds(), 3);
        d += (offset < 0 ? "+" : "-") + Bus._pad(offsetHours, 2) + ":" + Bus._pad(offsetMinutes, 2);

        return d;
    },

    _serializeJson: function (data) {
        const replacer = function (key, value) {
            const origionalValue = this[key];
            if (origionalValue instanceof Date) {
                return Bus._dateToString(origionalValue);
            }
            return value;
        };
        const str = JSON.stringify(data, replacer);
        return str;
    },

    _deserializeJson: function (data, modelType, hasMany) {
        if (data === null || modelType === null)
            return null;

        if (hasMany) {
            for (const index in data) {
                Bus._deserializeJson(data[index], modelType, false);
            }
        }
        else {
            for (const property in data) {
                const propertyType = modelType[property];
                const value = data[property];
                if (propertyType === "Date") {
                    if (value !== null) {
                        data[property] = new Date(value);
                    }
                }
                else if (propertyType !== "number" && propertyType !== "string" && propertyType !== "boolean") {
                    const hasMany = propertyType.indexOf("[]") >= 0;
                    try {
                        const subModel = ModelTypeDictionary[propertyType.replace("[]", "")];
                        Bus._deserializeJson(value, subModel, hasMany);
                    }
                    catch (exception) { const ok = 1; }
                }
            }
        }
    },

    _deserializeJsonNameless: function (data, modelType, hasMany) {
        if (data === null || modelType === null)
            return null;

        if (hasMany) {
            const deserializedItems = [];
            for (const index in data) {
                const deserializedItem = Bus._deserializeJsonNameless(data[index], modelType, false);
                deserializedItems.push(deserializedItem);
            }
            return deserializedItems;
        }
        else {
            const model = {};
            if (Object.keys(modelType).length !== data.length)
                throw "Model property counts do not match";
            let index = 0;
            for (const property in modelType) {
                const propertyType = modelType[property];
                let value = data[index];
                if (propertyType === "Date") {
                    if (value !== null) {
                        value = new Date(value);
                    }
                }
                else if (propertyType !== "number" && propertyType !== "string" && propertyType !== "boolean") {
                    const hasMany = propertyType.indexOf("[]") >= 0;
                    try {
                        const subModel = ModelTypeDictionary[propertyType.replace("[]", "")];
                        value = Bus._deserializeJsonNameless(value, subModel, hasMany);
                    }
                    catch (exception) { const ok = 1; }
                }
                model[property] = value;
                index++;
            }
            return model;
        }
    },

    _getRoute: function (provider) {
        let route = BusRoutes[provider];
        if (typeof route !== "string" || route === null || route === "")
            route = BusRoutes["Gateway"];
        if (typeof route !== "string" || route === null || route === "")
            throw "Provider " + provider + " or 'gateway' not defined in BusRoutes";
        return route;
    },

    _isCors: function (url) {
        return url.toLowerCase().indexOf("http") === 0;
    },

    _onFail: function (jqXHR, url, onFail) {
        let errorText;
        if (jqXHR.responseText) {
            errorText = "Error " + jqXHR.status + ": " + jqXHR.responseText;
            console.log(errorText);
        }
        else {
            errorText = "Server Error " + jqXHR.status;
            console.log(errorText + ": " + url);
        }
        if (onFail && typeof onFail === "function") {
            onFail(errorText);
        }
    },

    _customHeaders: {},
    setHeader: function (header, value) {
        Bus._customHeaders[header] = value;
    },

    Call: function (provider, method, args, modelType, hasMany, onComplete, onFail) {
        const route = Bus._getRoute(provider);
        const isCors = Bus._isCors(route);

        console.log("Calling " + provider + "." + method);

        const postData = {
            ProviderType: provider,
            ProviderMethod: method,
            ProviderArguments: args === null || args === "" ? undefined : args
        };

        if (postData.ProviderArguments !== undefined) {
            if (!Array.isArray(postData.ProviderArguments))
                throw "Arguments must be an array";
            for (let i = 0; i < postData.ProviderArguments.length; i++) {
                postData.ProviderArguments[i] = Bus._serializeJson(postData.ProviderArguments[i]);
            }
        }

        const jsonNameless = modelType !== null && modelType !== undefined;
        const accept = jsonNameless ? "application/jsonnameless; charset=utf-8" : "application/json; charset=utf-8";

        const headers = {};
        headers["Provider-Type"] = provider;
        for (const property in Bus._customHeaders) {
            const value = Bus._customHeaders[property];
            if (value !== null && value !== "") {
                if (typeof value === "function")
                    headers[property] = value();
                else
                    headers[property] = value.toString();
            }
        }

        $.ajax({
            url: route,
            type: "POST",
            data: JSON.stringify(postData),
            contentType: "application/json; charset=utf-8",
            dataType: "json",
            accepts: {
                json: accept
            },
            headers: headers,
            crossDomain: isCors
        })
            .done(function (data, textStatus, jqXHR) {
                const responseContentType = jqXHR.getResponseHeader("content-type");
                const responseJsonNameless = responseContentType.includes("application/jsonnameless");

                let deserialized = data;
                if (responseJsonNameless) {
                    deserialized = Bus._deserializeJsonNameless(data, modelType, hasMany);
                } else {
                    Bus._deserializeJson(deserialized, modelType, hasMany);
                }

                if (onComplete && typeof onComplete === "function")
                    onComplete(deserialized);
            })
            .fail(function (jqXHR) {
                if (jqXHR.status === 200) {
                    if (onComplete && typeof onComplete === "function")
                        onComplete();
                } else {
                    Bus._onFail(jqXHR, route, onFail);
                }
            });
    },

    Dispatch: function (command, onComplete, onFail) {
        const type = command.CommandType;
        const route = Bus._getRoute(type);
        const isCors = Bus._isCors(route);

        console.log("Dispatching " + type);

        const postData = {
            MessageType: type,
            MessageData: Bus._serializeJson(command),
            MessageAwait: false
        };

        const headers = {};
        headers["Provider-Type"] = type;
        for (const property in Bus._customHeaders) {
            const value = Bus._customHeaders[property];
            if (value !== null && value !== "") {
                if (typeof value === "function")
                    headers[property] = value();
                else
                    headers[property] = value.toString();
            }
        }

        $.ajax({
            url: route,
            type: "POST",
            data: JSON.stringify(postData),
            contentType: "application/json; charset=utf-8",
            headers: headers,
            crossDomain: isCors
        })
            .done(function () {
                if (onComplete && typeof onComplete === "function")
                    onComplete();
            })
            .fail(function (jqXHR) {
                Bus._onFail(jqXHR, route, onFail);
            });
    },

    DispatchAwait: function (command, onComplete, onFail) {
        const type = command.CommandType;
        const route = Bus._getRoute(type);
        const isCors = Bus._isCors(route);

        console.log("Dispatching " + type);

        const postData = {
            MessageType: type,
            MessageData: Bus._serializeJson(command),
            MessageAwait: true
        };

        const headers = {};
        headers["Provider-Type"] = type;
        for (const property in Bus._customHeaders) {
            const value = Bus._customHeaders[property];
            if (value !== null && value !== "") {
                if (typeof value === "function")
                    headers[property] = value();
                else
                    headers[property] = value.toString();
            }
        }

        $.ajax({
            url: route,
            type: "POST",
            data: JSON.stringify(postData),
            contentType: "application/json; charset=utf-8",
            headers: headers,
            crossDomain: isCors
        })
            .done(function () {
                if (onComplete && typeof onComplete === "function")
                    onComplete();
            })
            .fail(function (jqXHR) {
                Bus._onFail(jqXHR, route, onFail);
            });
    }
};