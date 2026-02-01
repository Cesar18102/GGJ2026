using Teams;
using UnityEngine;

public class PutEvidenceActionHandler : GameActionHandler
{
    public override void OnSceneObjectClicked(GameObject sceneObject)
    {
        if (sceneObject.tag == "Spot")
        {
            Spot spot = sceneObject.GetComponent<Spot>();

            PlayerNetState state = PlayerTeamController.GetLocalPlayer().NetworkObject.GetComponent<PlayerNetState>();

            state.SpendEvidenceServerRpc(1);
            spot.PutItem();
        }
    }
}
