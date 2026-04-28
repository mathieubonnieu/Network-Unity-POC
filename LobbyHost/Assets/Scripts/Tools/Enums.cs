using System;
using UnityEngine;

[Serializable]
public enum SceneState
{
    Bootstrap,
    MainScene,
    Lobby,
    Game,
}

[Serializable]
public enum InputMode { Player, UI }