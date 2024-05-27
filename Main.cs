using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Product
{
    public int ProductId { get; set; } // The primary key.
    [StringLength(40)]
    public string ProductName { get; set; } = null!;
    [Column(TypeName = "money")]
    public decimal? UnitPrice { get; set; }
    public short? UnitsInStock { get; set; }
    public bool Discontinued { get; set; }
    // These two properties define the foreign key relationship
    // to the Categories table.
    public int CategoryId { get; set; }
    public virtual Category Category { get; set; } = null!;
}

public class Category
{
    // These properties map to columns in the database.
    public int CategoryId { get; set; } // The primary key.
    [StringLength(15)]
    public string CategoryName { get; set; } = null!;
    [Column(TypeName = "ntext")]
    public string? Description { get; set; }
    // Defines a navigation property for related rows.
    public virtual ICollection<Product> Products { get; set; }
    // To enable developers to add products to a Category, we must
    // initialize the navigation property to an empty collection.
    // This also avoids an exception if we get a member like Count.
    = new HashSet<Product>();
}

public interface IProduct
{
    string? Description { get; set; }
}