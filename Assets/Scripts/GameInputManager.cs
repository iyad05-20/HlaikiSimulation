using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class GameInputManager : MonoBehaviour
{
    //public Event EventHandler OnInteractions;
    private InputSystemActions inputSystemActions;
    public event EventHandler OnInteraction;
    public event EventHandler OnSubmitUI;
    public event EventHandler OnEndUI;
    public event EventHandler OnOptions;
    
    private void Awake()
    {
        inputSystemActions =new InputSystemActions();
        inputSystemActions.Player.Enable(); 
        inputSystemActions.UI.Enable(); 
        inputSystemActions.Player.Interact.performed += Interact_performed;
        inputSystemActions.Player.Options.performed += Options_performed;
        inputSystemActions.UI.SubmitUI.performed += Submit_performed;
        inputSystemActions.UI.EndUI.performed += End_performed;
    }

    private void Options_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnOptions?.Invoke(this, EventArgs.Empty);
    }

    private void Submit_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnSubmitUI?.Invoke(this, EventArgs.Empty);
    }

    private void End_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnEndUI?.Invoke(this, EventArgs.Empty);
    }

    private void Interact_performed(UnityEngine.InputSystem.InputAction.CallbackContext obj)
    {
        OnInteraction?.Invoke(this, EventArgs.Empty);
    }
    public Vector2 InputVector()
    {
        Vector2 inputVector = inputSystemActions.Player.Move.ReadValue<Vector2>();
        return inputVector;
    }

    public Vector2 LookVector()
    {
        return inputSystemActions.Player.Look.ReadValue<Vector2>();
    }

    public bool IsSprinting()
    {
        return inputSystemActions.Player.Sprint.IsPressed();
    }

    public bool WasJumpPressed()
    {
        return inputSystemActions.Player.Jump.WasPressedThisFrame();
    }

    // Dans GameInputManager.cs
    public void DisablePlayerMap()
    {
        inputSystemActions.Player.Disable();
    }

    public void EnablePlayerMap()
    {
        inputSystemActions.Player.Enable();
    }
}
