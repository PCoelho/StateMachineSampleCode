using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using PauloToolbox.Tools;

/*

  This class is similar to the UIStateMachine.cs, but works with snapshots, not
  with state replication.

  In a snapshot based system you keep the same game objects, make the changes
  per state, and push the "Record" button. This saves the state as metadata.
  Conversely, on a state replication system you're instead making a copy of each
  element per state, and the system figures our how to animate between them.
  This works similar to Figma: based on game object name.

  Snapshot systems are more flexible and easier to manage, but they do require a
  bit of bespoke code to make work. State replication systems don't need code
  (with some exceptions), and allow the creator a bit more flexibility and
  control. However, it has limitations regarding nesting. 

  At this point, I recommend using the snapshot based system (this one) over the
  state replication one (UIStateMachine.cs).

  === USAGE ===

  To use this, you must create a specification of this abstraction that
  includes the states this animator will consider.

  Once that is done, to record a new state:
  1) Select the state from the dropdown in the Unity Editor
  2) Make all the necessary modifications to the child objects that are animator
     controlled (must have a aUIAnimator component)
  3) Go back to the aUIAnimationController and press "Snapshot"

  This will record all the information as metadata on the respective game
  objects. See "UI Animator Sample" scene for example.

  */

namespace PauloToolbox.UI {
  public abstract class aUIAnimationController<T> : MonoBehaviourPTB where T : struct, IConvertible {

    public delegate void StateEventChange(T oldState, T newState);
    public StateEventChange OnNewStateTransitionCompleted;

    public abstract StateMachine<T> SM { get; }

    [SerializeField]
    [Bundle]
    [Tooltip("This is meant to be used in editor ONLY so we can record the states.")]
    protected T editorState;

    [SerializeField]
    [InspectorReadOnlyAttribute]
    [Bundle]
    protected string currentStateName = "";

    [Header("Settings")]

    [Bundle]
    [SerializeField]
    protected float _duration = 0.2f;
    public virtual float duration => _duration;

    [Bundle]
    [SerializeField]
    protected Ease _ease = Ease.OutQuad;
    public virtual Ease ease => _ease;

    [Bundle]
    public bool verbose = false;

    public virtual List<aUIAnimator<T>> allAnimators =>
      GetComponentsInChildren<aUIAnimator<T>>(true)
      .Where(a => a.animationController == this).ToList();

    public virtual T state {
      get => SM.state;
      set {
        if (EqualityComparer<T>.Default.Equals(value, state))
          return;
        PLog($"Attempting to switch state {state.ToString()} => {value.ToString()}");
        SM.state = value;
        PLog($"State is now {SM.state.ToString()}");
        currentStateName = SM.state.ToString();
        if (currentStateName == "")
          currentStateName = "<i>invalid</i>";
      }
    }

#if UNITY_EDITOR
    private void OnValidate() {
      if (Application.isPlaying)
        return;
      state = editorState;
      SM.OnStateChanged -= HandleStateSwitch;
      SM.OnStateChanged += HandleStateSwitch;
    }
#endif

    protected virtual void OnEnable() {
      SM.OnStateChanged += HandleStateSwitch;
    }

    protected virtual void OnDisable() {
      SM.OnStateChanged -= HandleStateSwitch;
    }

    private void HandleStateSwitch(T prevState, T newState) {
      foreach (var (a, i) in allAnimators.WithIndex()) {
        if (i == 0)
          a.SwitchToState(newState, OnComplete: () =>
            OnNewStateTransitionCompleted?.Invoke(prevState, newState));
        a.SwitchToState(newState);
      }
    }

    [ShowTestButton(true)]
    public void Snapshot() {
      if (Application.isPlaying) {
        Debug.Log(C + $"You can't do this while it's playing.");
        return;
      }
      allAnimators.ForEach(a => a.Snapshot(state));
      Debug.Log(C + $"recorded props");
    }

  }
}
