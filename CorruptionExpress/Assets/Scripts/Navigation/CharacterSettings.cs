using UnityEngine;

namespace Assets.Scripts.Navigation
{
    public class CharacterSettings : MonoBehaviour
    {
        [SerializeField]
        private FaceDirection _staringFaceDirection;

        [SerializeField]
        private float _speed = 2.5f;

        public FaceDirection GetStartingFaceDirection() => _staringFaceDirection;
        public float GetSpeed() => _speed;
    }
}
