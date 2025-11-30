using Pets.Domain;
using Pets.Domain.Commands;
using Pets.Service;
using System.Diagnostics;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Serialization;

Console.WriteLine("Starting");

var timer = Stopwatch.StartNew();

//Zerra.SourceGeneration.Register.Method(typeof(Zerra.CQRS.IBusInternal), "_DispatchCommandWithResultInternalAsync-AdoptPetCommand", true, null, [], static (object instance, object[] args) => ((Zerra.CQRS.IBusInternal)instance)._DispatchCommandWithResultInternalAsync<int>((Pets.Domain.Commands.AdoptPetCommand)args[0], (Type)args[1], (string)args[2], (CancellationToken)args[3]), static (object task) => ((Task<int>)task).Result);

//ISerializer serializer = new SystemTextJsonSerializer(TempJsonSerializerGeneration.Default.Options);

//ISerializer serializer = new ZerraJsonSerializer();

ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);
ILogger log = new Logger();
IBusLogger busLog = new BusLogger();

var busScopes = new BusScopes();
busScopes.AddScope<IThing>(new Thing("Hello"));

IBus busServer = Bus.New("pets-service-server", log, busLog, busScopes);
busServer.AddHandler<IPetsQueryHandler>(new PetsQueryHandler());
busServer.AddHandler<IPetsCommandHandler>(new PetsCommandHandler());
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
busServer.AddQueryServer<IPetsQueryHandler>(server);
busServer.AddCommandConsumer<IPetsCommandHandler>(server);

IBus busClient = Bus.New("pets-service-client", log, busLog, busScopes);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, log);
busClient.AddQueryClient<IPetsQueryHandler>(client);
busClient.AddCommandProducer<IPetsCommandHandler>(client);

Console.WriteLine($"Elapsed: {timer.ElapsedMilliseconds} ms");

var species = await busClient.Call<IPetsQueryHandler>().GetSpecies();
Console.WriteLine($"Species count: {species.Length}");

var specie = species.First();
var breeds = await busClient.Call<IPetsQueryHandler>().GetBreedsBySpecies(specie.ID);
Console.WriteLine($"Breed count: {breeds.Length} for {specie.Name}");

var breed = breeds.First();
var result = await busClient.DispatchAwaitAsync(new AdoptPetCommand()
{
    PetID = Guid.NewGuid(),
    BreedID = breed.ID,
    Name = "Fido",
});
Console.WriteLine($"AdoptPetCommand Result: {result}");

var pets = await busClient.Call<IPetsQueryHandler>().GetPets();
Console.WriteLine($"Pets count: {pets.Count}");

busClient.Call<IPetsQueryHandler>().GetVoid();
var nonTask = busClient.Call<IPetsQueryHandler>().GetNonTask();
await busClient.Call<IPetsQueryHandler>().GetTaskOnly();

Console.WriteLine($"Elapsed: {timer.ElapsedMilliseconds} ms");

Console.WriteLine("Done");
Console.ReadLine();
