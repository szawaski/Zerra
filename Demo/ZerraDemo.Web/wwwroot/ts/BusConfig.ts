export const BusRoutes: { [index: string]: string } = {
    "Gateway": "/CQRS"

    //"IWeatherQueryProvider": "http://localhost:8005",
    //"SetWeatherCommand": "http://localhost:8006"
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