using System.Collections;
using TMPro;
using UnityEngine;

public class InfoHeadController : MonoBehaviour
{
    [SerializeField] private RectTransform _head;
    [SerializeField] private Vector2 _hiddenPosition;
    [SerializeField] private Vector2 _shownPosition;
    [SerializeField] private float _headShowDuration = 0.5f;

    [SerializeField] private GameObject _infoHolder;
    [SerializeField] private TMP_Text _infoText;

    [SerializeField]
    private string[] _infos;

    private bool _isShown = false;
    private int _infoIndex = 0;

    public void OnHeadClick()
    {
        _isShown = !_isShown;
        StartCoroutine(Animate(_isShown));
    }

    public void OnNextInfo()
    {
        if (_infoIndex < _infos.Length - 1)
        {
            _infoIndex++;
        }

        UpdateInfo();
    }

    public void OnPrevInfo()
    {
        if (_infoIndex > 0)
        {
            _infoIndex--;
        }

        UpdateInfo();
    }

    IEnumerator Animate(bool show)
    {
        float t = 0f;
        Vector2 fromPos = _head.anchoredPosition;
        Vector2 toPos = show ? _shownPosition : _hiddenPosition;

        while (t < _headShowDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / _headShowDuration);
            k = 1f - Mathf.Pow(1f - k, 3f);

            _head.anchoredPosition = Vector2.Lerp(fromPos, toPos, k);
            yield return null;
        }

        _head.anchoredPosition = toPos;
        _infoHolder.SetActive(_isShown);
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        _infoText.text = _infos[_infoIndex];
    }
}
