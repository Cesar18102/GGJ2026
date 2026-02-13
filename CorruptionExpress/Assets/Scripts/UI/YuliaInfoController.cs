using System.Collections;
using TMPro;
using UnityEngine;

public class YuliaInfoController : MonoBehaviour
{
    [SerializeField] private RectTransform _yuliaHead;
    [SerializeField] private Vector2 _hiddenPosition;
    [SerializeField] private Vector2 _shownPosition;
    [SerializeField] private float _headShowDuration = 0.5f;

    [SerializeField] private GameObject _infoHolder;
    [SerializeField] private TMP_Text _infoText;

    private bool _isYuliaShown = false;
    private int _infoIndex = 0;

    private string[] _infos = new string[]
    {
        "Бу",
        "Прибрать НАБУ",
        "Я цю систему парадила! Я введу тебе в курс діла.",
        "Перекрию вас як газову трубу"
    };

    public void OnYuliaClick()
    {
        _isYuliaShown = !_isYuliaShown;
        StartCoroutine(Animate(_isYuliaShown));
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
        Vector2 fromPos = _yuliaHead.anchoredPosition;
        Vector2 toPos = show ? _shownPosition : _hiddenPosition;

        while (t < _headShowDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / _headShowDuration);
            k = 1f - Mathf.Pow(1f - k, 3f);

            _yuliaHead.anchoredPosition = Vector2.Lerp(fromPos, toPos, k);
            yield return null;
        }

        _yuliaHead.anchoredPosition = toPos;
        _infoHolder.SetActive(_isYuliaShown);
        UpdateInfo();
    }

    private void UpdateInfo()
    {
        _infoText.text = _infos[_infoIndex];
    }
}
