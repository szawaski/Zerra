// Copyright © KaKush LLC
// Written By Steven Zawaski
// Licensed to you under the MIT license

export interface ICommand { }

export class Bus {

    //the gateway serving this file writes its own route here
    private static _routes: { [index: string]: string } = { "Gateway": "/CQRS" };
    private static _models: { [index: string]: any } = {};
    private static _failCallback: ((message: string, url: string) => void) | null = null;
    private static _customHeaders: { [index: string]: any } = {};

    //sets the url of the gateway, or of the one serving a single query interface or command when the name is one
    public static setRoute(name: string, url: string): void {
        Bus._routes[name] = url;
    }

    //called for every failed query or command, before its promise rejects
    public static onFail(callback: ((message: string, url: string) => void) | null): void {
        Bus._failCallback = callback;
    }

    //sent with every request, a function value is called for each request
    public static setHeader(header: string, value: any): void {
        Bus._customHeaders[header] = value;
    }

    //called by the generated models module
    public static addModels(models: { [index: string]: any }): void {
        for (const name in models)
            Bus._models[name] = models[name];
    }

    private static _pad(value: number, length: number): string {
        let str = value.toString();
        while (str.length < length)
            str = "0" + str;
        return str;
    }

    private static _dateToString(value: Date): string {
        const offset = value.getTimezoneOffset();
        const offsetHours = Math.abs(Math.floor(offset / 60));
        const offsetMinutes = Math.abs(offset - (Math.floor(offset / 60) * 60));

        let d = value.getFullYear() + "-" + Bus._pad(value.getMonth() + 1, 2) + "-" + Bus._pad(value.getDate(), 2);
        d += "T" + Bus._pad(value.getHours(), 2) + ":" + Bus._pad(value.getMinutes(), 2) + ":" + Bus._pad(value.getSeconds(), 2) + "." + Bus._pad(value.getMilliseconds(), 3);
        d += (offset < 0 ? "+" : "-") + Bus._pad(offsetHours, 2) + ":" + Bus._pad(offsetMinutes, 2);

        return d;
    }

    //query arguments are byte arrays on the server, which JSON carries as base64
    private static _toBase64(text: string): string {
        const bytes = new TextEncoder().encode(text);
        let binary = "";
        for (let i = 0; i < bytes.length; i++)
            binary += String.fromCharCode(bytes[i]);
        return btoa(binary);
    }

    private static _serializeJson(data: any): string {
        const replacer = function (this: any, key: any, value: any) {
            const origionalValue = this[key];
            if (origionalValue instanceof Date) {
                return Bus._dateToString(origionalValue);
            }
            return value;
        };
        const str = JSON.stringify(data, replacer);
        return str;
    }

    //the type of a property's values for deserializing, "Date[]" is "Date" and "OrderModel[]" is the OrderModel type, undefined when unknown
    private static _elementType(propertyType: string): any {
        const elementType = propertyType.replace("[]", "");
        if (elementType === "Date" || elementType === "number" || elementType === "string" || elementType === "boolean")
            return elementType;
        return Bus._models[elementType];
    }

    //converts parsed JSON to the model type, objects are updated in place, use the returned value
    private static _deserializeJson(data: any, modelType: any, hasMany: boolean): any {
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
    }

    //converts nameless JSON, where each object is an array of its values in model property order, to the model type
    private static _deserializeJsonNameless(data: any, modelType: any, hasMany: boolean): any {
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

        const model: { [index: string]: any } = {};
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
    }

    private static _getRoute(provider: string): string {
        let route = Bus._routes[provider];
        if (typeof route !== "string" || route === "")
            route = Bus._routes["Gateway"];
        if (typeof route !== "string" || route === "")
            throw "Provider " + provider + " or Gateway has no route, call Bus.setRoute";
        return route;
    }

    private static _getHeaders(): { [index: string]: string } {
        const headers: { [index: string]: string } = {};
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
    }

