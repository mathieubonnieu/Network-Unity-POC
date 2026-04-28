using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerReadyItem : MonoBehaviour
{
    public TMP_Text playerName;
    public Image playerReady;
    
    public Color readyColor;
    public Color notReadyColor;


    public void SetPlayerItem(string name, bool isReady)
    {
        playerName.text = name;
        playerReady.color = isReady ? readyColor : notReadyColor;
    }
}
