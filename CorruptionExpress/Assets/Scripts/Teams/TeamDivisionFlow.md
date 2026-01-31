# Team Division System - Game Flow

## Overview

The team division system assigns players to either **Corrupt Officials** or **NABU** teams when the game starts, with team-specific UI and phased action availability.

## Team Distribution Formula

```
nabuCount = Max(1, Floor(playerCount * 0.4))
```

| Players | NABU | Corrupt Officials |
|---------|------|-------------------|
| 2       | 1    | 1                 |
| 3       | 1    | 2                 |
| 4       | 1    | 3                 |
| 5       | 2    | 3                 |
| 6       | 2    | 4                 |
| 7       | 2    | 5                 |

---

## Game Flow Sequence

```
Host Clicks "Start Game" (Lobby)
         │
         ▼
┌─────────────────────────────────┐
│   Scene Loads (GameScene)       │
│   NetworkObjects spawn          │
└─────────────────┬───────────────┘
                  │
                  ▼
┌─────────────────────────────────┐
│   GameStateManager.OnNetworkSpawn()
│   (Host starts GameFlowCoroutine)
└─────────────────┬───────────────┘
                  │
                  ▼
┌─────────────────────────────────┐
│   TeamManager.AssignTeams()     │
│   - Shuffle players randomly    │
│   - Assign via NetworkVariable  │
└─────────────────┬───────────────┘
                  │
                  ▼
┌─────────────────────────────────┐
│   Phase: TeamReveal (5 sec)     │
│   - Full-screen team display    │
│   - All actions blocked         │
└─────────────────┬───────────────┘
                  │
                  ▼
┌─────────────────────────────────┐
│   Phase: NabuWaiting (15 sec)   │
│   - NABU: Waiting overlay       │
│   - Officials: Can navigate     │
└─────────────────┬───────────────┘
                  │
                  ▼
┌─────────────────────────────────┐
│   Phase: MainGameplay           │
│   - Everyone can act            │
│   - Team indicator visible      │
└─────────────────────────────────┘
```

---

## Component Responsibilities

### GameStateManager
- **Location**: `Assets/Scripts/GameState/GameStateManager.cs`
- **Role**: Orchestrates game phases on the host
- **Key Property**: `NetworkVariable<GamePhase> CurrentPhase`
- **Flow**: Runs coroutine that transitions through phases with timers

### TeamManager
- **Location**: `Assets/Scripts/Teams/TeamManager.cs`
- **Role**: Assigns teams randomly on the host
- **Key Method**: `AssignTeams()` - shuffles players, distributes teams
- **Sync**: Sets `PlayerTeamController.AssignedTeam` which auto-syncs

### PlayerTeamController
- **Location**: `Assets/Scripts/Teams/PlayerTeamController.cs`
- **Role**: Per-player team tracking and action permission
- **Key Property**: `NetworkVariable<Team> AssignedTeam`
- **Key Property**: `CanPerformActions` - checks phase + team

### GameUI
- **Location**: `Assets/Scripts/UI/GameUI.cs`
- **Role**: Displays team-related UI panels
- **Panels**:
  - TeamRevealPanel - Full-screen team announcement
  - NabuWaitingPanel - Waiting message for NABU players
  - TeamIndicator - Corner indicator during gameplay

### RoomController
- **Location**: `Assets/Scripts/Rooms/RoomController.cs`
- **Role**: Room navigation with team-based restrictions
- **Key Method**: `CanNavigate()` - checks `PlayerTeamController.CanPerformActions`

---

## Data Synchronization

```
┌──────────────────────────────────────────────────────────────┐
│                          HOST                                │
│                                                              │
│   GameStateManager              TeamManager                  │
│   ┌──────────────┐             ┌──────────────┐             │
│   │ CurrentPhase │             │ AssignTeams()│             │
│   │ (NetworkVar) │             └──────┬───────┘             │
│   └──────┬───────┘                    │                     │
│          │                            │                     │
│          │                   Sets AssignedTeam              │
│          │                   on PlayerTeamController        │
│          │                            │                     │
└──────────┼────────────────────────────┼─────────────────────┘
           │                            │
     ══════╪════════════════════════════╪══════  Network Sync
           │                            │
┌──────────┼────────────────────────────┼─────────────────────┐
│          ▼                            ▼         ALL CLIENTS │
│                                                              │
│   ┌──────────────┐             ┌──────────────────┐         │
│   │   GameUI     │◄────────────│PlayerTeamController│        │
│   │              │             │                  │         │
│   │ - Subscribes │             │ - AssignedTeam   │         │
│   │   to phase   │             │ - CanPerformActions│        │
│   │   changes    │             │                  │         │
│   └──────────────┘             └────────┬─────────┘         │
│                                         │                   │
│                                ┌────────▼─────────┐         │
│                                │  RoomController  │         │
│                                │  (checks CanPerformActions)│
│                                └──────────────────┘         │
└─────────────────────────────────────────────────────────────┘
```

---

## Action Permissions by Phase

| Phase                | Corrupt Officials | NABU  |
|----------------------|-------------------|-------|
| WaitingForAssignment | ❌ Blocked        | ❌ Blocked |
| TeamReveal           | ❌ Blocked        | ❌ Blocked |
| NabuWaiting          | ✅ Can Act        | ❌ Blocked |
| MainGameplay         | ✅ Can Act        | ✅ Can Act |

---

## Setup Instructions

### In Unity Editor:

1. **Game Scene** - Add these GameObjects with NetworkObject component:
   - `TeamManager` with `TeamManager` component
   - `GameStateManager` with `GameStateManager` component
   - `GameUI` (Canvas) with `GameUI` component and UI panels

2. **NetworkPlayer Prefab** - Add `PlayerTeamController` component

---

## Code References

- Team enum: `Assets/Scripts/Teams/TeamTypes.cs:3`
- Phase enum: `Assets/Scripts/Teams/TeamTypes.cs:10`
- Nabu calculation: `Assets/Scripts/Teams/TeamManager.cs:109`
- Action check: `Assets/Scripts/Teams/PlayerTeamController.cs:18`
- Phase flow: `Assets/Scripts/GameState/GameStateManager.cs:68`
