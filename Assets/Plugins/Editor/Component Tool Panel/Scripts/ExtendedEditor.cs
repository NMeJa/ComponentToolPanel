using System;
using System.Reflection;
using UnityEditor;

namespace ComponentToolPanel
{
	public abstract class ExtendedEditor : Editor
	{
		//I know not the best name. But it's the best I can come up with. 
		private const BindingFlags BindingFlagsBothPublicAndNon =
			BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

		private const string EnableName = "OnEnable";
		private const string DisableName = "OnDisable";
		protected Editor defaultEditor;
		private Editor fallbackEditor;

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
			if (!ComponentToolPanel.IsUsed) return;
			defaultEditor = CreateEditor(targets, DefaultEditorType);
			var fallbackEditorType = GetDefaultEditor();
			fallbackEditor = CreateEditor(targets, fallbackEditorType);
			var enableMethod = fallbackEditorType.GetMethod(EnableName, BindingFlagsBothPublicAndNon);

			if (enableMethod != null)
			{
				enableMethod.Invoke(fallbackEditor, null);
				enableMethod.Invoke(defaultEditor, null);
			}

			OnEnableInternal();
		}

		protected virtual void OnDisable()
		{
			if (!ComponentToolPanel.IsUsed) return;
			OnDisableInternal();
			var disableMethod = fallbackEditor.GetType().GetMethod(DisableName, BindingFlagsBothPublicAndNon);
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
			if (!ComponentToolPanel.IsUsed)
			{
				base.OnInspectorGUI();
				return;
			}

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
			// Not readable Linq conversion this is better
			// ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
			foreach (var editor in ComponentToolPanel.originalEditors)
			{
				if (editor.inspectedType != InspectedType) continue;
				if (editor.inspectorType == GetType() || editor.inspectorType == DefaultEditorType) continue;

				return editor.inspectorType;
			}

			return DefaultEditorType;
		}
	}
}