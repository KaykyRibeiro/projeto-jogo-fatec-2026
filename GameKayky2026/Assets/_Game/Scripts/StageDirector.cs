using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

namespace ValeDasReliquias
{
    [DefaultExecutionOrder(-100)]
    public class StageDirector : MonoBehaviour
    {
        [Range(0, 3)] public int stageNumber;
        public string stageTitle;
        [TextArea] public string objective;
        public string nextScene;
        public bool autoAdvance;
        public CollectionGoals goals = new CollectionGoals();
        public ThirdPersonController player;
        public AudioClip collectSound, keySound, hurtSound, healSound, doorSound;
        public bool Completed { get; private set; }
        public bool LockedDoorOpened { get; private set; }
        public string Prompt { get; private set; }
        public DoorInteraction CurrentDoor { get; private set; }
        float scanTime;

        void Awake()
        {
            goals.Reset();
            GameSession.Ensure().Register(this);
        }
        void Start()
        {
            if (player == null) return;
            var input = player.GetComponent<PlayerInput>();
            input.neverAutoSwitchControlSchemes = true;
            if (Keyboard.current != null && Mouse.current != null) input.SwitchCurrentControlScheme("KeyboardMouse", Keyboard.current, Mouse.current);
            SetControls(GameSession.Instance.IsPlaying);
        }
        void Update()
        {
            var session = GameSession.Instance;
            if (stageNumber == 0 || player == null || session == null || !session.IsPlaying || session.Loading) { Prompt = ""; return; }
            if (player.transform.position.y < -8) session.ChangeHealth(-GameSession.MaxHealth);
            if (Time.unscaledTime >= scanTime)
            {
                scanTime = Time.unscaledTime + 0.08f;
                CurrentDoor = null;
                float distance = 3.2f;
                foreach (var door in DoorInteraction.Active)
                {
                    float d = Vector3.Distance(player.transform.position, door.transform.position);
                    if (d < distance) { distance = d; CurrentDoor = door; }
                }
            }
            Prompt = CurrentDoor == null ? "" : CurrentDoor.GetPrompt(session.HasKey);
            if (CurrentDoor != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) CurrentDoor.TryInteract();
        }
        public bool Collect(ItemKind kind)
        {
            if (!GameSession.Instance.IsPlaying || GameSession.Instance.Loading || Completed) return false;
            if (!goals.TryCollect(kind)) return false;
            if (goals.Complete && autoAdvance) { Completed = true; StartCoroutine(AdvanceAfterFeedback()); }
            else if (goals.Complete) GameSession.Instance.Notify("Coleção completa! Siga até o arco verde de saída.");
            return true;
        }
        IEnumerator AdvanceAfterFeedback()
        {
            GameSession.Instance.Notify("Coleção completa! Próxima etapa desbloqueada.");
            yield return new WaitForSeconds(1.2f);
            Advance();
        }
        public void MarkDoorOpened() { LockedDoorOpened = true; }
        public bool TryExit()
        {
            if (!GameSession.Instance.IsPlaying || Completed) return false;
            if (stageNumber == 2 && !LockedDoorOpened) { GameSession.Instance.Notify("Encontre a chave e abra o portão de saída."); return false; }
            if (stageNumber == 3 && !goals.Complete) { GameSession.Instance.Notify("Ainda faltam relíquias. Confira os contadores."); return false; }
            Completed = true;
            Advance();
            return true;
        }
        void Advance()
        {
            if (!GameSession.Instance.IsPlaying) return;
            if (stageNumber == 3) GameSession.Instance.Win();
            else if (!string.IsNullOrEmpty(nextScene)) GameSession.Instance.LoadScene(nextScene);
        }
        public void SetControls(bool enabled)
        {
            if (player == null) return;
            player.GameplayEnabled = enabled;
            var input = player.GetComponent<StarterAssetsInputs>();
            input.cursorLocked = enabled;
            input.cursorInputForLook = enabled;
            input.move = Vector2.zero; input.look = Vector2.zero; input.jump = false; input.sprint = false;
            var playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.isActiveAndEnabled)
            {
                if (enabled) playerInput.ActivateInput(); else playerInput.DeactivateInput();
            }
        }
    }
}
