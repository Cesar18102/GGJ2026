using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RoomController : MonoBehaviour
{
    [Header("Room Configuration")] [SerializeField]
    private List<RoomData> rooms = new();

    [SerializeField] private int startingRoomIndex;

    [Header("Main Camera")] [SerializeField]
    private Camera mainCamera;

    [Header("Preview Textures")] [SerializeField]
    private RenderTexture leftPreviewTexture;

    [SerializeField] private RenderTexture rightPreviewTexture;


    [Header("Preview UI Elements")] [SerializeField]
    private GameObject leftPreviewPanel;

    [SerializeField] private GameObject rightPreviewPanel;
    [SerializeField] private RawImage leftPreviewImage;
    [SerializeField] private RawImage rightPreviewImage;

    [Header("Navigation Buttons")] [SerializeField]
    private Button leftNavigationButton;

    [SerializeField] private Button rightNavigationButton;

    private int _currentIndex;

    private void Start()
    {
        _currentIndex = Mathf.Clamp(startingRoomIndex, 0, rooms.Count - 1);

        SetupButtonListeners();
        SetupPreviewClickHandlers();
        UpdateView();
    }

    private void SetupButtonListeners()
    {
        if (leftNavigationButton != null)
        {
            leftNavigationButton.onClick.AddListener(NavigateLeft);
        }

        if (rightNavigationButton != null)
        {
            rightNavigationButton.onClick.AddListener(NavigateRight);
        }
    }

    private void SetupPreviewClickHandlers()
    {
        if (leftPreviewImage != null)
        {
            var leftButton = leftPreviewImage.GetComponent<Button>();
            if (leftButton == null)
            {
                leftButton = leftPreviewImage.gameObject.AddComponent<Button>();
            }

            leftButton.onClick.AddListener(NavigateLeft);
        }

        if (rightPreviewImage != null)
        {
            var rightButton = rightPreviewImage.GetComponent<Button>();
            if (rightButton == null)
            {
                rightButton = rightPreviewImage.gameObject.AddComponent<Button>();
            }

            rightButton.onClick.AddListener(NavigateRight);
        }
    }

    public void NavigateLeft()
    {
        if (_currentIndex <= 0)
        {
            return;
        }

        _currentIndex--;
        UpdateView();
    }

    public void NavigateRight()
    {
        if (_currentIndex >= rooms.Count - 1)
        {
            return;
        }

        _currentIndex++;
        UpdateView();
    }

    public void NavigateToRoom(int index)
    {
        _currentIndex = Mathf.Clamp(index, 0, rooms.Count - 1);
        UpdateView();
    }

    private void UpdateView()
    {
        if (rooms.Count == 0)
        {
            return;
        }

        UpdateMainCamera();
        UpdateLeftPreview();
        UpdateRightPreview();
    }

    private void UpdateMainCamera()
    {
        var currentRoom = rooms[_currentIndex];
        if (mainCamera == null || currentRoom.roomCamera == null)
        {
            return;
        }

        mainCamera.transform.position = currentRoom.roomCamera.transform.position;
        mainCamera.transform.rotation = currentRoom.roomCamera.transform.rotation;
    }

    private void UpdateLeftPreview()
    {
        var hasPreviousRoom = _currentIndex > 0;
        if (leftPreviewPanel != null)
        {
            leftPreviewPanel.SetActive(hasPreviousRoom);
        }

        if (!hasPreviousRoom)
        {
            return;
        }

        var prevIndex = _currentIndex - 1;
        var prevRoom = rooms[prevIndex];

        if (prevRoom.roomCamera == null || leftPreviewTexture == null)
        {
            return;
        }

        prevRoom.roomCamera.targetTexture = leftPreviewTexture;
        prevRoom.roomCamera.Render();
    }

    private void UpdateRightPreview()
    {
        var hasNextRoom = _currentIndex < rooms.Count - 1;

        if (rightPreviewPanel != null)
        {
            rightPreviewPanel.SetActive(hasNextRoom);
        }

        if (!hasNextRoom)
        {
            return;
        }

        var nextIndex = _currentIndex + 1;
        var nextRoom = rooms[nextIndex];

        if (nextRoom.roomCamera == null || rightPreviewTexture == null)
        {
            return;
        }

        nextRoom.roomCamera.targetTexture = rightPreviewTexture;
        nextRoom.roomCamera.Render();
    }

    public int CurrentRoomIndex => _currentIndex;
    public string CurrentRoomName => rooms.Count > 0 ? rooms[_currentIndex].roomName : "";
    public int RoomCount => rooms.Count;
}