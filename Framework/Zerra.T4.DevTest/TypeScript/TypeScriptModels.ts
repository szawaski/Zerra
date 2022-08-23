import { Bus, ICommand } from "./Bus";

export class Balance1Model {
    AccountID!: string;
    Balance!: number;
    LastTransactionDate!: Date | null;
    LastTransactionAmount!: number | null;
}

const Balance1ModelType =
{
    AccountID: "string",
    Balance: "number",
    LastTransactionDate: "Date",
    LastTransactionAmount: "number",
}

export class Transaction1Model {
    AccountID!: string;
    Date!: Date;
    Amount!: number;
    Description!: string | null;
    Balance!: number;
    Event!: string | null;
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

export class Balance2Model {
    AccountID!: string;
    Balance!: number;
    LastTransactionDate!: Date | null;
    LastTransactionAmount!: number | null;
}

const Balance2ModelType =
{
    AccountID: "string",
    Balance: "number",
    LastTransactionDate: "Date",
    LastTransactionAmount: "number",
}

export class Transaction2Model {
    AccountID!: string;
    Date!: Date;
    Amount!: number;
    Description!: string | null;
    Balance!: number;
    Event!: string | null;
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

export class SpeciesModel {
    ID!: string;
    Name!: string | null;
}

const SpeciesModelType =
{
    ID: "string",
    Name: "string",
}

export class BreedModel {
    ID!: string;
    Name!: string | null;
}

const BreedModelType =
{
    ID: "string",
    Name: "string",
}

export class PetModel {
    ID!: string;
    Name!: string | null;
    Breed!: string | null;
    Species!: string | null;
    LastEaten!: Date | null;
    AmountEaten!: number | null;
    LastPooped!: Date | null;
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

export class WeatherModel {
    Date!: Date;
    WeatherType!: string;
}

const WeatherModelType =
{
    Date: "Date",
    WeatherType: "string",
}

export const ModelTypeDictionary: any[string] =
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

export class ILedger1QueryProvider {
    public static HasBalance(accountID: string): Promise<boolean> {
        return Bus.Call("ILedger1QueryProvider", "HasBalance", [accountID], null, false);
    }
    public static GetBalance(accountID: string): Promise<Balance1Model | null> {
        return Bus.Call("ILedger1QueryProvider", "GetBalance", [accountID], Balance1ModelType, false);
    }
    public static GetTransactions(accountID: string): Promise<Transaction1Model[] | null> {
        return Bus.Call("ILedger1QueryProvider", "GetTransactions", [accountID], Transaction1ModelType, true);
    }
}

export class ILedger2QueryProvider {
    public static HasBalance(accountID: string): Promise<boolean> {
        return Bus.Call("ILedger2QueryProvider", "HasBalance", [accountID], null, false);
    }
    public static GetBalance(accountID: string): Promise<Balance2Model | null> {
        return Bus.Call("ILedger2QueryProvider", "GetBalance", [accountID], Balance2ModelType, false);
    }
    public static GetTransactions(accountID: string): Promise<Transaction2Model[] | null> {
        return Bus.Call("ILedger2QueryProvider", "GetTransactions", [accountID], Transaction2ModelType, true);
    }
}

export class IPetsQueryProvider {
    public static GetSpecies(): Promise<SpeciesModel[] | null> {
        return Bus.Call("IPetsQueryProvider", "GetSpecies", [], SpeciesModelType, true);
    }
    public static GetBreeds(speciesID: string): Promise<BreedModel[] | null> {
        return Bus.Call("IPetsQueryProvider", "GetBreeds", [speciesID], BreedModelType, true);
    }
    public static GetPets(): Promise<PetModel[] | null> {
        return Bus.Call("IPetsQueryProvider", "GetPets", [], PetModelType, true);
    }
    public static GetPet(id: string): Promise<PetModel | null> {
        return Bus.Call("IPetsQueryProvider", "GetPet", [id], PetModelType, false);
    }
    public static IsHungry(id: string): Promise<boolean> {
        return Bus.Call("IPetsQueryProvider", "IsHungry", [id], null, false);
    }
    public static NeedsToPoop(id: string): Promise<boolean> {
        return Bus.Call("IPetsQueryProvider", "NeedsToPoop", [id], null, false);
    }
}

export class IWeatherQueryProvider {
    public static GetWeather(): Promise<WeatherModel | null> {
        return Bus.Call("IWeatherQueryProvider", "GetWeather", [], WeatherModelType, false);
    }
    public static TestStreams(): Promise<any> {
        return Bus.Call("IWeatherQueryProvider", "TestStreams", [], null, false);
    }
}

export class Deposit1Command implements ICommand {
    constructor(properties: Deposit1Command) {
        const self: any = this;
        const props: any = properties;
        Object.keys(props).forEach(key => self[key] = props[key]);
        self["CommandType"] = "Deposit1Command";
    }
    AccountID!: string;
    Amount!: number;
    Description!: string | null;
}

export class Transfer1Command implements ICommand {
    constructor(properties: Transfer1Command) {
        const self: any = this;
        const props: any = properties;
        Object.keys(props).forEach(key => self[key] = props[key]);
        self["CommandType"] = "Transfer1Command";
    }
    FromAccountID!: string;
    ToAccountID!: string;
    Amount!: number;
    Description!: string | null;
}

export class Withdraw1Command implements ICommand {
    constructor(properties: Withdraw1Command) {
        const self: any = this;
        const props: any = properties;
        Object.keys(props).forEach(key => self[key] = props[key]);
        self["CommandType"] = "Withdraw1Command";
    }
    AccountID!: string;
    Amount!: number;
    Description!: string | null;
}

export class Deposit2Command implements ICommand {
    constructor(properties: Deposit2Command) {
        const self: any = this;
        const props: any = properties;
        Object.keys(props).forEach(key => self[key] = props[key]);
        self["CommandType"] = "Deposit2Command";
    }
    AccountID!: string;
    Amount!: number;
    Description!: string | null;
}

export class Transfer2Command implements ICommand {
    constructor(properties: Transfer2Command) {
        const self: any = this;
        const props: any = properties;
        Object.keys(props).forEach(key => self[key] = props[key]);
        self["CommandType"] = "Transfer2Command";
    }
    FromAccountID!: string;
    ToAccountID!: string;
    Amount!: number;
    Description!: string | null;
}

export class Withdraw2Command implements ICommand {
    constructor(properties: Withdraw2Command) {
        const self: any = this;
        const props: any = properties;
        Object.keys(props).forEach(key => self[key] = props[key]);
        self["CommandType"] = "Withdraw2Command";
    }
    AccountID!: string;
    Amount!: number;
    Description!: string | null;
}

export class AdoptPetCommand implements ICommand {
    constructor(properties: AdoptPetCommand) {
        const self: any = this;
        const props: any = properties;
        Object.keys(props).forEach(key => self[key] = props[key]);
        self["CommandType"] = "AdoptPetCommand";
    }
    PetID!: string;
    BreedID!: string;
    Name!: string | null;
}

export class FeedPetCommand implements ICommand {
    constructor(properties: FeedPetCommand) {
        const self: any = this;
        const props: any = properties;
        Object.keys(props).forEach(key => self[key] = props[key]);
        self["CommandType"] = "FeedPetCommand";
    }
    PetID!: string;
    Amount!: number;
}

export class LetPetOutToPoopCommand implements ICommand {
    constructor(properties: LetPetOutToPoopCommand) {
        const self: any = this;
        const props: any = properties;
        Object.keys(props).forEach(key => self[key] = props[key]);
        self["CommandType"] = "LetPetOutToPoopCommand";
    }
    PetID!: string;
}

export class SetWeatherCommand implements ICommand {
    constructor(properties: SetWeatherCommand) {
        const self: any = this;
        const props: any = properties;
        Object.keys(props).forEach(key => self[key] = props[key]);
        self["CommandType"] = "SetWeatherCommand";
    }
    WeatherType!: string;
}

