namespace Pets.Domain.Models
{
    public class BreedModel
    {
        public Guid ID { get; set; }
        public Guid SpeciesID { get; set; }
        public string? Name { get; set; }
        public string? SpeciesName { get; set; }
    }
}
