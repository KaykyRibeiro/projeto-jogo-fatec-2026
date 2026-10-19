using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace ValeDasReliquias
{
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }
        public ScreenMode Mode { get; private set; }
        public float Health { get; private set; } = 100;
        public const float MaxHealth = 100;
        public bool HasKey { get; private set; }
        public float Volume { get; private set; }
        public float Sensitivity { get; private set; }
        public bool IsPlaying => Mode == ScreenMode.Playing;
        public bool Loading { get; private set; }
        public StageDirector Stage { get; private set; }
        public string Notice { get; private set; }
        public float NoticeUntil { get; private set; }
        float entryHealth = 100;
        ScreenMode optionsReturn;
        AudioSource effects;

        public static GameSession Ensure()
        {
            if (Instance == null) new GameObject("Sessão da partida").AddComponent<GameSession>();
            return Instance;
        }
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat("Vale.Volume", 0.65f));
            Sensitivity = Mathf.Clamp(PlayerPrefs.GetFloat("Vale.Sensitivity", 1f), 0.2f, 3f);
            effects = gameObject.AddComponent<AudioSource>();
            effects.spatialBlend = 0;
            effects.ignoreListenerPause = true;
            AudioListener.volume = Volume;
            Application.targetFrameRate = 60;
        }
        public void Register(StageDirector stage)
        {
            Stage = stage;
            Loading = false;
            HasKey = false;
            Notice = "";
            entryHealth = Health > 0 ? Health : MaxHealth;
            SetMode(stage.stageNumber == 0 ? ScreenMode.Menu : ScreenMode.Playing);
        }
        void Update()
        {
            if (Stage == null || Loading) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (Mode == ScreenMode.Options) CloseOptions();
                else if (Mode == ScreenMode.Playing) SetMode(ScreenMode.Paused);
                else if (Mode == ScreenMode.Paused) Resume();
            }
            if (Stage.player != null) Stage.player.LookSensitivity = Sensitivity;
        }
        public void SetMode(ScreenMode mode)
        {
            Mode = mode;
            bool playing = mode == ScreenMode.Playing;
            Time.timeScale = mode == ScreenMode.Menu || mode == ScreenMode.Playing ? 1 : 0;
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
            if (Stage != null) Stage.SetControls(playing);
        }
        public void StartGame() { Health = MaxHealth; LoadScene("cena-1"); }
        public void RestartStage() { Health = entryHealth; LoadScene(SceneManager.GetActiveScene().name); }
        public void ReturnToMenu() { Health = MaxHealth; LoadScene("Menu"); }
        public void Resume() { if (Mode == ScreenMode.Paused) SetMode(ScreenMode.Playing); }
        public void OpenOptions() { optionsReturn = Mode; SetMode(ScreenMode.Options); }
        public void CloseOptions() { PlayerPrefs.Save(); SetMode(optionsReturn); }
        public void SetVolume(float value) { Volume = Mathf.Clamp01(value); AudioListener.volume = Volume; PlayerPrefs.SetFloat("Vale.Volume", Volume); }
        public void SetSensitivity(float value) { Sensitivity = Mathf.Clamp(value, 0.2f, 3f); PlayerPrefs.SetFloat("Vale.Sensitivity", Sensitivity); }
        public void Quit() { PlayerPrefs.Save(); Application.Quit(); }
        public void LoadScene(string scene)
        {
            if (Loading) return;
            Loading = true;
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene(scene);
        }
        public bool ChangeHealth(float amount)
        {
            if (!IsPlaying || Loading || amount == 0 || (amount > 0 && Health >= MaxHealth)) return false;
            Health = Mathf.Clamp(Health + amount, 0, MaxHealth);
            Notify(amount < 0 ? "Você perdeu vida. Procure uma maleta vermelha." : "Vida recuperada.");
            if (Health <= 0) SetMode(ScreenMode.Defeat);
            return true;
        }
        public void GiveKey() { HasKey = true; Notify("Chave encontrada! Volte ao portão trancado."); }
        public void Win() { if (IsPlaying) SetMode(ScreenMode.Victory); }
        public void Notify(string text) { Notice = text; NoticeUntil = Time.unscaledTime + 4; }
        public void Play(AudioClip clip, float gain = 0.6f) { if (clip != null) effects.PlayOneShot(clip, gain); }
        void OnDestroy() { if (Instance == this) { Instance = null; Time.timeScale = 1; } }
    }
}
