using Unity.Netcode;

public class SpotNetState : NetworkBehaviour
{
    public NetworkVariable<bool> HasItem = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
}
