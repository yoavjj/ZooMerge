using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditorInternal;
using System;
using TMPro;

public class windowHierarchyPrefrences : EditorWindow
{
	private HierarchyPrefrences prefrences;
	private Type[] options = new Type[] {
		typeof(Camera),
		typeof(Animator),
		typeof(Image),
		typeof(RawImage),
		typeof(Mask),
		typeof(Text),
		typeof(HorizontalLayoutGroup),
		typeof(VerticalLayoutGroup),
		typeof(GridLayoutGroup),
		typeof(ContentSizeFitter),
		typeof(LayoutElement),
		typeof(Canvas),
		typeof(CanvasGroup),
		typeof(Scrollbar),
		typeof(ScrollRect),
		typeof(Slider),
		typeof(Toggle),
		typeof(Dropdown),
		typeof(Button),
		typeof(MeshRenderer),
		typeof(SpriteRenderer),
		typeof(TextMesh),
		typeof(SpriteMask),
		typeof(ParticleSystem),
		typeof(BoxCollider),
		typeof(CapsuleCollider),
		typeof(MeshCollider),
		typeof(CapsuleCollider2D),
		typeof(SphereCollider),
		typeof(CircleCollider2D),
		typeof(PolygonCollider2D),
		typeof(Light),
		typeof(AudioSource),
		typeof(UnityEngine.EventSystems.EventSystem),
		typeof(TextMeshPro),
		typeof(TextMeshProUGUI),
	};



	[MenuItem("Window/Editor/Hierarchy Icons", false, 30)]
	public static void ShowWindow()
	{
		var window = GetWindow<windowHierarchyPrefrences>();
		window.ShowPopup();
	}


	private ReorderableList typesOrder;

	private Vector2 scroll;


	private MonoScript customComponent;

	private void drawTypesOrderElement(Rect rect, int index, bool isActive, bool isForced)
	{
		var item = ((Type)typesOrder.list[index]);
		GUI.Label(rect, GetContentForType(item, item.Name));
	}

	public static GUIContent GetContentForType(Type t, string name = null)
	{
		var content = EditorGUIUtility.ObjectContent(null, t);
		if (content == null)
			return null;
		if (name != null)
			content.text = name;
		content.image = Hierarchy.IconTexture(t);
		return content;
	}


	private void onReorderTypesOrderCallback(ReorderableList list)
	{
		prefrences.Save();
		Hierarchy.Init();
	}

	private void drawTypesOrderHeader(Rect rect)
	{
		GUI.Label(rect, "Icons Order");
	}

	bool drawDragArea = false;

