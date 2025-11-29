namespace Pets.Domain.Models
{
    public sealed class PetModel
    {
        public Guid ID { get; set; }
        public Guid? BreedID { get; set; }

        public string? Name { get; set; }
        public string? Breed { get; set; }
        public string? Species;

        public DateTime? LastEaten { get; set; }
        public int? AmountEaten { get; set; }
        public DateTime? LastPooped { get; set; }
    }
}
