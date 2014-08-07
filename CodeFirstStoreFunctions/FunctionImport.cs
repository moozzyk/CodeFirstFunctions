// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;
    using System.Linq;

    internal class FunctionImport
    {
        private readonly string _name;
        private readonly EdmType[] _returnTypes;
        private readonly KeyValuePair<string, EdmType>[] _parameters;
        private readonly string _resultColumnName;
        private readonly string _databaseSchema;
        private readonly bool _isComposable;

        public FunctionImport(string name, IEnumerable<KeyValuePair<string, EdmType>> parameters, 
            EdmType[] returnTypes, string resultColumnName, string databaseSchema, bool isComposable)
      {
            Debug.Assert(!string.IsNullOrWhiteSpace(name), "invalid name");
            Debug.Assert(parameters != null, "parameters is null");
            Debug.Assert(parameters.All(p => p.Value != null), "invalid parameter type");
            Debug.Assert(returnTypes != null && returnTypes.Length > 0, "returnTypes array is null or empty");
            Debug.Assert(!isComposable || returnTypes.Length == 1, "multiple return types for composable function");

            _name = name;
            _returnTypes = returnTypes;
            _parameters = parameters.ToArray();
            _resultColumnName = resultColumnName;
            _databaseSchema = databaseSchema;
            _isComposable = isComposable;
        }

        public string Name
        {
            get { return _name; }
        }

        public EdmType[] ReturnTypes
        { 
            get { return _returnTypes; } 
        }

        public IEnumerable<KeyValuePair<string, EdmType>> Parameters
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

        public bool IsComposable
        {
            get { return _isComposable; }
        }
    }
}
