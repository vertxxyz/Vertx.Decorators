using System;
using UnityEngine;

namespace Vertx.Attributes
{
	/// <summary>
	/// Decorates a [<see cref="SerializeReference"/>] field, providing instances of a type that can easily be added via a dropdown.  
	/// </summary>
	public class TypeProviderAttribute : PropertyAttribute
	{
		public readonly Type Type;
		public TypeProviderAttribute(Type type, int order = 100)
		{
			Type = type;
			this.order = order;
		}
	}
}