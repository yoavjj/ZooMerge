using UnityEditor;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


[InitializeOnLoad]
class Hierarchy
{
	private static HierarchyPrefrences prefrences;
	public static HierarchyPrefrences Prefrences
	{
		get
		{
			return prefrences;
		}
	}

	private static int updateCount;

	private static bool hasValidItems = false;

	private static List<HierarchyItem> groups;

	//
	public static void Init()
	{
		prefrences = HierarchyPrefrences.Get();
		groups = new List<HierarchyItem>();
		for (int i = 0; i < prefrences.types.Count; i++)
		{
			if (prefrences.types[i] == null)
				continue; // Skip null types to avoid crashing

			Texture image = Hierarchy.IconTexture(prefrences.types[i]);
			groups.Add(new HierarchyItem(prefrences.types[i], new List<HierarchyItemObjectState>(), image));
		}
	}
	//
	static Hierarchy()
	{
		Init();
		EditorApplication.update += Update;
		EditorApplication.hierarchyWindowItemOnGUI += HierarchyItemCB;
		EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
	}

	private static void OnPlayModeStateChanged(PlayModeStateChange state)
	{
		// Force refresh right when play mode changes so icons don't "disappear"
		if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
		{
			updateCount = 0;
			hasValidItems = false;
			GetObjects();                 // do a scan immediately
			EditorApplication.RepaintHierarchyWindow();
		}
	}
	/*public static Texture2D IconTexture (Type type)
	{
		var image = AssetPreview.GetMiniTypeThumbnail (type);
		if (image == null) {
			var p = string.Format ("Assets/00_Trash/{0} Icon.png", type.Name);
			image = AssetDatabase.LoadAssetAtPath<Texture2D> (p);
		}
		if (image == null && (typeof(MonoBehaviour).IsAssignableFrom (type))) {
			image = EditorGUIUtility.FindTexture("cs Script Icon");
		}
		return image;
	} */

	private static Dictionary<Type, Texture2D> typeToIconMap = new Dictionary<Type, Texture2D>();

	public static Texture2D IconTexture(Type type)
	{
		if (typeToIconMap.TryGetValue(type, out Texture2D iconTexture))
		{
			return iconTexture;
		}

		iconTexture = AssetPreview.GetMiniTypeThumbnail(type);
		if (iconTexture == null)
		{
			var path = $"Assets/00_Trash/{type.Name} Icon.png";
			iconTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
		}
		if (iconTexture == null && typeof(MonoBehaviour).IsAssignableFrom(type))
		{
			iconTexture = EditorGUIUtility.FindTexture("cs Script Icon");
		}

		typeToIconMap[type] = iconTexture;
		return iconTexture;
	}


	static void HierarchyWindowChanged()
	{
		Debug.LogError("HierarchyWindowChanged");
	}

	static void GetObjects()
	{
		if (Application.isPlaying)
		{
			int skip = Mathf.Max(1, prefrences.frameSkip); // use preference
			updateCount = (updateCount + 1) % skip;

			// ✅ If we don't have valid items yet, don't throttle (draw icons immediately)
			if (updateCount != 0 && hasValidItems)
				return;
		}
	
		GameObject[] go = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
	
		foreach (var group in groups)
			group.objects.Clear();

		hasValidItems = false;

		foreach (GameObject g in go)
		{
			var componenets = g.GetComponents<Component>();
			foreach (var c in componenets)
			{
				foreach (var item in groups)
				{
					if (item.type.IsInstanceOfType(c))
					{
						var enabled = true;
						if (c is Behaviour) enabled = ((Behaviour)c).enabled;
						else if (c is Renderer) enabled = ((Renderer)c).enabled;
						else if (c is Collider) enabled = ((Collider)c).enabled;

						item.objects.Add(new HierarchyItemObjectState(g.GetInstanceID(), enabled));
						hasValidItems = true;
					}
				}
			}
		}
	}

	static void Update()
	{

		if (!prefrences.runInPlayMode && EditorApplication.isPlaying)
		{
			return;
		}
		GetObjects();
	}

	private static void DrawAlignedRight(int instanceID, Rect selectionRect)
	{
		selectionRect.x = selectionRect.x + selectionRect.width - 18;
		selectionRect.width = selectionRect.height;

		foreach (var item in groups)
		{
			for (int index = 0; index < item.objects.Count; index++)
			{
				var objectState = item.objects[index];
				if (objectState.instanceId == instanceID)
				{
					if (!objectState.isEnabled)
						GUI.color = prefrences.disabledColorComponent;
					GUI.DrawTexture(selectionRect, item.texture, ScaleMode.ScaleToFit);
					selectionRect.x += -selectionRect.width;
					GUI.color = Color.white;
				}
			}
		}
	}

	private static void DrawAlignedLeft(int instanceID, Rect selectionRect)
	{
		var obj = EditorUtility.InstanceIDToObject(instanceID);
		if (obj)
		{
			var content = new GUIContent(obj.name);
			selectionRect.x += GUI.skin.box.CalcSize(content).x;
		}

		selectionRect.width = selectionRect.height;

		foreach (var item in groups)
		{
			for (int index = 0; index < item.objects.Count; index++)
			{
				var objectState = item.objects[index];
				if (objectState.instanceId == instanceID)
				{
					if (!objectState.isEnabled)
						GUI.color = prefrences.disabledColorComponent;
					GUI.DrawTexture(selectionRect, item.texture, ScaleMode.ScaleToFit);
					selectionRect.x += (selectionRect.width);
					GUI.color = Color.white;
				}
			}
		}
	}

	private static void DrawActivationToggle(int instanceID, ref Rect rowRect)
	{
		if (!prefrences.showActivationToggle)
			return;
		var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
		if (obj)
		{
			var togglRect = new Rect(rowRect);
			togglRect.x = togglRect.x + togglRect.width - 16;
			togglRect.width = togglRect.height;
			if (obj.transform.parent != null && !obj.transform.parent.gameObject.activeInHierarchy)
			{
				GUI.color = prefrences.disabledColorToggle;
			}
			rowRect.width -= togglRect.width;
			var active = GUI.Toggle(togglRect, obj.activeSelf, string.Empty);
			if (active != obj.activeSelf)
			{
				Undo.RecordObject(obj, "Changed object state from hierarchy");
				obj.SetActive(active);
			}

			GUI.color = Color.white;

		}
	}

	static void HierarchyItemCB(int instanceID, Rect selectionRect)
	{
		if (!prefrences.runInPlayMode && EditorApplication.isPlaying)
		{
			return;
		}

		DrawActivationToggle(instanceID, ref selectionRect);

		if (!hasValidItems)
		{
			return;
		}

		if (prefrences.alignedLeft)
			DrawAlignedLeft(instanceID, selectionRect);
		else
			DrawAlignedRight(instanceID, selectionRect);
	}


	private struct HierarchyItemObjectState
	{
		public int instanceId;
		public bool isEnabled;

		public HierarchyItemObjectState(int instanceId, bool isEnabled)
		{
			this.isEnabled = isEnabled;
			this.instanceId = instanceId;
		}
	}

	private struct HierarchyItem
	{
		public List<HierarchyItemObjectState> objects;
		public Type type;
		public Texture texture;


		public HierarchyItem(Type type, List<HierarchyItemObjectState> instanceIds, Texture texture)
		{
			this.type = type;
			this.objects = instanceIds;
			this.texture = texture;
		}
	}
}