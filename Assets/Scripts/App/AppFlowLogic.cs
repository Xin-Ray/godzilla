namespace Godzilla.App
{
    /// <summary>Boot → LoadingRoom → (Play | Placement) ; Placement —Confirm→ Play ; Play ⇄ Paused ; Paused → RePlace/Recalibrate.</summary>
    public static class AppFlowLogic
    {
        public static AppState Next(AppState s, AppEvent e)
        {
            switch (s)
            {
                case AppState.Boot:
                    return e == AppEvent.StartLoading ? AppState.LoadingRoom : s;
                case AppState.LoadingRoom:
                    if (e == AppEvent.RoomReady_Restored) return AppState.Play;
                    if (e == AppEvent.RoomReady_NeedsPlacement) return AppState.Placement;
                    return s;
                case AppState.Placement:
                    if (e == AppEvent.Confirm) return AppState.Play;
                    if (e == AppEvent.MenuShort) return AppState.Paused;
                    return s;
                case AppState.Play:
                    return e == AppEvent.MenuShort ? AppState.Paused : s;
                case AppState.Paused:
                    if (e == AppEvent.Resume || e == AppEvent.MenuShort) return AppState.Play;
                    if (e == AppEvent.RePlace) return AppState.Placement;
                    if (e == AppEvent.Recalibrate) return AppState.LoadingRoom;
                    return s;
            }
            return s;
        }
    }
}
