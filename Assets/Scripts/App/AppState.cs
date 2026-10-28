namespace Godzilla.App
{
    public enum AppState { Boot, LoadingRoom, Placement, Play, Paused }

    public enum AppEvent
    {
        StartLoading,
        RoomReady_Restored,
        RoomReady_NeedsPlacement,
        Confirm,
        MenuShort,
        Resume,
        RePlace,
        Recalibrate,
    }
}
