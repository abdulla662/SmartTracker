using SmartTracker.Domain.Common;

namespace SmartTracker.Domain.Entities
{
    public class Product : BaseEntity
    {
        public string Name { get;  set; }
        public decimal Price { get;  set; }
        public Product(string name, decimal price)
        {
            Name = name;
            Price = price;
        }
    }
}
