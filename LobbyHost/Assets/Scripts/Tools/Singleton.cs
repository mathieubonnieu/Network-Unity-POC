using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T instance;
    private static bool applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                                 "' already destroyed on application quit. Won't create again - returning null.");
                return null;
            }

            if (instance == null)
            {
                instance = FindObjectOfType<T>();

                if (instance == null)
                {
                    // OPTION 1 : Soit tu refuses de le créer automatiquement, pour éviter les bugs "fantômes" :
                    Debug.LogError("[Singleton] Instance of " + typeof(T) + " was requested, but none exists in the scene!");
                    return null;

                    // OPTION 2 : Si tu veux absolument créer dynamiquement, décommente ici :
                    //GameObject obj = new GameObject();
                    //obj.name = typeof(T).Name;
                    //instance = obj.AddComponent<T>();
                }
            }
            return instance;
        }
    }

    public virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
            DontDestroyOnLoad(this.gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    // CLEAN: à la destruction, nettoie la référence
    protected virtual void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    // Pour éviter les soucis d'instance à la fermeture de l'app
    protected virtual void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }
}