using NUnit.Framework;
using Godzilla.App;

namespace Godzilla.Tests
{
    public class AppFlowLogicTests
    {
        [Test] public void Boot_RoomLoading_GoesLoadingRoom() => Assert.AreEqual(AppState.LoadingRoom, AppFlowLogic.Next(AppState.Boot, AppEvent.StartLoading));
        [Test] public void LoadingRoom_Restored_GoesPlay() => Assert.AreEqual(AppState.Play, AppFlowLogic.Next(AppState.LoadingRoom, AppEvent.RoomReady_Restored));
        [Test] public void LoadingRoom_NeedsPlacement_GoesPlacement() => Assert.AreEqual(AppState.Placement, AppFlowLogic.Next(AppState.LoadingRoom, AppEvent.RoomReady_NeedsPlacement));
        [Test] public void Placement_Confirm_GoesPlay() => Assert.AreEqual(AppState.Play, AppFlowLogic.Next(AppState.Placement, AppEvent.Confirm));
        [Test] public void Play_MenuShort_Paused() => Assert.AreEqual(AppState.Paused, AppFlowLogic.Next(AppState.Play, AppEvent.MenuShort));
        [Test] public void Paused_Resume_Play() => Assert.AreEqual(AppState.Play, AppFlowLogic.Next(AppState.Paused, AppEvent.Resume));
        [Test] public void Paused_MenuShort_Play() => Assert.AreEqual(AppState.Play, AppFlowLogic.Next(AppState.Paused, AppEvent.MenuShort));
        [Test] public void Paused_RePlace_Placement() => Assert.AreEqual(AppState.Placement, AppFlowLogic.Next(AppState.Paused, AppEvent.RePlace));
        [Test] public void Paused_Recalibrate_LoadingRoom() => Assert.AreEqual(AppState.LoadingRoom, AppFlowLogic.Next(AppState.Paused, AppEvent.Recalibrate));
        [Test] public void Placement_MenuShort_Paused() => Assert.AreEqual(AppState.Paused, AppFlowLogic.Next(AppState.Placement, AppEvent.MenuShort));
        [Test] public void Play_Confirm_IsIgnored() => Assert.AreEqual(AppState.Play, AppFlowLogic.Next(AppState.Play, AppEvent.Confirm));
        [Test] public void Boot_MenuShort_IsIgnored() => Assert.AreEqual(AppState.Boot, AppFlowLogic.Next(AppState.Boot, AppEvent.MenuShort));
    }
}
