using Assets.Scripts.Input;
using System;
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
                leftPreview.OnNavigate += OnNavigateLeft;
            }

            if (rightPreview != null)
            {
                rightPreview.OnNavigate += OnNavigateRight;
            }
        }

        public void SetNavigationEnabled(bool enabled)
        {
            _navigationEnabled = enabled;
        }

        public bool IsNavigationEnabled => _navigationEnabled;

        private void OnNavigateLeft()
        {
            if (_currentIndex <= 0)
            {
                return;
            }

            if (_navigationEnabled)
            {
                Navigate(RoomMoveDirection.Left);
            }
        }

        private void OnNavigateRight()
        {
            if (_currentIndex >= rooms.Count - 1)
            {
                return;
            }

            if (_navigationEnabled)
            {
                Navigate(RoomMoveDirection.Right);
            }
        }

        public void Navigate(RoomMoveDirection direction)
        {
            DeactivatePreviewCameras();
            _currentIndex += direction.ToRoomIndexDelta();
            UpdateView();
        }

        public void NavigateToRoom(int index)
        {
            DeactivatePreviewCameras();
            _currentIndex = Mathf.Clamp(index, 0, rooms.Count - 1);
            UpdateView();
        }

        private void DeactivatePreviewCameras()
        {
            var leftRoom = _currentIndex > 0 ? rooms[_currentIndex - 1] : null;
            var rightRoom = _currentIndex < rooms.Count - 1 ? rooms[_currentIndex + 1] : null;

            leftRoom?.roomCamera?.gameObject.SetActive(false);
            rightRoom?.roomCamera?.gameObject.SetActive(false);
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
