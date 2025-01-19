using Shared.Log;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client.UI
{
    public class ServerControl : MonoBehaviour
    {
        private static readonly Slog.Area _log = new();

        public TMP_Dropdown serverDropdown;
        public Button serverRequestButton;
        public TMP_Text serverResponseText;
        
    }
}