// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System;

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class ParameterTypeAttribute : Attribute
    {
        private readonly Type _type;

        public ParameterTypeAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");    
            }

            _type = type;
        }

        public Type Type
        {
            get { return _type; }
        }
    }
}
