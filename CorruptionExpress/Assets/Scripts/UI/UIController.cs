using Assets.Scripts.GameState;
using Assets.Scripts.Input;
using Assets.Scripts.UI;
using GameState;
using System;
using System.Collections.Generic;
using Teams;
using TMPro;
using UnityEngine;

public class UIController : MonoBehaviour
{
    [SerializeField]
    private ItemSlotController[] _itemSlotControllers;

    [SerializeField]
    private GameObject _nabuPanel;

    [SerializeField]
    private TMPro.TMP_Text _nabuWaitText;

    [SerializeField]
    private GameObject _corruptionPanel;

    [SerializeField]
    private GameObject[] _moneyIndicators;

    [SerializeField]
    private GameObject _actionsContainer;

    [SerializeField]
    private GameObject _wearAction;

    [SerializeField]
    private GameObject _leftPreview;

    [SerializeField]
    private GameObject _rightPreview;

    [SerializeField]
    private GameObject _navigateLeft;

    [SerializeField]
    private GameObject _navigateRight;

    [SerializeField]
    private GameObject _winIndicator;

    [SerializeField]
    private TMP_Text _winText;

    [SerializeField]
    private AudioSource _nabuMusic;

    [SerializeField]
    private AudioSource _corruptionMusic;

    [SerializeField] 
    private GameObject _uiHolder;

    [SerializeField] 
    private TMP_Text _phaseNameText;

    public event EventHandler<InputData> OnInput;

    public void UpdateItems(IEnumerable<ItemController> items)
    {
        int i = 0;

        foreach (ItemController item in items)
        {
            if (i >= _itemSlotControllers.Length)
            {
                break;
            }

            _itemSlotControllers[i].SetItem(item);
            ++i;
        }

        for (; i < _itemSlotControllers.Length; ++i)
        {
            _itemSlotControllers[i].UnsetItem();
        }
    }

    public void UpdateItems(int count)
    {
        int i = 0;
        for (; i < count; ++i)
        {
            _moneyIndicators[i].SetActive(true);
        }

        for (; i < _moneyIndicators.Length; ++i)
        {
            _moneyIndicators[i].SetActive(false);
        }
    }

    public void StartMusic(Team team)
    {
        _nabuMusic.gameObject.SetActive(team == Team.Nabu);
        _corruptionMusic.gameObject.SetActive(team == Team.CorruptOfficials);
    }

    public void ShowPreparePanel(Team team)
    {
        _nabuPanel.SetActive(team == Team.Nabu);
        _corruptionPanel.SetActive(team == Team.CorruptOfficials);
    }

    public void SetRemainingItemsToWait(int remainingItems)
    {
        if (remainingItems > 0)
        {
            _nabuWaitText.text = $"Заходимо за {remainingItems}";
        }
        else
        {
            _nabuPanel.SetActive(false);
        }
    }

    public void CorruptionReady()
    {
        _corruptionPanel.SetActive(false);
    }

    public void UpdateState(UIState state)
    {
        _actionsContainer.SetActive(state.ActionsVisible);
        _wearAction.SetActive(state.WearActionVisible);

        TMP_Text wearActionTextHolder = _wearAction.GetComponentInChildren<TMP_Text>();
        if (wearActionTextHolder is not null)
        {
            wearActionTextHolder.text = state.WearActionText;
        }

        _leftPreview.SetActive(state.PreviewLeftVisible);
        _rightPreview.SetActive(state.PreviewRightVisible);
        _navigateLeft.SetActive(state.NavigateLeftVisible);
        _navigateRight.SetActive(state.NavigateRightVisible);

        _winIndicator.SetActive(state.WinTeam != Team.Unassigned);
        _winText.text = state.Reason switch
        {
            WinReason.None => string.Empty,
            WinReason.EvidencesFound => "Агенти НАБУ знайшли достатню кількість доказів. Честь маю!",
            WinReason.Deanon => "Значну кількість агентів було деанонімізовано!",
            WinReason.RoundsPassed => "Обшук пройшов невдало!"
        };
        
        _uiHolder.SetActive(state.TurnOrderUIShown);
        _phaseNameText.text = state.RoundPhaseTurnInfo;
    }

    public void OnWear() => OnInput?.Invoke(this, InputData.FromAction(ActionType.Wear));
    public void OnMove() => OnInput?.Invoke(this, InputData.FromAction(ActionType.Move));
    public void OnSearch() => OnInput?.Invoke(this, InputData.FromAction(ActionType.Search));
    public void OnPut() => OnInput?.Invoke(this, InputData.FromAction(ActionType.Put));
    public void OnMoveLeft() => OnInput?.Invoke(this, InputData.FromMoveDirection(RoomMoveDirection.Left));
    public void OnMoveRight() => OnInput?.Invoke(this, InputData.FromMoveDirection(RoomMoveDirection.Right));
}