using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rooms
{
    public class RoomPreview : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private RawImage image;
        [SerializeField] private RenderTexture previewTexture;
        [SerializeField] private Button navigationButton;
        [SerializeField] private Button maximizeButton;
        [SerializeField] private Button minimizeButton;

        public event Action OnNavigate;

        private void Start()
        {
            image?.GetComponent<Button>()?.onClick.AddListener(Navigate);
            navigationButton?.onClick.AddListener(Navigate);

            maximizeButton?.onClick.AddListener(Maximize);
            minimizeButton?.onClick.AddListener(Minimize);
        }

        private void Maximize()
        {
            SetPanelContentVisible(true);
            maximizeButton?.gameObject.SetActive(false);
            minimizeButton?.gameObject.SetActive(true);
        }

        private void Minimize()
        {
            SetPanelContentVisible(false);
            maximizeButton?.gameObject.SetActive(true);
            minimizeButton?.gameObject.SetActive(false);
        }

        private void SetPanelContentVisible(bool visible)
        {
            if (panel == null)
            {
                return;
            }

            foreach (Transform child in panel.transform)
            {
                var go = child.gameObject;
                if (go == maximizeButton?.gameObject || go == minimizeButton?.gameObject)
                {
                    continue;
                }

                go.SetActive(visible);
            }
        }

        private void Navigate()
        {
            OnNavigate?.Invoke();
        }

        public void UpdatePreview(Camera roomCamera)
        {
            var hasRoom = roomCamera != null;

            panel?.SetActive(hasRoom);
            navigationButton?.gameObject.SetActive(hasRoom);

            if (!hasRoom || previewTexture == null)
            {
                return;
            }

            roomCamera.targetTexture = previewTexture;
            roomCamera.Render();
        }

        public void Hide()
        {
            panel?.SetActive(false);
            navigationButton?.gameObject.SetActive(false);
        }
    }
}
