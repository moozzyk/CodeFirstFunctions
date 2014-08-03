// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

using System;

namespace CodeFirstStoreFunctions
{
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Xunit;

    public enum AirportType { International }

    public class Airport
    {
        [Key]
        public string IATACode { get; set; }
        public string CityCode { get; set; }
        public string CountryCode { get; set; }
        public string Name { get; set; }
        public byte TerminalCount { get; set; }
        public AirportType Type { get; set; }
    }

    public class Airport_ResultType
    {
        public string IATACode { get; set; }
        public string CityCode { get; set; }
        public string CountryCode { get; set; }
        public string Name { get; set; }
        public byte TerminalCount { get; set; }
    }

    public class MyContext : DbContext
    {
        static MyContext()
        {
            Database.SetInitializer(new MyContextInitializer());
        }

        public DbSet<Airport> Airports { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Airport>().Property(a => a.IATACode).IsFixedLength().IsUnicode().HasMaxLength(3);
            modelBuilder.Entity<Airport>().Property(a => a.CityCode).IsFixedLength().IsUnicode().HasMaxLength(3);
            modelBuilder.Entity<Airport>().Property(a => a.CountryCode).IsFixedLength().IsUnicode().HasMaxLength(2);

            modelBuilder.ComplexType<Airport_ResultType>();

            modelBuilder.Conventions.Add(new FunctionsConvention<MyContext>("dbo"));
        }

        [DbFunctionDetails(ResultColumnName = "TerminalCount")]
        [DbFunction("CodeFirstStoreFunctions", "GetUniqueTerminalCount")]
        public virtual IQueryable<byte> GetUniqueTerminalCount()
        {
            return ((IObjectContextAdapter)this).ObjectContext
                .CreateQuery<byte>(
                    string.Format("[{0}].{1}", GetType().Name, "[GetUniqueTerminalCount]()"));
        }

        [DbFunctionDetails(ResultColumnName = "Type")]
        [DbFunction("CodeFirstStoreFunctions", "GetAirportType")]
        public virtual IQueryable<AirportType> GetAirportType(AirportType airportType)
        {
            var airportTypeParameter = new ObjectParameter("AirportType", airportType);

            return ((IObjectContextAdapter)this).ObjectContext
                .CreateQuery<AirportType>(
                    string.Format("[{0}].{1}", GetType().Name, "[GetAirportType](@AirportType)"),
                    airportTypeParameter);
        }

        [DbFunctionDetails(ResultColumnName = "Type")]
        [DbFunction("CodeFirstStoreFunctions", "GetPassedAirportType")]
        public virtual IQueryable<int?> GetPassedAirportType(int? airportType)
        {
            var airportTypeParameter =
                airportType.HasValue
                    ? new ObjectParameter("AirportType", airportType)
                    : new ObjectParameter("AirportType", typeof (int?)); 

            return ((IObjectContextAdapter)this).ObjectContext
                .CreateQuery<int?>(
                    string.Format("[{0}].{1}", GetType().Name, "[GetPassedAirportType](@AirportType)"),
                    airportTypeParameter);
        }

        [DbFunctionDetails(ResultColumnName = "Type")]
        [DbFunction("CodeFirstStoreFunctions", "GetPassedAirportTypeEnum")]
        public virtual IQueryable<AirportType?> GetPassedAirportTypeEnum(AirportType? airportType)
        {
            var airportTypeParameter =
                airportType.HasValue
                    ? new ObjectParameter("AirportType", airportType)
                    : new ObjectParameter("AirportType", typeof(AirportType?)); 

            return ((IObjectContextAdapter)this).ObjectContext
                .CreateQuery<AirportType?>(
                    string.Format("[{0}].{1}", GetType().Name, "[GetPassedAirportTypeEnum](@AirportType)"),
                    airportTypeParameter);
        }

        [DbFunction("CodeFirstStoreFunctions", "GetAirports_ComplexType")]
        public virtual IQueryable<Airport_ResultType> GetAirports_ComplexType(string countryCode)
        {
            var countryCodeParameter = countryCode != null ?
                new ObjectParameter("CountryCode", countryCode) :
                new ObjectParameter("CountryCode", typeof(string));

            return ((IObjectContextAdapter)this).ObjectContext
                .CreateQuery<Airport_ResultType>(
                    string.Format("[{0}].{1}", GetType().Name, "[GetAirports_ComplexType](@CountryCode)"),
                    countryCodeParameter);
        }

