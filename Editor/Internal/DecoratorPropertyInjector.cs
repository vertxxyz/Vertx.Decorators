using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace Vertx.Decorators.Editor
{
	internal static partial class DecoratorPropertyInjector
	{
		internal static SerializedProperty Current { get; private set; }
		private static object handler;

		private static Type propertyHandlerType;
		private static Type PropertyHandlerType => propertyHandlerType ??= propertyHandlerType = Type.GetType(propertyHandlerTypeName);

		private static MethodInfo getHeightMethod;
		private static MethodInfo GetHeightMethod(Type handlerType) => getHeightMethod ??= handlerType.GetMethod(getHeightMethodName, BindingFlags.Public | BindingFlags.Instance);

		private const string propertyHandlerTypeName = "UnityEditor.PropertyHandler,UnityEditor";

		private const string propertyGuiMethodName = "OnGUI";
		private const string getHeightMethodName = "GetHeight";

		private static void Prefix(SerializedProperty property, object ctx)
		{
			Current = property;
			handler = ctx;
		}

		[InitializeOnLoadMethod]
		private static void Initialise() => Inject();


		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, OpCode opCode)
		{
			bool injected = false;
			foreach (var instruction in instructions)
			{
				if (!injected)
				{
					// There is not a ldargX as the first instruction usually, so we know we have previously injected if this is the case.
					if (instruction.opcode != opCode)
					{
						// Loads the argument at index X ("property") onto the evaluation stack.
						yield return new CodeInstruction(opCode);
						// Loads "this" onto the evaluation stack.
						yield return new CodeInstruction(OpCodes.Ldarg_0);
						// Call our new method.
						var codeInstruction = new CodeInstruction(OpCodes.Call,
							typeof(DecoratorPropertyInjector).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
						yield return codeInstruction;
					}

					injected = true;
				}

				yield return instruction;
			}

			if (!injected)
				Debug.Log("Missing Injection");
		}

		private static IEnumerable<CodeInstruction> TranspilerLdarg1(IEnumerable<CodeInstruction> instructions) => Transpiler(instructions, OpCodes.Ldarg_1);
		private static IEnumerable<CodeInstruction> TranspilerLdarg2(IEnumerable<CodeInstruction> instructions) => Transpiler(instructions, OpCodes.Ldarg_2);


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
					typeof(DecoratorPropertyInjector).GetMethod(nameof(TranspilerLdarg2), BindingFlags.NonPublic | BindingFlags.Static)
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
					typeof(DecoratorPropertyInjector).GetMethod(nameof(TranspilerLdarg1), BindingFlags.NonPublic | BindingFlags.Static)
				)
			);
		}
	}
}