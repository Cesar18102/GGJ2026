using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpotPulse : MonoBehaviour
{
    [Header("Alpha pulse")]
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 0.6f;
    [SerializeField] private float alphaSpeed = 2f;

    [Header("Scale pulse")]
    [SerializeField] private float scaleAmplitude = 0.05f;
    [SerializeField] private float scaleSpeed = 2f;

    private SpriteRenderer _sr;
    private Vector3 _baseScale;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        float t = Mathf.Sin(Time.time * alphaSpeed) * 0.5f + 0.5f;

        // альфа
        Color c = _sr.color;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        _sr.color = c;

        // масштаб
        float scaleOffset = Mathf.Sin(Time.time * scaleSpeed) * scaleAmplitude;
        transform.localScale = _baseScale * (1f + scaleOffset);
    }
}