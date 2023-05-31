const Balance1ModelType =
{
    AccountID: "string",
    Balance: "number",
    LastTransactionDate: "Date",
    LastTransactionAmount: "number",
}

const Transaction1ModelType =
{
    AccountID: "string",
    Date: "Date",
    Amount: "number",
    Description: "string",
    Balance: "number",
    Event: "string",
}

const Balance2ModelType =
{
    AccountID: "string",
    Balance: "number",
    LastTransactionDate: "Date",
    LastTransactionAmount: "number",
}

const Transaction2ModelType =
{
    AccountID: "string",
    Date: "Date",
    Amount: "number",
    Description: "string",
    Balance: "number",
    Event: "string",
}

const SpeciesModelType =
{
    ID: "string",
    Name: "string",
}

const BreedModelType =
{
    ID: "string",
    Name: "string",
}

const PetModelType =
{
    ID: "string",
    Name: "string",
    Breed: "string",
    Species: "string",
    LastEaten: "Date",
    AmountEaten: "number",
    LastPooped: "Date",
}

const WeatherModelType =
{
    Date: "Date",
    WeatherType: "string",
}

const ModelTypeDictionary =
{
    Balance1Model: Balance1ModelType,
    Transaction1Model: Transaction1ModelType,
    Balance2Model: Balance2ModelType,
    Transaction2Model: Transaction2ModelType,
    SpeciesModel: SpeciesModelType,
    BreedModel: BreedModelType,
    PetModel: PetModelType,
    WeatherModel: WeatherModelType,
}

const ILedger1QueryProvider = {
    HasBalance: function(accountID, onComplete, onFail) {
        Bus.Call("ILedger1QueryProvider", "HasBalance", [accountID], null, false, onComplete, onFail);
    },
    GetBalance: function(accountID, onComplete, onFail) {
        Bus.Call("ILedger1QueryProvider", "GetBalance", [accountID], Balance1ModelType, false, onComplete, onFail);
    },
    GetTransactions: function(accountID, onComplete, onFail) {
        Bus.Call("ILedger1QueryProvider", "GetTransactions", [accountID], Transaction1ModelType, true, onComplete, onFail);
    },
}

const ILedger2QueryProvider = {
    HasBalance: function(accountID, onComplete, onFail) {
        Bus.Call("ILedger2QueryProvider", "HasBalance", [accountID], null, false, onComplete, onFail);
    },
    GetBalance: function(accountID, onComplete, onFail) {
        Bus.Call("ILedger2QueryProvider", "GetBalance", [accountID], Balance2ModelType, false, onComplete, onFail);
    },
    GetTransactions: function(accountID, onComplete, onFail) {
        Bus.Call("ILedger2QueryProvider", "GetTransactions", [accountID], Transaction2ModelType, true, onComplete, onFail);
    },
}

const IPetsQueryProvider = {
    GetSpecies: function(onComplete, onFail) {
        Bus.Call("IPetsQueryProvider", "GetSpecies", [], SpeciesModelType, true, onComplete, onFail);
    },
    GetBreeds: function(speciesID, onComplete, onFail) {
        Bus.Call("IPetsQueryProvider", "GetBreeds", [speciesID], BreedModelType, true, onComplete, onFail);
    },
    GetPets: function(onComplete, onFail) {
        Bus.Call("IPetsQueryProvider", "GetPets", [], PetModelType, true, onComplete, onFail);
    },
    GetPet: function(id, onComplete, onFail) {
        Bus.Call("IPetsQueryProvider", "GetPet", [id], PetModelType, false, onComplete, onFail);
    },
    IsHungry: function(id, onComplete, onFail) {
        Bus.Call("IPetsQueryProvider", "IsHungry", [id], null, false, onComplete, onFail);
    },
    NeedsToPoop: function(id, onComplete, onFail) {
        Bus.Call("IPetsQueryProvider", "NeedsToPoop", [id], null, false, onComplete, onFail);
    },
}

const IWeatherQueryProvider = {
    GetWeather: function(onComplete, onFail) {
        Bus.Call("IWeatherQueryProvider", "GetWeather", [], WeatherModelType, false, onComplete, onFail);
    },
    TestStreams: function(onComplete, onFail) {
        Bus.Call("IWeatherQueryProvider", "TestStreams", [], null, false, onComplete, onFail);
    },
}

const Deposit1Command = function(properties) {
    this.AccountID = (properties === undefined || properties.AccountID === undefined) ? null : properties.AccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Deposit1Command";
}

const Transfer1Command = function(properties) {
    this.FromAccountID = (properties === undefined || properties.FromAccountID === undefined) ? null : properties.FromAccountID;
    this.ToAccountID = (properties === undefined || properties.ToAccountID === undefined) ? null : properties.ToAccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Transfer1Command";
}

const Withdraw1Command = function(properties) {
    this.AccountID = (properties === undefined || properties.AccountID === undefined) ? null : properties.AccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Withdraw1Command";
}

const Deposit2Command = function(properties) {
    this.AccountID = (properties === undefined || properties.AccountID === undefined) ? null : properties.AccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Deposit2Command";
}

const Transfer2Command = function(properties) {
    this.FromAccountID = (properties === undefined || properties.FromAccountID === undefined) ? null : properties.FromAccountID;
    this.ToAccountID = (properties === undefined || properties.ToAccountID === undefined) ? null : properties.ToAccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Transfer2Command";
}

const Withdraw2Command = function(properties) {
    this.AccountID = (properties === undefined || properties.AccountID === undefined) ? null : properties.AccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Withdraw2Command";
}

const AdoptPetCommand = function(properties) {
    this.PetID = (properties === undefined || properties.PetID === undefined) ? null : properties.PetID;
    this.BreedID = (properties === undefined || properties.BreedID === undefined) ? null : properties.BreedID;
    this.Name = (properties === undefined || properties.Name === undefined) ? null : properties.Name;
    this.CommandType = "AdoptPetCommand";
}

const FeedPetCommand = function(properties) {
    this.PetID = (properties === undefined || properties.PetID === undefined) ? null : properties.PetID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.CommandType = "FeedPetCommand";
}

const LetPetOutToPoopCommand = function(properties) {
    this.PetID = (properties === undefined || properties.PetID === undefined) ? null : properties.PetID;
    this.CommandType = "LetPetOutToPoopCommand";
}

const SetWeatherCommand = function(properties) {
    this.WeatherType = (properties === undefined || properties.WeatherType === undefined) ? null : properties.WeatherType;
    this.CommandType = "SetWeatherCommand";
}

