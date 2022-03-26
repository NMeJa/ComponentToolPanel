using System;
using UnityEditor;
using UnityEngine;

namespace ComponentToolPanel
{
	[CustomEditor(typeof(Transform), true)]
	[CanEditMultipleObjects]
	public class TransformInspector : ExtendedEditor
	{
		private static Texture transformIcon;

		private Transform transform;

		protected override string DefaultEditorTypeName => "UnityEditor.TransformInspector, UnityEditor";

		protected override Type InspectedType => typeof(Transform);

		protected override void OnEnableInternal()
		{
			transform = target as Transform;
			if (transformIcon == null)
				transformIcon = EditorGUIUtility.ObjectContent(transform, typeof(Transform)).image;
		}

		protected override void OnCustomInspectorGUI()
		{
			EditorGUILayout.LabelField("Local Space", EditorStyles.boldLabel);
			defaultEditor.OnInspectorGUI();

			//Show World Space Transform
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("World Space", EditorStyles.boldLabel);

			GUI.enabled = false;
			var localPosition = transform.localPosition;
			var position = transform.position;

			var localRotation = transform.localRotation;
			var rotation = transform.rotation;

			var localScale = transform.localScale;
			var scale = transform.lossyScale;

			defaultEditor.OnInspectorGUI();
			position = localPosition;
			transform.localPosition = position;
			rotation = localRotation;
			transform.localRotation = rotation;
			scale = localScale;
			transform.localScale = scale;
			GUI.enabled = true;

			if (targets.Length > 1) return;
			if (transform.parent == null && transform.childCount <= 0) return;

			var labelWidth = EditorGUIUtility.labelWidth;
			var fieldWidth = EditorGUIUtility.fieldWidth;
			var fontSize = EditorStyles.label.fontSize;
			EditorGUIUtility.labelWidth = 1;
			EditorGUIUtility.fieldWidth = 1;
			EditorStyles.label.fontSize = 9;

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			{
				var text = string.Empty;
				if (transform.parent != null)
				{
					EditorGUILayout.BeginHorizontal();
					{
						var e = Event.current;
						EditorGUILayout.LabelField(new GUIContent("Root: " + transform.root.name, transformIcon));
						if (e.type == EventType.MouseDown && e.button == 0 &&
						    GUILayoutUtility.GetLastRect().Contains(e.mousePosition))
						{
							if (e.clickCount == 2)
							{
								Selection.activeTransform = transform.root;
							}
							else
							{
								EditorGUIUtility.PingObject(transform.root);
							}
						}

						EditorGUILayout.LabelField(new GUIContent("Parent: " + transform.parent.name, transformIcon));
						if (e.type == EventType.MouseDown && e.button == 0 &&
						    GUILayoutUtility.GetLastRect().Contains(e.mousePosition))
						{
							if (e.clickCount == 2)
							{
								Selection.activeTransform = transform.parent;
							}
							else
							{
								EditorGUIUtility.PingObject(transform.parent);
							}
						}
					}
					EditorGUILayout.EndHorizontal();
					text = "Sibling Index: " + transform.GetSiblingIndex() + "\t";
				}

				if (transform.childCount > 0)
				{
					text += "Child Count: " + transform.childCount;
				}

				EditorGUILayout.LabelField(text);
				if (transform.parent != null && GUILayout.Button("UnParent", EditorStyles.miniButton))
				{
					Undo.SetTransformParent(transform, null, "UnParent");
					transform.SetParent(null);
				}
			}
			EditorGUILayout.EndVertical();
			EditorGUIUtility.labelWidth = labelWidth;
			EditorGUIUtility.fieldWidth = fieldWidth;
			EditorStyles.label.fontSize = fontSize;
		}
	}
}