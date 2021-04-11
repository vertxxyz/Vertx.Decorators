using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Vertx.Attributes;
using Vertx.Utilities;
using Vertx.Utilities.Editor;

namespace Vertx.Decorators.Editor
{
	[CustomPropertyDrawer(typeof(TypeProviderAttribute))]
	public class TypeProviderDecorator : DecoratorDrawerWithProperty
	{
		private static readonly int managedRefStringLength = "managedReference<".Length;

		private static readonly Dictionary<string, (string fullTypeName, GUIContent defaultLabel)> fullTypeNameLookup = new Dictionary<string, (string, GUIContent)>();
		private static readonly Dictionary<int, GUIContent> typeLabelLookup = new Dictionary<int, GUIContent>();
		private static Texture2D warnTexture;

		protected override void OnGUI(Rect position, SerializedProperty property)
		{
			if (property == null)
			{
				EditorGUI.HelpBox(position, "Code injection has failed to retrieve property.", MessageType.Error);
				return;
			}

			if (property.propertyType != SerializedPropertyType.ManagedReference)
			{
				EditorGUI.HelpBox(position, "Property is not a SerializeReference field.", MessageType.Warning);
				return;
			}

			float border = EditorGUIUtility.standardVerticalSpacing;
			position.y += border * 2;
			position.height -= border;

			string propName = ObjectNames.NicifyVariableName(property.name);

			Type type = ((TypeProviderAttribute) attribute).Type;
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

			float remainingHeight = GetRemainingHeight();
			float border2 = border * 2;
			background.x -= border;
			background.width += border2;
			background.y -= border;
			//Header
			EditorGUIUtils.DrawHeaderWithBackground(background, GUIContent.none);
			background.y += background.height;
			background.y -= border;
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

		private Type GetRelevantType(SerializedProperty property)
		{
			Type type = ((TypeProviderAttribute) attribute).Type;
			if (type != null)
				return type;
			EditorUtils.GetObjectFromProperty(property, out _, out FieldInfo fieldInfo);
			return EditorUtils.GetSerializedTypeFromFieldInfo(fieldInfo);
		}

		protected override float GetHeight(SerializedProperty property) => EditorGUIUtils.HeightWithSpacing + EditorGUIUtility.standardVerticalSpacing * 2;
	}
}