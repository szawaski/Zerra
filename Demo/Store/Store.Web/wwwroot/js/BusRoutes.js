//Every query and command goes to the gateway on this site, see UseCqrsApiGateway in Program.cs
const BusRoutes = {
    "Gateway": "/CQRS"
};

//Called by Bus.js for every failed query or command, the message is the exception message from the service that threw it
const BusFail = function (message, url) {
    Store.toast(message, "error");
};
