const routes: { [index: string]: string } = {
    "Gateway": "/CQRS"
};
export const SetBusRoute = function (name: string, url: string) {
    routes[name] = url;
}

export const BusRoutes = function (): { [index: string]: string } {
    return routes;
}

let busFailCallback: ((message: string) => void) | null = null;
export const SetBusFailCallback = function (callback: (message: string) => void) {
    busFailCallback = callback;
};

export const BusFail = function (message: string, url: string, setHeader: (header: string, value: any) => void, retry: () => void, retryCount: number, reject: (reason?: any) => void) {
    if (busFailCallback !== null)
        busFailCallback(message);
    reject(message);
};
