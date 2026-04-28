using System.Collections;
using UnityEngine;

namespace LobbyHost
{
    public class CanvasExtension
    {
        public static void Hide(CanvasGroup canvasGroup)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        public static void Show(CanvasGroup canvasGroup)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        public static IEnumerator ShowAfterSeveralFrame(CanvasGroup canva)
        {
            yield return new WaitForEndOfFrame();
            Show(canva);
        }
    }
    
    public class Constants
    {
        public const string PLAYER_PREFS_MASTER_VOLUME = "Master";
        public const string PLAYER_PREFS_MUSIC_VOLUME = "Music";
        public const string PLAYER_PREFS_SFX_VOLUME = "SFX";
        public const string PLAYER_PREFS_AMBIENT_VOLUME = "Ambient";
        
        public const string PLAYER_PREFS_TIMEFORMAT = "TimeFormat";
    }
    
    public class MathExtension
    {
        public static Vector3 GetOffsetPosition(Vector3 from, Vector3 target, float distance)
        {
            Vector3 dir = target - from;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector3.forward;

            return target - dir.normalized * distance;
        }
    }
}