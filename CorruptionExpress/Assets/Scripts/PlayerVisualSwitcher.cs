using Teams;
using Unity.Netcode;
using UnityEngine;

public class PlayerVisualSwitcher : NetworkBehaviour
{
    [SerializeField] private GameObject visualNabu;
    [SerializeField] private GameObject visualCorrupt;

    private PlayerTeamController _team;

    private void Awake()
    {
        _team = GetComponent<PlayerTeamController>();
    }

    public override void OnNetworkSpawn()
    {
        Apply(_team.AssignedTeam.Value);
        _team.AssignedTeam.OnValueChanged += (_, t) => Apply(t);
    }

    private void Apply(Team t)
    {
        if (visualNabu != null)
        {
            visualNabu.SetActive(t == Team.Nabu);
        }

        if (visualCorrupt != null)
        {
            visualCorrupt.SetActive(t == Team.CorruptOfficials);
        }
    }
}
