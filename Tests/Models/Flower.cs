namespace MongoDB.Entities.Tests.Models
{
    public class Flower : Entity
    {
        public string Name { get; set; }
        public string Color { get; set; }
        public Many<CustomerWithCustomId> Customers { get; set; }

        public Flower()
        {
            this.InitOneToMany(x => Customers);
        }
    }
}
