using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vertx.Decorators.Editor
{
	internal static partial class DecoratorPropertyInjector
	{
		private static FieldInfo m_DecoratorDrawers;
		private static FieldInfo DecoratorDrawers =>
			m_DecoratorDrawers ??= m_DecoratorDrawers = PropertyHandlerType.GetField("m_DecoratorDrawers", BindingFlags.NonPublic | BindingFlags.Instance);
		
		private static PropertyInfo propertyDrawer;
		private static PropertyInfo PropertyDrawer =>
			propertyDrawer ??= propertyDrawer = PropertyHandlerType.GetProperty("propertyDrawer", BindingFlags.NonPublic | BindingFlags.Instance);

		private static MethodInfo singlePropertyHeight;
		private static MethodInfo SinglePropertyHeight =>
			singlePropertyHeight ?? typeof(EditorGUI).GetMethod("GetSinglePropertyHeight", BindingFlags.NonPublic | BindingFlags.Static);

		public static bool TryGetDecoratorDrawers(out List<DecoratorDrawer> decoratorDrawers, out Type handlerType)
		{
			handlerType = PropertyHandlerType;
			FieldInfo decoratorDrawersFieldInfo = DecoratorDrawers;
			if (handlerType == null || decoratorDrawersFieldInfo == null)
			{
				decoratorDrawers = null;
				return false;
			}

			decoratorDrawers = (List<DecoratorDrawer>) decoratorDrawersFieldInfo.GetValue(Handler);
			return true;
		}

		private static readonly object[] getHeightParams = new object[3];

		public static float GetHeightFromThis(DecoratorDrawerWithProperty decorator)
		{
			var propertyDrawerPropertyInfo = PropertyDrawer;
			if (!TryGetDecoratorDrawers(out var decoratorDrawers, out var handlerType) || propertyDrawerPropertyInfo == null)
				return 0;
			float height = 0;
			foreach (DecoratorDrawer decoratorDrawer in decoratorDrawers)
			{
				height += decoratorDrawer.GetHeight();
				if (decoratorDrawer == decorator)
					break;
			}

			MethodInfo getHeightMethod = GetHeightMethod(handlerType);
			getHeightParams[0] = Current;
			getHeightParams[1] = GUIContent.none;
			getHeightParams[2] = true;

			return (float) getHeightMethod.Invoke(Handler, getHeightParams) - height;
		}

		internal static float GetPropertyHeightRaw()
		{
			MethodInfo getHeightMethod = GetHeightMethod(PropertyHandlerType);
			getHeightParams[0] = Current;
			getHeightParams[1] = GUIContent.none;
			getHeightParams[2] = true;

			return (float) getHeightMethod.Invoke(Handler, getHeightParams);
		}
	}
}