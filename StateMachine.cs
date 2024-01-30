using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PauloToolbox.Tools {

  /// <summary>
  /// Creates a generic statemachine for any class.
  /// Check example below
  /// </summary>
  /// <typeparam name="T">Enum only!</typeparam>
  public class StateMachine<T> where T : struct, IConvertible {

    public delegate void StateEvent(T prevState, T newState);
    public StateEvent OnStateChanged;

    /// <summary>
    /// If the state you're setting is the same as the current one, should I refresh the current state?
    /// </summary>
    public bool refreshIfSameState = false;

    public delegate void GenericEvent();
    private Dictionary<T, GenericEvent> transitionEnterFns = new Dictionary<T, GenericEvent>();
    private Dictionary<T, GenericEvent> transitionExitFns = new Dictionary<T, GenericEvent>();
    private Dictionary<T, Func<bool>> transitionGateFns = new Dictionary<T, Func<bool>>();
    private Func<T, T, bool> generalGateFn;

    private T _state;
    /// <summary>
    /// Gets or sets the state of the machine.
    /// On Set, calls Action for new state if defined.
    /// </summary>
    /// <value>New state</value>
    public T state {
      get {
        return _state;
      }
      set {
        // if the same, refresh
        if (EqualityComparer<T>.Default.Equals(value, _state)) {
          if (refreshIfSameState)
            RefreshState();
          return;
        }
        // check for a general gate fn
        if (generalGateFn != null && generalGateFn.Invoke(_state, value) == false)
          return;
        // if it has a gate function, check it.
        // if it failed, don't change the state
        if (transitionGateFns.ContainsKey(value))
          if (transitionGateFns[value]?.Invoke() == false)
            return;
        // if we have an exit-transition function, do it first
        if (transitionExitFns.ContainsKey(_state))
          transitionExitFns[_state]?.Invoke();
        // flip the switch and trigger callback
        var oldState = _state;
        _state = value;
        OnStateChanged?.Invoke(oldState, value);
        // execute the post transition fns, if exist
        if (transitionEnterFns.ContainsKey(_state))
          transitionEnterFns[_state]?.Invoke();
      }
    }

    /// <summary>
    /// Same as calling state, but uses indexes instead of T
    /// </summary>
    public int stateIndex {
      get => Convert.ToInt32(state);
      set {
        var strT = GetValues();
        if (!value.Between(0, strT.Count - 1)) {
          Debug.LogError("State switch changed because the index is OOB to the Enum list.");
          return;
        }
        T nT;
        if (Enum.TryParse<T>(strT[value], out nT))
          state = nT;
        else
          Debug.LogError("State switch failed.");
      }
    }

    public List<string> GetValues() => Enum.GetNames(typeof(T)).ToList();

    /// <summary>
    /// Creates a new state machine. Does not call any function upon init.
    /// </summary>
    /// <param name="init">Inital state</param>
    public StateMachine(T init) {
      if (!typeof(T).IsEnum)
        throw new ArgumentException("T must be an enumeration");
      _state = init;
    }

    /// <summary>
    /// Re-runs the transition Action for this stateF
    /// </summary>
    public void RefreshState() {
      if (transitionEnterFns.ContainsKey(state))
        transitionEnterFns[state]?.Invoke();
    }

    /// <summary>
    /// Defines the action for a state
    /// </summary>
    /// <param name="state">state</param>
    /// <param name="enterMethod">it's function</param>
    public void AddTransitionEnterMethod(T state, GenericEvent enterMethod) {
      if (!typeof(T).IsEnum)
        throw new ArgumentException("T must be an enumeration");
      if (!transitionEnterFns.ContainsKey(state))
        transitionEnterFns[state] = enterMethod;
      else
        transitionEnterFns[state] += enterMethod;
    }

    /// <summary>
    /// Defines the action for a state that is exiting
    /// </summary>
    /// <param name="state">state</param>
    /// <param name="exitMethod">it's function</param>
    public void AddTransitionExitMethod(T state, GenericEvent exitMethod) {
      if (!typeof(T).IsEnum)
        throw new ArgumentException("T must be an enumeration");
      if (!transitionExitFns.ContainsKey(state))
        transitionExitFns[state] = exitMethod;
      else
        transitionExitFns[state] += exitMethod;
    }

    /// <summary>
    /// Defines a function that if it fails, the statemachine will not switch states
    /// </summary>
    /// <param name="state">state</param>
    /// <param name="method">it's function</param>
    public void SetGateMethod(T state, Func<bool> method) {
      if (!typeof(T).IsEnum)
        throw new ArgumentException("T must be an enumeration");
      transitionGateFns[state] = method;
    }

    public void SetGateMethod(Func<T, T, bool> method) => generalGateFn = method;

  }

}

/*
public class Example {

  public enum States { A, B, C }
  public StateMachine<States> stateMachine;

  private void Start() {
    stateMachine = new StateMachine<States>(States.A);
    stateMachine.AddTransitionEnterMethod(States.A, DoStateA);
    stateMachine.AddTransitionEnterMethod(States.B, DoStateB);
    stateMachine.AddTransitionEnterMethod(States.C, DoStateC);
  }

  private void DoStateA() { ... }
  private void DoStateB() { ... }
  private void DoStateC() { ... }

}
*/