    private static _onReject(status: number, responseText: string | null, url: string, reject: (reason?: any) => void): void {
        let errorText: string;
        let exception: any = null;
        if (responseText) {
            try { exception = JSON.parse(responseText); } catch { exception = null; }
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
        if (Bus._failCallback !== null)
            Bus._failCallback(errorText, url);
        reject(errorText);
    }

    //posts to the gateway, a JSON result is read as the model type, a Blob result as a Blob
    private static _post(route: string, body: BodyInit, headers: { [index: string]: string }, modelType: any, hasMany: boolean): Promise<any> {
        return new Promise<any>(function (resolve, reject) {
            fetch(route, { method: "POST", body: body, headers: headers })
                .then(res => {
                    if (res.status !== 200) {
                        return res.text().then(text => Bus._onReject(res.status, text, route, reject));
                    }
                    if (modelType === "Blob")
                        return res.blob().then(blob => resolve(blob));
                    return res.text().then(text => {
                        let deserialized = text === "" ? null : JSON.parse(text);
                        if (modelType !== null && modelType !== undefined) {
                            const responseContentType = res.headers.get("content-type");
                            if (responseContentType !== null && responseContentType.includes("application/jsonnameless"))
                                deserialized = Bus._deserializeJsonNameless(deserialized, modelType, hasMany);
                            else
                                deserialized = Bus._deserializeJson(deserialized, modelType, hasMany);
                        }
                        resolve(deserialized);
                    });
                })
                .catch(() => Bus._onReject(0, null, route, reject));
        });
    }

    public static call(provider: string, method: string, args: any[], modelType: any, hasMany: boolean): Promise<any> {
        const route = Bus._getRoute(provider);

        console.log("Calling " + provider + "." + method);

        const postData = {
            ProviderType: provider,
            ProviderMethod: method,
            ProviderArguments: args === null ? undefined : args,
            Source: "TypeScript"
        };

        let upload: Blob | null = null;
        if (postData.ProviderArguments !== undefined) {
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
        const postJson = JSON.stringify(postData);
        let body: BodyInit = postJson;
        if (upload !== null) {
            const json = new TextEncoder().encode(postJson);
            const length = new Uint8Array(4);
            new DataView(length.buffer).setInt32(0, json.length, true);
            body = new Blob([length, json, new Uint8Array(4), upload]);
            headers["Upload-Stream"] = "true";
        }

        return Bus._post(route, body, headers, modelType, hasMany);
    }

    public static dispatchAsync(command: ICommand): Promise<void> {
        const commandAny: any = command;
        const type: string = commandAny["CommandType"];
        if (type === undefined)
            throw "Command has no CommandType. Expected an instance of a command type using the 'new' keyword.";
        const route = Bus._getRoute(type);

        console.log("Dispatching " + type);

        const postData = {
            MessageType: type,
            MessageData: Bus._serializeJson(command),
            MessageAwait: false,
            MessageResult: commandAny["CommandWithResult"],
            Source: "TypeScript"
        };

        const headers = Bus._getHeaders();
        headers["Content-Type"] = "application/json; charset=utf-8";

        return Bus._post(route, JSON.stringify(postData), headers, null, false).then(() => { });
    }

    public static dispatchAwaitAsync(command: ICommand): Promise<any> {
        const commandAny: any = command;
        const type: string = commandAny["CommandType"];
        const hasResult: boolean = commandAny["CommandWithResult"];
        const resultType: any = commandAny["ResultType"];
        if (type === undefined)
            throw "Command has no CommandType. Expected an instance of a command type using the 'new' keyword.";
        const route = Bus._getRoute(type);

        console.log("Dispatching " + type);

        const postData = {
            MessageType: type,
            MessageData: Bus._serializeJson(command),
            MessageAwait: true,
            MessageResult: hasResult,
            Source: "TypeScript"
        };

        const headers = Bus._getHeaders();
        headers["Content-Type"] = "application/json; charset=utf-8";
        if (hasResult)
            headers["Accept"] = resultType !== null && resultType !== undefined ? "application/jsonnameless; charset=utf-8" : "application/json; charset=utf-8";

        return Bus._post(route, JSON.stringify(postData), headers, resultType, commandAny["ResultTypeHasMany"]).then(result => hasResult ? result : undefined);
    }
}
