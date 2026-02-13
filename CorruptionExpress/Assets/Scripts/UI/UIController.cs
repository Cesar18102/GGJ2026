using Assets.Scripts.Input;
using Assets.Scripts.UI;
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
        _wearAction.GetComponentInChildren<TMP_Text>().text = state.WearActionText;
        _leftPreview.SetActive(state.PreviewsVisible);
        _rightPreview.SetActive(state.PreviewsVisible);
        _navigateLeft.SetActive(state.NavigationVisible);
        _navigateRight.SetActive(state.NavigationVisible);
    }

    public void OnWear() => OnInput?.Invoke(this, InputData.FromAction(ActionType.Wear));
    public void OnMove() => OnInput?.Invoke(this, InputData.FromAction(ActionType.Move));
    public void OnSearch() => OnInput?.Invoke(this, InputData.FromAction(ActionType.Search));
    public void OnPut() => OnInput?.Invoke(this, InputData.FromAction(ActionType.Put));
    public void OnMoveLeft() => OnInput?.Invoke(this, InputData.FromMoveDirection(RoomMoveDirection.Left));
    public void OnMoveRight() => OnInput?.Invoke(this, InputData.FromMoveDirection(RoomMoveDirection.Right));
}