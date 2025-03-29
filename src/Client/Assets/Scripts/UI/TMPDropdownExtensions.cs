namespace Client.UI
{
    public static class TMPDropdownExtensions
    {
        /// <summary>
        /// Call this after modifying options while the dropdown is displayed to make sure the visual is up to date.
        ///
        /// Fixed original variant
        ///     https://stackoverflow.com/questions/55516877/unity-dynamically-update-dropdown-list-when-opened-while-not-losing-focus-on-in
        /// </summary>
        public static void RefreshOptions(this TMPro.TMP_Dropdown dropdown)
        {
            if (!dropdown.IsExpanded)
                return;

            dropdown.enabled = false;
            dropdown.enabled = true;
            dropdown.Show();
        }
    }
}