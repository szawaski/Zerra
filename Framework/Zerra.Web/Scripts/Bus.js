// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

const Bus = {

    //the gateway serving this file writes its own route here
    _routes: { "Gateway": "/CQRS" },
    _models: {},
    _failCallback: null,
    _customHeaders: {},

    //sets the url of the gateway, or of the one serving a single query interface or command when the name is one
    setRoute: function (name, url) {
        Bus._routes[name] = url;
    },

    //called for every failed query or command, after its own onFail
    onFail: function (callback) {
        Bus._failCallback = callback;
    },

    //sent with every request, a function value is called for each request
    setHeader: function (header, value) {
        Bus._customHeaders[header] = value;
    },

    //called by the generated models script
    addModels: function (models) {
        for (const name in models)
            Bus._models[name] = models[name];
    },

    createModel: function (modelTypeName) {
        const model = {};
        const modelType = Bus._models[modelTypeName];
        for (const property in modelType) {
            const propertyType = modelType[property];
            let value = null;
            if (propertyType.includes("[]"))
                value = [];
            else if (Bus._models[propertyType] !== undefined)
                value = Bus.createModel(propertyType);
            model[property] = value;
        }
        return model;
    },

    //the type of a property's values for deserializing, "Date[]" is "Date" and "OrderModel[]" is the OrderModel type, undefined when unknown
    _elementType: function (propertyType) {
        const elementType = propertyType.replace("[]", "");
        if (elementType === "Date" || elementType === "number" || elementType === "string" || elementType === "boolean")
            return elementType;
        return Bus._models[elementType];
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

    //query arguments are byte arrays on the server, which JSON carries as base64
    _toBase64: function (text) {
        const bytes = new TextEncoder().encode(text);
        let binary = "";
        for (let i = 0; i < bytes.length; i++)
            binary += String.fromCharCode(bytes[i]);
        return btoa(binary);
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

    //converts parsed JSON to the model type, objects are updated in place, use the returned value
    _deserializeJson: function (data, modelType, hasMany) {
        if (data === null || data === undefined || modelType === null || modelType === undefined)
            return data;

        if (hasMany) {
            for (const index in data)
                data[index] = Bus._deserializeJson(data[index], modelType, false);
            return data;
        }

        if (modelType === "Date")
            return new Date(data);
        if (modelType === "number" || modelType === "string" || modelType === "boolean")
            return data;

        for (const property in data) {
            const propertyType = modelType[property];
            if (propertyType === undefined)
                continue;
            try {
                data[property] = Bus._deserializeJson(data[property], Bus._elementType(propertyType), propertyType.indexOf("[]") >= 0);
            }
            catch (exception) { const ok = 1; }
        }
        return data;
    },

    //converts nameless JSON, where each object is an array of its values in model property order, to the model type
    _deserializeJsonNameless: function (data, modelType, hasMany) {
        if (data === null || data === undefined || modelType === null || modelType === undefined)
            return data;

        if (hasMany) {
            const deserializedItems = [];
            for (const index in data)
                deserializedItems.push(Bus._deserializeJsonNameless(data[index], modelType, false));
            return deserializedItems;
        }

        if (modelType === "Date")
            return new Date(data);
        if (modelType === "number" || modelType === "string" || modelType === "boolean")
            return data;

        const model = {};
        if (Object.keys(modelType).length !== data.length)
            throw "Model property counts do not match";
        let index = 0;
        for (const property in modelType) {
            const propertyType = modelType[property];
            let value = data[index];
            try {
                value = Bus._deserializeJsonNameless(value, Bus._elementType(propertyType), propertyType.indexOf("[]") >= 0);
            }
            catch (exception) { const ok = 1; }
            model[property] = value;
            index++;
        }
        return model;
    },

    _getRoute: function (provider) {
        let route = Bus._routes[provider];
        if (typeof route !== "string" || route === "")
            route = Bus._routes["Gateway"];
        if (typeof route !== "string" || route === "")
            throw "Provider " + provider + " or Gateway has no route, call Bus.setRoute";
        return route;
    },

    _getHeaders: function () {
        const headers = {};
        for (const property in Bus._customHeaders) {
            const value = Bus._customHeaders[property];
            if (value !== null && value !== "") {
                if (typeof value === "function")
                    headers[property] = value();
                else
                    headers[property] = value.toString();
            }
        }
        return headers;
    },

    _onFail: function (status, responseText, url, onFail) {
        let errorText;
        let exception = null;
        if (responseText) {
            try { exception = JSON.parse(responseText); } catch (parseException) { exception = null; }
        }
        if (exception && typeof exception.Message === "string") {
            errorText = exception.Message;
            console.log("Error " + status + " " + exception.ExceptionType + ": " + errorText);
        }
        else if (status === 0) {
            errorText = "Connection failed";
            console.log(errorText + ": " + url);
        }
        else if (responseText) {
            errorText = "Error " + status + ": " + responseText;
            console.log(errorText);
        }
        else {
            errorText = "Server Error " + status;
            console.log(errorText + ": " + url);
        }
        if (onFail && typeof onFail === "function")
            onFail(errorText);
        if (Bus._failCallback)
            Bus._failCallback(errorText, url);
    },

    //posts to the gateway, a JSON result is read as the model type, a Blob result as a Blob
    _post: function (route, body, headers, modelType, hasMany, onComplete, onFail) {
        fetch(route, { method: "POST", body: body, headers: headers })
            .then(function (res) {
                if (res.status !== 200) {
                    return res.text().then(function (text) {
                        Bus._onFail(res.status, text, route, onFail);
                    });
                }
                if (modelType === "Blob")
                    return res.blob().then(function (blob) {
                        if (onComplete && typeof onComplete === "function")
                            onComplete(blob);
                    });
                return res.text().then(function (text) {
                    let deserialized = text === "" ? null : JSON.parse(text);
                    if (modelType !== null && modelType !== undefined) {
                        const responseContentType = res.headers.get("content-type");
                        if (responseContentType !== null && responseContentType.includes("application/jsonnameless"))
                            deserialized = Bus._deserializeJsonNameless(deserialized, modelType, hasMany);
                        else
                            deserialized = Bus._deserializeJson(deserialized, modelType, hasMany);
                    }
                    if (onComplete && typeof onComplete === "function")
                        onComplete(deserialized);
                });
            })
            .catch(function (error) {
                Bus._onFail(0, null, route, onFail);
            });
    },

    call: function (provider, method, args, modelType, hasMany, onComplete, onFail) {
        const route = Bus._getRoute(provider);

        console.log("Calling " + provider + "." + method);

        const postData = {
            ProviderType: provider,
            ProviderMethod: method,
            ProviderArguments: args === null || args === "" ? undefined : args,
            Source: "JavaScript"
        };

        let upload = null;
        if (postData.ProviderArguments !== undefined) {
            if (!Array.isArray(postData.ProviderArguments))
                throw "Arguments must be an array";
            for (let i = 0; i < postData.ProviderArguments.length; i++) {
                if (postData.ProviderArguments[i] instanceof Blob) {
                    if (upload !== null)
                        throw "Only one Blob argument can be uploaded";
                    upload = postData.ProviderArguments[i];
                    postData.ProviderArguments[i] = null;
                    continue;
                }
                postData.ProviderArguments[i] = Bus._toBase64(Bus._serializeJson(postData.ProviderArguments[i]));
            }
        }

        const jsonNameless = modelType !== null && modelType !== undefined && modelType !== "Blob";
        const headers = Bus._getHeaders();
        headers["Content-Type"] = "application/json; charset=utf-8";
        headers["Accept"] = jsonNameless ? "application/jsonnameless; charset=utf-8" : "application/json; charset=utf-8";

        //an upload body is {int32 little endian json length}{json}{int32 0}{blob}
        let body = JSON.stringify(postData);
        if (upload !== null) {
            const json = new TextEncoder().encode(body);
            const length = new Uint8Array(4);
            new DataView(length.buffer).setInt32(0, json.length, true);
            body = new Blob([length, json, new Uint8Array(4), upload]);
            headers["Upload-Stream"] = "true";
        }

        Bus._post(route, body, headers, modelType, hasMany, onComplete, onFail);
    },

    dispatch: function (command, onComplete, onFail) {
        const type = command.CommandType;
        const route = Bus._getRoute(type);

        console.log("Dispatching " + type);

        const postData = {
            MessageType: type,
            MessageData: Bus._serializeJson(command),
            MessageAwait: false,
            MessageResult: command.CommandWithResult,
            Source: "JavaScript"
        };

        const headers = Bus._getHeaders();
        headers["Content-Type"] = "application/json; charset=utf-8";

        Bus._post(route, JSON.stringify(postData), headers, null, false, function () {
            if (onComplete && typeof onComplete === "function")
                onComplete();
        }, onFail);
    },

    dispatchAwait: function (command, onComplete, onFail) {
        const type = command.CommandType;
        const hasResult = command.CommandWithResult;
        const resultType = command.ResultType;
        const route = Bus._getRoute(type);

        console.log("Dispatching " + type);

        const postData = {
            MessageType: type,
            MessageData: Bus._serializeJson(command),
            MessageAwait: true,
            MessageResult: hasResult,
            Source: "JavaScript"
        };

        const headers = Bus._getHeaders();
        headers["Content-Type"] = "application/json; charset=utf-8";
        if (hasResult)
            headers["Accept"] = resultType !== null && resultType !== undefined ? "application/jsonnameless; charset=utf-8" : "application/json; charset=utf-8";

        Bus._post(route, JSON.stringify(postData), headers, resultType, command.ResultTypeHasMany, function (result) {
            if (onComplete && typeof onComplete === "function")
                onComplete(hasResult ? result : undefined);
        }, onFail);
    }
};
