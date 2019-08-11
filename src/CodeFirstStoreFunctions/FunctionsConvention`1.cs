// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System;
    using System.Data.Entity;

    public class FunctionsConvention<T> : FunctionsConvention
        where T : DbContext
    {
        public FunctionsConvention(string defaultSchema)
            : base(defaultSchema, typeof(T))
        {
        }
    }
}