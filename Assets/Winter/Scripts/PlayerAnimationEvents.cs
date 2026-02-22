using UnityEngine;

namespace Winter.Player
{
    public class PlayerAnimationEvents : MonoBehaviour
    {
        public void AnimEvent_Step()
        {
            var controller = PlayerController.Instance;
            if (controller != null)
            {
                controller.OnStepSFX?.Invoke();
            }

        }
    }
}

