using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using UnityEngine.InputSystem;
using StarterAssets;

namespace ValeDasReliquias.Editor
{
    public static class GameBuilder
    {
        const string Root = "Assets/_Game";
        const string Animations = "Assets/Starter Assets/Runtime/ThirdPersonController/Character/Animations/";
        static Material stone, gold, dark, clay, leaf, trunk, glow, ruby;
        static readonly Dictionary<string, Material> Surfaces = new Dictionary<string, Material>();
        static readonly Dictionary<ItemKind, GameObject> Items = new Dictionary<ItemKind, GameObject>();
        static AnimatorController playerAnimator;
        static Terrain terrain;
        static Transform scenery;

        [MenuItem("Vale das Relíquias/Construir projeto e fases")]
        public static void Generate()
        {
            foreach (var folder in new[] { "Animations", "Materials", "Meshes", "Prefabs", "Terrain", "Art/Generated" }) Directory.CreateDirectory(Root + "/" + folder);
            Directory.CreateDirectory("../BuildLogs");
            AssetDatabase.Refresh();
            PrepareImports();
            PreparePipeline();
            PrepareInput();
            MakeMaterials();
            MakePlayerAnimator();
            MakeItems();
            for (int stage = 0; stage <= 3; stage++) MakeScene(stage);
            EditorBuildSettings.scenes = new[] { "Menu", "cena-1", "cena-2", "cena-3" }.Select(n => new EditorBuildSettingsScene("Assets/Scenes/" + n + ".unity", true)).ToArray();
            PlayerSettings.companyName = "Projeto Fatec";
            PlayerSettings.productName = "Vale das Relíquias";
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/Menu.unity");
            File.WriteAllText("../BuildLogs/generated.txt", "Menu e três fases gerados em " + DateTime.UtcNow.ToString("O"));
            Debug.Log("VALE_GENERATION_OK");
        }
        static void PrepareImports()
        {
            foreach (var path in Directory.GetFiles(Root + "/Art", "*.jpg", SearchOption.AllDirectories))
            {
                var importer = AssetImporter.GetAtPath(path.Replace('\\', '/')) as TextureImporter;
                if (importer == null) continue;
                bool normal = path.Contains("NormalGL");
                importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = !normal && !path.Contains("Roughness") && !path.Contains("AmbientOcclusion");
                importer.maxTextureSize = 1024; importer.SaveAndReimport();
            }
            foreach (var name in new[] { "Button", "Panel" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/Art/Professor/" + name + ".png");
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true;
                importer.spriteBorder = name == "Button" ? new Vector4(30, 30, 30, 30) : new Vector4(32, 32, 32, 32); importer.SaveAndReimport();
            }
            foreach (var path in Directory.GetFiles(Root + "/Art/Surfaces", "*.png", SearchOption.AllDirectories))
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                importer.sRGBTexture = false; importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.SaveAndReimport();
            }
        }
        static void PreparePipeline()
        {
            string path = Root + "/Materials/RenderPipeline.asset";
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path) == null) AssetDatabase.CopyAsset("Assets/Settings/PC_RPAsset.asset", path);
            string rendererPath = Root + "/Materials/Renderer.asset";
            if (AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath) == null) AssetDatabase.CopyAsset("Assets/Settings/PC_Renderer.asset", rendererPath);
            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            var rendererSettings = new SerializedObject(renderer); rendererSettings.FindProperty("m_RenderingMode").intValue = 0; rendererSettings.ApplyModifiedPropertiesWithoutUndo();
            foreach (var feature in renderer.rendererFeatures)
            {
                var featureSettings = new SerializedObject(feature); var downsample = featureSettings.FindProperty("m_Settings.Downsample");
                if (downsample != null) { downsample.boolValue = true; featureSettings.ApplyModifiedPropertiesWithoutUndo(); }
            }
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            var serialized = new SerializedObject(pipeline);
            serialized.FindProperty("m_RendererDataList").GetArrayElementAtIndex(0).objectReferenceValue = renderer;
            serialized.FindProperty("m_AdditionalLightsRenderingMode").intValue = 0;
            serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue = false;
            serialized.FindProperty("m_MainLightShadowmapResolution").intValue = 1024;
            serialized.FindProperty("m_ShadowDistance").floatValue = 70;
            serialized.FindProperty("m_ShadowCascadeCount").intValue = 2;
            serialized.FindProperty("m_MSAA").intValue = 2;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            GraphicsSettings.defaultRenderPipeline = pipeline;
            int quality = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = pipeline; }
            QualitySettings.SetQualityLevel(quality, false);
        }
        static void PrepareInput()
        {
            Directory.CreateDirectory(Root + "/Input");
            string source = "Assets/Starter Assets/Runtime/InputSystem/StarterAssets.inputactions";
            var actions = InputActionAsset.FromJson(File.ReadAllText(source));
            foreach (var scheme in actions.controlSchemes.ToArray()) if (scheme.name != "KeyboardMouse") actions.RemoveControlScheme(scheme.name);
            File.WriteAllText(Root + "/Input/Player.inputactions", actions.ToJson());
            UnityEngine.Object.DestroyImmediate(actions); AssetDatabase.Refresh();
        }
        static Material Mat(string name, Color color, float metallic = 0, float smoothness = 0.25f)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
            mat.color = color; mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }
        static void MakeMaterials()
        {
            Surfaces.Clear();
            foreach (string folder in Directory.GetDirectories(Root + "/Art/Surfaces"))
            {
                string id = Path.GetFileName(folder);
                var mat = Mat(id, Color.white);
                string prefix = folder.Replace('\\', '/') + "/" + id + "_1K-JPG_";
                mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "Color.jpg"));
                mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "NormalGL.jpg")); mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale", 0.65f);
                mat.SetTexture("_OcclusionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(prefix + "AmbientOcclusion.jpg")); mat.EnableKeyword("_OCCLUSIONMAP");
                mat.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(folder.Replace('\\', '/') + "/MetallicSmoothness.png")); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); mat.SetFloat("_Smoothness", 1);
                mat.mainTextureScale = new Vector2(2, 2);
                Surfaces[id] = mat;
            }
            stone = Surfaces["Rock030"]; gold = Mat("Ouro", new Color(0.92f, 0.56f, 0.13f), 0.72f, 0.68f);
            dark = Mat("Ferro escuro", new Color(0.06f, 0.1f, 0.105f), 0.55f, 0.28f);
            clay = Mat("Cerâmica turquesa", new Color(0.06f, 0.48f, 0.43f), 0.15f, 0.6f);
            leaf = Mat("Folhas", new Color(0.16f, 0.29f, 0.17f)); trunk = Mat("Madeira", new Color(0.22f, 0.13f, 0.075f));
            ruby = Mat("Kit vermelho", new Color(0.8f, 0.065f, 0.06f), 0.05f, 0.4f);
            glow = Mat("Luz das relíquias", new Color(0.18f, 0.8f, 0.7f), 0.25f, 0.7f); glow.EnableKeyword("_EMISSION"); glow.SetColor("_EmissionColor", new Color(0.1f, 0.7f, 0.48f) * 1.2f);
        }
        static AnimationClip Clip(string file) => AssetDatabase.LoadAllAssetsAtPath(Animations + file + ".fbx").OfType<AnimationClip>().First(c => !c.name.StartsWith("__"));
        static void MakePlayerAnimator()
        {
            string path = Root + "/Animations/Player.controller";
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
            playerAnimator = AnimatorController.CreateAnimatorControllerAtPath(path);
            foreach (var p in new[] { "Grounded", "Jump", "FreeFall", "Dance1", "Headspin Start", "GangnamStyle", "Dance2", "Waving", "Chutar", "Idle" }) playerAnimator.AddParameter(p, AnimatorControllerParameterType.Bool);
            playerAnimator.AddParameter("Speed", AnimatorControllerParameterType.Float); playerAnimator.AddParameter("MotionSpeed", AnimatorControllerParameterType.Float);
            var machine = playerAnimator.layers[0].stateMachine;
            var move = machine.AddState("Locomoção", new Vector3(200, 80)); machine.defaultState = move;
            var blend = new BlendTree { name = "Andar e correr", blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(blend, playerAnimator);
            blend.AddChild(Clip("Stand--Idle.anim"), 0); blend.AddChild(Clip("Locomotion--Walk_N.anim"), 2); blend.AddChild(Clip("Locomotion--Run_N.anim"), 5.335f); move.motion = blend;
            var jump = machine.AddState("Salto", new Vector3(460, 0)); jump.motion = Clip("Jump--Jump.anim");
            var air = machine.AddState("No ar", new Vector3(700, 50)); air.motion = Clip("Jump--InAir.anim");
            Transition(move, jump, "Jump", true);
            var fall = Transition(move, air, "Grounded", false); fall.AddCondition(AnimatorConditionMode.IfNot, 0, "Jump");
            var j = jump.AddTransition(air); j.hasExitTime = true; j.exitTime = 0.65f; j.duration = 0.06f;
            Transition(air, move, "Grounded", true);
            string[] names = { "Dance1", "Headspin Start", "GangnamStyle", "Dance2", "Waving", "Chutar" };
            string[] files = { "Dancing1", "Headspin Start", "Gangnam Style", "Dancing2", "Waving", "Mma Kick" };
            for (int i = 0; i < names.Length; i++)
            {
                var state = machine.AddState(names[i], new Vector3(100 + (i % 3) * 240, 270 + (i / 3) * 150)); state.motion = Clip(files[i]);
                Transition(move, state, names[i], true); Transition(state, move, names[i], false);
            }
        }
        static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to, string parameter, bool value)
        {
            var t = from.AddTransition(to); t.hasExitTime = false; t.duration = 0.12f; t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameter); return t;
        }
        static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = Root + "/Meshes/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); return existing; }
            mesh.name = name; AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, Vector3 pos)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); go.transform.SetParent(parent, false); go.transform.localPosition = pos;
            go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = material; return go;
        }
        static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material material, bool collision = true)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collision) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static Mesh Lathe(Vector2[] profile, int segments = 32)
        {
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int j = 0; j < profile.Length; j++) for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments; vertices.Add(new Vector3(Mathf.Cos(angle) * profile[j].x, profile[j].y, Mathf.Sin(angle) * profile[j].x)); uv.Add(new Vector2((float)i / segments, (float)j / (profile.Length - 1)));
                if (i < segments && j < profile.Length - 1) { int a = j * (segments + 1) + i, b = a + segments + 1; triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static Mesh Tube(Vector3[] path, float[] radius, int segments = 12)
        {
            var vertices = new List<Vector3>(); var triangles = new List<int>();
            for (int j = 0; j < path.Length; j++)
            {
                var tangent = (path[Math.Min(j + 1, path.Length - 1)] - path[Math.Max(0, j - 1)]).normalized;
                var right = Vector3.Cross(tangent, Vector3.forward).normalized; if (right.sqrMagnitude < 0.1f) right = Vector3.right; var up = Vector3.Cross(tangent, right).normalized;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2 / segments; vertices.Add(path[j] + radius[j] * (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)));
                    if (j < path.Length - 1 && i < segments) { int a = j * (segments + 1) + i, b = a + segments + 1; triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); }
                }
            }
            var mesh = new Mesh(); mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static Mesh Diamond()
        {
            var v = new[] { Vector3.up * 0.7f, Vector3.right * 0.38f, Vector3.forward * 0.38f, Vector3.left * 0.38f, Vector3.back * 0.38f, Vector3.down * 0.7f };
            int[] tris = { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 };
            var mesh = new Mesh(); mesh.vertices = tris.Select(i => v[i]).ToArray(); mesh.triangles = Enumerable.Range(0, tris.Length).ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds(); return mesh;
        }
        static void MakeItems()
        {
            Items.Clear();
            foreach (ItemKind kind in Enum.GetValues(typeof(ItemKind)))
            {
                var root = new GameObject(kind.ToString());
                var collectible = root.AddComponent<Collectible>(); collectible.kind = kind;
                var collider = root.AddComponent<SphereCollider>(); collider.isTrigger = true; collider.center = Vector3.up * 0.85f; collider.radius = kind == ItemKind.Bomb ? 0.65f : 0.85f;
                var rb = root.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                var visual = new GameObject("Visual").transform; visual.SetParent(root.transform, false); visual.localPosition = Vector3.up * 0.85f;
                if (kind == ItemKind.Coin)
                {
                    var disk = Primitive("Moeda personalizada", PrimitiveType.Cylinder, visual, Vector3.zero, new Vector3(0.78f, 0.045f, 0.78f), gold, false); disk.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    var ring = SaveMesh("Borda da moeda", Lathe(new[] { new Vector2(0.32f, -0.06f), new Vector2(0.39f, -0.06f), new Vector2(0.39f, 0.06f), new Vector2(0.32f, 0.06f) }));
                    MeshObject("Borda", visual, ring, gold, Vector3.zero).transform.localRotation = Quaternion.Euler(90, 0, 0);
                    Primitive("Selo central", PrimitiveType.Cube, visual, new Vector3(0, 0, -0.07f), new Vector3(0.2f, 0.2f, 0.025f), dark, false).transform.localRotation = Quaternion.Euler(0, 0, 45);
                }
                else if (kind == ItemKind.Diamond) MeshObject("Diamante facetado", visual, SaveMesh("Diamante", Diamond()), glow, Vector3.zero);
                else if (kind == ItemKind.Teapot)
                {
                    MeshObject("Corpo da chaleira", visual, SaveMesh("Chaleira corpo", Lathe(new[] { new Vector2(0, -0.4f), new Vector2(0.28f, -0.38f), new Vector2(0.43f, -0.18f), new Vector2(0.46f, 0), new Vector2(0.36f, 0.22f), new Vector2(0.24f, 0.28f) })), clay, Vector3.zero);
                    MeshObject("Tampa", visual, SaveMesh("Chaleira tampa", Lathe(new[] { new Vector2(0, 0.35f), new Vector2(0.12f, 0.34f), new Vector2(0.28f, 0.27f), new Vector2(0, 0.27f) })), gold, Vector3.zero);
                    Primitive("Puxador", PrimitiveType.Sphere, visual, new Vector3(0, 0.4f, 0), Vector3.one * 0.12f, gold, false);
                    MeshObject("Bico", visual, SaveMesh("Chaleira bico", Tube(new[] { new Vector3(0.33f, -0.05f, 0), new Vector3(0.55f, 0, 0), new Vector3(0.65f, 0.17f, 0), new Vector3(0.76f, 0.26f, 0) }, new[] { 0.15f, 0.12f, 0.09f, 0.085f })), clay, Vector3.zero);
                    var handle = Enumerable.Range(0, 21).Select(i => new Vector3(-0.34f - Mathf.Sin(i * Mathf.PI / 20) * 0.31f, 0.25f - i * 0.027f, 0)).ToArray();
                    MeshObject("Alça", visual, SaveMesh("Chaleira alça", Tube(handle, Enumerable.Repeat(0.065f, handle.Length).ToArray())), gold, Vector3.zero);
                }
                else
                {
                    string file = kind == ItemKind.Key ? "Chave" : kind == ItemKind.Bomb ? "Bomba" : "Vida";
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Art/Professor/" + file + ".fbx");
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(source); model.transform.SetParent(visual, false);
                    foreach (var animator in model.GetComponentsInChildren<Animator>()) UnityEngine.Object.DestroyImmediate(animator);
                    var renderers = model.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                    float target = kind == ItemKind.Key ? 1.35f : 0.9f; float scale = target / Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
                    var localCenter = visual.InverseTransformPoint(bounds.center);
                    model.transform.localScale *= scale; model.transform.localPosition -= localCenter * scale;
                    Material material = kind == ItemKind.Key ? gold : kind == ItemKind.Bomb ? dark : ruby;
                    foreach (var r in renderers) r.sharedMaterials = Enumerable.Repeat(material, r.sharedMaterials.Length).ToArray();
                    if (kind == ItemKind.Health)
                    {
                        var med = Mat("Maleta de vida", Color.white, 0, 0.3f); med.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Professor/caixa-socorro.png");
                        foreach (var r in renderers) r.sharedMaterial = med;
                    }
                    if (kind == ItemKind.Bomb) Primitive("Estopim aceso", PrimitiveType.Sphere, visual, new Vector3(0, 0.47f, 0), Vector3.one * 0.1f, gold, false);
                }
                string apath = Root + "/Animations/" + kind + ".controller";
                if (AssetDatabase.LoadAssetAtPath<AnimatorController>(apath) != null) AssetDatabase.DeleteAsset(apath);
                var controller = AnimatorController.CreateAnimatorControllerAtPath(apath);
                var clip = new AnimationClip { name = "Giro e flutuação" };
                if (kind != ItemKind.Bomb) clip.SetCurve("Visual", typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.Linear(0, 0, 4, 360));
                clip.SetCurve("Visual", typeof(Transform), "localPosition.y", new AnimationCurve(new Keyframe(0, 0.85f), new Keyframe(2, 1.03f), new Keyframe(4, 0.85f)));
                var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, settings);
                string cpath = Root + "/Animations/" + kind + ".anim"; if (AssetDatabase.LoadAssetAtPath<AnimationClip>(cpath) != null) AssetDatabase.DeleteAsset(cpath); AssetDatabase.CreateAsset(clip, cpath);
                controller.layers[0].stateMachine.AddState("Animação").motion = clip; root.AddComponent<Animator>().runtimeAnimatorController = controller;
                Items[kind] = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + kind + ".prefab"); UnityEngine.Object.DestroyImmediate(root);
            }
        }
        static void MakeScene(int stage)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var director = new GameObject("Regras da fase").AddComponent<StageDirector>(); director.stageNumber = stage;
            director.stageTitle = new[] { "Menu", "O pátio das relíquias", "Os portões do vale", "A última travessia" }[stage];
            director.objective = stage == 1 ? "Recupere duas peças de cada tipo para seguir às ruínas." : stage == 2 ? "Atravesse a porta livre, encontre a chave e destranque o portão." : "Recupere as relíquias e alcance o arco verde. Evite as bombas.";
            director.nextScene = stage == 1 ? "cena-2" : stage == 2 ? "cena-3" : ""; director.autoAdvance = stage == 1;
            if (stage == 2) director.goals.coins = director.goals.diamonds = director.goals.teapots = 0;
            director.collectSound = Sound(1); director.keySound = Sound(3); director.hurtSound = Sound(4); director.healSound = Sound(2); director.doorSound = Sound(5);
            scenery = new GameObject("Cenário").transform;
            SetupLighting(); MakeTerrain(stage);
            MakeLandscape(stage);
            if (stage == 0) MenuCamera();
            else
            {
                MakePlayer(director);
                if (stage == 1) LayoutOne();
                if (stage == 2) LayoutTwo();
                if (stage == 3) LayoutThree();
            }
            var ui = new GameObject("Interface", typeof(RectTransform)).AddComponent<GameUI>(); ui.font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Art/Professor/Rubik-Bold.ttf"); ui.buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Professor/Button.png"); ui.panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Professor/Panel.png"); ui.Construct();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), "Assets/Scenes/" + (stage == 0 ? "Menu" : "cena-" + stage) + ".unity");
        }
        static AudioClip Sound(int i) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Art/Professor/Som0" + i + ".mp3");
        static void SetupLighting()
        {
            var sun = new GameObject("Sol").AddComponent<Light>(); sun.type = LightType.Directional; sun.color = new Color(1, 0.91f, 0.76f); sun.intensity = 2.1f; sun.shadows = LightShadows.Soft; sun.transform.rotation = Quaternion.Euler(43, -28, 0);
            RenderSettings.sun = sun; RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(0.5f, 0.65f, 0.72f); RenderSettings.ambientEquatorColor = new Color(0.35f, 0.44f, 0.42f); RenderSettings.ambientGroundColor = new Color(0.16f, 0.2f, 0.19f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(0.58f, 0.69f, 0.69f); RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogDensity = 0.007f;
            var sky = Mat("Céu", Color.white); sky.shader = Shader.Find("Skybox/Procedural"); sky.SetColor("_SkyTint", new Color(0.43f, 0.56f, 0.62f)); sky.SetColor("_GroundColor", new Color(0.27f, 0.34f, 0.3f)); sky.SetFloat("_AtmosphereThickness", 1.2f); sky.SetFloat("_Exposure", 1.15f); RenderSettings.skybox = sky;
            var volume = new GameObject("Cor e atmosfera").AddComponent<Volume>(); volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>(); var tonemap = profile.Add<Tonemapping>(true); tonemap.mode.Override(TonemappingMode.ACES); var bloom = profile.Add<Bloom>(true); bloom.intensity.Override(0.18f); bloom.threshold.Override(1.1f);
            string path = Root + "/Materials/Atmosfera.asset"; var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (existing == null) { AssetDatabase.CreateAsset(profile, path); foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile); volume.sharedProfile = profile; } else { volume.sharedProfile = existing; UnityEngine.Object.DestroyImmediate(profile); }
        }
        static void MakeTerrain(int stage)
        {
            var data = new TerrainData { heightmapResolution = 257, alphamapResolution = 256, baseMapResolution = 512, size = new Vector3(200, 16, 112) };
            // Register TerrainData before creating its generated texture subassets.
            string terrainPath = Root + "/Terrain/Terreno-" + stage + ".asset";
            if (AssetDatabase.LoadAssetAtPath<TerrainData>(terrainPath) != null) AssetDatabase.DeleteAsset(terrainPath);
            AssetDatabase.CreateAsset(data, terrainPath);
            float[,] heights = new float[257, 257];
            for (int z = 0; z < 257; z++) for (int x = 0; x < 257; x++)
            {
                float wx = x / 256f * 200 - 100, wz = z / 256f * 112 - 8;
                float edge = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(22, 87, Mathf.Abs(wx)));
                float border = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(43, 56, Mathf.Abs(wz - 48)));
                float hill = (Mathf.PerlinNoise(x * 0.024f + stage, z * 0.024f) * 8 + 5) * Mathf.Max(edge, border);
                float path = stage == 3 ? 0.5f * (Mathf.Sin(wz * 0.09f) + 1) * Mathf.SmoothStep(0, 1, Mathf.InverseLerp(10, 23, wz)) : 0;
                heights[z, x] = (hill + path) / 16;
            }
            data.SetHeights(0, 0, heights);
            string[] ids = { "Grass001", "Gravel027", "Rock030", "Ground037" };
            var layers = ids.Select(id =>
            {
                string path = Root + "/Terrain/" + id + ".terrainlayer";
                var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(path);
                if (layer == null) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, path); }
                layer.diffuseTexture = Surfaces[id].mainTexture as Texture2D; layer.normalMapTexture = Surfaces[id].GetTexture("_BumpMap") as Texture2D;
                layer.maskMapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Art/Surfaces/" + id + "/TerrainMask.png");
                layer.tileSize = Vector2.one * (id.StartsWith("Rock") ? 5 : 3); layer.normalScale = 0.5f; layer.smoothness = 0.15f; return layer;
            }).ToArray();
            data.terrainLayers = layers;
            float[,,] paint = new float[256, 256, 4];
            for (int z = 0; z < 256; z++) for (int x = 0; x < 256; x++)
            {
                float wx = x / 255f * 200 - 100, wz = z / 255f * 112 - 8;
                float road = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(1.7f, 3.3f, Mathf.Abs(wx - Mathf.Sin(wz * 0.085f) * 2)));
                float rock = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(18, 52, Mathf.Abs(wx))); float dirt = Mathf.PerlinNoise(x * 0.09f, z * 0.09f) * 0.3f;
                paint[z, x, 1] = road * (1 - rock); paint[z, x, 2] = rock; paint[z, x, 3] = dirt * (1 - rock) * (1 - road); paint[z, x, 0] = 1 - paint[z, x, 1] - paint[z, x, 2] - paint[z, x, 3];
            }
            data.SetAlphamaps(0, 0, paint);
            data.SetBaseMapDirty(); EditorUtility.SetDirty(data);
            var obj = Terrain.CreateTerrainGameObject(data); obj.name = "Terreno do vale"; obj.transform.position = new Vector3(-100, 0, -8); obj.transform.SetParent(scenery);
            terrain = obj.GetComponent<Terrain>(); terrain.heightmapPixelError = 6; terrain.drawInstanced = true;
            var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/Terreno.mat"); if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")); AssetDatabase.CreateAsset(material, Root + "/Materials/Terreno.mat"); }
            material.enableInstancing = true; material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_MASKMAP"); material.SetFloat("_NumLayersCount", 4); terrain.materialTemplate = material; terrain.Flush();
        }
        static Vector3 At(float x, float z, float y = 0) => new Vector3(x, terrain.SampleHeight(new Vector3(x, 0, z)) + y, z);
        static void MakePlayer(StageDirector director)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/meus personagens/prefabs/PlayerKayky.prefab");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(source); player.name = "PlayerKayky"; player.transform.position = At(0, 8, 0.1f); player.transform.rotation = Quaternion.identity;
            director.player = player.GetComponent<ThirdPersonController>(); director.player.MoveSpeed = 3.2f; director.player.SprintSpeed = 6.5f; director.player.CameraAngleOverride = 12;
            player.GetComponent<Animator>().runtimeAnimatorController = playerAnimator;
            var input = player.GetComponent<PlayerInput>(); input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(Root + "/Input/Player.inputactions"); input.defaultControlScheme = "KeyboardMouse"; input.neverAutoSwitchControlSchemes = true; input.defaultActionMap = "Player";
            var cam = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener), typeof(CinemachineBrain)); cam.tag = "MainCamera"; cam.GetComponent<Camera>().farClipPlane = 300; cam.GetComponent<Camera>().GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var follow = new GameObject("Câmera de acompanhamento").AddComponent<CinemachineCamera>(); follow.Follow = director.player.CinemachineCameraTarget.transform; follow.Lens.FieldOfView = 55;
            var body = follow.gameObject.AddComponent<CinemachineThirdPersonFollow>(); body.CameraDistance = 5.5f; body.ShoulderOffset = new Vector3(0.55f, 0.3f, 0); body.Damping = new Vector3(0.1f, 0.15f, 0.2f); body.AvoidObstacles.Enabled = true; body.AvoidObstacles.CollisionFilter = 1; body.AvoidObstacles.IgnoreTag = "Player";
            PrefabUtility.SaveAsPrefabAsset(player, Root + "/Prefabs/Player.prefab");
        }
        static void MenuCamera()
        {
            var camera = new GameObject("MainCamera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>(); camera.tag = "MainCamera"; camera.transform.position = new Vector3(18, 12, 4); camera.transform.LookAt(new Vector3(-1, 2, 36)); camera.fieldOfView = 52; camera.farClipPlane = 300; camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            Arch(0, 38, true); Item(ItemKind.Diamond, 0, 38); Item(ItemKind.Coin, -3, 35); Item(ItemKind.Teapot, 3, 35);
        }
        static void Item(ItemKind kind, float x, float z)
        {
            var item = (GameObject)PrefabUtility.InstantiatePrefab(Items[kind]); item.transform.position = At(x, z, 0.1f);
            if (kind != ItemKind.Bomb && kind != ItemKind.Health) Primitive("Pedestal", PrimitiveType.Cylinder, scenery, At(x, z, 0.07f), new Vector3(1.2f, 0.08f, 1.2f), stone);
        }
        static void LayoutOne()
        {
            Item(ItemKind.Coin, -3, 17); Item(ItemKind.Coin, 4, 25); Item(ItemKind.Diamond, 1, 34); Item(ItemKind.Diamond, -6, 46); Item(ItemKind.Teapot, 7, 39); Item(ItemKind.Teapot, 1, 56);
            Arch(0, 65, true); Sign("O PÁTIO DAS RELÍQUIAS", -5, 11); Sign("DUAS PEÇAS DE CADA TIPO", 7, 47);
        }
        static void LayoutTwo()
        {
            Door(28, false); Door(65, true); Item(ItemKind.Key, -10, 45); Arch(0, 82, true); Exit(82); Sign("E  /  INTERAGIR COM PORTAS", -6, 18); Sign("UMA CHAVE ENTRE AS RUÍNAS", -11, 37);
            foreach (int z in new[] { 28, 65 }) foreach (int side in new[] { -1, 1 }) Primitive("Muralha", PrimitiveType.Cube, scenery, new Vector3(side * 13.5f, 2.25f, z), new Vector3(23, 4.5f, 0.9f), stone);
            foreach (int side in new[] { -1, 1 }) { var bound = Primitive("Limite das ruínas", PrimitiveType.Cube, scenery, new Vector3(side * 25, 3, 46), new Vector3(1, 6, 84), stone); }
            Primitive("Fundo das ruínas", PrimitiveType.Cube, scenery, new Vector3(0, 3, 3), new Vector3(50, 6, 1), stone);
        }
        static void LayoutThree()
        {
            Item(ItemKind.Coin, -4, 20); Item(ItemKind.Diamond, 7, 33); Item(ItemKind.Teapot, -7, 47); Item(ItemKind.Coin, 3, 60); Item(ItemKind.Diamond, -4, 75); Item(ItemKind.Teapot, 5, 85);
            foreach (var p in new[] { new Vector2(0, 24), new Vector2(-2, 39), new Vector2(2, 50), new Vector2(0, 66), new Vector2(-7, 71), new Vector2(5, 78) }) Item(ItemKind.Bomb, p.x, p.y);
            Item(ItemKind.Health, -8, 32); Item(ItemKind.Health, 8, 62); Item(ItemKind.Health, -5, 86); Arch(0, 95, true); Exit(95); Sign("BOMBAS TIRAM VIDA", -6, 14); Sign("MALETAS RECUPERAM VIDA", 8, 30);
        }
        static void Exit(float z)
        {
            var go = new GameObject("Saída da fase", typeof(BoxCollider), typeof(ExitGate), typeof(Rigidbody)); go.transform.position = At(0, z, 1.5f); var c = go.GetComponent<BoxCollider>(); c.isTrigger = true; c.size = new Vector3(5, 3, 3); var body = go.GetComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
        }
        static void Door(float z, bool locked)
        {
            var root = new GameObject(locked ? "Portão com chave" : "Porta livre"); root.transform.position = At(0, z);
            var d = root.AddComponent<DoorInteraction>(); d.requiresKey = locked;
            var hinge = new GameObject("Dobradiça").transform; hinge.SetParent(root.transform, false); hinge.localPosition = new Vector3(-2, 0, 0); d.hinge = hinge;
            Primitive("Folha", PrimitiveType.Cube, hinge, new Vector3(2, 2, 0), new Vector3(4, 4, 0.25f), locked ? dark : trunk);
            for (int i = 0; i < 5; i++) Primitive("Reforço", PrimitiveType.Cube, hinge, new Vector3(0.4f + i * 0.8f, 2, -0.15f), new Vector3(0.09f, 3.8f, 0.08f), gold, false);
            Primitive("Maçaneta", PrimitiveType.Sphere, hinge, new Vector3(3.5f, 1.7f, -0.25f), Vector3.one * 0.2f, gold, false);
            foreach (int side in new[] { -1, 1 }) Primitive("Batente", PrimitiveType.Cube, root.transform, new Vector3(side * 2.35f, 2.2f, 0), new Vector3(0.55f, 4.4f, 0.8f), stone);
            Primitive("Verga", PrimitiveType.Cube, root.transform, new Vector3(0, 4.4f, 0), new Vector3(5.25f, 0.6f, 0.8f), stone);
            string path = Root + "/Animations/Porta.controller"; var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);
                foreach (string name in new[] { "Fechada", "Abrir", "Fechar" })
                {
                    var clip = new AnimationClip { name = name }; float begin = name == "Fechar" ? -95 : 0, end = name == "Abrir" ? -95 : 0;
                    clip.SetCurve("Dobradiça", typeof(Transform), "localEulerAnglesRaw.y", AnimationCurve.EaseInOut(0, begin, 0.8f, end)); AssetDatabase.CreateAsset(clip, Root + "/Animations/Porta-" + name + ".anim"); controller.layers[0].stateMachine.AddState(name).motion = clip;
                }
            }
            d.animator = root.AddComponent<Animator>(); d.animator.runtimeAnimatorController = controller;
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + (locked ? "PortaTrancada" : "PortaLivre") + ".prefab");
        }
        static void Arch(float x, float z, bool lit)
        {
            foreach (int side in new[] { -1, 1 }) Primitive("Coluna do arco", PrimitiveType.Cube, scenery, At(x + side * 3, z, 2.5f), new Vector3(1, 5, 1.25f), stone);
            Primitive("Topo do arco", PrimitiveType.Cube, scenery, At(x, z, 5.2f), new Vector3(7.2f, 1, 1.5f), stone);
            if (lit) foreach (int side in new[] { -1, 1 }) Primitive("Sinal de saída", PrimitiveType.Cube, scenery, At(x + side * 2.4f, z - 0.7f, 2.5f), new Vector3(0.12f, 4, 0.1f), glow, false);
        }
        static void Sign(string text, float x, float z)
        {
            var go = new GameObject(text, typeof(TextMesh)); go.transform.SetParent(scenery); go.transform.position = At(x, z, 2.7f); go.transform.rotation = Quaternion.identity;
            var tm = go.GetComponent<TextMesh>(); tm.text = text; tm.fontSize = 48; tm.characterSize = 0.05f; tm.anchor = TextAnchor.MiddleCenter; tm.color = new Color(0.95f, 0.8f, 0.42f);
            Primitive("Placa", PrimitiveType.Cube, scenery, At(x, z + 0.04f, 2.7f), new Vector3(6, 0.8f, 0.12f), dark, false);
        }
        static void MakeLandscape(int stage)
        {
            UnityEngine.Random.InitState(71 + stage);
            var cone = SaveMesh("Copa", Lathe(new[] { new Vector2(0, 0), new Vector2(1.4f, 0), new Vector2(0, 3.5f) }, 9));
            for (int i = 0; i < 48; i++)
            {
                float side = i % 2 == 0 ? -1 : 1, x = side * UnityEngine.Random.Range(14, 62), z = UnityEngine.Random.Range(0, 100); float scale = UnityEngine.Random.Range(0.7f, 1.8f);
                var tree = new GameObject("Árvore").transform; tree.SetParent(scenery); tree.position = At(x, z); tree.localScale = Vector3.one * scale;
                Primitive("Tronco", PrimitiveType.Cylinder, tree, new Vector3(0, 1.1f, 0), new Vector3(0.4f, 1.1f, 0.4f), trunk);
                MeshObject("Copa", tree, cone, leaf, new Vector3(0, 1.2f, 0)); MeshObject("Copa alta", tree, cone, leaf, new Vector3(0, 2.7f, 0)).transform.localScale = Vector3.one * 0.75f;
            }
            for (int i = 0; i < 28; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * UnityEngine.Random.Range(10, 35), z = UnityEngine.Random.Range(7, 96);
                var rock = Primitive("Rocha", PrimitiveType.Sphere, scenery, At(x, z, 0.3f), new Vector3(UnityEngine.Random.Range(1.5f, 4), UnityEngine.Random.Range(1, 2.5f), UnityEngine.Random.Range(1.5f, 3)), Surfaces["Rock035"]);
                rock.transform.rotation = UnityEngine.Random.rotation;
            }
            for (int i = 0; i < 6; i++)
            {
                float x = i % 2 == 0 ? -10 : 11, z = 18 + i * 11;
                Primitive("Coluna antiga", PrimitiveType.Cube, scenery, At(x, z, 1.8f), new Vector3(1.1f, 3.6f, 1.1f), stone);
                Primitive("Capitel", PrimitiveType.Cube, scenery, At(x, z, 3.7f), new Vector3(1.6f, 0.4f, 1.6f), stone);
            }
        }
        [MenuItem("Vale das Relíquias/Compilar Windows")]
        public static void Build()
        {
            Directory.CreateDirectory("../Builds/Windows");
            var result = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(), "../Builds/Windows/ValeDasReliquias.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
            File.WriteAllText("../BuildLogs/build-result.txt", result.summary.result + " | errors=" + result.summary.totalErrors + " | bytes=" + result.summary.totalSize);
            if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Windows build failed");
            Debug.Log("VALE_BUILD_OK");
        }
        public static void RebuildAndBuild() { Generate(); GameAssetValidation.Validate(); Build(); }
        public static void GenerateAndValidate() { Generate(); GameAssetValidation.Validate(); }
    }
}
