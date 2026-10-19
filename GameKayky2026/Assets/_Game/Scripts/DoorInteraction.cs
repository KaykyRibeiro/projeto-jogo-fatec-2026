using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ValeDasReliquias
{
    public class DoorInteraction : MonoBehaviour
    {
        public static readonly HashSet<DoorInteraction> Active = new HashSet<DoorInteraction>();
        public bool requiresKey;
        public Transform hinge;
        public Animator animator;
        public bool IsOpen { get; private set; }
        public bool IsMoving { get; private set; }
        void OnEnable() { Active.Add(this); }
        void OnDisable() { Active.Remove(this); }
        public string GetPrompt(bool hasKey)
        {
            if (requiresKey && !hasKey) return "PORTÃO TRANCADO  ·  Procure a chave dourada";
            if (IsMoving) return "Aguarde a porta...";
            return IsOpen ? "E  ·  Fechar porta" : "E  ·  Abrir porta";
        }
        public bool TryInteract()
        {
            var session = GameSession.Instance;
            if (session == null || !session.IsPlaying || IsMoving || session.Stage?.player == null) return false;
            if (Vector3.Distance(transform.position, session.Stage.player.transform.position) > 3.2f) return false;
            if (requiresKey && !session.HasKey) { session.Notify("O portão precisa de uma chave."); return false; }
            IsOpen = !IsOpen;
            if (IsOpen && requiresKey) session.Stage.MarkDoorOpened();
            session.Play(session.Stage.doorSound, 0.3f);
            StartCoroutine(Animate());
            return true;
        }
        IEnumerator Animate()
        {
            IsMoving = true;
            animator.Play(IsOpen ? "Abrir" : "Fechar", 0, 0);
            yield return new WaitForSeconds(0.85f);
            IsMoving = false;
        }
    }
}
