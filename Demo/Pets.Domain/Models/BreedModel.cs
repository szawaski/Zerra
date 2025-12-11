namespace Pets.Domain.Models
{
    public class BreedModel
    {
        public required Guid ID { get; init; }
        public Guid SpeciesID { get; set; }
        public string? Name { get; set; }
        public string? SpeciesName { get; set; }
    }
}
