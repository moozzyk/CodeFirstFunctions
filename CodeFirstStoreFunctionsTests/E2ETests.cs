// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Data.Entity;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Linq;
    using Xunit;

    public enum AirportType { Regional, International }

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
        public AirportType Type { get; set; }
    }

    public class Vehicle
    {
        public int Id { get; set; }

        public DateTime ProductionDate { get; set; }
    }

    public class Aircraft : Vehicle
    {
        public string Code { get; set; }
    }

    public class MyContext : DbContext
    {
        static MyContext()
        {
            Database.SetInitializer(new MyContextInitializer());
        }

        public DbSet<Airport> Airports { get; set; }

        public DbSet<Vehicle> Vehicles { get; set; }

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

        [DbFunction("MyContext", "GetAircraft")]
        public virtual IQueryable<Aircraft> GetAircraft()
        {
            return ((IObjectContextAdapter)this).ObjectContext
                .CreateQuery<Aircraft>(
                    string.Format("[{0}].{1}", GetType().Name, "[GetAircraft]()"));
        }

        [DbFunction("MyContext", "MyCustomTVF")]
        [DbFunctionDetails(ResultColumnName = "Number")]
        public virtual IQueryable<int> FunctionWhoseNameIsDifferentThenTVFName()
        {
            return ((IObjectContextAdapter)this).ObjectContext
                .CreateQuery<int>(
                    string.Format("[{0}].{1}", GetType().Name, "[MyCustomTVF]()"));
        }

        [DbFunction("MyContext", "GetUniqueTerminalCountSP")]
        public virtual ObjectResult<byte> GetUniqueTerminalCountSProc()
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

        public virtual ObjectResult<Aircraft> GetAircraftSP()
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<Aircraft>("GetAircraftSP");
        }

        [DbFunctionDetails(
            ResultTypes = new[] {typeof (Airport_ResultType), typeof (Airport), typeof (Aircraft), typeof (int)})]
        public virtual ObjectResult<Airport_ResultType> MultipleResultSets()
        {
            return ((IObjectContextAdapter) this).ObjectContext
                .ExecuteFunction<Airport_ResultType>("MultipleResultSets");
        }

        public ObjectResult<AirportType> GetAirportTypesWithOutputParameter(
            [ParameterType(typeof(AirportType?))] ObjectParameter airportType)
        {
            return ((IObjectContextAdapter)this).ObjectContext
                .ExecuteFunction<AirportType>("GetAirportTypesWithOutputParameter", airportType);
        }

        [DbFunction("CodeFirstDatabaseSchema", "EchoNumber")]
        public static int? ScalarFuncEchoNumber(int? number)
        {
            throw new NotSupportedException();
        }

        [DbFunctionDetails(ResultColumnName = "Number")]
        [DbFunction("MyContext", "GetXmlInfo")]
        public virtual ObjectResult<int> GetXmlInfo([ParameterType(typeof(string), StoreType = "xml")] ObjectParameter xml)
        {
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction<int>("GetXmlInfo", xml);
        }

        [DbFunction("CodeFirstDatabaseSchema", "SQUARE")]
        [DbFunctionDetails(IsBuiltIn = true)]
        public static float Square(float number)
        {
            throw new NotSupportedException();
        }

        [DbFunction("CodeFirstDatabaseSchema", "FORMAT")]
        [DbFunctionDetails(IsBuiltIn = true)]
        public static string Format(DateTime dateTime, string format, string culture)
        {
            throw new NotSupportedException();
        }

        [DbFunction("CodeFirstDatabaseSchema", "CURRENT_TIMESTAMP")]
        [DbFunctionDetails(IsBuiltIn = true, IsNiladic = true)]
        public static DateTime? CurrentTimestamp()
        {
            throw new NotSupportedException();
        }
    }

    #region initializer

