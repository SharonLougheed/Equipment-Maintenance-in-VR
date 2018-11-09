
using System;

public interface IObjectiveCommands {

    event Action CompletionEvent;
    void OnObjectiveStart();
    void OnObjectiveReset();
    void OnObjectiveFinish();
}
