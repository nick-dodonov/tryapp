using TMPro;
using UnityEngine;

public class HudLogic : MonoBehaviour
{
    public TMP_Text versionText;

    private void Start()
    {
        Shared.StaticLog.Info("==== starting client ====");
        versionText.text = $"Version: {Application.version}";
    }
}
