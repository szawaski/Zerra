using System;

namespace ZerraDemo.Domain.Pets.Models
{
    public class PetModel
    {
        public Guid ID { get; set; }
        public string Name { get; set; }
        public string Breed { get; set; }
        public string Species { get; set; }

        public DateTime? LastEaten { get; set; }
        public int? AmountEaten { get; set; }
        public DateTime? LastPooped { get; set; }
    }
}
