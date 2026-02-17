using Assets.Scripts.GameState;
using GameState;
using Teams;

namespace Assets.Scripts.UI
{
    public struct UIState
    {
        public bool PreviewLeftVisible { get; set; }
        public bool PreviewRightVisible { get; set; }
        public bool NavigateLeftVisible { get; set; }
        public bool NavigateRightVisible { get; set; }
        public bool ActionsVisible { get; set; }
        public bool WearActionVisible { get; set; }
        public string WearActionText { get; set; }
        public Team WinTeam { get; set; }
        public WinReason Reason { get; set; }
        public bool TurnOrderUIShown { get; set; }
        public string RoundPhaseTurnInfo { get; set; }
    }
}
