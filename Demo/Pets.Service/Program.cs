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

Console.WriteLine("Starting");

var timer = Stopwatch.StartNew();

MapCustomizations.Register(new DefineModelAToModelB());

var modelA = ModelA.GetModelA();
var modelB = modelA.Map<ModelA, ModelB>();
if (int.Parse(modelA.PropA.ToString() + "1") != modelB.PropB)
    throw new Exception("Mapping Failed");
if (modelA.PropC != modelB.PropD)
    throw new Exception("Mapping Failed");

modelA = modelB.Map<ModelB, ModelA>();
if (default != modelA.PropA)
    throw new Exception("Mapping Failed");
if (modelA.PropC != modelB.PropD)
    throw new Exception("Mapping Failed");



ISerializer serializer = new ZerraByteSerializer();
IEncryptor encryptor = new ZerraEncryptor("test", SymmetricAlgorithmType.AESwithPrefix);
ILog log = new Logger();
IBusLog busLog = new BusLogger();

var busServices = new BusServices();
busServices.AddService<IThing>(new Thing("Hello"));

Console.WriteLine($"Elapsed: {timer.ElapsedMilliseconds} ms");
timer.Restart();

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

Console.WriteLine($"Elapsed: {timer.ElapsedMilliseconds} ms");
timer.Restart();

var species = await busClient.Call<IPetsQueryHandler>().GetSpecies();
Console.WriteLine($"Species count: {species.Length}");

Console.WriteLine($"Elapsed: {timer.ElapsedMilliseconds} ms");
timer.Restart();

var specie = species.First();
var breeds = await busClient.Call<IPetsQueryHandler>().GetBreedsBySpecies(specie.ID);
Console.WriteLine($"Breed count: {breeds.Length} for {specie.Name}");

Console.WriteLine($"Elapsed: {timer.ElapsedMilliseconds} ms");
timer.Restart();

var breed = breeds.First();
var result = await busClient.DispatchAwaitAsync(new AdoptPetCommand()
{
    PetID = Guid.NewGuid(),
    BreedID = breed.ID,
    Name = "Fido",
});
Console.WriteLine($"AdoptPetCommand Result: {result}");

Console.WriteLine($"Elapsed: {timer.ElapsedMilliseconds} ms");
timer.Restart();

var pets = await busClient.Call<IPetsQueryHandler>().GetPets();
Console.WriteLine($"Pets count: {pets.Count}");

Console.WriteLine($"Elapsed: {timer.ElapsedMilliseconds} ms");
timer.Restart();

////busClient.Call<IPetsQueryHandler>().GetVoid();
////var nonTask = busClient.Call<IPetsQueryHandler>().GetNonTask();
////await busClient.Call<IPetsQueryHandler>().GetTaskOnly();

////Console.WriteLine($"Elapsed: {timer.ElapsedMilliseconds} ms");
////timer.Restart();

Console.WriteLine("Done");
Console.ReadLine();
