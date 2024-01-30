using PauloToolbox.Tools;
using PauloToolbox.UI;

public class UIAnimatorSample : aUIAnimationController<UIAnimatorSample.EmotionalState> {

  public enum EmotionalState { 
    Happy,
    Exhalted,
    Sad,
  }

  private StateMachine<EmotionalState> _stateMachine;
    public override StateMachine<EmotionalState> SM {
      get {
        if (_stateMachine == null)
          _stateMachine = new StateMachine<EmotionalState>(EmotionalState.Happy);
        return _stateMachine;
      }
    }
    
}
