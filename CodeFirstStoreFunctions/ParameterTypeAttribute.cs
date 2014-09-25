// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System;

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ParameterTypeAttribute : Attribute
    {
        public ParameterTypeAttribute()
        {
        }

        public ParameterTypeAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; set; }

        public string StoreType { get; set; }
    }
}
