using UnityEngine;

[RequireComponent(typeof(Animator))]
public class Seller : MonoBehaviour
{
    [Header("Animation Settings")]
    [Tooltip("The name of the idle state in the Animator Controller.")]
    [SerializeField] private string idleStateName = "Idle";

    private Animator _animator;
    private int _idleStateHash;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _idleStateHash = Animator.StringToHash(idleStateName);
    }

    private void Start()
    {
        PlayIdleAnimation();
    }

    /// <summary>
    /// Plays the idle animation smoothly using CrossFade.
    /// </summary>
    public void PlayIdleAnimation()
    {
        if (_animator != null && _animator.runtimeAnimatorController != null)
        {
            _animator.CrossFade(_idleStateHash, 0.1f);
        }
    }
}
