using Pets.Domain;
using Pets.Domain.Commands;
using Pets.Service;
using System.Diagnostics;
using Zerra.CQRS;
using Zerra.CQRS.Network;
using Zerra.Encryption;
using Zerra.Logging;
using Zerra.Map;
using Zerra.Serialization;

Console.WriteLine();
var timer = Stopwatch.StartNew();

ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);
ILog log = new Logger();
IBusLog busLog = new BusLogger();

var busServices = new BusServices();
busServices.AddService<IThing>(new Thing("Hello"));

IBusSetup busServer = Bus.New("pets-service-server", log, busLog, busServices);
busServer.AddHandler<IPetsQueryHandler>(new PetsQueryHandler());
busServer.AddHandler<IPetsCommandHandler>(new PetsCommandHandler());
var server = new TcpCqrsServer("localhost:9001", serializer, encryptor, log);
busServer.AddQueryServer<IPetsQueryHandler>(server);
busServer.AddCommandConsumer<IPetsCommandHandler>(server);

IBusSetup busClient = Bus.New("pets-service-client", log, busLog, busServices);
var client = new TcpCqrsClient("localhost:9001", serializer, encryptor, log);
busClient.AddQueryClient<IPetsQueryHandler>(client);
busClient.AddCommandProducer<IPetsCommandHandler>(client);

Console.WriteLine($"Started Bus: {timer.ElapsedMilliseconds} ms");
Console.WriteLine();
timer.Restart();

var species = await busClient.Call<IPetsQueryHandler>().GetSpecies();
Console.WriteLine($"Call GetSpecies: {timer.ElapsedMilliseconds} ms");
timer.Restart();

var specie = species.First();
var breeds = await busClient.Call<IPetsQueryHandler>().GetBreedsBySpecies(specie.ID);
Console.WriteLine($"Call GetBreedsBySpecies: {timer.ElapsedMilliseconds} ms");
timer.Restart();

var breed = breeds.First();
var result = await busClient.DispatchAwaitAsync(new AdoptPetCommand()
{
    PetID = Guid.NewGuid(),
    BreedID = breed.ID,
    Name = "Fido",
});
Console.WriteLine($"Dispatch Await Async AdoptPetCommand: {timer.ElapsedMilliseconds} ms");
timer.Restart();

var pets = await busClient.Call<IPetsQueryHandler>().GetPets();
Console.WriteLine($"Call GetPets: {timer.ElapsedMilliseconds} ms");
timer.Restart();

busClient.Call<IPetsQueryHandler>().GetVoid();
Console.WriteLine($"Call Void Return: {timer.ElapsedMilliseconds} ms");
timer.Restart();

var nonTask = busClient.Call<IPetsQueryHandler>().GetNonTask();
Console.WriteLine($"Call Non-Task: {timer.ElapsedMilliseconds} ms");
timer.Restart();

await busClient.Call<IPetsQueryHandler>().GetTaskOnly();
Console.WriteLine($"Call Task Without Value: {timer.ElapsedMilliseconds} ms");
timer.Restart();

var modelA = ModelA.GetModelA();
var modelB = modelA.Map<ModelA, ModelB>();
if (modelA.Prop1 != modelB.Prop1) throw new Exception("Mapping Failed");
if (641 != modelB.PropB) throw new Exception("Mapping Failed");
if (128 != modelB.PropD) throw new Exception("Mapping Failed");

modelA = modelB.Map<ModelB, ModelA>();
if (64 != modelA.PropA) throw new Exception("Mapping Failed");
if (128 != modelA.PropC) throw new Exception("Mapping Failed");
Console.WriteLine($"Map Two Ways: {timer.ElapsedMilliseconds} ms");
timer.Restart();

Console.WriteLine();
Console.WriteLine("Done");
Console.ReadLine();
