using Unity.Netcode;

public class SpotNetState : NetworkBehaviour
{
    public NetworkVariable<int> ItemsCount = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
}
