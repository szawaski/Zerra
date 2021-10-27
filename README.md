# Zerra Framework: Fast Powerful CQRS and Agnostic Repository with Event Sourcing
- Container ready for Docker/Kubernetes
- Run solutions as monoliths or split out domain pieces into **Microservices**
- Call services agnostically as they run in the assembly or remotely
- Communication requires little configuration and no code
- Repositories agnostically access different datastores including **Event Sourcing**
- Transparent security transfers claims across services
- **High Speed** using advanced memory optimizations and Intermediate Language generation.
- Check out the ZerraDemo Project in the code base for a complete example

# Quick Start Guide

## Installing
You can find all the packages on NuGet with the Zerra namespace. Start with the core framework for the quick start. [https://www.nuget.org/packages/Zerra/]
- Install-Package Zerra

## Constructing a Query
**If the domains are referenced in the same running project, they will automatically find the implementations with no other code needed.  If domains are running seperatly see Network Setup below.**\
\
Create a universally shared domain project for the following classes if you do not have one already.\
\
Create a model for the weather.
```csharp
public class WeatherModel
{
    public string WeatherType { get; set; }
}
```
Make an interface for queries. The attribute is to enable the calls over a network when domains run independently.  This project should contain nothing but interfaces and models, no logic or property mapping.
```csharp
[ServiceExposed]
public interface IWeatherQueryProvider : IBaseProvider
{
    Task<WeatherModel> GetWeather();
}
```
Make the implementation in a project just for that domain.
```csharp
public class WeatherQueryProvider : IWeatherQueryProvider
{
    public async Task<WeatherModel> GetWeather()
    {
        var weatherType = await WeatherServer.GetWeather();
        return new WeatherModel
        {
            Date = DateTime.UtcNow,
            WeatherType = weatherType
        };
    }
}
```
Calling from C#
```csharp
var weather = Bus.Call<IWeatherQueryProvider>().GetWeather();
```
Calling from TypeScript (see Generating The Front End below)
```typescript
const weather = await IWeatherQueryProvider.GetWeather();
````
Calling from JavaScript (see Generating The Front End below)
```javascript
const weather = IWeatherQueryProvider.GetWeather(function(data){
  //completed
  }, function(){
  //error
  }
);
```

## Constructing a Command
**If the domains are referenced in the same running project, they will automatically find the implementations with no other code needed.  If domains are running seperatly see Network Setup below.**\
\
Create a universally shared domain project for the following classes if you do not have one already.\
\
Make a command to set the weather. The attribute is to enable the command over a network when domains run independently. This project should contain nothing but interfaces and models, no logic or property mapping.
```csharp
[ServiceExposed]
public class SetWeatherCommand : ICommand
{
    public string WeatherType { get; set; }
}
```
Make an interface in a common domain project. You can put all the domain commands in the same class or do one per class. This project should contain nothing but interfaces and models, no logic or property mapping.
```csharp
public interface IWeatherCommandHandler : IBaseProvider,
    ICommandHandler<SetWeatherCommand>
{
}
```
Make the implementation in a project just for that domain.
```csharp
public class WeatherCommandHandler : IWeatherCommandHandler
{
    public async Task Handle(SetWeatherCommand command)
    {
        await WeatherServer.SetWeather(command.WeatherType);
    }
}
```
Dispatching from C#
```csharp
var command = new SetSeatherCommand()
{
    WeatherType = "Windy"
};
await Bus.DispatchAsync(command);
```
Dispatching from TypeScript (see Generating The Front End below)
```typescript
var command = new SetSeatherCommand(
{
    WeatherType = "Windy"
});
await Bus.DispatchAsync(command);
```
Dispatching from JavaScript (see Generating The Front End below)
```javascript
var command = new SetSeatherCommand(
{
    WeatherType = "Windy"
});
Bus.DispatchAsync(command, function(){
    //success
}, function(){
    //failed
});
```
### Special Dispatching
The framework has an acknowledgement system that will wait for the return of a signal when a command has completed execution. This is helpful when eventual consistency is not adequate and needing more immediate confirmation. This will also return errors with actual exception information if the command fails. This will not return domain data, that is an anti-pattern of CQRS.  This works simularly for TypeScript and JavaScript
```csharp
await Bus.DispatchAwaitAsync(command);
```

## Generating The Front End
There are a set of T4 files that will generate TypeScript or JavaScript front ends from the domain that will communicate with gateway (see Gateway For The Front End below).  They mirror the structure of calling it in C# natively to make it very easy.\
You will need:
- Bus.ts [https://github.com/szawaski/Zerra/tree/master/Framework/Zerra.Web/TypeScript/Bus.ts]
- BusConfig.ts [https://github.com/szawaski/Zerra/tree/master/Framework/Zerra.Web/TypeScript/BusConfig.ts]
- TypeScriptModels.tt  [https://github.com/szawaski/Zerra/tree/master/Framework/Zerra.Web/TypeScript/TypeScriptModels.tt]
- Zerra.T4.dll [https://www.nuget.org/packages/Zerra.T4/]

The JavaScript versions are here: [https://github.com/szawaski/Zerra/tree/master/Framework/Zerra.Web/JavaScript].  The T4 file should find the needed domain files run with a Host Enviroment like Visual Studio.  Otherwise you make need to edit it to point to the root of the domain project to scan.  BusConfig.ts can be edited to connect through the gateway or specify service connections directly.  If connecting to the services directly use Bus.SetHeader(name, value) from Bus.ts for adding authentication to be read by IApiAuthorizer.

## Gateway For The Front End
If you are using the prefered TcpInternal setup then clients outside the internal network need a gateway to access the backend such as those using TypeScript and JavaScript.  In an ASPNET front end project add the gateway to the Startup.cs referencing the Zerra.Web assembly. Make sure it comes after the authentication. Claims are transparently passed to the services.
```csharp
public void Configure(IApplicationBuilder app)
{
    //...

    app.UseAuthentication();

    app.UseCQRSGateway();

    //...
}
```

## Network Setup
Create a cqrssettings.config file.
- MessageHost: the address of the event streaming service if used.
- RelayUrl: if the special relay/load balancer is used.
- RelayKey: if the special relay/load balancer is used.
- Services:  Each application needs a service entry.
  - Name: The assembly name of the application.
  - ExternalUrl: Where the service will be found by external services. This is also the default host address if none is provided by environmental variables (ASPNET compatible). 
  - EncryptionKey: If supplied will encrypt the network traffic. It uses AES with a randomizer so the data will never look the same for the same encrypted bytes.
  - Types: List the interface types for commands and queries that each service implements.
  - InstantiateTypes: If there are any other startup needs it will instantiate any classes listed.
```json
{
  "MessageHost": "",
  "RelayUrl": "",
  "RelayKey": "",
  "Services": [
    {
      "Name": "ZerraDemo.Web"
    },
    {
      "Name": "ZerraDemo.Service.Weather",
      "ExternalUrl": "localhost:9002",
      "EncryptionKey": "SecretTransportKey",
      "Types": [
        "IWeatherQueryProvider",
        "IWeatherCommandHandler"
      ],
      "InstantiateTypes": []
    }
  ]
}
```
Add the code to Program.cs Main.  You many consider adding a common project with startup static function.  The cqrssettings.json can then be shared by all the projects as well.
```csharp
static void Main(string[] args)
{
    Config.LoadConfiguration(args);
   
    var serviceSettings = Zerra.CQRS.Settings.CQRSSettings.Get();

    var serviceCreatorInternal = new TcpInternalServiceCreator();


    //Enable one of the following service options
    //----------------------------------------------------------

    //Option1A: Enable this for Tcp for backend only services
    var serviceCreator = serviceCreatorInternal;

    //Option1B: Enable this for Http which can be access directly from a front end
    //var authorizor = new DemoCookieApiAuthorizer();
    //var serviceCreator = new TcpApiServiceCreator(authorizor, null);

    //Option1C: Enable this using RabbitMQ for event streaming commands/events
    //var serviceCreator = new RabbitServiceCreator(serviceSettings.MessageHost, serviceCreatorInternal);

    //Option1D: Enable this using Kafka for event streaming commands/events
    //var serviceCreator = new KafkaServiceCreator(serviceSettings.MessageHost, serviceCreatorInternal);



    //Enable one of the following routing options
    //----------------------------------------------------------

    //Option2A: Enable this for direct service communication, no custom relay/loadbalancer (can still use container balancers)
    Bus.StartServices(settingsName, serviceSettings, serviceCreator, null);

    //Option2B: Enable this to use the relay/loadbalancer
    //var relayRegister = new RelayRegister(serviceSettings.RelayUrl, serviceSettings.RelayKey);
    //Bus.StartServices(settingsName, serviceSettings, serviceCreator, relayRegister);
    
   
   
    //If no other serivce holds the application open such as ASPNET then used this to wait termination
    Bus.WaitUntilExit();
}
```
### Code-Only
You can also link up the networking without using the config file.\
Server Setup:
```csharp
var encryptionKey = SymmetricEncryptor.GetKey("SecretTransportKey");

//This example is not using a seperate event stream service for commands/events
var server = TcpRawCQRSServer.CreateDefault("http://localhost:9002", encryptionKey);
Bus.AddQueryServer(server);
Bus.AddCommandServer(server);
```
Client Setup:
```csharp
var encryptionKey = SymmetricEncryptor.GetKey("SecretTransportKey");

//This example is not using a seperate event stream service for commands/events
var client = TcpRawCQRSClient.CreateDefault("http://localhost:9002", encryptionKey);
Bus.AddQueryClient<IWeatherQueryProvider>(client);
Bus.AddCommandClient<IWeatherCommandHandler>(client);
```
