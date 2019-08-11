// Copyright (c) Pawel Kadluczka, Inc. All rights reserved. See License.txt in the project root for license information.

namespace CodeFirstStoreFunctions
{
    using System.Collections.Generic;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Diagnostics;

	internal static class Tools
	{
	    internal static List<EdmType> GetTypeHierarchy(EdmType edmType)
	    {
            Debug.Assert(edmType != null, "edmType is null");
            Debug.Assert(edmType.BuiltInTypeKind == BuiltInTypeKind.EntityType, "entity type expected");

	        var types = new List<EdmType> {edmType};
	        while (edmType.BaseType != null)
	        {
	            edmType = edmType.BaseType;
	            types.Add(edmType);
	        }

	        return types;
	    }
	}
}
