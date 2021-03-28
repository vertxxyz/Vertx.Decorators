using System;
using System.Collections.Generic;
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

		private static readonly Dictionary<string, GUIContent> detailsLookup = new Dictionary<string, GUIContent>();
		private static readonly Dictionary<string, GUIContent> typeLabelLookup = new Dictionary<string, GUIContent>();

		protected override void OnGUI(Rect position, SerializedProperty property)
		{
			if (property == null)
			{
				EditorGUI.HelpBox(position, "Code injection has failed to retrieve property.", MessageType.Error);
				return;
			}

			if (property.propertyType != SerializedPropertyType.ManagedReference)
			{
				EditorGUI.HelpBox(position, "Property is not a SerializeReference field.", MessageType.Error);
				return;
			}

			float border = EditorGUIUtility.standardVerticalSpacing;
			position.y += border;
			position.height -= border;

			string propName = ObjectNames.NicifyVariableName(property.name);

			GUIContent label;
			if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
			{
				if (!detailsLookup.TryGetValue(propName, out label))
					detailsLookup.Add(propName, label = new GUIContent($"Set {propName} ({((TypeProviderAttribute) attribute).Type.Name})"));
			}
			else
			{
				if (!typeLabelLookup.TryGetValue(property.type, out var typeLabel))
				{
					typeLabelLookup.Add(
						property.type,
						typeLabel = new GUIContent(
							ObjectNames.NicifyVariableName(
								property.type.Substring(managedRefStringLength, property.type.Length - managedRefStringLength - 1)
							)
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
			EditorGUIUtils.DrawHeaderWithBackground(background, GUIContent.none);
			background.y += background.height;
			background.height = remainingHeight + border2;
			EditorGUI.DrawRect(background, EditorGUIUtils.HeaderColor);
			
			if(GUI.Button(position, label, EditorStyles.popup))
				ShowPropertyDropdown();
			
			void ShowPropertyDropdown()
			{
				AdvancedDropdown dropdown;
				Type type = ((TypeProviderAttribute) attribute).Type;
				if (type.IsSubclassOf(typeof(AdvancedDropdownAttribute)))
				{
					dropdown = AdvancedDropdownUtils.CreateAdvancedDropdownFromAttribute(type,
						propName,
						OnSelected
					);
				}
				else
				{
					dropdown = AdvancedDropdownUtils.CreateAdvancedDropdownFromType(
						((TypeProviderAttribute) attribute).Type,
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

		protected override float GetHeight(SerializedProperty property) => EditorGUIUtils.HeightWithSpacing + EditorGUIUtility.standardVerticalSpacing;
	}
}