import { BusRoutes, BusFail } from "./BusConfig"
import { ModelTypeDictionary } from "./TypeScriptModels";

export interface ICommand { }

export class Bus {

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

    private static _deserializeJson(data: any, modelType: any, hasMany: boolean): any {
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
    }

    private static _deserializeJsonNameless(data: any, modelType: any, hasMany: boolean): any {
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
            const model: { [index: string]: any } = {};
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
    }

    private static _getRoute(provider: string): string {
        if (typeof BusRoutes === "undefined")
            throw "BusRoutes not defined";

        const routes = BusRoutes();

        let route = routes[provider];
        if (typeof route !== "string" || route === null || route === "")
            route = routes["Gateway"];
        if (typeof route !== "string" || route === null || route === "")
            throw "Provider " + provider + " or 'gateway' not defined in busRoutes";
        return route;
    }

    private static _onReject(retry: () => void, retryCount: number, responseText: string, url: string, reject: (reason?: any) => void): void {
        let errorText: string;
        if (responseText) {
            errorText = responseText;
            console.log("Error: " + errorText);
        }
        else {
            errorText = "Server Error";
            console.log("Server Error:" + url);
        }
        if (BusFail) {
            BusFail(errorText, url, Bus.SetHeader, retry, retryCount, reject);
        }
        reject(errorText);
    }

    private static _customHeaders: { [index: string]: any } = {};
    public static SetHeader(header: string, value: any) {
        Bus._customHeaders[header] = value;
    }

    public static Call(provider: string, method: string, args: any[], modelType: any, hasMany: boolean): Promise<any> {
        return new Promise<any>(function (resolve, reject) {
            const route = Bus._getRoute(provider);

            console.log("Call: " + provider + "." + method);

            const postData = {
                ProviderType: provider,
                ProviderMethod: method,
                ProviderArguments: args === null ? undefined : args
            };

            if (postData.ProviderArguments !== undefined) {
                for (let i = 0; i < postData.ProviderArguments.length; i++) {
                    postData.ProviderArguments[i] = Bus._serializeJson(postData.ProviderArguments[i]);
                }
            }

            const hasJsonNameless = modelType !== null;
            const accept = hasJsonNameless ? "application/jsonnameless; charset=utf-8" : "application/json; charset=utf-8";

            const headers: HeadersInit = {};
            headers["Provider-Type"] = provider;
            headers["Content-Type"] = "application/json; charset=utf-8";
            headers["Accept"] = "application/json; charset=utf-8";
            for (const property in Bus._customHeaders) {
                const value = Bus._customHeaders[property];
                if (value !== null) {
                    if (typeof value === "function")
                        headers[property] = value();
                    else
                        headers[property] = value.toString();
                }
            }

            const caller: any = function (retry: any, retryCount: number) {

                fetch(route, {
                    method: "POST",
                    body: JSON.stringify(postData),
                    headers: headers
                }).then(res => {

                    const responseContentType = res.headers.get("content-type");
                    const responseJson = responseContentType?.includes("application/json");
                    const responseJsonNameless = responseContentType?.includes("application/jsonnameless");

                    if (responseJson || res.status != 200) {
                        res.text().then((data) => {
                            if (res.status == 200) {

                                if (data == null || data == "") {
                                    resolve(null);
                                    return;
                                }

                                let deserialized = JSON.parse(data);

                                if (responseJsonNameless)
                                    deserialized = Bus._deserializeJsonNameless(deserialized, modelType, hasMany);
                                else
                                    Bus._deserializeJson(deserialized, modelType, hasMany);

                                resolve(deserialized);
                            }
                            else {
                                const retrier = function () { retry(retry, retryCount + 1); }
                                Bus._onReject(retrier, retryCount, data, route, reject);
                            }
                        });
                    }
                    else {
                        res.blob().then((data) => {
                            resolve(data);
                        })
                    }

                }).catch(err => {
                    const retrier = function () { retry(retry, retryCount + 1); }
                    let message = err.toString();
                    if (message == "TypeError: Failed to fetch")
                        message = "Connection failed";
                    Bus._onReject(retrier, retryCount, message, route, reject);
                });

            };

            const callerRetry: any = caller;
            caller(callerRetry, 0);
        });
    }

    public static DispatchAsync(command: ICommand): Promise<void> {
        return new Promise<void>(function (resolve, reject) {
            const commmandAny: any = command;
            const type: string = commmandAny["CommandType"];
            const route = Bus._getRoute(type);

            console.log("Dispatch: " + type);

            if (type === "Object")
                throw "Command is of type Object. Expected an instance of a command type using the 'new' keyword.";

            const postData = {
                MessageType: type,
                MessageData: Bus._serializeJson(command),
                MessageAwait: false
            };

            const headers: HeadersInit = {};
            headers["Provider-Type"] = type;
            headers["Content-Type"] = "application/json; charset=utf-8";
            headers["Accept"] = "application/json; charset=utf-8";
            for (const property in Bus._customHeaders) {
                const value = Bus._customHeaders[property];
                if (value !== null) {
                    if (typeof value === "function")
                        headers[property] = value();
                    else
                        headers[property] = value.toString();
                }
            }

            const caller: any = function (retry: any, retryCount: number) {

                fetch(route, {
                    method: "POST",
                    body: JSON.stringify(postData),
                    headers: headers
                }).then(res => {

                    res.text().then((data) => {

                        if (res.status == 200) {
                            resolve();
                        }
                        else {
                            const retrier = function () { retry(retry, retryCount + 1); }
                            Bus._onReject(retrier, retryCount, data, route, reject);
                        }
                    });

                }).catch(err => {
                    const retrier = function () { retry(retry, retryCount + 1); }
                    let message = err.toString();
                    if (message == "TypeError: Failed to fetch")
                        message = "Connection failed";
                    Bus._onReject(retrier, retryCount, message, route, reject);
                });

            };

            const callerRetry: any = caller;
            caller(callerRetry, 0);
        });
    }

    public static DispatchAwaitAsync(command: ICommand): Promise<void> {
        return new Promise<void>(function (resolve, reject) {
            const commmandAny: any = command;
            const type: string = commmandAny["CommandType"];
            const route = Bus._getRoute(type);

            console.log("DispatchAwait: " + type);

            const postData = {
                MessageType: type,
                MessageData: Bus._serializeJson(command),
                MessageAwait: true
            };

            const headers: HeadersInit = {};
            headers["Provider-Type"] = type;
            headers["Content-Type"] = "application/json; charset=utf-8";
            headers["Accept"] = "application/json; charset=utf-8";
            for (const property in Bus._customHeaders) {
                const value = Bus._customHeaders[property];
                if (value !== null) {
                    if (typeof value === "function")
                        headers[property] = value();
                    else
                        headers[property] = value.toString();
                }
            }

            const caller: any = function (retry: any, retryCount: number) {

                fetch(route, {
                    method: "POST",
                    body: JSON.stringify(postData),
                    headers: headers
                }).then(res => {

                    res.text().then((data) => {
                        if (res.status == 200) {
                            resolve();
                        }
                        else {
                            const retrier = function () { retry(retry, retryCount + 1); }
                            Bus._onReject(retrier, retryCount, data, route, reject);
                        }
                    });

                }).catch(err => {
                    const retrier = function () { retry(retry, retryCount + 1); }
                    let message = err.toString();
                    if (message == "TypeError: Failed to fetch")
                        message = "Connection failed";
                    Bus._onReject(retrier, retryCount, message, route, reject);
                });

            };

            const callerRetry: any = caller;
            caller(callerRetry, 0);
        });
    }
}
