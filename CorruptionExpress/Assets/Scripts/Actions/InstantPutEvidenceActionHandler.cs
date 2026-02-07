using Assets.Scripts.Input;
using System.Collections;
using Teams;
using UnityEngine;

public class InstantPutEvidenceActionHandler : GameActionHandler
{
    public override bool CanExecute(InputData input) => input.HasSpotInput();
    public override void Execute(InputData input)
    {
        if (CanExecute(input))
        {
            //PlayerNetState state = PlayerTeamController.GetLocalPlayer().NetworkObject.GetComponent<PlayerNetState>();

            //state.SpendEvidenceServerRpc(1);
            //spot.PutItem();
        }
    }

    public override IEnumerator WaitForEnd() {
        yield break;
    }

    public override IEnumerator WaitForStart() {
        yield break;
    }
}
