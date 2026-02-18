using Assets.Scripts.Helpers;
using Teams;
using Unity.Netcode;
using UnityEngine;

public class PlayerVisualSwitcher : NetworkBehaviour
{
    [SerializeField] private GameObject visualNabu;
    [SerializeField] private GameObject visualCorrupt;

    [SerializeField] private Texture2D _cursorNabu;
    [SerializeField] private Texture2D _cursorCorrupt;

    private PlayerNetState _team;

    private void Awake()
    {
        _team = GetComponent<PlayerNetState>();
    }

    public override void OnNetworkSpawn()
    {
        Apply(_team.AssignedTeam.Value);
        _team.AssignedTeam.OnValueChanged += (_, t) => Apply(t);
    }

    private void Apply(Team t)
    {
        visualNabu.SetActive(t == Team.Nabu);
        visualCorrupt.SetActive(t == Team.CorruptOfficials);

        if (PlayersHelper.GetLocalPlayer().OwnerClientId == OwnerClientId)
        {
            Texture2D cursor = t switch
            {
                Team.Nabu => _cursorNabu,
                Team.CorruptOfficials => _cursorCorrupt,
                _ => null
            };

            if (cursor != null)
            {
                Cursor.SetCursor(cursor, Vector2.zero, CursorMode.Auto);
            }
        }
    }
}
