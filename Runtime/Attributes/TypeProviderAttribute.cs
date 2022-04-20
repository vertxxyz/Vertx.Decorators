using System;
using UnityEngine;

namespace Vertx.Attributes
{
	/// <summary>
	/// Decorates a [<see cref="SerializeReference"/>] field, providing instances of a type that can easily be added via a dropdown.  
	/// </summary>
	public class TypeProviderAttribute : PropertyAttribute
	{
		[Flags]
		public enum Display
		{
			NoFeatures = 0,
			/// <summary>
			/// Shows the constrained type in brackets (Type)
			/// </summary>
			ShowTypeConstraint = 1,
			/// <summary>
			/// Shows "Set to null" in the context menu.
			/// </summary>
			AllowSetToNull = 1 << 1,
			Default = ShowTypeConstraint | AllowSetToNull
		}

		public readonly Type Type;
		public readonly Display Features;

		public TypeProviderAttribute() : this(null, Display.Default) { }
		public TypeProviderAttribute(Display features = Display.Default) : this(null, features) { }

		public TypeProviderAttribute(Type type = null, Display features = Display.Default) :
			this(type, 100, features) { }

		public TypeProviderAttribute(Type type = null, int order = 100, Display features = Display.Default)
		{
			Type = type;
			Features = features;
			this.order = order;
		}
	}
}