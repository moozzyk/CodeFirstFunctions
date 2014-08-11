namespace CodeFirstStoreFunctionsSamples
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using CodeFirstStoreFunctions;
    using System.Collections.Generic;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;

internal class MultupleResultSetsContextInitializer : DropCreateDatabaseAlways<MultipleResultSetsContext>
{
    public override void InitializeDatabase(MultipleResultSetsContext context)
    {
        base.InitializeDatabase(context);

        context.Database.ExecuteSqlCommand(
        "CREATE PROCEDURE [dbo].[CustomersOrdersAndAnswer] @Answer int OUT AS " +
        "SET @Answer = 42 " +
        "SELECT [Id], [Name] FROM [dbo].[Customers] " +
        "SELECT [Id], [Customer_Id], [Description] FROM [dbo].[Orders] " +
        "SELECT -42 AS [Answer]");
    }

    protected override void Seed(MultipleResultSetsContext ctx)
    {
        ctx.Customers.Add(new Customer
        {
            Name = "ALFKI",
            Orders = new List<Order>
                {
                    new Order {Description = "Pens"},
                    new Order {Description = "Folders"}
                }
        });

        ctx.Customers.Add(new Customer
        {
            Name = "WOLZA",
            Orders = new List<Order> { new Order { Description = "Tofu" } }
        });
    }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public virtual ICollection<Order> Orders { get; set; }
}

public class Order
{
    public int Id { get; set; }
    public string Description { get; set; }
    public virtual Customer Customer { get; set; }
}

public class MultipleResultSetsContext : DbContext
{
    static MultipleResultSetsContext()
    {
        Database.SetInitializer(new MultupleResultSetsContextInitializer());
    }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Conventions.Add(new FunctionsConvention<MultipleResultSetsContext>("dbo"));
    }

    public DbSet<Customer> Customers { get; set; }
    public DbSet<Order> Orders { get; set; }

    [DbFunction("MultipleResultSetsContext", "CustomersOrdersAndAnswer")]
    [DbFunctionDetails(ResultTypes = new[] { typeof(Customer), typeof(Order), typeof(int) })]
    public virtual ObjectResult<Customer> MultipleResultSets([ParameterType(typeof(int))] ObjectParameter answer)
    {
        return ((IObjectContextAdapter)this).ObjectContext
            .ExecuteFunction<Customer>("CustomersOrdersAndAnswer", answer);
    }
}

class MultipleResultSetsSample
{
    public void Run()
    {
        using (var ctx = new MultipleResultSetsContext())
        {
            var answerParam = new ObjectParameter("Answer", typeof (int));

            var result1 = ctx.MultipleResultSets(answerParam);

            Console.WriteLine("Customers:");
            foreach (var c in result1)
            {
                Console.WriteLine("Id: {0}, Name: {1}", c.Id, c.Name);
            }

            var result2 = result1.GetNextResult<Order>();

            Console.WriteLine("Orders:");
            foreach (var e in result2)
            {
                Console.WriteLine("Id: {0}, Description: {1}, Customer Name {2}", e.Id, e.Description, e.Customer.Name);
            }

            var result3 = result2.GetNextResult<int>();
            Console.WriteLine("Wrong Answer: {0}", result3.Single());

            Console.WriteLine("Correct answer from output parameter: {0}", answerParam.Value);
        }
    }
}
}
