// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;

    internal class ParameterDescriptor
    {
        private readonly string _name;
        private readonly EdmType _edmType;
        private readonly bool _isOutParam;

        public ParameterDescriptor(string name, EdmType edmType, bool isOutParam)
        {
            Debug.Assert(name != null, "name is null");
            Debug.Assert(edmType != null, "edmType is null");

            _name = name;
            _edmType = edmType;
            _isOutParam = isOutParam;
        }

        public string Name
        {
            get { return _name; }
        }

        public EdmType EdmType
        {
            get { return _edmType; }
        }

        public bool IsOutParam
        {
            get { return _isOutParam; }
        }
    }
}
