using System.Collections.Generic;
using UnityEngine;

namespace Rooms
{
    public class RoomController : MonoBehaviour
    {
        [Header("Room Configuration")]
        [SerializeField] private List<RoomData> rooms = new();
        [SerializeField] private int startingRoomIndex;

        [Header("Main Camera")]
        [SerializeField] private Camera mainCamera;

        [Header("Previews")]
        [SerializeField] private RoomPreview leftPreview;
        [SerializeField] private RoomPreview rightPreview;

        private int _currentIndex;
        private bool _navigationEnabled = true;

        private void Start()
        {
            _currentIndex = Mathf.Clamp(startingRoomIndex, 0, rooms.Count - 1);

            SetupPreviews();
            UpdateView();
        }

        private void SetupPreviews()
        {
            if (leftPreview != null)
            {
                leftPreview.OnNavigate += NavigateLeft;
            }

            if (rightPreview != null)
            {
                rightPreview.OnNavigate += NavigateRight;
            }
        }

        public void SetNavigationEnabled(bool enabled)
        {
            _navigationEnabled = enabled;
        }

        public bool IsNavigationEnabled => _navigationEnabled;

        public void NavigateLeft()
        {
            if (!_navigationEnabled || _currentIndex <= 0)
            {
                return;
            }

            _currentIndex--;
            UpdateView();
        }

        public void NavigateRight()
        {
            if (!_navigationEnabled || _currentIndex >= rooms.Count - 1)
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

            var leftRoom = _currentIndex > 0 ? rooms[_currentIndex - 1] : null;
            var rightRoom = _currentIndex < rooms.Count - 1 ? rooms[_currentIndex + 1] : null;

            leftPreview?.UpdatePreview(leftRoom?.roomCamera);
            rightPreview?.UpdatePreview(rightRoom?.roomCamera);
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

        public int CurrentRoomIndex => _currentIndex;
        public string CurrentRoomName => rooms.Count > 0 ? rooms[_currentIndex].roomName : "";
        public int RoomCount => rooms.Count;
    }
}
