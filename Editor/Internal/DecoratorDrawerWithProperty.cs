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

		public sealed override float GetHeight() => TryGetProperty(out var property) ? GetHeight(property) : base.GetHeight();

		/// <summary>
		/// Override this method to make your own GUI for the decorator.
		/// </summary>
		/// <param name="position">Rectangle on the screen to use for the decorator GUI.</param>
		/// <param name="property">The property this decorator is attached to.</param>
		protected abstract void OnGUI(Rect position, SerializedProperty property);

		private bool inGetHeight;

		/// <summary>
		/// Override this method to specify how tall the GUI for this decorator is in pixels.
		/// </summary>
		/// <param name="property">The property this decorator is attached to.</param>
		/// <returns>The height needed to draw this decorator.</returns>
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