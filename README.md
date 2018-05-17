# Store Functions for EntityFramework CodeFirst

Currently Entity Framework Code First approach does not natively support store functions (Table Valued Functions (TVFs) and Stored procedures). However opening the mapping API (and a few other minor changes) made it possible to use TVFs and stored procedures with Code First. This project uses the basic building blocks to build end to end experience allowing using TVFs in Linq queries and invoking stroed procedures without having to drop down to SQL. 

#### This project was moved from https://codefirstfunctions.codeplex.com

You may still find some useful information there:

 - Old discussion board - https://codefirstfunctions.codeplex.com/discussions
 - Issues - https://codefirstfunctions.codeplex.com/workitem/list/basic

# How to get it

You can get it from NuGet - just install the [EntityFramework.CodeFirstStoreFunctions NuGet package](http://www.nuget.org/packages/EntityFramework.CodeFirstStoreFunctions)

# How to use it

 - [See what's new in Beta](http://blog.3d-logic.com/2014/08/11/the-beta-version-of-store-functions-for-entityframework-6-1-1-code-first-available)
 - [The 1.0.0 version released](https://blog.3d-logic.com/2014/10/18/the-final-version-of-the-store-functions-for-entityframework-6-1-1-code-first-convention-released)

The project uses a custom convention to discover TVF and sored proc stub functions which are then mapped to corresponding store functions. This blog post describes in more details how to use the convnention [Support for Store Functions (TVFs and Stored Procs) in Entity Framework 6.1](http://blog.3d-logic.com/2014/04/09/support-for-store-functions-tvfs-and-stored-procs-in-entity-framework-6-1/). Below you can find an example that uses the convention to map a method to a TVF and to a stored proc. Note that the code below only shows store functions that return entities but it is also possible to use coplex or scalar types. These scenarios are covered by [End-to-end tests](https://github.com/moozzyk/CodeFirstFunctions/blob/master/CodeFirstStoreFunctionsTests/E2ETests.cs).

```C#
public class Customer
{
    public int Id { get; set; }
 
    public string Name { get; set; }
 
    public string ZipCode { get; set; }
}
 
public class MyContext : DbContext
{
    static MyContext()
    {
        Database.SetInitializer(new MyContextInitializer());
    }
 
    public DbSet<Customer> Customers { get; set; }
 
    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Conventions.Add(new FunctionsConvention<MyContext>("dbo"));
    }
 
    [DbFunction("MyContext", "CustomersByZipCode")]
    public IQueryable<Customer> CustomersByZipCode(string zipCode)
    {
        var zipCodeParameter = zipCode != null ?
            new ObjectParameter("ZipCode", zipCode) :
            new ObjectParameter("ZipCode", typeof(string));
 
        return ((IObjectContextAdapter)this).ObjectContext
            .CreateQuery<Customer>(
                string.Format("[{0}].{1}", GetType().Name, 
                    "[CustomersByZipCode](@ZipCode)"), zipCodeParameter);
    }
 
    public ObjectResult<Customer> GetCustomersByName(string name)
    {
        var nameParameter = name != null ?
            new ObjectParameter("Name", name) :
            new ObjectParameter("Name", typeof(string));
 
        return ((IObjectContextAdapter)this).ObjectContext.
            ExecuteFunction("GetCustomersByName", nameParameter);
    }
}
 
public class MyContextInitializer : DropCreateDatabaseAlways<MyContext>
{
    public override void InitializeDatabase(MyContext context)
    {
        base.InitializeDatabase(context);
 
        context.Database.ExecuteSqlCommand(
            "CREATE PROCEDURE [dbo].[GetCustomersByName] @Name nvarchar(max) AS " +
            "SELECT [Id], [Name], [ZipCode] " +
            "FROM [dbo].[Customers] " +
            "WHERE [Name] LIKE (@Name)");
 
        context.Database.ExecuteSqlCommand(
            "CREATE FUNCTION [dbo].[CustomersByZipCode](@ZipCode nchar(5)) " +
            "RETURNS TABLE " +
            "RETURN " +
            "SELECT [Id], [Name], [ZipCode] " +
            "FROM [dbo].[Customers] " + 
            "WHERE [ZipCode] = @ZipCode");
    }
 
    protected override void Seed(MyContext context)
    {
        context.Customers.Add(new Customer {Name = "John", ZipCode = "98052"});
        context.Customers.Add(new Customer { Name = "Natasha", ZipCode = "98210" });
        context.Customers.Add(new Customer { Name = "Lin", ZipCode = "98052" });
        context.Customers.Add(new Customer { Name = "Josh", ZipCode = "90210" });
        context.Customers.Add(new Customer { Name = "Maria", ZipCode = "98074" });
        context.SaveChanges();
    }
}
 
class Program
{
    static void Main()
    {
        using (var ctx = new MyContext())
        {
            const string zipCode = "98052";
            var q = ctx.CustomersByZipCode(zipCode)
                .Where(c => c.Name.Length > 3);
            //Console.WriteLine(((ObjectQuery)q).ToTraceString());
            Console.WriteLine("TVF: CustomersByZipCode('{0}')", zipCode);
            foreach (var customer in q)
            {
                Console.WriteLine("Id: {0}, Name: {1}, ZipCode: {2}", 
                    customer.Id, customer.Name, customer.ZipCode);
            }
 
            const string name = "Jo%";
            Console.WriteLine("\nStored procedure: GetCustomersByName '{0}'", name);
            foreach (var customer in ctx.GetCustomersByName(name))
            {
                Console.WriteLine("Id: {0}, Name: {1}, ZipCode: {2}", 
                    customer.Id, customer.Name, customer.ZipCode);   
            }
        }
    }
}
```
