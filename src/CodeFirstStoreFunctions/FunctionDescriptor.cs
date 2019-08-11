// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;
    using System.Linq;

    internal class FunctionDescriptor
    {
        private readonly string _name;
        private readonly EdmType[] _returnTypes;
        private readonly ParameterDescriptor[] _parameters;
        private readonly string _resultColumnName;
        private readonly string _databaseSchema;
        private readonly StoreFunctionKind _storeFunctionKind;
        private readonly bool? _isBuiltIn;
        private readonly bool? _isNiladic;

        public FunctionDescriptor(string name, IEnumerable<ParameterDescriptor> parameters,
            EdmType[] returnTypes, string resultColumnName, string databaseSchema, StoreFunctionKind storeFunctionKind, bool? isBuiltIn, bool? isNiladic)
      {
            Debug.Assert(!string.IsNullOrWhiteSpace(name), "invalid name");
            Debug.Assert(parameters != null, "parameters is null");
            Debug.Assert(parameters.All(p => p.EdmType != null), "invalid parameter type");
            Debug.Assert(returnTypes != null && returnTypes.Length > 0, "returnTypes array is null or empty");
            Debug.Assert(storeFunctionKind == StoreFunctionKind.StoredProcedure|| returnTypes.Length == 1, "multiple return types for non-sproc");

            _name = name;
            _returnTypes = returnTypes;
            _parameters = parameters.ToArray();
            _resultColumnName = resultColumnName;
            _databaseSchema = databaseSchema;
            _storeFunctionKind = storeFunctionKind;
            _isBuiltIn = isBuiltIn;
            _isNiladic = isNiladic;
      }

        public string Name
        {
            get { return _name; }
        }

        public EdmType[] ReturnTypes
        {
            get { return _returnTypes; }
        }

        public IEnumerable<ParameterDescriptor> Parameters
        {
            get { return _parameters; }
        }

        public string ResultColumnName
        {
            get { return _resultColumnName; }
        }

        public string DatabaseSchema
        {
            get { return _databaseSchema; }
        }

        public StoreFunctionKind StoreFunctionKind
        {
            get { return _storeFunctionKind; }
        }

        public bool? IsBuiltIn
        {
            get { return _isBuiltIn; }
        }

        public bool? IsNiladic
        {
            get { return _isNiladic; }
        }
    }
}
