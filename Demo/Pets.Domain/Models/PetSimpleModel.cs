namespace Pets.Domain.Models
{
    public sealed class PetSimpleModel
    {
        public required int ID { get; set; }
        public required string? Name { get; set; }
        public required string? Type { get; set; }
    }
}
