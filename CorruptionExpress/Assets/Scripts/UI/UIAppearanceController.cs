using Teams;
using UnityEngine;
using UnityEngine.UI;

public class UIAppearanceController : MonoBehaviour
{
    [SerializeField]
    private Sprite _previewWrapperNabu;

    [SerializeField]
    private Sprite _previewWrapperCorrupt;

    [SerializeField]
    private Image _leftPreviewWrapper;

    [SerializeField]
    private Image _rightPreviewWrapper;

    [SerializeField]
    private Sprite _leftArrowNabu;

    [SerializeField]
    private Sprite _rightArrowNabu;

    [SerializeField]
    private Sprite _leftArrowCorrupt;

    [SerializeField]
    private Sprite _rightArrowCorrupt;

    [SerializeField]
    private Image _leftArrow;

    [SerializeField]
    private Image _rightArrow;

    [SerializeField]
    private Sprite _minimizeButtonNabu;

    [SerializeField]
    private Sprite _minimizeButtonCorrupt;

    [SerializeField]
    private Image _leftMinimizeButton;

    [SerializeField]
    private Image _rightMinimizeButton;

    [SerializeField]
    private Sprite _maximizeButtonNabu;

    [SerializeField]
    private Sprite _maximizeButtonCorrupt;

    [SerializeField]
    private Image _leftMaximizeButton;

    [SerializeField]
    private Image _rightMaximizeButton;

    public void UpdateUI(Team team)
    {
        Sprite previewWrapperSprite = GetSpriteAccordingToTeam(_previewWrapperNabu, _previewWrapperCorrupt, team);

        _leftPreviewWrapper.sprite = previewWrapperSprite;
        _rightPreviewWrapper.sprite = previewWrapperSprite;

        _leftArrow.sprite = GetSpriteAccordingToTeam(_leftArrowNabu, _leftArrowCorrupt, team);
        _rightArrow.sprite = GetSpriteAccordingToTeam(_rightArrowNabu, _rightArrowCorrupt, team);

        Sprite minimizeButtonSprite = GetSpriteAccordingToTeam(_minimizeButtonNabu, _minimizeButtonCorrupt, team);

        _leftMinimizeButton.sprite = minimizeButtonSprite;
        _rightMinimizeButton.sprite = minimizeButtonSprite;
 
        Sprite maximizeButtonSprite = GetSpriteAccordingToTeam(_maximizeButtonNabu, _maximizeButtonCorrupt, team);

        _leftMaximizeButton.sprite = maximizeButtonSprite;
        _rightMaximizeButton.sprite = maximizeButtonSprite;
    }

    private Sprite GetSpriteAccordingToTeam(Sprite nabuSprite, Sprite corruptSprite, Team team)
    {
        return team switch
        {
            Team.Nabu => nabuSprite,
            Team.CorruptOfficials => corruptSprite,
            _ => throw new UnityException("team is not assigned while attempting to set customized UI")
        };
    }
}
