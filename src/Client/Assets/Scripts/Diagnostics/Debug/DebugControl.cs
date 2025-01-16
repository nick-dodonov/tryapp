using System;
using System.Collections.Generic;
using System.Linq;
using Shared.Log;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Client.Diagnostics.Debug
{
    public class DebugControl : MonoBehaviour
    {
        private readonly Slog.Area _log = new();

        public TMP_Dropdown actionDropdown;
        public Button actionButton;

        private readonly List<(string Text, Action Action)> _options = new();

        private void OnEnable()
        {
            _options.Clear();
            _options.Add(("Test1", () => _log.Info("Test1")));
            _options.Add(("Test2", () => _log.Info("Test2")));

            actionDropdown.options.Clear();
            actionDropdown.options.AddRange(_options.Select(x => new TMP_Dropdown.OptionData(x.Text)));
            actionDropdown.RefreshShownValue();
            
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(DoAction);
        }

        private void DoAction()
        {
            var optionIdx = actionDropdown.value;
            var option = _options[optionIdx];
            _log.Info(option.Text);
            throw new NotImplementedException("TODO");
        }
    }
}
