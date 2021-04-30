using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Vertx.Utilities;
using Vertx.Utilities.Editor;

namespace Vertx.Decorators.Editor
{
	internal static class TypeProviderDecorator
	{
		
		private static readonly int managedRefStringLength = "managedReference<".Length;

		private static readonly Dictionary<string, (string fullTypeName, GUIContent defaultLabel)> fullTypeNameLookup = new Dictionary<string, (string, GUIContent)>();
		private static readonly Dictionary<int, GUIContent> typeLabelLookup = new Dictionary<int, GUIContent>();
		private static Texture2D warnTexture;

		private static readonly float decoratorHeight = EditorGUIUtils.HeightWithSpacing + EditorGUIUtility.standardVerticalSpacing;

		private static PropertyInfo inspectorMode;
		private static int GetInspectorMode(SerializedObject serializedObject)
		{
			if (inspectorMode == null)
				inspectorMode = typeof(SerializedObject).GetProperty("inspectorMode", BindingFlags.Instance | BindingFlags.NonPublic);
			return (int) inspectorMode.GetValue(serializedObject);
		}

		public static float GetPropertyHeight(SerializedProperty property)
		{
			if (property == null || property.propertyType != SerializedPropertyType.ManagedReference)
				return 0;

			if (GetInspectorMode(property.serializedObject) != 0)
				return 0;
			return decoratorHeight;
		}

		public static void OnGUI(ref Rect totalPosition)
		{
			try
			{
				SerializedProperty property = DecoratorPropertyInjector.Current;

				if (property == null || property.propertyType != SerializedPropertyType.ManagedReference)
					return;

				if (GetInspectorMode(property.serializedObject) != 0)
					return;
				
				//float totalPropertyHeight = totalPosition.height;
				totalPosition.height -= decoratorHeight;
				Rect position = totalPosition;
				totalPosition.y += EditorGUIUtils.HeightWithSpacing  + EditorGUIUtility.standardVerticalSpacing;
				position.height = decoratorHeight;

				float border = EditorGUIUtility.standardVerticalSpacing;
				position.y += border;
				position.height -= border;

				string propName = ObjectNames.NicifyVariableName(property.name);

				Type type = null; //TODO ((TypeProviderAttribute) attribute).Type;
				string typeNameSimple = type?.Name ?? property.managedReferenceFieldTypename;

				if (!fullTypeNameLookup.TryGetValue(typeNameSimple, out var group))
				{
					// Populate the name of the type associated with the property
					string fullTypeName = GetRelevantType(property).Name;
					if (fullTypeName.EndsWith("Attribute"))
						fullTypeName = fullTypeName.Substring(0, fullTypeName.Length - 9);
					group = (
						fullTypeName, // fullTypeName
						new GUIContent($"Null ({fullTypeName})") // defaultLabel
					);
					fullTypeNameLookup.Add(typeNameSimple, group);
				}

				GUIContent label;
				bool referenceIsAssigned;
				if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
				{
					// No reference has been assigned.
					referenceIsAssigned = false;
					label = group.defaultLabel;
				}
				else
				{
					referenceIsAssigned = true;

					int hashCode = property.type.GetHashCode() ^ group.fullTypeName.GetHashCode();
					if (!typeLabelLookup.TryGetValue(hashCode, out var typeLabel))
					{
						typeLabelLookup.Add(
							hashCode,
							typeLabel = new GUIContent(
								// Assigned Type (Type Constraint)
								$"{ObjectNames.NicifyVariableName(property.type.Substring(managedRefStringLength, property.type.Length - managedRefStringLength - 1))} ({group.fullTypeName})"
							)
						);
					}

					label = typeLabel;
				}

				Rect background = position;

				// Ideally we could use the total property height Unity has already calculated, but sadly this value does not function in all circumstances
				// We will need to recalculate the correct height to draw the background.
				float remainingHeight = DecoratorPropertyInjector.GetPropertyHeightRaw() - decoratorHeight;
				float border2 = border * 2;
				background.x -= border;
				background.width += border2;
				background.y -= border;
				
				//Header
				EditorGUIUtils.DrawHeaderWithBackground(background, GUIContent.none);
				background.y += background.height;
				background.height = remainingHeight + border2;
				//Background
				EditorGUI.DrawRect(background, EditorGUIUtils.HeaderColor);

				position.y -= 1;
				//Header - Prefix Label
				Rect area = EditorGUI.PrefixLabel(position, EditorGUIUtility.TrTempContent(property.displayName), EditorStyles.boldLabel);

				if (!referenceIsAssigned)
				{
					// Draw warning icon if no reference is assigned.
					if (warnTexture == null)
						warnTexture = EditorGUIUtility.FindTexture("console.warnicon.inactive.sml");
					GUI.DrawTexture(new Rect(area.x - 17, area.y + 1, 16, 16), warnTexture);
				}

				Event e = Event.current;
				if (e.type == EventType.MouseDown && e.button == 1)
				{
					// Context Menu on the prefix label.
					Rect prefixLabelArea = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
					if (prefixLabelArea.Contains(e.mousePosition))
					{
						GenericMenu menu = new GenericMenu();
						if (referenceIsAssigned)
						{
							menu.AddItem(new GUIContent("Set to Null"), false, () =>
							{
								property.managedReferenceValue = null;
								property.serializedObject.ApplyModifiedProperties();
							});

							menu.AddItem(new GUIContent("Reset Values To Defaults"), false, () =>
							{
								Type t = EditorUtils.GetObjectFromProperty(property, out _, out _).GetType();
								property.managedReferenceValue = Activator.CreateInstance(t);
								property.serializedObject.ApplyModifiedProperties();
							});
						}
						else
						{
							menu.AddDisabledItem(new GUIContent("Set to Null"), false);
							menu.AddDisabledItem(new GUIContent("Reset Values To Defaults"), false);
						}

						menu.ShowAsContext();
					}
				}

				// Header - Dropdown
				if (GUI.Button(area, label, EditorStyles.popup))
					ShowPropertyDropdown();

				void ShowPropertyDropdown()
				{
					AdvancedDropdown dropdown;
					Type type = GetRelevantType(property);

					if (type.IsSubclassOf(typeof(AdvancedDropdownAttribute)))
					{
						dropdown = AdvancedDropdownUtils.CreateAdvancedDropdownFromAttribute(
							type,
							propName,
							OnSelected
						);
					}
					else
					{
						dropdown = AdvancedDropdownUtils.CreateAdvancedDropdownFromType(
							type,
							propName,
							OnSelected
						);
					}

					dropdown.Show(position);

					void OnSelected(AdvancedDropdownElement element)
					{
						property.managedReferenceValue = Activator.CreateInstance(element.Type);
						property.serializedObject.ApplyModifiedProperties();
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		private static Type GetRelevantType(SerializedProperty property)
		{
			Type type = null; // TODO ((TypeProviderAttribute) attribute).Type;
			if (type != null)
				return type;
			EditorUtils.GetObjectFromProperty(property, out _, out FieldInfo fieldInfo);
			return EditorUtils.GetSerializedTypeFromFieldInfo(fieldInfo);
		}
	}
}