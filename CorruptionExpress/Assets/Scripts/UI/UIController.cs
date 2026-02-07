using Assets.Scripts.Input;
using System;
using System.Collections.Generic;
using Teams;
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

    public event EventHandler<InputData> OnInput;

    public void UpdateItems(IEnumerable<ItemController> items)
    {
        int i = 0;

        foreach(ItemController item in items)
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

    public void OnWear() => OnInput?.Invoke(this, GetInputData(ActionType.Wear));
    public void OnMove() => OnInput?.Invoke(this, GetInputData(ActionType.Move));
    public void OnSearch() => OnInput?.Invoke(this, GetInputData(ActionType.Search));
    public void OnPut() => OnInput?.Invoke(this, GetInputData(ActionType.Put));

    private InputData GetInputData(ActionType action)
    {
        return new InputData()
        {
            ActionType = action,
            MoveDirection = RoomMoveDirection.None,
            SpotInput = SpotInput.Empty,
            TargetClientId = 0
        };
    }
}