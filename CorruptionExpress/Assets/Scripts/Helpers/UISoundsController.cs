using UnityEngine;

public class UISoundsController : MonoBehaviour
{
    [SerializeField] private AudioSource _uiAudio;

    public void ClickSound()
    {
        if (_uiAudio.isPlaying)
        {
            _uiAudio.Stop();
            _uiAudio.time = 0;
        }

        _uiAudio.Play();
    }
}
