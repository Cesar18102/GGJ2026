using Assets.Scripts.Actions;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.ActionHandlers
{
    public static class MoveActionHandler
    {
        public static IEnumerator MoveCo(PlayerNetState state, NavNode2D currentNode, NavNode2D targetNode, Vector3? desiredPosition = null)
        {
            Debug.Log($"Player {state.OwnerClientId}: Move start");

            List<NavNode2D> path = GraphPathfinder2D.FindPath(currentNode, targetNode);

            if (path is null || path.Count < 2)
            {
                path = new List<NavNode2D>() { currentNode, targetNode };
            }

            state.CurrentAnimationType.Value = AnimationType.Move;

            for (int i = 0; i < path.Count; i++) 
            {
                NavNode2D node = path[i];
                Vector3 position = i == path.Count - 1 ? (desiredPosition ?? node.transform.position) : node.transform.position;

                FaceDirection desiredFaceDirection = position.x > state.NetworkObject.transform.position.x ?
                    FaceDirection.Right : FaceDirection.Left;

                UpdateFaceDirection(state, desiredFaceDirection);

                currentNode = node;
                Vector3 desiredScale = Vector3.one * node.GetDesiredScale();

                yield return MoveTo(state.NetworkObject, (Vector2)position, desiredScale);
            }

            state.CurrentPosition.Value = targetNode.transform.GetSiblingIndex();
            state.CurrentAnimationType.Value = AnimationType.None;

            Debug.Log($"Player {state.OwnerClientId}: Move end");
        }

        public static void UpdateFaceDirection(PlayerNetState state, FaceDirection desiredFaceDirection)
        {
            if (state.CurrentFaceDirection != desiredFaceDirection)
            {
                state.CurrentFaceDirection = desiredFaceDirection;
                state.NetworkObject.transform.Rotate(0, 180, 0);
            }
        }

        public static IEnumerator MoveTo(NetworkObject player, Vector2 target, Vector3 targetScale)
        {
            float totalDist = ((Vector2)player.transform.position - target).magnitude;
            float currentDist = totalDist;

            float speed = player.GetComponent<PlayerNetState>().Speed;

            Vector3 startingScale = player.transform.localScale;
            Vector3 dScale = targetScale - startingScale;

            while (currentDist > 0.0025f)
            {
                player.transform.position = Vector2.MoveTowards(player.transform.position, target, speed * Time.deltaTime);
                currentDist = ((Vector2)player.transform.position - target).magnitude;

                player.transform.localScale = startingScale + dScale * (totalDist - currentDist) / totalDist;

                yield return null;
            }
        }
    }
}
