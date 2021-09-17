# Zerra
Zerra Framework: Fast Powerful CQRS and Agnostic Repository with Event Sourcing
- Run solutions as monoliths or split out domain pieces to run independently
- The calling code doesn't know if it's part of the assembly or running elsewhere on the network
- Communication requires almost no configuration using fast TCP communication
- Agnostic repositories for different datastores including Event Sourcing

# Constructing a Query
Make an interface in a common domain project. The attribute is to enable the calls over a network when domains run independently.  This project should contain nothing but interfaces and models, no logic or property mapping.
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
Calling from TypeScript
```typescript
const weather = await IWeatherQueryProvider.GetWeather();
````
Calling from JavaScript
```javascript
const weather = IWeatherQueryProvider.GetWeather(function(data){
  //completed
  }, function(){
  //error
  }
);
```

# Constructing a Command
Make a command in a common domain project. The attribute is to enable the command over a network when domains run independently. This project should contain nothing but interfaces and models, no logic or property mapping.
```csharp
[ServiceExposed]
public class SetWeatherCommand : ICommand
{
    public WeatherType WeatherType { get; set; }
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
    WeatherType = WeatherType.Windy
};
await Bus.DispatchAsync(command);
```
Dispatching from TypeScript
```typescript
var command = new SetSeatherCommand(
{
    WeatherType = "Windy"
});
await Bus.DispatchAsync(command);
```
Dispatching from JavaScript
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
**Special Dispatching**: The framework has an acknowledgement system that will wait for the return of a signal when a command has completed execution. This is helpful when eventual consistency is not adequate and needing more immediate confirmation. This will also return errors with actual exception information if the command fails. This will not return domain data, that is an anti-pattern of CQRS.  This works simularly for TypeScript and JavaScript
```csharp
await Bus.DispatchAwaitAsync(command);
```
