using UnityEngine;

namespace ValeDasReliquias
{
    public class Collectible : MonoBehaviour
    {
        public ItemKind kind;
        [Min(1)] public float amount = 25;
        public bool Consumed { get; private set; }
        void OnTriggerEnter(Collider other) { if (other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null) TryCollect(); }
        void OnTriggerStay(Collider other) { if (!Consumed && kind == ItemKind.Health && other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null) TryCollect(); }
        public bool TryCollect()
        {
            var session = GameSession.Instance;
            if (Consumed || session == null || !session.IsPlaying || session.Loading || session.Stage == null) return false;
            AudioClip clip;
            switch (kind)
            {
                case ItemKind.Health: if (!session.ChangeHealth(amount)) return false; clip = session.Stage.healSound; break;
                case ItemKind.Bomb: if (!session.ChangeHealth(-amount)) return false; clip = session.Stage.hurtSound; break;
                case ItemKind.Key: if (session.HasKey) return false; session.GiveKey(); clip = session.Stage.keySound; break;
                default: if (!session.Stage.Collect(kind)) return false; clip = session.Stage.collectSound; break;
            }
            Consumed = true;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            session.Play(clip);
            Destroy(gameObject);
            return true;
        }
    }
}
