extern "C" void RegisterStaticallyLinkedModulesGranular()
{
	void RegisterModule_SharedInternals();
	RegisterModule_SharedInternals();

	void RegisterModule_Core();
	RegisterModule_Core();

	void RegisterModule_AndroidJNI();
	RegisterModule_AndroidJNI();

	void RegisterModule_Animation();
	RegisterModule_Animation();

	void RegisterModule_Audio();
	RegisterModule_Audio();

	void RegisterModule_GraphicsStateCollectionSerializer();
	RegisterModule_GraphicsStateCollectionSerializer();

	void RegisterModule_Grid();
	RegisterModule_Grid();

	void RegisterModule_HierarchyCore();
	RegisterModule_HierarchyCore();

	void RegisterModule_HotReload();
	RegisterModule_HotReload();

	void RegisterModule_AssetBundle();
	RegisterModule_AssetBundle();

	void RegisterModule_Input();
	RegisterModule_Input();

	void RegisterModule_InputLegacy();
	RegisterModule_InputLegacy();

	void RegisterModule_IMGUI();
	RegisterModule_IMGUI();

	void RegisterModule_InputForUI();
	RegisterModule_InputForUI();

	void RegisterModule_JSONSerialize();
	RegisterModule_JSONSerialize();

	void RegisterModule_ParticleSystem();
	RegisterModule_ParticleSystem();

	void RegisterModule_Physics();
	RegisterModule_Physics();

	void RegisterModule_Physics2D();
	RegisterModule_Physics2D();

	void RegisterModule_Properties();
	RegisterModule_Properties();

	void RegisterModule_RuntimeInitializeOnLoadManagerInitializer();
	RegisterModule_RuntimeInitializeOnLoadManagerInitializer();

	void RegisterModule_SpriteShape();
	RegisterModule_SpriteShape();

	void RegisterModule_Tilemap();
	RegisterModule_Tilemap();

	void RegisterModule_SpriteMask();
	RegisterModule_SpriteMask();

	void RegisterModule_TLS();
	RegisterModule_TLS();

	void RegisterModule_Terrain();
	RegisterModule_Terrain();

	void RegisterModule_TextRendering();
	RegisterModule_TextRendering();

	void RegisterModule_TextCoreFontEngine();
	RegisterModule_TextCoreFontEngine();

	void RegisterModule_TextCoreTextEngine();
	RegisterModule_TextCoreTextEngine();

	void RegisterModule_UI();
	RegisterModule_UI();

	void RegisterModule_UIElements();
	RegisterModule_UIElements();

	void RegisterModule_UnityAnalyticsCommon();
	RegisterModule_UnityAnalyticsCommon();

	void RegisterModule_Subsystems();
	RegisterModule_Subsystems();

	void RegisterModule_UnityWebRequest();
	RegisterModule_UnityWebRequest();

	void RegisterModule_UnityWebRequestAssetBundle();
	RegisterModule_UnityWebRequestAssetBundle();

	void RegisterModule_VFX();
	RegisterModule_VFX();

	void RegisterModule_XR();
	RegisterModule_XR();

	void RegisterModule_VR();
	RegisterModule_VR();

}

template <typename T> void RegisterUnityClass(const char*);
template <typename T> void RegisterStrippedType(int, const char*, const char*);

void InvokeRegisterStaticallyLinkedModuleClasses()
{
	// Do nothing (we're in stripping mode)
}

