using TMPro;
using UnityEngine;

public class HudLogic : MonoBehaviour
{
    public TMP_Text versionText;

    private void Start()
    {
        versionText.text = $"Version: {Application.version}";
    }
}
