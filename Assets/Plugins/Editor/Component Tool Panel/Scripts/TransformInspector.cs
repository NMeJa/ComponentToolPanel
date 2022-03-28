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
			DrawLocalTransform();
			DrawWorldTransform();

			if (targets.Length > 1) return;
			if (transform.parent == null && transform.childCount <= 0) return;

			DrawTransformInfoBox();
		}

		private void DrawWorldTransform()
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("World Space", EditorStyles.boldLabel);

			GUI.enabled = false;
			ShowWorldTransform();
			GUI.enabled = true;
		}

		private void DrawLocalTransform()
		{
			EditorGUILayout.LabelField("Local Space", EditorStyles.boldLabel);
			defaultEditor.OnInspectorGUI();
		}

		private void ShowWorldTransform()
		{
			var worldPosition = transform.position;
			var worldRotation = transform.rotation;
			var worldScale = transform.lossyScale;

			EditorGUILayout.Vector3Field("Position", worldPosition);
			EditorGUILayout.Vector3Field("Rotation", worldRotation.eulerAngles);
			EditorGUILayout.Vector3Field("Scale", worldScale);
		}

		private void DrawTransformInfoBox()
		{
			var labelWidth = EditorGUIUtility.labelWidth;
			var fieldWidth = EditorGUIUtility.fieldWidth;
			var fontSize = EditorStyles.label.fontSize;
			EditorGUIUtility.labelWidth = 1;
			EditorGUIUtility.fieldWidth = 1;
			EditorStyles.label.fontSize = 9;

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(EditorStyles.helpBox);
			ShowTransformInfoBox();
			EditorGUILayout.EndVertical();

			EditorGUIUtility.labelWidth = labelWidth;
			EditorGUIUtility.fieldWidth = fieldWidth;
			EditorStyles.label.fontSize = fontSize;
		}

		private void ShowTransformInfoBox()
		{
			var text = string.Empty;
			if (transform.parent != null)
			{
				EditorGUILayout.BeginHorizontal();
				ShowParentAndRootTransform();
				EditorGUILayout.EndHorizontal();

				text = $"Sibling Index: {transform.GetSiblingIndex()}\t";
			}

			ShowSiblingAndChildrenCount(text);
			UnParent();
		}

		private void ShowParentAndRootTransform()
		{
			var currentEvent = Event.current;
			var root = transform.root;
			EditorGUILayout.LabelField(new GUIContent($"Root: {root.name}", transformIcon));
			SelectGameObject(currentEvent, root);

			var parent = transform.parent;
			EditorGUILayout.LabelField(new GUIContent("Parent: " + parent.name, transformIcon));
			SelectGameObject(currentEvent, parent);
		}

		private void SelectGameObject(Event e, Transform selectedTransform)
		{
			var lastRect = GUILayoutUtility.GetLastRect();
			if (e.type != EventType.MouseDown || e.button != 0 || !lastRect.Contains(e.mousePosition)) return;

			if (e.clickCount == 2)
			{
				Selection.activeTransform = selectedTransform;
			}
			else
			{
				EditorGUIUtility.PingObject(selectedTransform);
			}
		}

		private void ShowSiblingAndChildrenCount(string text)
		{
			if (transform.childCount > 0)
				text += $"Child Count: {transform.childCount}";

			EditorGUILayout.LabelField(text);
		}

		private void UnParent()
		{
			if (transform.parent is null || !GUILayout.Button("UnParent", EditorStyles.miniButton)) return;
			Undo.SetTransformParent(transform, null, "UnParent");
			transform.SetParent(null);
		}
	}
}