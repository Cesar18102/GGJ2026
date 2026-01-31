using UnityEngine;

public class GoToSpotActionHandler : GameActionHandler
{
    [SerializeField]
    private CharacterNavigationController _characterNav;

    public GoToSpotActionHandler() : base()
    {

    }

    public override void OnSceneObjectClicked(GameObject sceneObject)
    {
        if (sceneObject.tag == "Spot")
        {
            Spot spot = sceneObject.GetComponent<Spot>();
            _characterNav.GoTo(spot);
        }
    }
}