class AnimationClip; template <> void RegisterUnityClass<AnimationClip>(const char*);
class Animator; template <> void RegisterUnityClass<Animator>(const char*);
class AnimatorController; template <> void RegisterUnityClass<AnimatorController>(const char*);
class AnimatorOverrideController; template <> void RegisterUnityClass<AnimatorOverrideController>(const char*);
class Avatar; template <> void RegisterUnityClass<Avatar>(const char*);
class Motion; template <> void RegisterUnityClass<Motion>(const char*);
class RuntimeAnimatorController; template <> void RegisterUnityClass<RuntimeAnimatorController>(const char*);
class AssetBundle; template <> void RegisterUnityClass<AssetBundle>(const char*);
class AudioBehaviour; template <> void RegisterUnityClass<AudioBehaviour>(const char*);
class AudioClip; template <> void RegisterUnityClass<AudioClip>(const char*);
class AudioListener; template <> void RegisterUnityClass<AudioListener>(const char*);
class AudioManager; template <> void RegisterUnityClass<AudioManager>(const char*);
class AudioResource; template <> void RegisterUnityClass<AudioResource>(const char*);
class SampleClip; template <> void RegisterUnityClass<SampleClip>(const char*);
class Behaviour; template <> void RegisterUnityClass<Behaviour>(const char*);
class BuildSettings; template <> void RegisterUnityClass<BuildSettings>(const char*);
class Camera; template <> void RegisterUnityClass<Camera>(const char*);
namespace Unity { class Component; } template <> void RegisterUnityClass<Unity::Component>(const char*);
class ComputeShader; template <> void RegisterUnityClass<ComputeShader>(const char*);
class Cubemap; template <> void RegisterUnityClass<Cubemap>(const char*);
class CubemapArray; template <> void RegisterUnityClass<CubemapArray>(const char*);
class DelayedCallManager; template <> void RegisterUnityClass<DelayedCallManager>(const char*);
class EditorExtension; template <> void RegisterUnityClass<EditorExtension>(const char*);
class GameManager; template <> void RegisterUnityClass<GameManager>(const char*);
class GameObject; template <> void RegisterUnityClass<GameObject>(const char*);
class GlobalGameManager; template <> void RegisterUnityClass<GlobalGameManager>(const char*);
class GraphicsSettings; template <> void RegisterUnityClass<GraphicsSettings>(const char*);
class InputManager; template <> void RegisterUnityClass<InputManager>(const char*);
class LODGroup; template <> void RegisterUnityClass<LODGroup>(const char*);
class LevelGameManager; template <> void RegisterUnityClass<LevelGameManager>(const char*);
class Light; template <> void RegisterUnityClass<Light>(const char*);
class LightProbeProxyVolume; template <> void RegisterUnityClass<LightProbeProxyVolume>(const char*);
class LightProbes; template <> void RegisterUnityClass<LightProbes>(const char*);
class LightingSettings; template <> void RegisterUnityClass<LightingSettings>(const char*);
class LightmapSettings; template <> void RegisterUnityClass<LightmapSettings>(const char*);
class LowerResBlitTexture; template <> void RegisterUnityClass<LowerResBlitTexture>(const char*);
class Material; template <> void RegisterUnityClass<Material>(const char*);
class Mesh; template <> void RegisterUnityClass<Mesh>(const char*);
class MeshFilter; template <> void RegisterUnityClass<MeshFilter>(const char*);
class MeshRenderer; template <> void RegisterUnityClass<MeshRenderer>(const char*);
class MonoBehaviour; template <> void RegisterUnityClass<MonoBehaviour>(const char*);
class MonoManager; template <> void RegisterUnityClass<MonoManager>(const char*);
class MonoScript; template <> void RegisterUnityClass<MonoScript>(const char*);
class NamedObject; template <> void RegisterUnityClass<NamedObject>(const char*);
class Object; template <> void RegisterUnityClass<Object>(const char*);
class PlayerSettings; template <> void RegisterUnityClass<PlayerSettings>(const char*);
class PreloadData; template <> void RegisterUnityClass<PreloadData>(const char*);
class QualitySettings; template <> void RegisterUnityClass<QualitySettings>(const char*);
class RayTracingShader; template <> void RegisterUnityClass<RayTracingShader>(const char*);
namespace UI { class RectTransform; } template <> void RegisterUnityClass<UI::RectTransform>(const char*);
class ReflectionProbe; template <> void RegisterUnityClass<ReflectionProbe>(const char*);
class RenderSettings; template <> void RegisterUnityClass<RenderSettings>(const char*);
class RenderTexture; template <> void RegisterUnityClass<RenderTexture>(const char*);
class Renderer; template <> void RegisterUnityClass<Renderer>(const char*);
class ResourceManager; template <> void RegisterUnityClass<ResourceManager>(const char*);
class RuntimeInitializeOnLoadManager; template <> void RegisterUnityClass<RuntimeInitializeOnLoadManager>(const char*);
class Shader; template <> void RegisterUnityClass<Shader>(const char*);
class ShaderNameRegistry; template <> void RegisterUnityClass<ShaderNameRegistry>(const char*);
class Skybox; template <> void RegisterUnityClass<Skybox>(const char*);
class SortingGroup; template <> void RegisterUnityClass<SortingGroup>(const char*);
class Sprite; template <> void RegisterUnityClass<Sprite>(const char*);
class SpriteAtlas; template <> void RegisterUnityClass<SpriteAtlas>(const char*);
class SpriteRenderer; template <> void RegisterUnityClass<SpriteRenderer>(const char*);
class TagManager; template <> void RegisterUnityClass<TagManager>(const char*);
class TextAsset; template <> void RegisterUnityClass<TextAsset>(const char*);
class Texture; template <> void RegisterUnityClass<Texture>(const char*);
class Texture2D; template <> void RegisterUnityClass<Texture2D>(const char*);
class Texture2DArray; template <> void RegisterUnityClass<Texture2DArray>(const char*);
class Texture3D; template <> void RegisterUnityClass<Texture3D>(const char*);
class TimeManager; template <> void RegisterUnityClass<TimeManager>(const char*);
class Transform; template <> void RegisterUnityClass<Transform>(const char*);
class GridLayout; template <> void RegisterUnityClass<GridLayout>(const char*);
class ParticleSystem; template <> void RegisterUnityClass<ParticleSystem>(const char*);
class ParticleSystemRenderer; template <> void RegisterUnityClass<ParticleSystemRenderer>(const char*);
class Collider; template <> void RegisterUnityClass<Collider>(const char*);
class PhysicsManager; template <> void RegisterUnityClass<PhysicsManager>(const char*);
class Rigidbody; template <> void RegisterUnityClass<Rigidbody>(const char*);
class BoxCollider2D; template <> void RegisterUnityClass<BoxCollider2D>(const char*);
class CircleCollider2D; template <> void RegisterUnityClass<CircleCollider2D>(const char*);
class Collider2D; template <> void RegisterUnityClass<Collider2D>(const char*);
class EdgeCollider2D; template <> void RegisterUnityClass<EdgeCollider2D>(const char*);
class Physics2DSettings; template <> void RegisterUnityClass<Physics2DSettings>(const char*);
class PhysicsMaterial2D; template <> void RegisterUnityClass<PhysicsMaterial2D>(const char*);
class PolygonCollider2D; template <> void RegisterUnityClass<PolygonCollider2D>(const char*);
class Rigidbody2D; template <> void RegisterUnityClass<Rigidbody2D>(const char*);
class SpriteShapeRenderer; template <> void RegisterUnityClass<SpriteShapeRenderer>(const char*);
class Terrain; template <> void RegisterUnityClass<Terrain>(const char*);
class TerrainData; template <> void RegisterUnityClass<TerrainData>(const char*);
namespace TextRendering { class Font; } template <> void RegisterUnityClass<TextRendering::Font>(const char*);
namespace TextRenderingPrivate { class TextMesh; } template <> void RegisterUnityClass<TextRenderingPrivate::TextMesh>(const char*);
class Tilemap; template <> void RegisterUnityClass<Tilemap>(const char*);
class TilemapRenderer; template <> void RegisterUnityClass<TilemapRenderer>(const char*);
namespace UI { class Canvas; } template <> void RegisterUnityClass<UI::Canvas>(const char*);
namespace UI { class CanvasGroup; } template <> void RegisterUnityClass<UI::CanvasGroup>(const char*);
namespace UI { class CanvasRenderer; } template <> void RegisterUnityClass<UI::CanvasRenderer>(const char*);
class UIRenderer; template <> void RegisterUnityClass<UIRenderer>(const char*);
class VFXManager; template <> void RegisterUnityClass<VFXManager>(const char*);
class VisualEffect; template <> void RegisterUnityClass<VisualEffect>(const char*);
class VisualEffectAsset; template <> void RegisterUnityClass<VisualEffectAsset>(const char*);
class VisualEffectObject; template <> void RegisterUnityClass<VisualEffectObject>(const char*);

