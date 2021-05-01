using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Vertx.Decorators.Editor
{
	internal static partial class DecoratorPropertyInjector
	{
		internal static SerializedProperty Current => current?.Copy();
		private static SerializedProperty current;
		internal static object handler;

		private static Type propertyHandlerType;
		private static Type PropertyHandlerType => propertyHandlerType ??= propertyHandlerType = Type.GetType(propertyHandlerTypeName);

		private static MethodInfo getHeightMethod;
		private static MethodInfo GetHeightMethod(Type handlerType) => getHeightMethod ??= handlerType.GetMethod(getHeightMethodName, BindingFlags.Public | BindingFlags.Instance);

		private const string propertyHandlerTypeName = "UnityEditor.PropertyHandler,UnityEditor";

		private const string propertyGuiMethodName = "OnGUI";
		private const string getHeightMethodName = "GetHeight";

		internal static void OnGUIPrefix(SerializedProperty property, object ctx)
		{
			current = property;
			handler = ctx;
		}
		
		internal static void GetHeightPrefix(ref float height, SerializedProperty property, object ctx)
		{
			current = property;
			handler = ctx;
			
			height += SerializeReferenceDecorator.GetPropertyHeight(property);
		}

		[InitializeOnLoadMethod]
		private static void Initialise() => Inject();

		private static void Inject()
		{
			var handlerType = PropertyHandlerType;
			if (handlerType == null)
			{
				//Debug.LogWarning($"{nameof(DecoratorPropertyInjector)} could not find {propertyHandlerTypeName}");
				return;
			}

			Harmony harmony = new Harmony("com.vertx.decorator.injection");
			PatchOnGUI(harmony, handlerType);
			PatchGetHeight(harmony, handlerType);
		}

		private static void PatchOnGUI(Harmony harmony, Type handlerType)
		{
			MethodInfo onGUIMethod = handlerType.GetMethod(propertyGuiMethodName, BindingFlags.NonPublic | BindingFlags.Instance);

			if (onGUIMethod == null)
			{
				//Debug.LogWarning($"{nameof(DecoratorPropertyInjector)} could not find {propertyHandlerTypeName}.{propertyGuiMethodName} method");
				return;
			}

			harmony.Patch
			(
				onGUIMethod,
				transpiler:
				new HarmonyMethod(
					typeof(Transpiler_OnGUI).GetMethod(nameof(Transpiler_OnGUI.Transpiler), BindingFlags.NonPublic | BindingFlags.Static)
				)
			);
		}

		private static void PatchGetHeight(Harmony harmony, Type handlerType)
		{
			MethodInfo getHeightMethod = GetHeightMethod(handlerType);

			if (getHeightMethod == null)
			{
				//Debug.LogWarning($"{nameof(DecoratorPropertyInjector)} could not find {propertyHandlerTypeName}.{getHeightMethodName} method");
				return;
			}

			harmony.Patch
			(
				getHeightMethod,
				transpiler:
				new HarmonyMethod(
					typeof(Transpiler_GetHeight).GetMethod(nameof(Transpiler_GetHeight.Transpiler), BindingFlags.NonPublic | BindingFlags.Static)
				)
			);
		}
	}
}