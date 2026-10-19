using UnityEngine;

namespace ValeDasReliquias
{
    public class ExitGate : MonoBehaviour
    {
        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<StarterAssets.ThirdPersonController>() != null) GameSession.Instance?.Stage?.TryExit();
        }
    }
}
