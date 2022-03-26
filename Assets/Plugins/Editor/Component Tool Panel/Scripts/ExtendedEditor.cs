using System;
using System.Reflection;
using UnityEditor;

namespace ComponentToolPanel
{
	public abstract class ExtendedEditor : UnityEditor.Editor
	{
		protected UnityEditor.Editor defaultEditor;
		private UnityEditor.Editor fallbackEditor;

		public bool IsExtended
		{
			get => EditorPrefs.GetBool(GetType().Name + " CTP extended", false);
			set => EditorPrefs.SetBool(GetType().Name + " CTP extended", value);
		}

		private Type DefaultEditorType => Type.GetType(DefaultEditorTypeName);

		protected abstract string DefaultEditorTypeName { get; }
		protected abstract Type InspectedType { get; }

		private void OnEnable()
		{
			defaultEditor = CreateEditor(targets, DefaultEditorType);
			var fallbackEditorType = GetDefaultEditor();
			fallbackEditor = CreateEditor(targets, fallbackEditorType);
			var enableMethod =
				fallbackEditorType.GetMethod("OnEnable",
				                             BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (enableMethod != null)
			{
				enableMethod.Invoke(fallbackEditor, null);
				enableMethod.Invoke(defaultEditor, null);
			}

			OnEnableInternal();
		}

		protected virtual void OnDisable()
		{
			OnDisableInternal();
			var disableMethod = fallbackEditor.GetType()
			                                  .GetMethod("OnDisable",
			                                             BindingFlags.Instance | BindingFlags.NonPublic |
			                                             BindingFlags.Public);
			if (disableMethod != null)
			{
				disableMethod.Invoke(fallbackEditor, null);
				disableMethod.Invoke(defaultEditor, null);
			}

			DestroyImmediate(fallbackEditor);
			DestroyImmediate(defaultEditor);
		}

		protected virtual void OnEnableInternal() { }
		protected virtual void OnDisableInternal() { }

		public sealed override void OnInspectorGUI()
		{
			if (IsExtended)
			{
				OnCustomInspectorGUI();
			}
			else
			{
				fallbackEditor.OnInspectorGUI();
			}
		}

		protected abstract void OnCustomInspectorGUI();

		private Type GetDefaultEditor()
		{
			foreach (var editor in ComponentToolPanel.originalEditors)
			{
				if (editor.inspectedType != InspectedType) continue;

				if (editor.inspectorType != GetType() && editor.inspectorType != DefaultEditorType)
					return editor.inspectorType;
			}

			return DefaultEditorType;
		}
	}
}