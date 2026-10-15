const FeedPetCommandResultType =
{
    AmountEaten: "number",
}

const ModelTypeDictionary =
{
    FeedPetCommandResult: FeedPetCommandResultType,
}

const Deposit1Command = function(properties) {
    this.AccountID = (properties === undefined || properties.AccountID === undefined) ? null : properties.AccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Deposit1Command";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const Transfer1Command = function(properties) {
    this.FromAccountID = (properties === undefined || properties.FromAccountID === undefined) ? null : properties.FromAccountID;
    this.ToAccountID = (properties === undefined || properties.ToAccountID === undefined) ? null : properties.ToAccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Transfer1Command";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const Withdraw1Command = function(properties) {
    this.AccountID = (properties === undefined || properties.AccountID === undefined) ? null : properties.AccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Withdraw1Command";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const Deposit2Command = function(properties) {
    this.AccountID = (properties === undefined || properties.AccountID === undefined) ? null : properties.AccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Deposit2Command";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const Transfer2Command = function(properties) {
    this.FromAccountID = (properties === undefined || properties.FromAccountID === undefined) ? null : properties.FromAccountID;
    this.ToAccountID = (properties === undefined || properties.ToAccountID === undefined) ? null : properties.ToAccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Transfer2Command";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const Withdraw2Command = function(properties) {
    this.AccountID = (properties === undefined || properties.AccountID === undefined) ? null : properties.AccountID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.Description = (properties === undefined || properties.Description === undefined) ? null : properties.Description;
    this.CommandType = "Withdraw2Command";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const AdoptPetCommand = function(properties) {
    this.PetID = (properties === undefined || properties.PetID === undefined) ? null : properties.PetID;
    this.BreedID = (properties === undefined || properties.BreedID === undefined) ? null : properties.BreedID;
    this.Name = (properties === undefined || properties.Name === undefined) ? null : properties.Name;
    this.CommandType = "AdoptPetCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const FeedPetCommand = function(properties) {
    this.PetID = (properties === undefined || properties.PetID === undefined) ? null : properties.PetID;
    this.Amount = (properties === undefined || properties.Amount === undefined) ? null : properties.Amount;
    this.CommandType = "FeedPetCommand";
    this.CommandWithResult = true;
    this.ResultType = FeedPetCommandResultType;
    this.ResultTypeHasMany = false;
}

const LetPetOutToPoopCommand = function(properties) {
    this.PetID = (properties === undefined || properties.PetID === undefined) ? null : properties.PetID;
    this.CommandType = "LetPetOutToPoopCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const SetWeatherCommand = function(properties) {
    this.WeatherType = (properties === undefined || properties.WeatherType === undefined) ? null : properties.WeatherType;
    this.CommandType = "SetWeatherCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

const SetWeatherCachedCommand = function(properties) {
    this.WeatherType = (properties === undefined || properties.WeatherType === undefined) ? null : properties.WeatherType;
    this.CommandType = "SetWeatherCachedCommand";
    this.CommandWithResult = false;
    this.ResultType = null;
    this.ResultTypeHasMany = false;
}

