//Zerra Generated File
#if NET5_0_OR_GREATER

using System.Runtime.CompilerServices;
using Zerra.Reflection;

namespace Zerra.SourceGeneration
{
    public static class TypeDetailInitializer
    {
#pragma warning disable CA2255
        [ModuleInitializer]
#pragma warning restore CA2255
        public static void Initialize()
        {
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger1.Command.SourceGeneration.Deposit1CommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger1.Command.SourceGeneration.Transfer1CommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger1.Command.SourceGeneration.Withdraw1CommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger1.Models.SourceGeneration.Balance1ModelTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger1.Models.SourceGeneration.Transaction1ModelTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger2.Command.SourceGeneration.Deposit2CommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger2.Command.SourceGeneration.Transfer2CommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger2.Command.SourceGeneration.Withdraw2CommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger2.Events.SourceGeneration.Deposit2EventTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger2.Events.SourceGeneration.Transfer2EventTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger2.Events.SourceGeneration.Withdraw2EventTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger2.Models.SourceGeneration.Balance2ModelTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Ledger2.Models.SourceGeneration.Transaction2ModelTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Pets.Commands.SourceGeneration.AdoptPetCommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Pets.Commands.SourceGeneration.FeedPetCommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Pets.Commands.SourceGeneration.FeedPetCommandResultTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Pets.Commands.SourceGeneration.LetPetOutToPoopCommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Pets.Exceptions.SourceGeneration.PoopExceptionTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Pets.Models.SourceGeneration.BreedModelTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Pets.Models.SourceGeneration.PetModelTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Pets.Models.SourceGeneration.SpeciesModelTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.WeatherCached.Commands.SourceGeneration.SetWeatherCachedCommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.WeatherCached.Events.SourceGeneration.WeatherChangedEventTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.WeatherCached.Models.SourceGeneration.WeatherCachedModelTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Weather.Commands.SourceGeneration.SetWeatherCommandTypeDetail());
            TypeAnalyzer.InitializeTypeDetail(new ZerraDemo.Domain.Weather.Models.SourceGeneration.WeatherModelTypeDetail());
        }
    }
}

#endif
