using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Shared.Boot.Version;
using Shared.Log;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Diagnostics.Debug
{
    public class DebugControl : MonoBehaviour
    {
        private static readonly Slog.Area _log = new();

        public TMP_Text debugText;

        //TODO: possibly replace with thirdparty console commands (IngameDebugControl)
        public Button tryActionButton;
        public TMP_Dropdown tryActionDropdown;

        public Button runtimeButton;
        public RuntimePanel runtimePanel;

        private readonly List<(string Text, MethodInfo Method)> _options = new();

        //TODO: per-project setup for include/exclude list
        private static readonly string[] _excludeAssemblyPrefixes = new[]
        {
            "mscorlib",
            "netstandard",
            "System",
            "Mono",
            "Microsoft",

            "Unity",
            "UnityEngine",
            "UnityEditor",
            "nunit",
            "Bee",

            "JetBrains",
            "Newtonsoft",
            "ZString",
            "UniTask",
            "RuntimeInspector",
            "Utilities",
        };

        private void OnEnable()
        {
            //UnityVersionProvider.BuildInfo;
            debugText.text = $"{Application.version} | {UnityVersionProvider.BuildInfo.GetShortDescription()}";

            _options.Clear();

            //CollectOptions(Assembly.GetExecutingAssembly());
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                if (IsAssemblyExcluded(assembly))
                    continue;
                CollectOptions(assembly);
            }

            tryActionDropdown.options.Clear();
            tryActionDropdown.options.AddRange(_options.Select(x => new TMP_Dropdown.OptionData(x.Text)));
            tryActionDropdown.RefreshShownValue();

            tryActionButton.onClick.RemoveAllListeners();
            tryActionButton.onClick.AddListener(DoAction);
            
            runtimeButton.onClick.RemoveAllListeners();
            runtimeButton.onClick.AddListener(() =>
                runtimePanel.gameObject.SetActive(!runtimePanel.gameObject.activeSelf));
        }

        private static bool IsAssemblyExcluded(Assembly assembly)
        {
            if (assembly.IsDynamic) 
                return true; //no exported types in dynamic assembly
            foreach (var prefix in _excludeAssemblyPrefixes)
            {
                if (assembly.FullName.StartsWith(prefix))
                    return true;
            }
            return false;
        }

        private void CollectOptions(Assembly assembly)
        {
            try
            {
                var exportedTypes = assembly.GetExportedTypes();
                foreach (var type in exportedTypes)
                {
                    const BindingFlags bindingFlags =
                        BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
                    var methods = type.GetMethods(bindingFlags);
                    foreach (var method in methods)
                    {
                        var attribute = method.GetCustomAttribute<DebugActionAttribute>();
                        if (attribute == null)
                            continue;
                        _options.Add((
                            $"{type.Name}.{method.Name}",
                            method
                        ));
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void DoAction()
        {
            var optionIdx = tryActionDropdown.value;
            var option = _options[optionIdx];
            _log.Info(option.Text);

            //option.Action();
            option.Method.Invoke(null, Array.Empty<object>());
        }

        //[Preserve, DebugAction]
        public static void DebugTest() => _log.Info(".");
    }
}