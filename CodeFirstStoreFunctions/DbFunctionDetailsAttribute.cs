﻿// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System;

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class DbFunctionDetailsAttribute : Attribute
    {
        private bool _isBuiltIn;
        private bool _isNiladic;

        /// <summary>
        /// Gets or sets the name of the database schema of the store function.
        /// </summary>
        public string DatabaseSchema { get; set; }

        /// <summary>
        /// Gets or sets the name of the name of the the column returned by a function mapped to a collection of scalar results.
        /// </summary>
        public string ResultColumnName { get; set; }

        /// <summary>
        /// Gets or sets the types returned by a function mapped to a stored procedure returning multuple resultsets.
        /// </summary>
        public Type[] ResultTypes { get; set; }

        /// <summary>
        /// Gets or sets whether the function is a niladic function.
        /// </summary>
        public bool IsNiladic
        {
            get
            {
                return _isNiladic;
            }

            set
            {
                _isNiladic = value;
                IsNiladicPropertySet = true;
            }
        }

        /// <summary>
        /// Gets or sets whether the function is a built in function.
        /// </summary>
        public bool IsBuiltIn
        {
            get
            {
                return _isBuiltIn;
            }

            set
            {
                _isBuiltIn = value;
                IsBuiltInPropertySet = true;
            }
        }

        internal bool IsBuiltInPropertySet { get; private set; }
        internal bool IsNiladicPropertySet { get; private set; }
    }
}
