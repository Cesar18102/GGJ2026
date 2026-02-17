using Assets.Scripts.Actions;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class CharacterSoundsController : MonoBehaviour
{
    [SerializeField]
    private AudioClip _stepsSound;

    [SerializeField]
    private AudioClip _searchSound;

    private void Awake()
    {
        GetComponentInParent<PlayerNetState>().CurrentAnimationType.OnValueChanged += OnAnimationTypeChanged;
    }

    private void OnAnimationTypeChanged(AnimationType oldState, AnimationType newState)
    {
        AudioSource source = GetComponent<AudioSource>();

        source.generator = newState switch
        {
            AnimationType.Move => _stepsSound,
            AnimationType.Search or AnimationType.SearchPlayer => _searchSound,
            _ => null
        };

        source.Play();
    }
}