        [DbFunction("MyContext", "GetAirports")]
        public virtual IQueryable<Airport> GetAirports(string countryCode)
        {
            var countryCodeParameter = countryCode != null ?
                new ObjectParameter("CountryCode", countryCode) :
                new ObjectParameter("CountryCode", typeof(string));

            return ((IObjectContextAdapter)this).ObjectContext
                .CreateQuery<Airport>(
                    string.Format("[{0}].{1}", GetType().Name, "[GetAirports](@CountryCode)"),
                    countryCodeParameter);
        }

        public virtual ObjectResult<byte> GetUniqueTerminalCountSP()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<byte>("GetUniqueTerminalCountSP");
        }

        public virtual ObjectResult<Airport_ResultType> GetAirports_ComplexTypeSP(string countryCode)
        {
            var countryCodeParameter = countryCode != null ?
                new ObjectParameter("CountryCode", countryCode) :
                new ObjectParameter("CountryCode", typeof(string));

            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Airport_ResultType>("GetAirports_ComplexTypeSP", countryCodeParameter);
        }

        public ObjectResult<Airport> GetAirportsSP(string countryCode)
        {
            var countryCodeParameter = countryCode != null ?
                new ObjectParameter("CountryCode", countryCode) :
                new ObjectParameter("CountryCode", typeof(string));

            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Airport>("GetAirportsSP", countryCodeParameter);            
        }

