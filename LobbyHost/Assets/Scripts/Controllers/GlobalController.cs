using UnityEngine;

public class GlobalController : Singleton<GlobalController>
{
    public SceneController sceneController;
    public InputController inputController;
    public EventController eventController;
}
