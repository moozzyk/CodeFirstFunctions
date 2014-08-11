
namespace CodeFirstStoreFunctionsSamples
{
    using System;
    using System.Data.Entity;
    using System.Linq;
    using CodeFirstStoreFunctions;

internal class ScalarFunctionContextInitializer : DropCreateDatabaseAlways<ScalarFunctionContext>
{
    public override void InitializeDatabase(ScalarFunctionContext context)
    {
        base.InitializeDatabase(context);

        context.Database.ExecuteSqlCommand(
            "CREATE FUNCTION [dbo].[DateTimeToString] (@value datetime) RETURNS nvarchar(26) AS " +
            "BEGIN RETURN CONVERT(nvarchar(26), @value, 109) END");
    }

    protected override void Seed(ScalarFunctionContext ctx)
    {
        ctx.People.AddRange(new[]
        {
            new Person {Name = "John", DateOfBirth = new DateTime(1954, 12, 15, 23, 37, 0)},
            new Person {Name = "Madison", DateOfBirth = new DateTime(1994, 7, 3, 11, 42, 0)},
            new Person {Name = "Bronek", DateOfBirth = new DateTime(1923, 1, 26, 17, 11, 0)}
        });
    }
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime DateOfBirth { get; set; }
}

internal class ScalarFunctionContext : DbContext
{
    static ScalarFunctionContext()
    {
        Database.SetInitializer(new ScalarFunctionContextInitializer());
    }

    public DbSet<Person> People { get; set; }

    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
        modelBuilder.Conventions.Add(new FunctionsConvention("dbo", typeof (Functions)));
    }
}

internal static class Functions
{
    [DbFunction("CodeFirstDatabaseSchema", "DateTimeToString")]
    public static string DateTimeToString(DateTime date)
    {
        throw new NotSupportedException();
    }
}

internal class ScalarFunctionSample
{
    public void Run()
    {
        using (var ctx = new ScalarFunctionContext())
        {
            Console.WriteLine("Query:");

            var bornAfterNoon =
               ctx.People.Where(
                 p => Functions.DateTimeToString(p.DateOfBirth).EndsWith("PM"));

            Console.WriteLine(bornAfterNoon.ToString());

            Console.WriteLine("People born after noon:");

            foreach (var person in bornAfterNoon)
            {
                Console.WriteLine("Name {0}, Date of birth: {1}",
                    person.Name, person.DateOfBirth);
            }
        }
    }
}
}
