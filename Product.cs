using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VPLAssistPlus.Models
{
    public class Product
    {
        public int Id { get; set; }               // unique
        public string Name { get; set; }          // e.g., "Mouse"
        public string Category { get; set; }      // e.g., "Electronics"
        public double Price { get; set; }         // e.g., 1500
        public int Quantity { get; set; }         // e.g., 10

        public Product()
        {
            Name = "";
            Category = "";
        }

        public Product(int id, string name, string category, double price, int quantity)
        {
            Id = id;
            Name = name ?? "";
            Category = category ?? "";
            Price = price;
            Quantity = quantity;
        }

        public double TotalValue()
        {
            return Price * Quantity;
        }

        public override string ToString()
        {
            return Id + " - " + Name + " (" + Category + ")";
        }
    }
}