	void OnGUI()
	{
		prefrences = Hierarchy.Prefrences;
		Event evt = Event.current;
		if (typesOrder == null)
		{
			typesOrder = new ReorderableList(prefrences.types, typeof(Type));
			typesOrder.displayAdd = false;
			typesOrder.drawHeaderCallback = drawTypesOrderHeader;
			typesOrder.drawElementCallback = drawTypesOrderElement;
			typesOrder.onReorderCallback = onReorderTypesOrderCallback;
		}
		GUILayout.BeginVertical();
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		var runInPlayMode = EditorGUILayout.ToggleLeft("Run in Play mode", prefrences.runInPlayMode);
		if (runInPlayMode != prefrences.runInPlayMode)
		{
			prefrences.runInPlayMode = runInPlayMode;
			prefrences.Save();
			Hierarchy.Init();
		}
		var alignedLeft = EditorGUILayout.ToggleLeft("Left aligned components icons", prefrences.alignedLeft);
		if (alignedLeft != prefrences.alignedLeft)
		{
			prefrences.alignedLeft = alignedLeft;
			prefrences.Save();
			Hierarchy.Init();
		}
		var showActivationToggle = EditorGUILayout.ToggleLeft("Activation Toggle", prefrences.showActivationToggle);
		if (showActivationToggle != prefrences.showActivationToggle)
		{
			prefrences.showActivationToggle = showActivationToggle;
			prefrences.Save();
			Hierarchy.Init();
		}

		var disabledColorComponent = EditorGUILayout.ColorField("components", prefrences.disabledColorComponent);
		if (disabledColorComponent != prefrences.disabledColorComponent)
		{
			prefrences.disabledColorComponent = disabledColorComponent;
			prefrences.Save();
			Hierarchy.Init();
		}

		var disabledColorToggle = EditorGUILayout.ColorField("toggle", prefrences.disabledColorToggle);
		if (disabledColorToggle != prefrences.disabledColorToggle)
		{
			prefrences.disabledColorToggle = disabledColorToggle;
			prefrences.Save();
			Hierarchy.Init();
		}

		GUILayout.FlexibleSpace();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		scroll = GUILayout.BeginScrollView(scroll, GUI.skin.textArea);
		GUILayout.BeginHorizontal();
		GUILayout.BeginVertical();
		GUILayout.BeginHorizontal();
		/* if (GUILayout.Button ("Select All")) {
			for (int i = 0; i < options.Length; i++) {
				var included = prefrences.types.Contains (options [i]);
				if (!included) {
					prefrences.types.Add (options [i]);
				}
			}
			prefrences.Save ();
			Hierarchy.Init ();
		}
		if (GUILayout.Button ("Reset")) {
			prefrences.types.Clear ();
			prefrences.Save ();
			Hierarchy.Init ();
		}
		*/

		GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.fontSize = 9;
		buttonStyle.fontStyle = FontStyle.Bold;
		buttonStyle.normal.textColor = Color.white;
		buttonStyle.normal.background = Texture2D.whiteTexture;

		if (GUILayout.Button("Select All", buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(25)))
		{
			for (int i = 0; i < options.Length; i++)
			{
				var included = prefrences.types.Contains(options[i]);
				if (!included)
				{
					prefrences.types.Add(options[i]);
				}
			}
			prefrences.Save();
			Hierarchy.Init();
		}

		if (GUILayout.Button("Reset", buttonStyle, GUILayout.ExpandWidth(true), GUILayout.Height(25)))
		{
			prefrences.types.Clear();
			prefrences.Save();
			Hierarchy.Init();
		}


		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		for (int i = 0; i < options.Length; i++)
		{
			var included = prefrences.types.Contains(options[i]);
			var content = GetContentForType(options[i], options[i].Name);
			var includedAfter = EditorGUILayout.ToggleLeft(content, included);
			if (includedAfter != included)
			{
				if (included)
				{
					prefrences.types.Remove(options[i]);
				}
				else
				{
					prefrences.types.Add(options[i]);
				}
				prefrences.Save();
				Hierarchy.Init();
			}
		}
		GUILayout.EndVertical();
		GUILayout.BeginVertical(GUILayout.MinWidth(300));
		typesOrder.DoLayoutList();
		GUILayout.EndVertical();
		GUILayout.EndHorizontal();
		GUILayout.EndScrollView();
		GUILayout.EndVertical();
		var dragAndDropRect = GUILayoutUtility.GetLastRect();
		switch (evt.type)
		{
			case EventType.DragUpdated:
			case EventType.DragPerform:
				if (!dragAndDropRect.Contains(evt.mousePosition))
					break;
				DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				drawDragArea = true;
				if (evt.type == EventType.DragPerform)
				{
					DragAndDrop.AcceptDrag();
					foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences)
					{
						if (dragged_object is MonoScript)
						{
							var type = ((MonoScript)dragged_object).GetClass();
							if (!prefrences.types.Contains(type))
							{
								prefrences.types.Add(type);
								prefrences.Save();
								Hierarchy.Init();
							}
						}
					}
				}
				break;
		}
		if (drawDragArea)
		{
			GUI.Box(dragAndDropRect, "Add component");
		}
		if (evt.type == EventType.Repaint)
		{
			drawDragArea = false;
		}
	}
}

[Serializable]
public class HierarchyPrefrences
{
	private const string SAVE_KEY = "hierarchy_prefrences";
	private static HierarchyPrefrences instance;

	public bool runInPlayMode = false;

	public bool alignedLeft = false;

	public bool showActivationToggle = true;

	public int frameSkip = 30;

	public Color disabledColorComponent = new Color(1f, 1f, 1f, 0.5f);

	public Color disabledColorToggle = new Color(1f, 1f, 1f, 0.5f);

	[SerializeField]
	private List<string> components = new List<string>();

	[NonSerializedAttribute]
	public List<Type> types = new List<Type>();

	public event Action OnChange;

	public void Save()
	{
		components = new List<string>(types.Count);
		for (int i = 0; i < types.Count; i++)
		{

			components.Add(types[i].ToString() + ", " + types[i].Assembly.GetName().Name);
		}
		EditorPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(this));
		if (OnChange != null)
		{
			OnChange();
		}
	}

	private void Init()
	{
		types = new List<Type>(components.Count);
		for (int i = 0; i < components.Count; i++)
		{
			var t = Type.GetType(components[i]);
			types.Add(t);
		}
	}
	public static HierarchyPrefrences Get()
	{
		if (instance == null)
		{
			if (EditorPrefs.HasKey(SAVE_KEY))
			{
				try
				{
					instance = JsonUtility.FromJson<HierarchyPrefrences>(EditorPrefs.GetString(SAVE_KEY));
					instance.Init();
				}
				catch (Exception)
				{
					instance = new HierarchyPrefrences();
				}
			}
			else
			{
				instance = new HierarchyPrefrences();
			}
		}
		return instance;
	}
}
