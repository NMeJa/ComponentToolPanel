using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace ComponentToolPanel
{
	//SuppressMessage are for making sure that older versions of Unity don't have problem, while accidentally refactoring the code
	[SuppressMessage("ReSharper", "ArrangeObjectCreationWhenTypeEvident")]
	[SuppressMessage("ReSharper", "ConvertToNullCoalescingCompoundAssignment")]
	[CustomEditor(typeof(GameObject), true)]
	[CanEditMultipleObjects]
	[SuppressMessage("ReSharper", "UnusedParameter.Local")]
	public class ComponentToolPanel : Editor
	{
		#region Enable/Disable

		public static bool IsUsed { get; private set; } = true;

		[MenuItem("Tools/Component Tool Panel/Enable", false, 0)]
		private static void EnableUse()
		{
			IsUsed = true;
			ClearSelection();
		}

		[MenuItem("Tools/Component Tool Panel/Disable", false, 1)]
		private static void DisableUse()
		{
			IsUsed = false;
			ClearSelection();
		}

		private static void ClearSelection() => Selection.activeGameObject = null;

		#endregion

		private const string GameObjectInspectorTypeName = "UnityEditor.GameObjectInspector, UnityEditor";
		private const string AddComponentWindowTypeName = "UnityEditor.AddComponentWindow, UnityEditor";
		private const string CustomEditorAttributesTypeName = "UnityEditor.CustomEditorAttributes, UnityEditor";
		private const string FoldoutPrefsKey = "Component Tool Panel expanded";
		private const string DummyName = "Component Tool Panel Dummy";
		private const string CopiedComponentIDPrefsKey = "Component Tool Panel Copied Component ID";

		private static readonly List<ExtendedEditors> extendedEditors = new List<ExtendedEditors>
		{
			new ExtendedEditors(typeof(Transform), typeof(TransformInspector))
		};

		public static readonly List<ExtendedEditors> originalEditors = new List<ExtendedEditors>();

		private static readonly Dictionary<Type, List<Type>> componentDependencies = new Dictionary<Type, List<Type>>
		{
			{
				typeof(Camera), new List<Type>
				{
					typeof(FlareLayer)
				}
			},
			{
				typeof(MeshRenderer), new List<Type>
				{
					typeof(TextMesh)
				}
			},
			{
				typeof(ParticleSystemRenderer), new List<Type>
				{
					typeof(ParticleSystem)
				}
			},
			{
				typeof(SkinnedMeshRenderer), new List<Type>
				{
					typeof(Cloth)
				}
			},
			{
				typeof(Rigidbody), new List<Type>
				{
					typeof(HingeJoint),
					typeof(FixedJoint),
					typeof(SpringJoint),
					typeof(CharacterJoint),
					typeof(ConfigurableJoint),
					typeof(ConstantForce)
				}
			},
			{
				typeof(Rigidbody2D), new List<Type>
				{
					typeof(DistanceJoint2D),
					typeof(HingeJoint2D),
					typeof(SliderJoint2D),
					typeof(SpringJoint2D),
					typeof(WheelJoint2D),
					typeof(ConstantForce2D)
				}
			},
			{
				typeof(RectTransform), new List<Type>
				{
					typeof(Canvas)
				}
			},
			{
				typeof(Canvas), new List<Type>
				{
					typeof(CanvasScaler)
				}
			},
			{
				typeof(CanvasRenderer), new List<Type>
				{
					typeof(Text),
					typeof(Image),
					typeof(RawImage)
				}
			}
		};

		private static MonoScript[] allScripts;
		private List<Component>[] components; //List of components attached to each of the gameObjects

		private Editor defaultEditor; //UnityEditor.GameObjectInspector
		private bool[] foldouts; //used when targets.count > 1
		private GameObject[] gameObjects; //GameObjects being inspected
		private ReorderableList[] lists;
		private Transform[] transforms; //List containing the transforms of evey game object
		private bool IsTargetAmountLimitReached => targets.Length > 7;

		private static MonoScript[] AllScripts => allScripts = allScripts ?? MonoImporter.GetAllRuntimeMonoScripts();

		//When targets.Count == 1, Save the state of the foldout
		private static bool Foldout
		{
			get => EditorPrefs.GetBool(FoldoutPrefsKey, true);
			set => EditorPrefs.SetBool(FoldoutPrefsKey, value);
		}

		//This component is attached to a hidden GameObject, retrieved by its InstanceID
		private static Component CopiedComponent
		{
			get
			{
				var obj = EditorUtility.InstanceIDToObject(EditorPrefs.GetInt(CopiedComponentIDPrefsKey, 0));
				if (obj is Component component) return component;

				EditorPrefs.DeleteKey(CopiedComponentIDPrefsKey);
				return null;
			}
			set => EditorPrefs.SetInt(CopiedComponentIDPrefsKey, value.GetInstanceID());
		}


		private void OnEnable()
		{
			if (!IsUsed) return;
			//Initialization Stuff 
			InitializeCustomInspectors();
			defaultEditor = CreateEditor(targets, Type.GetType(GameObjectInspectorTypeName));
			if (IsTargetAmountLimitReached) return;

			gameObjects = new GameObject[targets.Length];
			lists = new ReorderableList[targets.Length];
			components = new List<Component>[targets.Length];
			transforms = new Transform[targets.Length];
			foldouts = new bool[targets.Length];
			for (var i = 0; i < targets.Length; i++)
			{
				InitializeList(i);
			}

			defaultEditor.GetType().GetMethod("OnEnable")?.Invoke(defaultEditor, null);
		}

		private void OnDisable()
		{
			//This avoids leaks
			DestroyImmediate(defaultEditor);
		}

		public void OnDestroy()
		{
			defaultEditor.GetType().GetMethod("OnDestroy")?.Invoke(defaultEditor, null);
		}

		public void OnSceneDrag(SceneView sceneView, int index)
		{
			defaultEditor.GetType().GetMethod("OnSceneDrag")?.Invoke(defaultEditor, new object[] {sceneView, index});
		}

		private void InitializeCustomInspectors()
		{
			Type type = Type.GetType(CustomEditorAttributesTypeName);
			IList list = type?.GetField("kSCustomMultiEditors", BindingFlags.Static | BindingFlags.NonPublic)
			                 ?.GetValue(null) as IList;
			if (list == null) return;
			for (int i = list.Count - 1; i >= 0; i--)
			{
				Type inspectedType = (Type) list[i].GetType()
				                                   .GetField("m_InspectedType",
				                                             BindingFlags.Instance | BindingFlags.Public)
				                                   ?.GetValue(list[i]);
				try
				{
					ExtendedEditors extendedEditor = extendedEditors.First(x => x.inspectedType == inspectedType);
					FieldInfo inspectorTypeField = list[i].GetType().GetField("m_InspectorType",
					                                                          BindingFlags.Instance |
					                                                          BindingFlags.Public);
					Type inspectorType = (Type) inspectorTypeField?.GetValue(list[i]);
					originalEditors.Add(new ExtendedEditors(inspectedType, inspectorType));

					if (inspectorType != extendedEditor.inspectorType)
						list[i].GetType().GetField("m_IsFallback", BindingFlags.Instance | BindingFlags.Public)
						       ?.SetValue(list[i], true);
				}
				catch
				{
					throw new Exception("Could not find the extended editor for " + inspectedType);
				}
			}
		}

		//This is called for every inspected gameObject
		private void InitializeList(int index)
		{
			foldouts[index] = true;
			var gameObject = gameObjects[index] = (GameObject) targets[index];
			var localComponent = components[index] = new List<Component>(gameObject.GetComponents<Component>());
			transforms[index] = gameObject.transform;

			//Remove the Transform component, as it's mostly unuseful
			localComponent.RemoveAt(0);

			var list = lists[index] = new ReorderableList(new List<Component>(localComponent), typeof(Component),
			                                              true, true, true, false);
			//No selected box is drawn
			list.drawElementBackgroundCallback += (rect, localComponentIndex, isActive, isFocused) => { };
			//For the header of the list, the transform is drawn
			list.drawHeaderCallback += rect => DrawElement(rect, gameObject.transform);
			list.drawElementCallback += (rect, localComponentIndex, isActive, isFocused) =>
				DrawElement(rect, localComponent[localComponentIndex]);

			//Open AddComponentWindow
			list.onAddDropdownCallback += (rect, _) =>
				{
					Type.GetType(AddComponentWindowTypeName)
					    ?.GetMethod("Show", BindingFlags.Static | BindingFlags.NonPublic)
					    ?.Invoke(null,
					             new object[]
					             {
						             new Rect(Screen.width / 2 - 230 / 2, rect.y, 230, 0),
						             Event.current.shift ? gameObjects : new[] {gameObjects[index]}
					             });
				};

			//Duplicated because... it works!
			list.onReorderCallback += internalList =>
				{
					//Move Down
					for (int i = 0; i < localComponent.Count; i++)
					{
						int indexOf = internalList.list.IndexOf(localComponent[i]);
						int difference = indexOf - i;
						if (difference <= 0) continue;

						for (int j = 0; j < Mathf.Abs(difference); j++)
						{
							ComponentUtility.MoveComponentDown(localComponent[i]);
						}
					}

					//Move Up
					localComponent = new List<Component>(gameObject.GetComponents<Component>());
					localComponent.RemoveAt(0);
					for (int i = localComponent.Count - 1; i >= 0; i--)
					{
						int indexOf = internalList.list.IndexOf(localComponent[i]);
						int difference = indexOf - i;
						if (difference >= 0) continue;

						for (int j = 0; j < Mathf.Abs(difference); j++)
						{
							ComponentUtility.MoveComponentUp(localComponent[i]);
						}
					}
				};
		}

		private void DrawElement(Rect originalRect, Component component)
		{
			Rect rect = new Rect(originalRect);
			rect.height = rect.width = 20;

			rect.x -= 4;
			if (component == null)
			{
				//Icon Handler
				rect.x += 30;
				EditorGUI.LabelField(rect, new GUIContent(Icons.WarningIcon));
				rect.x += 35;
				rect.width = originalRect.width;

				//Name Handler
				GUIStyle guiStyle = new GUIStyle(EditorStyles.boldLabel);
				guiStyle.normal.textColor = guiStyle.onNormal.textColor = new Color32(209, 137, 24, 255);
				EditorGUI.LabelField(rect, "Missing Component", guiStyle);
				return;
			}

			bool isRectTransform = component is RectTransform;
			bool isTransform = component is Transform;
			//Is the components common for every inspected GameObject?
			bool isCommon = targets.Length > 1 &&
			                components.ToList().TrueForAll(x =>
				                                               x.Exists(y => y != null &&
				                                                             y.GetType() == component.GetType()));
			if (isTransform)
			{
				rect.x += 14;
				rect.y -= 1;
			}

			//Extended Handler
			Type extendedEditorType = null;
			for (int i = 0; i < extendedEditors.Count; i++)
			{
				if (extendedEditors[i].inspectedType != component.GetType()) continue;
				extendedEditorType = extendedEditors[i].inspectorType;
				break;
			}

			if (extendedEditorType != null)
			{
				var editor = CreateEditor(component);

				ExtendedEditor extendedEditor = editor as ExtendedEditor;
				if (extendedEditor != null)
				{
					GUI.enabled = extendedEditor.IsExtended;
					EditorGUI.LabelField(rect, new GUIContent(Icons.FavoriteIcon, extendedEditor.IsExtended
						                                          ? "Extended Inspector"
						                                          : "Normal Inspector"));
					GUI.enabled = true;
					if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
						extendedEditor.IsExtended = !extendedEditor.IsExtended;
				}

				DestroyImmediate(editor);
			}

			rect.x += 18;

			//Foldout Handler
			rect.y += 2;
			EditorGUI.BeginChangeCheck();
			InternalEditorUtility.SetIsInspectorExpanded(component, EditorGUI.Foldout(rect, InternalEditorUtility
					                                             .GetIsInspectorExpanded(component),
				                                             string.Empty));
			//When Expanding/Collapsing the component, rebuild the inspector
			if (EditorGUI.EndChangeCheck())
			{
				bool value = InternalEditorUtility.GetIsInspectorExpanded(component);
				Component[] targetComponents = GetTargetComponents(component,
				                                                   GetTargetComponentMode.IncludeTransforms |
				                                                   GetTargetComponentMode.AllowMultiComponent |
				                                                   GetTargetComponentMode.AllowMultiGameObject);
				foreach (var targetComponent in targetComponents)
					InternalEditorUtility.SetIsInspectorExpanded(targetComponent, value);

				typeof(EditorUtility).GetMethod("ForceRebuildInspectors", BindingFlags.Static | BindingFlags.NonPublic)
				                     .Invoke(null, null);
			}

			//Icon Handler
			rect.y -= 2;
			rect.x += 12;
			Texture icon = EditorGUIUtility.ObjectContent(component, component.GetType()).image;
			EditorGUI.LabelField(rect, new GUIContent(icon));

			//Enable/Disable Handler
			rect.x += 20;
			rect.y += 2;
			if (EditorUtility.GetObjectEnabled(component) != -1)
			{
				bool oldValue = EditorUtility.GetObjectEnabled(component) == 1;
				bool newValue = EditorGUI.Toggle(rect, oldValue);
				if (oldValue != newValue)
				{
					Component[] targetComponents =
						GetTargetComponents(component,
						                    GetTargetComponentMode.AllowMultiComponent |
						                    GetTargetComponentMode.AllowMultiGameObject |
						                    GetTargetComponentMode.ExcludeDifferentTypes);
					foreach (var t in targetComponents)
					{
						Undo.RecordObject(t, (newValue ? "Enable" : "Disable ") + t.GetType().Name);
						EditorUtility.SetObjectEnabled(t, newValue);
					}
				}
			}

			//Name & Component Preview
			rect.x += 14;
			rect.width = originalRect.width - 125;
			GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
			//Highlight common components
			if (isCommon)
				style.normal.textColor = style.onNormal.textColor = new Color32(0, 128, 0, 255);
			EditorGUI.LabelField(rect, component.GetType().Name, style);
			if (rect.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseUp &&
			    Event.current.button == 1)
				ComponentPreviewWindow.Show(component);

			rect.x = originalRect.width + (isTransform ? 0 : 14) + 3;
			rect.height = rect.width = 16;

			//Remove Button
			if (!isTransform || isRectTransform)
			{
				if (isRectTransform)
					rect.y -= 2;
				if (GUI.Button(rect, new GUIContent(Icons.CloseIcon, "Remove Component"), GUIStyle.none))
				{
					Component[] targetComponents =
						GetTargetComponents(component,
						                    GetTargetComponentMode.AllowMultiComponent |
						                    GetTargetComponentMode.AllowMultiGameObject |
						                    GetTargetComponentMode.ExcludeDifferentTypes);
					foreach (var t in targetComponents)
					{
						string dependants = GetComponentDependants(t);
						if (dependants == string.Empty)
						{
							Undo.SetCurrentGroupName("Remove " + t.GetType().Name);
							Undo.DestroyObjectImmediate(t);
						}
						else
							EditorUtility.DisplayDialog("Can't remove component",
							                            "Can't remove " + t.GetType().Name +
							                            " because " +
							                            dependants + " depends on it", "Ok");
					}

					GUIUtility.ExitGUI();
				}

				if (isRectTransform)
					rect.y += 2;
			}

			if (isTransform)
				rect.y -= 1;


			//Reset Button
			rect.x -= 18;
			rect.width = 20;
			rect.height += 4;
			EditorGUI.LabelField(rect, new GUIContent(Icons.ResetIcon, "Reset Component"));
			if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
			{
				Component[] targetComponents = GetTargetComponents(component,
				                                                   GetTargetComponentMode.AllowMultiComponent |
				                                                   GetTargetComponentMode.AllowMultiGameObject |
				                                                   GetTargetComponentMode.ExcludeDifferentTypes |
				                                                   GetTargetComponentMode.IncludeTransforms);
				foreach (var t in targetComponents)
				{
					Undo.RecordObject(t, "Reset " + t.GetType().Name);
					Unsupported.SmartReset(t);
					//			GameObject temp = EditorUtility.CreateGameObjectWithHideFlags("Dummy",HideFlags.HideAndDontSave);
					//			bool isRectTransform = component is RectTransform;
					//			if (isRectTransform)
					//				temp.AddComponent<RectTransform>();
					//			EditorUtility.CopySerialized(isTransform?isRectTransform?temp.GetComponent<RectTransform>():temp.transform:temp.AddComponent(component.GetType()),component);
					//			DestroyImmediate(temp);
				}
			}

			rect.height -= 4;

			//Copy Button
			rect.x -= 17;
			rect.height += 1;
			EditorGUI.LabelField(rect, new GUIContent(Icons.DocumentIcon));
			rect.x += 2;
			rect.y += 2;
			EditorGUI.LabelField(rect, new GUIContent(Icons.DocumentIcon, "Copy Component"));
			//Create hidden game object and attach the copied components to it
			if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
			{
				var dummy = GameObject.Find(DummyName);
				if (dummy != null)
					DestroyImmediate(dummy);
				dummy = EditorUtility
					.CreateGameObjectWithHideFlags(DummyName, HideFlags.DontSave | HideFlags.HideInHierarchy);
				ComponentUtility.CopyComponent(component);
				if (component is Transform || isRectTransform)
					ComponentUtility.PasteComponentValues(dummy.transform);
				ComponentUtility.PasteComponentAsNew(dummy);
				CopiedComponent = dummy.GetComponent(component.GetType());
				ComponentUtility.CopyComponent(CopiedComponent);
				Undo.ClearUndo(dummy);
			}

			rect.height -= 1;

			//Inspect
			rect.x -= 18;
			rect.y -= 2;
			foreach (var t in AllScripts)
			{
				if (t == null)
					continue;
				if (t.GetClass() != component.GetType()) continue;

				string path = AssetDatabase.GetAssetPath(t);
				rect.height += 4;
				rect.width += 2;
				//Only non built-in, non .dll scripts can be edited
				if (path.EndsWith(".cs") || path.EndsWith(".js"))
				{
					EditorGUI.LabelField(rect, new GUIContent(Icons.EditIcon, "Inspect"));
					if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
					{
						if (Event.current.button == 0)
							AssetDatabase.OpenAsset(t);
						else
							EditorGUIUtility.PingObject(t);
					}
				}

				rect.height -= 4;
				rect.width -= 2;
			}

			rect.y += 2;

			//Paste Values Button
			rect.height += 1;
			rect.x -= 16;
			if (CopiedComponent != null && CopiedComponent.GetType() == component.GetType())
			{
				rect.y -= 3;
				EditorGUI.LabelField(rect, new GUIContent(Icons.FolderIcon));
				rect.x += 2;
				rect.y += 3;
				EditorGUI.LabelField(rect, new GUIContent(Icons.DocumentIcon, "Paste Component Values"));
				if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
				{
					Component[] targetComponents =
						GetTargetComponents(component,
						                    GetTargetComponentMode.AllowMultiComponent |
						                    GetTargetComponentMode.AllowMultiGameObject |
						                    GetTargetComponentMode.ExcludeDifferentTypes |
						                    GetTargetComponentMode.IncludeTransforms);
					foreach (var t in targetComponents)
						ComponentUtility.PasteComponentValues(t);
				}

				rect.x -= 2;
			}
		}

		protected override void OnHeaderGUI()
		{
			if (!IsUsed || IsTargetAmountLimitReached)
			{
				base.OnHeaderGUI();
				return;
			}

			//Paint the header of the default editor, then add custom functionality
			defaultEditor.DrawHeader();

			GUILayout.Space(2);
			GUIStyle style = new GUIStyle(EditorStyles.foldout)
			{
				fontStyle = FontStyle.Bold
			};
			for (int i = 0; i < targets.Length; i++)
			{
				EditorGUILayout.BeginHorizontal();
				bool foldout = (targets.Length > 1 ? foldouts[i] : Foldout);
				//A foldout is created for every inspected object
				//When there's only one gameObject, the state of the foldout is saved to (and retrieved from) EditorPrefs
				foldout = EditorGUI.Foldout(EditorGUILayout.GetControlRect(GUILayout.Height(13)), foldout,
				                            (targets.Length > 1 ? gameObjects[i].name + " " : string.Empty) +
				                            "Component Tool Panel", true, style);
				if (targets.Length > 1)
				{
					if (Event.current.shift)
						for (int j = 0; j < targets.Length; j++)
							foldouts[j] = foldout;
					else
						foldouts[i] = foldout;
				}
				else
					Foldout = foldout;

				//Draw the PasteComponentAsNew button
				if (foldout && CopiedComponent != null && CopiedComponent.GetType() != typeof(Transform) &&
				    (CopiedComponent.GetType() != typeof(RectTransform) ||
				     CopiedComponent is RectTransform &&
				     gameObjects[i].GetComponent<RectTransform>() == null))
				{
					Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(20));
					rect.height = 20;
					rect.y -= 1;
					EditorGUI.LabelField(rect, new GUIContent(Icons.FolderIcon));
					rect.x += 2;
					rect.y += 3;
					EditorGUI.LabelField(rect, new GUIContent(Icons.DocumentIcon, "Paste Component"));
					if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
					{
						//Shift for action on all GameObjects
						if (Event.current.shift)
						{
							foreach (var t in gameObjects)
							{
								//Check for [DisallowMultipleComponent] Attribute
								bool result = ComponentUtility.PasteComponentAsNew(t);
								if (!result)
									EditorUtility.DisplayDialog("Can't add the same component multiple times!",
									                            "The component " + CopiedComponent.GetType().Name +
									                            " can't be added because " +
									                            t.name +
									                            " already contains the same component.", "Cancel");
							}
						}
						else
						{
							//Check for [DisallowMultipleComponent] Attribute
							bool result = ComponentUtility.PasteComponentAsNew(gameObjects[i]);
							if (!result)
								EditorUtility.DisplayDialog("Can't add the same component multiple times!",
								                            "The component " + CopiedComponent.GetType().Name +
								                            " can't be added because " +
								                            gameObjects[i].name +
								                            " already contains the same component.", "Cancel");
						}
					}
				}

				EditorGUILayout.EndHorizontal();
				if (foldout)
				{
					float height = lists[i].elementHeight * components[i].Count + lists[i].headerHeight +
					               20; //20 = extra space for add button
					if (components[i].Count == 0)
						height += lists[i].elementHeight;
					Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(height));
					rect.x += 11;
					rect.width -= 11;
					//Handle DragAndDrop scripts
					if (rect.Contains(Event.current.mousePosition))
					{
						switch (Event.current.type)
						{
							case EventType.DragUpdated:
							case EventType.DragPerform:
								List<MonoScript> scripts =
									(from obj in DragAndDrop.objectReferences
									 let script = obj as MonoScript
									 where script != null && script.GetClass() != null &&
									       script.GetClass().IsSubclassOf(typeof(Component))
									 select obj as MonoScript).ToList();

								//When shift is held, add script to every selected GameObject
								DragAndDrop.visualMode = scripts.Count == 0 ? DragAndDropVisualMode.Rejected :
									Event.current.shift ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Link;
								if (Event.current.type == EventType.DragPerform)
								{
									DragAndDrop.AcceptDrag();
									if (Event.current.shift)
									{
										//Also, when selecting a prefab and an instance of that prefab, prevent the instance from getting duplicated component addition
										foreach (GameObject gameObject in gameObjects)
										{
											if (gameObjects.Contains(PrefabUtility
												                         .GetCorrespondingObjectFromSource(gameObject)))
												continue;
											foreach (MonoScript script in scripts)
												Undo.AddComponent(gameObject, script.GetClass());
										}
									}
									else
									{
										foreach (MonoScript script in scripts)
											Undo.AddComponent(gameObjects[i], script.GetClass());
									}
								}

								break;
						}
					}

					lists[i].DoList(rect);
				}
			}

			//Handle CopiedComponent box
			if ((Foldout || targets.Length > 1) && CopiedComponent != null)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.indentLevel++;
				Rect rect = GUILayoutUtility.GetRect(new GUIContent("Copied Component"), GUI.skin.label,
				                                     GUILayout.Height(18), GUILayout.Width(140));
				rect.y += 2;
				EditorGUI.LabelField(rect, "Copied Component");
				Color oldColor = GUI.color;
				GUI.color = Color.grey;
				EditorGUILayout.LabelField(GUIContent.none, EditorStyles.helpBox, GUILayout.Height(18));
				GUI.color = oldColor;
				rect = GUILayoutUtility.GetLastRect();
				EditorGUI.LabelField(rect,
				                     new GUIContent(CopiedComponent.GetType().Name, EditorGUIUtility
					                                    .ObjectContent(CopiedComponent, CopiedComponent.GetType())
					                                    .image));
				EditorGUI.indentLevel--;
				EditorGUILayout.EndHorizontal();
			}
		}

		//Utility method for getting components dependants
		private string GetComponentDependants(Component component)
		{
			Type type = component.GetType();
			Component[] components1 = component.GetComponents<Component>();
			HashSet<string> dependants = new HashSet<string>();
			bool hasBuiltInDependants = componentDependencies.Keys.Contains(type);
			for (int i = 1; i < components1.Length; i++)
			{
				if (components1[i] == component)
					continue;
				if (components1[i] == null)
					continue;

				//First, check if any other built-in attached component depends on it
				if (hasBuiltInDependants && componentDependencies[type].Contains(components1[i].GetType()))
					dependants.Add(components1[i].GetType().Name);

				//Then, find [RequireComponent] Attributes
				RequireComponent[] attribute =
					(RequireComponent[]) components1[i].GetType().GetCustomAttributes(typeof(RequireComponent), true);
				if (attribute.Length == 0)
					continue;
				if (type.IsAssignableFrom(attribute[0].m_Type0) ||
				    (attribute[0].m_Type1 != null && type.IsAssignableFrom(attribute[0].m_Type1)) ||
				    (attribute[0].m_Type2 != null && type.IsAssignableFrom(attribute[0].m_Type2)))
					dependants.Add(components1[i].GetType().Name);
			}

			//And convert to string for better display 
			string text = string.Empty;
			for (int i = 0; i < dependants.Count; i++)
			{
				text += dependants.ToList()[i];
				if (i != dependants.Count - 1)
					text += ", ";
			}

			return text;
		}

		private Component[] GetTargetComponents(Component mainTarget, GetTargetComponentMode mode)
		{
			List<Component> list = new List<Component>();
			if (mode == GetTargetComponentMode.None)
				return list.ToArray();

			bool multiComponent = (Event.current.control || Event.current.command) &&
			                      (mode & GetTargetComponentMode.AllowMultiComponent) ==
			                      GetTargetComponentMode.AllowMultiComponent;
			bool multiGameObject = Event.current.shift && ((mode & GetTargetComponentMode.AllowMultiGameObject) ==
			                                               GetTargetComponentMode.AllowMultiGameObject);

			for (int i = 0; i < gameObjects.Length; i++)
			{
				if (gameObjects[i] != mainTarget.gameObject && !multiGameObject)
					continue;

				// "<=" made on purpose in order to include Transforms if needed
				for (int j = 0; j <= components[i].Count; j++)
				{
					Component component;
					try
					{
						component = components[i][j];
					}
					catch
					{
						if ((mode & GetTargetComponentMode.IncludeTransforms) !=
						    GetTargetComponentMode.IncludeTransforms)
							break;
						component = transforms[i];
					}

					if (component == null)
						continue;
					if (mainTarget.GetType() != component.GetType() &&
					    (mode & GetTargetComponentMode.ExcludeDifferentTypes) ==
					    GetTargetComponentMode.ExcludeDifferentTypes)
						continue;
					if (!multiComponent && mainTarget.GetType() != component.GetType())
						continue;
					list.Add(component);
					if (!multiComponent)
						break;
				}
			}

			return list.ToArray();
		}

		public override void OnInspectorGUI()
		{
			if (!IsUsed || IsTargetAmountLimitReached)
			{
				base.OnInspectorGUI();
				return;
			}

			defaultEditor.OnInspectorGUI();
		}

		public override void ReloadPreviewInstances()
		{
			if (!IsUsed || IsTargetAmountLimitReached)
			{
				base.ReloadPreviewInstances();
				return;
			}

			defaultEditor.OnInspectorGUI();
		}

		public override bool HasPreviewGUI()
		{
			if (!IsUsed || IsTargetAmountLimitReached)
				return base.HasPreviewGUI();
			return defaultEditor.HasPreviewGUI();
		}

		public override void OnPreviewSettings()
		{
			if (!IsUsed || IsTargetAmountLimitReached)
			{
				base.OnPreviewSettings();
				return;
			}

			defaultEditor.OnPreviewSettings();
		}

		public override Texture2D RenderStaticPreview(
			string assetPath, UnityEngine.Object[] subAssets, int width, int height)
		{
			if (!IsUsed || IsTargetAmountLimitReached)
				return base.RenderStaticPreview(assetPath, subAssets, width, height);
			return defaultEditor.RenderStaticPreview(assetPath, subAssets, width, height);
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if (!IsUsed || IsTargetAmountLimitReached)
			{
				base.OnPreviewGUI(r, background);
				return;
			}

			defaultEditor.OnPreviewGUI(r, background);
		}

		[Flags]
		private enum GetTargetComponentMode
		{
			None = 0,
			IncludeTransforms = 1,
			ExcludeDifferentTypes = 2,
			AllowMultiComponent = 4,
			AllowMultiGameObject = 8
		}
	}
}