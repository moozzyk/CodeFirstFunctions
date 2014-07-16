using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeFirstStoreFunctions
{
	internal static class Tools
	{
		internal static List<EdmType> GetTypeHierarchy(EdmType t)
		{
			var types = new List<EdmType> { t };
			while (t.BaseType != null) {
				t = t.BaseType;
				types.Add(t);
			}
			return types;
		}
	}
}
