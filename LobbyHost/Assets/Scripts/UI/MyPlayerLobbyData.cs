using UnityEngine;

public class MyPlayerLobbyData : MonoBehaviour
{

   public void SetReady()
   {

      PlayersManager.Instance.RequestSetReady();
   }
}
