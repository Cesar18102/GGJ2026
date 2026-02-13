using Assets.Scripts.GameState;
using GameState;
using Teams;

namespace Assets.Scripts.UI
{
    public struct UIState
    {
        public bool PreviewsVisible { get; set; }
        public bool NavigationVisible { get; set; }
        public bool ActionsVisible { get; set; }
        public bool WearActionVisible { get; set; }
        public string WearActionText { get; set; }
        public Team WinTeam { get; set; }
        public WinReason Reason { get; set; }
    }
}
