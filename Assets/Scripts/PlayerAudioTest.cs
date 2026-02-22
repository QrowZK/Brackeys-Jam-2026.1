using UnityEngine;
using UnityEngine.InputSystem; // ¡Esto es lo nuevo!

public class PlayerAudioTest : MonoBehaviour
{
    [Header("Wwise Events")]
    public AK.Wwise.Event footstepEvent;
    public AK.Wwise.Event jumpEvent;
    
    [Header("Switch Settings")]
    private string switchGroup = "Surface_Type";

    void Update()
    {
        // Nueva forma de detectar la tecla F
        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            DetectSurfaceAndPlay();
        }

        // Nueva forma de detectar la tecla Espacio
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            PlayJumpSound();
        }
    }

    void DetectSurfaceAndPlay()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 5.0f))
        {
            string surfaceTag = hit.collider.tag;
            
            if (surfaceTag == "Concrete")
                AkSoundEngine.SetSwitch(switchGroup, "Concrete", gameObject);
            else if (surfaceTag == "Wood")
                AkSoundEngine.SetSwitch(switchGroup, "Wood", gameObject);

            if (footstepEvent.IsValid())
            {
                footstepEvent.Post(gameObject);
                Debug.Log("Pisando: " + surfaceTag);
            }
        }
    }

    void PlayJumpSound()
    {
        if (jumpEvent.IsValid())
        {
            jumpEvent.Post(gameObject);
            Debug.Log("¡Salto!");
        }
    }
}