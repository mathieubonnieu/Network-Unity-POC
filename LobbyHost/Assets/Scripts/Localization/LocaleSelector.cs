using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class LocaleSelector : MonoBehaviour
{
    private bool isactive = false;

    private void Start()
    {
        int ID = PlayerPrefs.GetInt("LocaleID", 0);
        ChangeLocale(ID);
    }
    public void ChangeLocale(int id)
    {
        if(isactive) return;
        StartCoroutine(SetLocale(id));
    }
    IEnumerator SetLocale(int id)
    {
        isactive = true;
        yield return LocalizationSettings.InitializationOperation;
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[id];
        PlayerPrefs.SetInt("LocaleID", id);
        isactive = false;
    }
}