//    public class MyContextInitializer : DropCreateDatabaseIfModelChanges<MyContext>
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

            context.Airports.Add(
            new Airport
            {
                IATACode = "OLM",
                CityCode = "OLM",
                CountryCode = "US",
                Name = "Olympia Regional Airport",
                TerminalCount = 1,
                Type = AirportType.Regional
            });

            context.Vehicles.Add(new Aircraft
            {
                Code = "AT7",
                ProductionDate = new DateTime(1929, 12, 7)
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
                "   [TerminalCount], " +
                "   [Type] " +
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
                "CREATE FUNCTION [dbo].[GetAircraft]() " +
                "RETURNS TABLE " +
                "RETURN " +
                "SELECT " +
                "    [Id], " +
                "    [Code], " +
                "    [ProductionDate]" +
                "FROM [dbo].[Vehicles] " +
                "WHERE [Discriminator] = N'Aircraft'");

            context.Database.ExecuteSqlCommand(
                "CREATE PROCEDURE [dbo].[GetAirports_ComplexTypeSP] @CountryCode nchar(3) AS " +
                "SELECT [IATACode], " +
                "   [CityCode], " +
                "   [CountryCode], " +
                "   [Name], " +
                "   [TerminalCount], " +
                "   [Type] " +
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

            context.Database.ExecuteSqlCommand(
                "CREATE PROCEDURE [dbo].[GetAircraftSP] AS " +
                "SELECT " +
                "    [Id], " +
                "    [Code], " +
                "    [ProductionDate]" +
                "FROM [dbo].[Vehicles] " +
                "WHERE [Discriminator] = N'Aircraft'");

            context.Database.ExecuteSqlCommand(
                "CREATE PROCEDURE [dbo].[MultipleResultSets] AS " +
                "SELECT [IATACode], " +
                "   [CityCode], " +
                "   [CountryCode], " +
                "   [Name], " +
                "   [TerminalCount], " +
                "   [Type] " +
                "FROM [dbo].[Airports] " +
                "SELECT [IATACode], " +
                "   [CityCode], " +
                "   [CountryCode], " +
                "   [Name], " +
                "   [TerminalCount], " +
                "   [Type] " +
                "FROM [dbo].[Airports] " +
                "SELECT " +
                "    [Id], " +
                "    [Code], " +
                "    [ProductionDate]" +
                "FROM [dbo].[Vehicles] " +
                "WHERE [Discriminator] = N'Aircraft' " +
                "SELECT 42 AS [Answer]");

            context.Database.ExecuteSqlCommand(
                "CREATE PROCEDURE [dbo].[GetAirportTypesWithOutputParameter] @AirportType int out AS " +
                "SELECT @AirportType = Max([Type]) " +
                "FROM [dbo].[Airports] " +
                "SELECT DISTINCT [Type] " +
                "FROM [dbo].[Airports] " +
                "WHERE [Type] = @AirportType");

            context.Database.ExecuteSqlCommand(
                "CREATE FUNCTION [dbo].[MyCustomTVF]()" +
                "RETURNS TABLE " +
                "RETURN " +
                "SELECT 1 AS [Number]");

            context.Database.ExecuteSqlCommand(
                "CREATE FUNCTION EchoNumber(@number int) RETURNS int AS " +
                "BEGIN  " +
                "   RETURN @number " +
                "END");

            context.Database.ExecuteSqlCommand(
                "CREATE PROCEDURE [dbo].[GetXmlInfo] @Xml xml out AS " +
                "SELECT @Xml = '<output />' " +
                "SELECT 1234 AS Number");
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
    [Extent1].[TerminalCount] AS [TerminalCount], 
    [Extent1].[Type] AS [Type]
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
                Assert.Equal(AirportType.International, result[0].Type);
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
        public void Can_invoke_TVF_returning_entities_of_non_base_type()
        {
            using (var ctx = new MyContext())
            {
                var aircraft = ctx.GetAircraft().ToList();

                Assert.Equal(1, aircraft.Count);
                Assert.Equal("AT7", aircraft[0].Code);
            }
        }

        [Fact]
        public void Can_invoke_stored_proc_mapped_to_primitive_types()
        {
            using (var ctx = new MyContext())
            {
                var result = ctx.GetUniqueTerminalCountSProc().ToList();
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

        [Fact]
        public void Can_invoke_sproc_returning_entities_of_non_base_type()
        {
            using (var ctx = new MyContext())
            {
                var aircraft = ctx.GetAircraftSP().ToList();

                Assert.Equal(1, aircraft.Count);
                Assert.Equal("AT7", aircraft[0].Code);
            }
        }

        [Fact]
        public void Can_invoke_stored_proc_with_multiple_resultsets()
        {
            using (var ctx = new MyContext())
            {
                var results = ctx.MultipleResultSets();
                Assert.Equal(5, results.ToList().Count);

                var secondResultSet = results.GetNextResult<Airport>();
                Assert.Equal(5, secondResultSet.ToList().Count);

                var thirdResultSet = secondResultSet.GetNextResult<Aircraft>();
                Assert.Equal(new[] { "AT7" }, thirdResultSet.ToList().Select(r => r.Code));

                var fourthResultSet = thirdResultSet.GetNextResult<int>();
                Assert.Equal(new[] { 42 }, fourthResultSet.ToList());
            }
        }

        [Fact]
        public void Can_invoke_stored_proc_with_out_parameter()
        {
            using (var ctx = new MyContext())
            {
                var airportTypeParameter = new ObjectParameter("AirportType", typeof (AirportType?));
                var results = ctx.GetAirportTypesWithOutputParameter(airportTypeParameter);

                Assert.Equal(1, results.ToList().Count);
                Assert.Equal(AirportType.International, airportTypeParameter.Value);
            }
        }

        [Fact]
        public void Method_name_and_the_TVF_name_dont_have_to_math()
        {
            using (var ctx = new MyContext())
            {
                Assert.Equal(new[] { 1 }, ctx.FunctionWhoseNameIsDifferentThenTVFName().ToList());
            }
        }

        [Fact]
        public void Can_invoke_scalar_function_returning_primitive_type_value()
        {
            const string expectedSql =
                @"SELECT 
    [Extent1].[Discriminator] AS [Discriminator], 
    [Extent1].[Id] AS [Id], 
    [Extent1].[ProductionDate] AS [ProductionDate], 
    [Extent1].[Code] AS [Code]
    FROM [dbo].[Vehicles] AS [Extent1]
    WHERE ([Extent1].[Discriminator] IN (N'Aircraft',N'Vehicle')) AND ([Extent1].[Id] = ([dbo].[EchoNumber]([Extent1].[Id])))";

            using (var ctx = new MyContext())
            {
                var q = ctx.Vehicles.Where(v => v.Id == MyContext.ScalarFuncEchoNumber(v.Id));
                Assert.Equal(expectedSql, q.ToString());
                Assert.Equal(1, q.Count());
            }
        }

        [Fact]
        public void Can_set_custom_store_parameter_type()
        {
            using (var ctx = new MyContext())
            {
                var xmlParameter = new ObjectParameter("Xml", typeof(string)) {Value = "<input />"};
                var q = ctx.GetXmlInfo(xmlParameter);
                Assert.Equal(1234, q.ToArray().FirstOrDefault());
                Assert.Equal("<output />", xmlParameter.Value);
            }
        }

        [Fact]
        public void Can_invoke_built_in_square()
        {
            const string expectedSql =
                @"SELECT 
    [Extent1].[IATACode] AS [IATACode], 
    [Extent1].[CityCode] AS [CityCode], 
    [Extent1].[CountryCode] AS [CountryCode], 
    [Extent1].[Name] AS [Name], 
    [Extent1].[TerminalCount] AS [TerminalCount], 
    [Extent1].[Type] AS [Type]
    FROM [dbo].[Airports] AS [Extent1]
    WHERE  CAST( [Extent1].[TerminalCount] AS real) = (SQUARE( CAST( [Extent1].[TerminalCount] AS real)))";

            using (var ctx = new MyContext())
            {
                var q = ctx.Airports.Where(a => a.TerminalCount == MyContext.Square(a.TerminalCount));
                Assert.Equal(expectedSql, q.ToString());
                Assert.Equal(3, q.Count());
            }
        }

        [Fact]
        public void Can_invoke_built_in_format()
        {
            const string expectedSql =
                @"SELECT 
    [Extent1].[Discriminator] AS [Discriminator], 
    [Extent1].[Id] AS [Id], 
    [Extent1].[ProductionDate] AS [ProductionDate], 
    [Extent1].[Code] AS [Code]
    FROM [dbo].[Vehicles] AS [Extent1]
    WHERE ([Extent1].[Discriminator] IN (N'Aircraft',N'Vehicle')) AND (N'1929. 12. 07.' = (FORMAT([Extent1].[ProductionDate], N'd', N'hu-hu')))";

            using (var ctx = new MyContext())
            {
                var q = ctx.Vehicles.Where(v => MyContext.Format(v.ProductionDate, "d", "hu-hu") == "1929. 12. 07.");
                Assert.Equal(expectedSql, q.ToString());
                Assert.Equal(1, q.Count());
            }
        }

        [Fact]
        public void Can_invoke_niladic_current_timestamp()
        {
            const string expectedSql =
               @"SELECT 
    [Limit1].[C1] AS [C1], 
    [Limit1].[C2] AS [C2]
    FROM ( SELECT TOP (1) 
        1 AS [C1], 
        CURRENT_TIMESTAMP AS [C2]
        FROM [dbo].[Vehicles] AS [Extent1]
        WHERE [Extent1].[Discriminator] IN (N'Aircraft',N'Vehicle')
    )  AS [Limit1]";

            using (var ctx = new MyContext())
            {
                var q = ctx.Vehicles.Select(x => new { TimeStamp = MyContext.CurrentTimestamp() }).Take(1);
                Assert.Equal(expectedSql, q.ToString());
                Assert.Equal(1, q.Count());
            }
        }
    }
}