        public virtual ObjectResult<AirportType> GetAirportTypeSP(AirportType airportType)
        {
            var airportTypeParameter = new ObjectParameter("AirportType", airportType);

            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<AirportType>("GetAirportTypeSP", airportTypeParameter);
        }
    }

    #region initializer

    //public class MyContextInitializer : DropCreateDatabaseIfModelChanges<MyContext>
    public class MyContextInitializer : DropCreateDatabaseAlways<MyContext>
    {
        protected override void Seed(MyContext context)
        {
            context.Airports.Add(
                new Airport
                {
                    IATACode = "WRO", 
                    CityCode = "WRO",
                    CountryCode = "PL",
                    Name = "Wroclaw Copernicus Airport",
                    TerminalCount = 2,
                    Type = AirportType.International
                });

            context.Airports.Add(
                new Airport
                {
                    IATACode = "LHR",
                    CityCode = "LON",
                    CountryCode = "GB",
                    Name = "London Heathrow Airport",
                    TerminalCount = 5,
                    Type = AirportType.International
                });

            context.Airports.Add(
                new Airport 
                {
                    IATACode = "LTN",
                    CityCode = "LON",
                    CountryCode = "GB",
                    Name = "London Luton Airport",
                    TerminalCount = 1,
                    Type = AirportType.International
                });

            context.Airports.Add(
                new Airport
                {
                    IATACode = "SEA",
                    CityCode = "SEA",
                    CountryCode = "US",
                    Name = "Seattle-Tacoma International Airport",
                    TerminalCount = 1,
                    Type = AirportType.International
                });

            context.SaveChanges();

            context.Database.ExecuteSqlCommand(
                "CREATE FUNCTION [dbo].[GetAirports] " +
                " (@CountryCode nchar(3)) " +
                "RETURNS TABLE " +
                "RETURN " +
                "SELECT [IATACode], " +
                "   [CityCode], " +
                "   [CountryCode], " +
                "   [Name], " +
                "   [TerminalCount], " +
                "   [Type] " +
                "FROM [dbo].[Airports] " +
                "WHERE [CountryCode] = @CountryCode");

            context.Database.ExecuteSqlCommand(
                "CREATE FUNCTION [dbo].[GetAirports_ComplexType] " +
                " (@CountryCode nchar(3)) " +
                "RETURNS TABLE " +
                "RETURN " +
                "SELECT [IATACode], " +
                "   [CityCode], " +
                "   [CountryCode], " +
                "   [Name], " +
                "   [TerminalCount] " +
                "FROM [dbo].[Airports] " +
                "WHERE [CountryCode] = @CountryCode");

            context.Database.ExecuteSqlCommand(
                "CREATE FUNCTION [dbo].[GetUniqueTerminalCount]()" +
                "RETURNS TABLE " +
                "RETURN " +
                "SELECT DISTINCT [TerminalCount] " +
                "FROM [dbo].[Airports]");

            context.Database.ExecuteSqlCommand(
                "CREATE FUNCTION [dbo].[GetAirportType](@AirportType int) " +
                "RETURNS TABLE " +
                "RETURN " +
                "SELECT [Type] " +
                "FROM [dbo].[Airports] " +
                "WHERE [Type] = @AirportType");

            context.Database.ExecuteSqlCommand(
                "CREATE FUNCTION [dbo].[GetPassedAirportType](@AirportType int) " +
                "RETURNS TABLE " +
                "RETURN " +
                "SELECT @AirportType AS [Type]");

            context.Database.ExecuteSqlCommand(
                "CREATE FUNCTION [dbo].[GetPassedAirportTypeEnum](@AirportType int) " +
                "RETURNS TABLE " +
                "RETURN " +
                "SELECT @AirportType AS [Type] ");

            context.Database.ExecuteSqlCommand(
                "CREATE PROCEDURE [dbo].[GetAirports_ComplexTypeSP] @CountryCode nchar(3) AS " +
                "SELECT [IATACode], " +
                "   [CityCode], " +
                "   [CountryCode], " +
                "   [Name], " +
                "   [TerminalCount] " +
                "FROM [dbo].[Airports] " +
                "WHERE [CountryCode] = @CountryCode");

            context.Database.ExecuteSqlCommand(
                "CREATE PROCEDURE [dbo].[GetAirportsSP] @CountryCode nchar(2) AS " +
                "SELECT [IATACode], " +
                "   [CityCode], " +
                "   [CountryCode], " +
                "   [Name], " +
                "   [TerminalCount], " +
                "   [Type] " +
                "FROM [dbo].[Airports] " +
                "WHERE [CountryCode] = @CountryCode");

            context.Database.ExecuteSqlCommand(
                "CREATE PROCEDURE [dbo].[GetUniqueTerminalCountSP] AS " +
                "SELECT DISTINCT [TerminalCount] " +
                "FROM [dbo].[Airports]");

            context.Database.ExecuteSqlCommand(
                "CREATE PROCEDURE [dbo].[GetAirportTypeSP] @AirportType int AS " +
                "SELECT [Type] " +
                "FROM [dbo].[Airports] " +
                "WHERE [Type] = @AirportType");
        }
    }
    
    #endregion

    public class E2ETests
    {
        [Fact]
        public void Can_invoke_primitive_TVFs_in_a_query()
        {
            using (var ctx = new MyContext())
            {
                var query = from x in ctx.GetUniqueTerminalCount()
                        where x > 1
                        select x;

                const string expectedSql = @"SELECT 
    [Extent1].[TerminalCount] AS [TerminalCount]
    FROM [dbo].[GetUniqueTerminalCount]() AS [Extent1]
    WHERE  CAST( [Extent1].[TerminalCount] AS int) > 1";
                
                Assert.Equal(expectedSql, ((ObjectQuery)query).ToTraceString());

                Assert.Equal(new byte[] {2,5},  query.ToArray());
            }
        }

        [Fact]
        public void Can_invoke_TVF_returning_complex_type_in_a_query()
        {
            using (var ctx = new MyContext())
            {
                var query = ctx.GetAirports_ComplexType("PL").Where(a => a.TerminalCount > 0);

                var sql = ((ObjectQuery)query).ToTraceString();

                const string expectedSql = @"SELECT 
    1 AS [C1], 
    [Extent1].[IATACode] AS [IATACode], 
    [Extent1].[CityCode] AS [CityCode], 
    [Extent1].[CountryCode] AS [CountryCode], 
    [Extent1].[Name] AS [Name], 
    [Extent1].[TerminalCount] AS [TerminalCount]
    FROM [dbo].[GetAirports_ComplexType](@CountryCode) AS [Extent1]
    WHERE  CAST( [Extent1].[TerminalCount] AS int) > 0";

                Assert.Equal(expectedSql, sql);

                var result = query.ToList();

                Assert.Equal(1, result.Count());
                Assert.Equal("WRO", result[0].IATACode);
                Assert.Equal("WRO", result[0].CityCode);
                Assert.Equal("PL", result[0].CountryCode);
                Assert.Equal(2, result[0].TerminalCount);
                Assert.Equal("Wroclaw Copernicus Airport", result[0].Name);
            }
        }

        [Fact]
        public void Can_invoke_TVF_mapped_to_EntitySet_in_a_query()
        {
            using (var ctx = new MyContext())
            {
                var query = ctx.GetAirports("GB").Where(a => a.TerminalCount > 0);

                var sql = ((ObjectQuery)query).ToTraceString();

                const string expectedSql = @"SELECT 
    [Extent1].[IATACode] AS [IATACode], 
    [Extent1].[CityCode] AS [CityCode], 
    [Extent1].[CountryCode] AS [CountryCode], 
    [Extent1].[Name] AS [Name], 
    [Extent1].[TerminalCount] AS [TerminalCount], 
    [Extent1].[Type] AS [Type]
    FROM [dbo].[GetAirports](@CountryCode) AS [Extent1]
    WHERE  CAST( [Extent1].[TerminalCount] AS int) > 0";

                Assert.Equal(expectedSql, sql);

                var result = query.ToList();

                Assert.Equal(2, result.Count());
                Assert.Equal("LHR", result[0].IATACode);
                Assert.Equal("LTN", result[1].IATACode);
            }
        }

        [Fact]
        public void Can_invoke_TVF_mapped_to_enum_type_in_a_query()
        {
            using (var ctx = new MyContext())
            {
                var query = ctx.GetAirportType(AirportType.International).Where(a => a != (AirportType)(-1));

                var sql = ((ObjectQuery)query).ToTraceString();

                const string expectedSql = @"SELECT 
    [Extent1].[Type] AS [Type]
    FROM [dbo].[GetAirportType](@AirportType) AS [Extent1]
    WHERE  NOT ((-1 =  CAST( [Extent1].[Type] AS int)) AND ([Extent1].[Type] IS NOT NULL))";

                Assert.Equal(expectedSql, sql);

                var result = query.ToList();

                Assert.Equal(4, result.Count());
                Assert.True(result.All(r => r == AirportType.International));
            }
        }

        [Fact]
        public void Can_invoke_TVF_with_nullable_parameter_returning_null_primitive_values()
        {
            using (var ctx = new MyContext())
            {
                var airports = ctx.GetPassedAirportType(null).Where(a => a == null).ToList();

                Assert.Equal(new int?[] {null}, airports);
            }
        }

        [Fact]
        public void Can_invoke_TVF_with_nullable_parameter_enum_returning_null_primitive_values()
        {
            using (var ctx = new MyContext())
            {
                var airports = ctx.GetPassedAirportTypeEnum(null).Where(a => a == null).ToList();

                Assert.Equal(new AirportType?[] { null }, airports);
            }
        }

        [Fact]
        public void Can_invoke_stored_proc_mapped_to_primitive_types()
        {
            using (var ctx = new MyContext())
            {
                var result = ctx.GetUniqueTerminalCountSP().ToList();
                Assert.Equal(new byte[] { 1, 2, 5 }, result.ToArray());
            }
        }

        [Fact]
        public void Can_invoke_stored_proc_mapped_to_ComplexTypes()
        {
            using (var ctx = new MyContext())
            {
                var result = ctx.GetAirports_ComplexTypeSP("PL").ToList();

                Assert.Equal(1, result.Count);
                Assert.Equal("WRO", result[0].IATACode);
            }
        }

        [Fact]
        public void Can_invoke_stored_proc_mapped_to_EntitySet()
        {
            using (var ctx = new MyContext())
            {
                var result = ctx.GetAirportsSP("PL").ToList();

                Assert.Equal(1, result.Count);
                Assert.Equal("WRO", result[0].IATACode);
            }
        }

        [Fact]
        public void Can_invoke_stored_proc_mapped_to_enums_types()
        {
            using (var ctx = new MyContext())
            {
                var result = ctx.GetAirportTypeSP(AirportType.International).ToList();

                Assert.Equal(4, result.Count());
                Assert.True(result.All(r => r == AirportType.International));
            }
        }
    }
}
