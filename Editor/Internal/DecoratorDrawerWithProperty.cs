using UnityEditor;
using UnityEngine;

namespace Vertx.Decorators.Editor
{
	public abstract class DecoratorDrawerWithProperty : DecoratorDrawer
	{
		private static bool TryGetProperty(out SerializedProperty property)
		{
			property = DecoratorPropertyInjector.Current;
			return property != null;
		}
		
		public sealed override void OnGUI(Rect position)
		{
			if (!TryGetProperty(out var property))
			{
				EditorGUI.HelpBox(position, "Decorator has failed, please update com.vertx.decorators or submit a github issue if there are no updates.", MessageType.Error);
				return;
			}
			OnGUI(position, property);
		}

		public override float GetHeight() => TryGetProperty(out var property) ? GetHeight(property) : base.GetHeight();

		protected abstract void OnGUI(Rect position, SerializedProperty property);

		private bool inGetHeight;

		protected virtual float GetHeight(SerializedProperty property)
		{
			inGetHeight = true;
			try
			{
				return base.GetHeight();
			}
			finally
			{
				inGetHeight = false;
			}
		}

		/// <summary>
		/// The height remaining to draw further decorators and the property itself.
		/// This method cannot be called from the GetHeight method, as it would be recursive.
		/// </summary>
		/// <returns>The space until the end of the property that is after this decorator.</returns>
		protected float GetRemainingHeight()
		{
			if (!inGetHeight)
				return DecoratorPropertyInjector.GetHeightFromThis(this);
			Debug.LogWarning($"\"{nameof(GetRemainingHeight)}\" cannot be called within the context of the \"{nameof(GetHeight)}\" method.");
			return 0;
		}
	}
}