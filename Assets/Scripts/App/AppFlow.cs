using System;
using UnityEngine;
using Godzilla.Core;
using Godzilla.Input;
using Godzilla.Placement;
using Godzilla.UI;

namespace Godzilla.App
{
    /// <summary>
    /// Runs the Boot → LoadingRoom → Placement/Play ⇄ Paused flow and routes input to the active consumer.
    /// Play-state input is published through <see cref="PlayInput"/> for the character controller (Task 11).
    /// </summary>
    public class AppFlow : MonoBehaviour
    {
        [SerializeField] GiantInput input;
        [SerializeField] WindowCalibration window;
        [SerializeField] CharacterPlacement placement;
        [SerializeField] PlayerHud hud;
        [SerializeField] DevPanel devPanel;

        public AppState State { get; private set; } = AppState.Boot;
        public event Action<AppState> StateChanged;
        /// <summary>Fired every frame in Play state with the current input (not while a panel is open).</summary>
        public event Action<InputFrame, float> PlayInput;

        public void Configure(GiantInput playerInput, WindowCalibration windowCalibration, CharacterPlacement characterPlacement, PlayerHud playerHud, DevPanel panel)
        {
            input = playerInput; window = windowCalibration; placement = characterPlacement; hud = playerHud; devPanel = panel;
        }

        void Start()
        {
            window.Calibrated += OnCalibrated;
            Fire(AppEvent.StartLoading);
            window.Calibrate();
        }

        void OnDestroy()
        {
            if (window != null) window.Calibrated -= OnCalibrated;
        }

        void OnCalibrated()
        {
            if (State != AppState.LoadingRoom) return;
            var result = placement.LoadFor(window.AnchorUuid, window.Mode.ToString());
            Fire(result == PlacementStore.LoadResult.Restored ? AppEvent.RoomReady_Restored : AppEvent.RoomReady_NeedsPlacement);
        }

        public void Fire(AppEvent e)
        {
            var next = AppFlowLogic.Next(State, e);
            if (next == State)
            {
                // Recalibrate while still loading (device scan never arrived): restart the load instead of ignoring it.
                if (e == AppEvent.Recalibrate && State == AppState.LoadingRoom) window.Calibrate();
                return;
            }
            Debug.Log($"[App] {State} --{e}--> {next}");
            State = next;
            OnEnter(next);
            StateChanged?.Invoke(next);
        }

        void OnEnter(AppState s)
        {
            if (hud != null) hud.OnState(s);
            if (s == AppState.LoadingRoom) window.Calibrate();
        }

        /// <summary>Tests / scripted demos: when set, replaces the device input for every frame until cleared.</summary>
        public InputFrame? InputOverride { get; set; }

        void Update()
        {
            if (input == null) return;
            var i = InputOverride ?? input.Current;
            float dt = Time.deltaTime;

            if (i.MenuHeld1s && devPanel != null) devPanel.Toggle();
            if (devPanel != null && devPanel.Visible)
            {
                devPanel.Navigate(i, dt);
                if (i.MenuPressed) devPanel.Toggle();
                return;
            }

            switch (State)
            {
                case AppState.Placement:
                    if (i.MenuPressed) { Fire(AppEvent.MenuShort); break; }
                    if (i.ToggleOccluderDebugPressed) window.SetOccluderVisible(!window.OccluderDebug);
                    if (i.ConfirmPressed) { placement.Save(); Fire(AppEvent.Confirm); break; }
                    placement.StepPlacement(i, dt);
                    if (hud != null) hud.ShowPlacement(placement.Data, window.Mode);
                    break;
                case AppState.Play:
                    if (i.MenuPressed) { Fire(AppEvent.MenuShort); break; }
                    PlayInput?.Invoke(i, dt);
                    break;
                case AppState.Paused:
                    if (i.MenuPressed) { Fire(AppEvent.MenuShort); break; }
                    hud?.NavigatePause(i, dt);
                    break;
            }
        }

        // Pause-menu actions
        public void Resume() => Fire(AppEvent.Resume);
        public void RePlace() => Fire(AppEvent.RePlace);
        public void Recalibrate() => Fire(AppEvent.Recalibrate);
        public void Quit() { Debug.Log("[App] quit"); Application.Quit(); }
    }
}
