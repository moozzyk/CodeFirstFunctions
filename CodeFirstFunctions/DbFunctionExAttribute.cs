// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstFunctions
{
    using System;
    using System.Data.Entity;

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class DbFunctionExAttribute : DbFunctionAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:System.Data.Entity.DbFunctionAttribute" /> class.
        /// </summary>
        /// <param name="namespaceName">The namespace of the mapped-to function.</param>
        /// <param name="functionName">The name of the mapped-to function.</param>
        public DbFunctionExAttribute(string namespaceName, string functionName)
            : base(namespaceName, functionName)
        {
        }

        /// <summary>
        /// Gets or sets the name of the database schema for the function.
        /// </summary>
        public string DatabaseSchema
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the column returned by a scalar function.
        /// </summary>
        public string ResultColumnName
        {
            get;
            set;
        }
    }
}
