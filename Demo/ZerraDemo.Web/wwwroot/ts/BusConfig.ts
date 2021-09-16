export const BusRoutes: { [index: string]: string } = {
    "Gateway": "/CQRS"
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