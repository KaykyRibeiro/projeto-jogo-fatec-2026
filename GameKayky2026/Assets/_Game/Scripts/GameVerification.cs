#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using StarterAssets;

namespace ValeDasReliquias
{
    // Explicit command-line verification only; inactive during ordinary play.
    public class GameVerification : MonoBehaviour
    {
        [Serializable] class Report { public List<string> passed = new List<string>(); public List<string> errors = new List<string>(); public string completedUtc; }
        Report report = new Report();
        string folder;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartRequested()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "--verify-game");
            if (index < 0 || index + 1 >= args.Length || FindFirstObjectByType<GameVerification>() != null) return;
            var runner = new GameObject("Verificação automática").AddComponent<GameVerification>(); runner.folder = args[index + 1]; DontDestroyOnLoad(runner.gameObject);
        }
        void Start()
        {
            Application.logMessageReceived += OnLog;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            // Batch players do not necessarily discover desktop devices. Supply both required devices
            // before PlayerInput pairs its KeyboardMouse scheme in the first gameplay scene.
            if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();
            if (Mouse.current == null) InputSystem.AddDevice<Mouse>();
            Directory.CreateDirectory(folder); StartCoroutine(Guard(Run()));
        }
        void OnLog(string text, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) report.errors.Add(text + "\n" + stack); }
        IEnumerator Guard(IEnumerator routine)
        {
            while (true)
            {
                object current;
                try { if (!routine.MoveNext()) break; current = routine.Current; }
                catch (Exception e) { report.errors.Add(e.ToString()); break; }
                yield return current;
            }
            report.completedUtc = DateTime.UtcNow.ToString("O");
            File.WriteAllText(Path.Combine(folder, "verification.json"), JsonUtility.ToJson(report, true));
            Debug.Log("VALE_QA_FINISHED checks=" + report.passed.Count + " errors=" + report.errors.Count);
            Application.Quit(report.errors.Count == 0 ? 0 : 1);
        }
        void Check(bool success, string name) { if (!success) throw new Exception("FAILED: " + name); report.passed.Add(name); Debug.Log("PASS: " + name); }
        void Key(params Key[] keys)
        {
            if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();
            if (!Keyboard.current.enabled) InputSystem.EnableDevice(Keyboard.current);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
        }
        void Teleport(Vector3 position)
        {
            var player = GameSession.Instance.Stage.player; var cc = player.GetComponent<CharacterController>();
            cc.enabled = false; player.transform.position = position; cc.enabled = true; Physics.SyncTransforms();
        }
        IEnumerator Click(UnityEngine.UI.Button button)
        {
            // Let the mode change update panel visibility and the canvas layout before pressing.
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)button.transform;
            Vector2 position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = position, buttons = 1 });
            yield return null; yield return null;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = position, buttons = 0 });
            yield return null; yield return null;
        }
        IEnumerator Capture(string name)
        {
            yield return null; yield return null;
            // A hidden Windows player may not present a swap chain. Render explicitly offscreen.
            var camera = Camera.main;
            var canvas = FindFirstObjectByType<GameUI>().GetComponent<Canvas>();
            var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera;
            int width = Screen.width, height = Screen.height;
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = camera.nearClipPlane + 0.1f;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
            var active = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            RenderTexture.active = active; Destroy(image); RenderTexture.ReleaseTemporary(target);
            canvas.renderMode = previousMode; canvas.worldCamera = previousCamera;
            yield return new WaitForSecondsRealtime(0.35f);
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(1);
            Check(GameSession.Instance.Mode == ScreenMode.Menu, "Menu is the initial scene");
            yield return Capture("01-menu");
            var ui = FindFirstObjectByType<GameUI>(); yield return Click(ui.optionsButton);
            Check(GameSession.Instance.Mode == ScreenMode.Options, "Options button opens its panel");
            float oldVolume = GameSession.Instance.Volume, oldSensitivity = GameSession.Instance.Sensitivity;
            ui.volumeSlider.value = 0.42f; ui.sensitivitySlider.value = 1.4f;
            Check(Mathf.Abs(AudioListener.volume - 0.42f) < 0.01f && Mathf.Abs(GameSession.Instance.Sensitivity - 1.4f) < 0.01f, "Volume and camera sliders change real settings");
            yield return Capture("02-opcoes");
            ui.volumeSlider.value = oldVolume; ui.sensitivitySlider.value = oldSensitivity; yield return Click(ui.backButton);
            Check(GameSession.Instance.Mode == ScreenMode.Menu, "Back button returns from options to menu");
            yield return Click(ui.playButton); yield return new WaitForSecondsRealtime(0.7f);
            var session = GameSession.Instance;
            Check(session.Stage.stageNumber == 1 && session.IsPlaying, "Play starts stage one");
            Check(session.Health == 100 && !session.HasKey, "New game resets player state");
            yield return Capture("03-fase-1");
            var player = session.Stage.player;
            var start = player.transform.position;
            Key(UnityEngine.InputSystem.Key.W, UnityEngine.InputSystem.Key.LeftShift); yield return new WaitForSecondsRealtime(0.65f); Key(); yield return new WaitForSecondsRealtime(0.3f);
            Debug.Log("Movement diagnostic: before=" + start + " after=" + player.transform.position + " active=" + player.GameplayEnabled + " keyboard=" + Keyboard.current.enabled + " scheme=" + player.GetComponent<PlayerInput>().currentControlScheme + " actions=" + player.GetComponent<PlayerInput>().currentActionMap?.enabled);
            Check(Vector3.Distance(start, player.transform.position) > 1.5f, "Keyboard input moves the character through the controller");
            float ground = player.transform.position.y;
            Key(UnityEngine.InputSystem.Key.Space); yield return new WaitForSecondsRealtime(0.22f);
            Check(player.transform.position.y > ground + 0.2f, "Space makes the character jump");
            Key(); yield return new WaitForSecondsRealtime(1);
            float before = player.CinemachineCameraTarget.transform.eulerAngles.y;
            if (Mouse.current == null) InputSystem.AddDevice<Mouse>();
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { delta = new Vector2(35, 0) }); yield return null; yield return null;
            Check(Mathf.Abs(Mathf.DeltaAngle(before, player.CinemachineCameraTarget.transform.eulerAngles.y)) > 1, "Mouse input rotates the camera target");
            Teleport(new Vector3(0, 0.1f, 8)); Key(); yield return new WaitForSecondsRealtime(0.5f);
            string[] names = { "Dance1", "Headspin Start", "GangnamStyle", "Dance2", "Waving", "Chutar" };
            Key[] keys = { UnityEngine.InputSystem.Key.Digit1, UnityEngine.InputSystem.Key.Digit2, UnityEngine.InputSystem.Key.Digit3, UnityEngine.InputSystem.Key.Digit4, UnityEngine.InputSystem.Key.Digit5, UnityEngine.InputSystem.Key.F };
            for (int i = 0; i < names.Length; i++)
            {
                Key(keys[i]); yield return new WaitForSecondsRealtime(0.35f);
                var animator = player.GetComponent<Animator>();
                Check(animator.GetBool(names[i]) && animator.GetCurrentAnimatorStateInfo(0).IsName(names[i]), "Animation enters: " + names[i]);
                Key(); yield return new WaitForSecondsRealtime(0.35f);
                Check(!animator.GetBool(names[i]) && animator.GetCurrentAnimatorStateInfo(0).IsName("Locomoção"), "Animation returns to movement: " + names[i]);
            }
            Key(UnityEngine.InputSystem.Key.Escape); yield return null; yield return null; Key(); yield return null;
            Check(session.Mode == ScreenMode.Paused && Time.timeScale == 0 && !player.GameplayEnabled, "Escape pauses time and player controls");
            yield return Capture("04-pausa");
            Check(!FindObjectsByType<Collectible>(FindObjectsSortMode.None)[0].TryCollect(), "Items cannot be collected during pause");
            FindFirstObjectByType<GameUI>().menuButton.onClick.Invoke(); yield return new WaitForSecondsRealtime(0.3f);
            Check(session.Mode == ScreenMode.Menu && Time.timeScale == 1, "Returning to menu from pause restores time");
            FindFirstObjectByType<GameUI>().playButton.onClick.Invoke(); yield return new WaitForSecondsRealtime(0.5f);
            var initial = FindObjectsByType<Collectible>(FindObjectsSortMode.None);
            Check(initial.Length == 6, "Stage one contains its six goal items");
            for (int i = 0; i < initial.Length; i++)
            {
                var item = initial[i]; Teleport(item.transform.position + Vector3.up * 0.08f); yield return new WaitForSecondsRealtime(0.2f);
                Check(item == null || item.Consumed, "Character collision collects item " + i);
                if (i == 0) { Check(!item.TryCollect(), "A consumed item cannot count twice"); Check(SceneManager.GetActiveScene().name == "cena-1", "Incomplete goals keep the current stage"); }
            }
            yield return new WaitForSecondsRealtime(1.4f);
            Check(session.Stage.stageNumber == 2, "Completing all three goals advances to stage two");
            yield return Capture("05-fase-2");
            var free = DoorInteraction.Active.First(d => !d.requiresKey); var locked = DoorInteraction.Active.First(d => d.requiresKey);
            Check(!free.TryInteract(), "Door rejects interaction outside reach");
            Teleport(free.transform.position + Vector3.back * 2.6f); yield return new WaitForSecondsRealtime(0.2f);
            Key(UnityEngine.InputSystem.Key.E); yield return null; yield return null; Key(); yield return new WaitForSecondsRealtime(0.9f);
            Check(free.IsOpen && Quaternion.Angle(free.hinge.localRotation, Quaternion.identity) > 85, "E opens the free door and animates its hinge");
            Check(free.TryInteract(), "An open door can be closed"); yield return new WaitForSecondsRealtime(0.9f);
            Check(!free.IsOpen && Quaternion.Angle(free.hinge.localRotation, Quaternion.identity) < 2, "Door closes fully");
            Teleport(locked.transform.position + Vector3.back * 2.6f); yield return new WaitForSecondsRealtime(0.15f);
            Check(!locked.TryInteract() && !locked.IsOpen, "Locked door stays closed without key");
            session.Stage.player.GetComponent<CharacterController>().Move(Vector3.forward * 6);
            Check(session.Stage.player.transform.position.z < locked.transform.position.z, "Closed door physically blocks the player");
            var key = FindObjectsByType<Collectible>(FindObjectsSortMode.None).First(c => c.kind == ItemKind.Key);
            Teleport(key.transform.position); yield return new WaitForSecondsRealtime(0.2f);
            Check(session.HasKey, "Key is collected through collision");
            Teleport(locked.transform.position + Vector3.back * 2.6f); yield return new WaitForSecondsRealtime(0.2f);
            Check(locked.TryInteract(), "Key unlocks the second door"); yield return new WaitForSecondsRealtime(1);
            session.Stage.player.GetComponent<CharacterController>().Move(Vector3.forward * 6);
            Check(session.Stage.player.transform.position.z > locked.transform.position.z, "Open door allows the player to pass");
            Teleport(FindFirstObjectByType<ExitGate>().transform.position); yield return new WaitForSecondsRealtime(0.5f);
            Check(session.Stage.stageNumber == 3, "Door stage exit loads stage three");
            yield return Capture("06-fase-3");
            Check(!session.Stage.TryExit() && session.IsPlaying, "Final exit requires the collection goals");
            var fullKit = FindObjectsByType<Collectible>(FindObjectsSortMode.None).First(c => c.kind == ItemKind.Health);
            Check(!fullKit.TryCollect() && !fullKit.Consumed, "Health kit is preserved at full health");
            var bomb = FindObjectsByType<Collectible>(FindObjectsSortMode.None).First(c => c.kind == ItemKind.Bomb);
            Teleport(bomb.transform.position); yield return new WaitForSecondsRealtime(0.2f);
            Check(session.Health == 75, "One bomb applies exactly 25 damage");
            Teleport(fullKit.transform.position); yield return new WaitForSecondsRealtime(0.2f);
            Check(session.Health == 100, "Kit heals and clamps health to the maximum");
            foreach (var b in FindObjectsByType<Collectible>(FindObjectsSortMode.None).Where(c => c.kind == ItemKind.Bomb).ToArray())
            {
                if (!session.IsPlaying) break;
                Teleport(b.transform.position); yield return new WaitForSecondsRealtime(0.2f);
            }
            Check(session.Health == 0 && session.Mode == ScreenMode.Defeat, "Zero health triggers defeat without negative health");
            yield return Capture("07-derrota");
            FindFirstObjectByType<GameUI>().resultRestartButton.onClick.Invoke(); yield return new WaitForSecondsRealtime(0.5f);
            Check(session.Health == 100 && session.IsPlaying && Time.timeScale == 1, "Retry restores phase entry state and time");
            Check(FindObjectsByType<Collectible>(FindObjectsSortMode.None).Count(c => c.kind == ItemKind.Bomb) == 6, "Retry restores consumed world objects");
            foreach (var item in FindObjectsByType<Collectible>(FindObjectsSortMode.None).Where(c => c.kind <= ItemKind.Teapot).ToArray())
            {
                Teleport(item.transform.position); yield return new WaitForSecondsRealtime(0.2f);
            }
            Check(session.Stage.goals.Complete && session.IsPlaying, "Final collection still requires reaching the exit");
            Teleport(FindFirstObjectByType<ExitGate>().transform.position); yield return new WaitForSecondsRealtime(0.3f);
            Check(session.Mode == ScreenMode.Victory, "Final exit produces victory");
            yield return Capture("08-vitoria");
            FindFirstObjectByType<GameUI>().resultRestartButton.onClick.Invoke(); yield return new WaitForSecondsRealtime(0.5f);
            Check(session.Stage.stageNumber == 1 && session.Health == 100 && session.Stage.goals.Coins == 0 && !session.HasKey, "Play again starts a fresh run");
            Check(report.errors.Count == 0, "No runtime errors or exceptions were logged");
        }
    }
}
#endif