void RegisterAllClasses()
{
void RegisterBuiltinTypes();
RegisterBuiltinTypes();
	//Total: 100 non stripped classes
	//0. AnimationClip
	RegisterUnityClass<AnimationClip>("Animation");
	//1. Animator
	RegisterUnityClass<Animator>("Animation");
	//2. AnimatorController
	RegisterUnityClass<AnimatorController>("Animation");
	//3. AnimatorOverrideController
	RegisterUnityClass<AnimatorOverrideController>("Animation");
	//4. Avatar
	RegisterUnityClass<Avatar>("Animation");
	//5. Motion
	RegisterUnityClass<Motion>("Animation");
	//6. RuntimeAnimatorController
	RegisterUnityClass<RuntimeAnimatorController>("Animation");
	//7. AssetBundle
	RegisterUnityClass<AssetBundle>("AssetBundle");
	//8. AudioBehaviour
	RegisterUnityClass<AudioBehaviour>("Audio");
	//9. AudioClip
	RegisterUnityClass<AudioClip>("Audio");
	//10. AudioListener
	RegisterUnityClass<AudioListener>("Audio");
	//11. AudioManager
	RegisterUnityClass<AudioManager>("Audio");
	//12. AudioResource
	RegisterUnityClass<AudioResource>("Audio");
	//13. SampleClip
	RegisterUnityClass<SampleClip>("Audio");
	//14. Behaviour
	RegisterUnityClass<Behaviour>("Core");
	//15. BuildSettings
	RegisterUnityClass<BuildSettings>("Core");
	//16. Camera
	RegisterUnityClass<Camera>("Core");
	//17. Component
	RegisterUnityClass<Unity::Component>("Core");
	//18. ComputeShader
	RegisterUnityClass<ComputeShader>("Core");
	//19. Cubemap
	RegisterUnityClass<Cubemap>("Core");
	//20. CubemapArray
	RegisterUnityClass<CubemapArray>("Core");
	//21. DelayedCallManager
	RegisterUnityClass<DelayedCallManager>("Core");
	//22. EditorExtension
	RegisterUnityClass<EditorExtension>("Core");
	//23. GameManager
	RegisterUnityClass<GameManager>("Core");
	//24. GameObject
	RegisterUnityClass<GameObject>("Core");
	//25. GlobalGameManager
	RegisterUnityClass<GlobalGameManager>("Core");
	//26. GraphicsSettings
	RegisterUnityClass<GraphicsSettings>("Core");
	//27. InputManager
	RegisterUnityClass<InputManager>("Core");
	//28. LODGroup
	RegisterUnityClass<LODGroup>("Core");
	//29. LevelGameManager
	RegisterUnityClass<LevelGameManager>("Core");
	//30. Light
	RegisterUnityClass<Light>("Core");
	//31. LightProbeProxyVolume
	RegisterUnityClass<LightProbeProxyVolume>("Core");
	//32. LightProbes
	RegisterUnityClass<LightProbes>("Core");
	//33. LightingSettings
	RegisterUnityClass<LightingSettings>("Core");
	//34. LightmapSettings
	RegisterUnityClass<LightmapSettings>("Core");
	//35. LowerResBlitTexture
	RegisterUnityClass<LowerResBlitTexture>("Core");
	//36. Material
	RegisterUnityClass<Material>("Core");
	//37. Mesh
	RegisterUnityClass<Mesh>("Core");
	//38. MeshFilter
	RegisterUnityClass<MeshFilter>("Core");
	//39. MeshRenderer
	RegisterUnityClass<MeshRenderer>("Core");
	//40. MonoBehaviour
	RegisterUnityClass<MonoBehaviour>("Core");
	//41. MonoManager
	RegisterUnityClass<MonoManager>("Core");
	//42. MonoScript
	RegisterUnityClass<MonoScript>("Core");
	//43. NamedObject
	RegisterUnityClass<NamedObject>("Core");
	//44. Object
	//Skipping Object
	//45. PlayerSettings
	RegisterUnityClass<PlayerSettings>("Core");
	//46. PreloadData
	RegisterUnityClass<PreloadData>("Core");
	//47. QualitySettings
	RegisterUnityClass<QualitySettings>("Core");
	//48. RayTracingShader
	RegisterUnityClass<RayTracingShader>("Core");
	//49. RectTransform
	RegisterUnityClass<UI::RectTransform>("Core");
	//50. ReflectionProbe
	RegisterUnityClass<ReflectionProbe>("Core");
	//51. RenderSettings
	RegisterUnityClass<RenderSettings>("Core");
	//52. RenderTexture
	RegisterUnityClass<RenderTexture>("Core");
	//53. Renderer
	RegisterUnityClass<Renderer>("Core");
	//54. ResourceManager
	RegisterUnityClass<ResourceManager>("Core");
	//55. RuntimeInitializeOnLoadManager
	RegisterUnityClass<RuntimeInitializeOnLoadManager>("Core");
	//56. Shader
	RegisterUnityClass<Shader>("Core");
	//57. ShaderNameRegistry
	RegisterUnityClass<ShaderNameRegistry>("Core");
	//58. Skybox
	RegisterUnityClass<Skybox>("Core");
	//59. SortingGroup
	RegisterUnityClass<SortingGroup>("Core");
	//60. Sprite
	RegisterUnityClass<Sprite>("Core");
	//61. SpriteAtlas
	RegisterUnityClass<SpriteAtlas>("Core");
	//62. SpriteRenderer
	RegisterUnityClass<SpriteRenderer>("Core");
	//63. TagManager
	RegisterUnityClass<TagManager>("Core");
	//64. TextAsset
	RegisterUnityClass<TextAsset>("Core");
	//65. Texture
	RegisterUnityClass<Texture>("Core");
	//66. Texture2D
	RegisterUnityClass<Texture2D>("Core");
	//67. Texture2DArray
	RegisterUnityClass<Texture2DArray>("Core");
	//68. Texture3D
	RegisterUnityClass<Texture3D>("Core");
	//69. TimeManager
	RegisterUnityClass<TimeManager>("Core");
	//70. Transform
	RegisterUnityClass<Transform>("Core");
	//71. GridLayout
	RegisterUnityClass<GridLayout>("Grid");
	//72. ParticleSystem
	RegisterUnityClass<ParticleSystem>("ParticleSystem");
	//73. ParticleSystemRenderer
	RegisterUnityClass<ParticleSystemRenderer>("ParticleSystem");
	//74. Collider
	RegisterUnityClass<Collider>("Physics");
	//75. PhysicsManager
	RegisterUnityClass<PhysicsManager>("Physics");
	//76. Rigidbody
	RegisterUnityClass<Rigidbody>("Physics");
	//77. BoxCollider2D
	RegisterUnityClass<BoxCollider2D>("Physics2D");
	//78. CircleCollider2D
	RegisterUnityClass<CircleCollider2D>("Physics2D");
	//79. Collider2D
	RegisterUnityClass<Collider2D>("Physics2D");
	//80. EdgeCollider2D
	RegisterUnityClass<EdgeCollider2D>("Physics2D");
	//81. Physics2DSettings
	RegisterUnityClass<Physics2DSettings>("Physics2D");
	//82. PhysicsMaterial2D
	RegisterUnityClass<PhysicsMaterial2D>("Physics2D");
	//83. PolygonCollider2D
	RegisterUnityClass<PolygonCollider2D>("Physics2D");
	//84. Rigidbody2D
	RegisterUnityClass<Rigidbody2D>("Physics2D");
	//85. SpriteShapeRenderer
	RegisterUnityClass<SpriteShapeRenderer>("SpriteShape");
	//86. Terrain
	RegisterUnityClass<Terrain>("Terrain");
	//87. TerrainData
	RegisterUnityClass<TerrainData>("Terrain");
	//88. Font
	RegisterUnityClass<TextRendering::Font>("TextRendering");
	//89. TextMesh
	RegisterUnityClass<TextRenderingPrivate::TextMesh>("TextRendering");
	//90. Tilemap
	RegisterUnityClass<Tilemap>("Tilemap");
	//91. TilemapRenderer
	RegisterUnityClass<TilemapRenderer>("Tilemap");
	//92. Canvas
	RegisterUnityClass<UI::Canvas>("UI");
	//93. CanvasGroup
	RegisterUnityClass<UI::CanvasGroup>("UI");
	//94. CanvasRenderer
	RegisterUnityClass<UI::CanvasRenderer>("UI");
	//95. UIRenderer
	RegisterUnityClass<UIRenderer>("UIElements");
	//96. VFXManager
	RegisterUnityClass<VFXManager>("VFX");
	//97. VisualEffect
	RegisterUnityClass<VisualEffect>("VFX");
	//98. VisualEffectAsset
	RegisterUnityClass<VisualEffectAsset>("VFX");
	//99. VisualEffectObject
	RegisterUnityClass<VisualEffectObject>("VFX");

}
