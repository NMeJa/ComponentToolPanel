using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Component = UnityEngine.Component;

namespace ComponentToolPanel
{
	//SuppressMessage are for making sure that older versions of Unity don't have problem, while accidentally refactoring the code
	[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
	public class ComponentPreviewWindow : EditorWindow
	{
		private const int Width = 400;
		private const BindingFlags BindingFlagsNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;
		private const BindingFlags BindingFlagsPublic = BindingFlags.Public | BindingFlags.Instance;
		private const string Parent = "m_Parent";
		private const string UnityEditorViewName = "UnityEditor.View, UnityEditor";
		private const string WindowName = "window";
		private const string DontSavetoLayoutName = "m_DontSaveToLayout";


		private static readonly Dictionary<ComponentPreviewWindow, Editor> windows =
			new Dictionary<ComponentPreviewWindow, Editor>();

		private static readonly Color frameColor = Color.black;
		private static Event lastEvent;
		private bool dragging;
		private Editor editor;

		private Vector2 lastMousePosition;

		private bool IsEditor
		{
			get
			{
				var b = false;
#if UNITY_2021_2_OR_NEWER
				b = editor is not {serializedObject: { }} || editor.serializedObject.targetObject == null;
#elif UNITY_2020_2_OR_NEWER
				b = !(editor is {serializedObject: { }}) || editor.serializedObject.targetObject == null;
#elif UNITY_2018_4_OR_NEWER
				b = editor == null || editor.serializedObject is null || editor.serializedObject.targetObject == null;
#else
				throw new NotSupportedException("Why are you even using This Editor?");
#endif
				return b;
			}
		}

		private void OnEnable()
		{
			EditorApplication.update += EditorUpdate;
			if (editor != null)
				windows.Add(this, editor);
		}

		private void OnDestroy()
		{
			DestroyImmediate(editor);
			EditorApplication.update -= EditorUpdate;
			windows.Remove(this);
		}

		private void OnGUI()
		{
			if (IsEditor) GUIUtility.ExitGUI();

			var currentEvent = Event.current;

			EditorGUILayout.BeginHorizontal();

			DrawTitle();
			WindowDragging(currentEvent);
			DrawCloseButton();

			EditorGUILayout.EndHorizontal();

			DrawEditor();
			var height = ResizeWindowBasedOnContent(currentEvent);
			DrawFrame(height, currentEvent);
		}

		private void DrawTitle()
		{
			var targetTypeName = editor.target.GetType().Name;
			var components = Selection.objects.Contains(((Component) editor.serializedObject.targetObject).gameObject);
			var targetName = components ? string.Empty : $" ({editor.serializedObject.targetObject.name})";
			EditorGUILayout.LabelField($"{targetTypeName}{targetName} Preview ", EditorStyles.boldLabel,
			                           GUILayout.Width(Width - 16 - 1));
		}

		private void WindowDragging(Event currentEvent)
		{
			var titleRect = GUILayoutUtility.GetLastRect();

			if (titleRect.Contains(currentEvent.mousePosition) && currentEvent.button == 0 &&
			    currentEvent.type == EventType.MouseDown)
			{
				dragging = true;
			}
			else if (dragging && currentEvent.type == EventType.MouseUp)
			{
				dragging = false;
				lastMousePosition = Vector2.zero;
			}
			else if (dragging && currentEvent.type == EventType.MouseDrag)
			{
				var screenPos = GUIUtility.GUIToScreenPoint(currentEvent.mousePosition);
				var delta = lastMousePosition == Vector2.zero ? Vector2.zero : screenPos - lastMousePosition;
				position = new Rect(position.x + delta.x, position.y + delta.y, position.width, position.height);
				lastMousePosition = screenPos;
			}
		}

		private void DrawCloseButton()
		{
			var buttonRect = GUILayoutUtility.GetRect(16, 18);
			buttonRect.x -= 9;
			buttonRect.y += 1;
			GUILayout.FlexibleSpace();
			if (GUI.Button(buttonRect, new GUIContent(Icons.CloseIcon), GUIStyle.none)) Close();
		}

		private void DrawEditor()
		{
			try
			{
				if (ActiveEditorTracker.sharedTracker.inspectorMode == InspectorMode.Normal)
				{
					editor.OnInspectorGUI();
				}
				else
				{
					editor.DrawDefaultInspector();
				}
			}
			catch
			{
				EditorGUILayout.HelpBox("The preview for this component is no longer available",
				                        MessageType.Warning);
				throw new WarningException("The preview for this component is no longer available");
			}
		}

		private float ResizeWindowBasedOnContent(Event currentEvent)
		{
			var height = GUILayoutUtility.GetRect(0, 0).y;
			if (currentEvent.type == EventType.Repaint)
				minSize = maxSize = new Vector2(Width, height);
			return height;
		}

		private static void DrawFrame(float height, Event currentEvent)
		{
			EditorGUI.DrawRect(new Rect(0, 0, Width, 1), frameColor);
			EditorGUI.DrawRect(new Rect(Width - 1, 0, 1, height), frameColor);
			EditorGUI.DrawRect(new Rect(0, height - 1, Width, 1), frameColor);
			EditorGUI.DrawRect(new Rect(0, 0, 1, height), frameColor);

			lastEvent = currentEvent;
		}

		//When a selection changes, repaint so that the title updates
		private void OnSelectionChange()
		{
			if (editor.serializedObject.targetObject is null)
				return;

			Repaint();
		}

		public static void Show(Component component)
		{
			var window = windows.FirstOrDefault(e => e.Value.target == component).Key;
			if (window == null)
			{
				window = CreateWindow(component);
			}

			var screenPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
			var x = Mathf.Min(screenPos.x, Screen.currentResolution.width - window.position.width);
			window.position = new Rect(x, screenPos.y, window.position.width, window.position.height);
			window.Focus();
		}

		private static ComponentPreviewWindow CreateWindow(Component component)
		{
			var window = CreateInstance<ComponentPreviewWindow>();
			window.editor = Editor.CreateEditor(component);
			window.ShowPopup();
			windows.Add(window, window.editor);

			//Don't save window to layout
			var parent = typeof(EditorWindow).GetField(Parent, BindingFlagsNonPublic)?.GetValue(window);
			var view = Type.GetType(UnityEditorViewName);
			var viewWindow = view?.GetProperty(WindowName, BindingFlagsPublic)?.GetValue(parent, null);
			var dontSaveToLayout = viewWindow?.GetType().GetField(DontSavetoLayoutName, BindingFlagsNonPublic);
			dontSaveToLayout?.SetValue(viewWindow, true);
			return window;
		}

		private void EditorUpdate()
		{
			Repaint();
			try
			{
				if (IsEditor) Close();
				if (lastEvent == null) return;

				//Close window if focused or mouseOver
				if (lastEvent.keyCode != KeyCode.Escape || lastEvent.type != EventType.KeyDown) return;

				if (lastEvent.shift)
				{
					windows.ToList().ForEach(x => x.Key.Close());
				}
				else if (mouseOverWindow == this || focusedWindow == this)
				{
					Close();
					lastEvent.Use();
				}
			}
			catch
			{
				Close();
			}
		}
	}
}