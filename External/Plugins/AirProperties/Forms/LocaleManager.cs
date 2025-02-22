﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using PluginCore;
using PluginCore.Localization;

namespace AirProperties
{
    public partial class LocaleManager : Form
    {
        private bool _customLocaleIsValid;
        private readonly List<string> _applicationLocales;
        private readonly ListItem[] _defaultLocales = // The default locales supported by the AIR application installer
        { 
            new ListItem(TextHelper.GetString("Locale.ChineseSimplified"), "zh"),
            new ListItem(TextHelper.GetString("Locale.ChineseTraditional"), "zh-Hant"),                                                
            new ListItem(TextHelper.GetString("Locale.Czech"), "cs"),
            new ListItem(TextHelper.GetString("Locale.Dutch"), "nl"),
            new ListItem(TextHelper.GetString("Locale.English"), "en"),
            new ListItem(TextHelper.GetString("Locale.French"), "fr"),
            new ListItem(TextHelper.GetString("Locale.German"), "de"),
            new ListItem(TextHelper.GetString("Locale.Italian"), "it"),
            new ListItem(TextHelper.GetString("Locale.Japanese"), "ja"),
            new ListItem(TextHelper.GetString("Locale.Korean"), "ko"),
            new ListItem(TextHelper.GetString("Locale.Polish"), "pl"),
            new ListItem(TextHelper.GetString("Locale.PortugueseSimple"), "pt"),
            new ListItem(TextHelper.GetString("Locale.Portuguese"), "pt-BR"),
            new ListItem(TextHelper.GetString("Locale.Russian"), "ru"),
            new ListItem(TextHelper.GetString("Locale.Spanish"), "es"),
            new ListItem(TextHelper.GetString("Locale.Swedish"), "sv"),
            new ListItem(TextHelper.GetString("Locale.Turkish"), "tr")
        };

        /// <summary>
        /// 
        /// </summary>
        public LocaleManager(List<string> applicationLocales)
        {
            InitializeComponent();
            InitializeLocalization();
            _applicationLocales = applicationLocales;
            LoadDefaultLocales();
        }

        private void InitializeLocalization()
        {
            OKButton.Text = TextHelper.GetString("Label.Ok");
            AddNewButton.Text = TextHelper.GetString("Label.Add");
            CancelButton1.Text = TextHelper.GetString("Label.Cancel");
            DefaultLocalesLabel.Text = TextHelper.GetString("Label.DefaultLocales");
            SelectedLocalesLabel.Text = TextHelper.GetString("Label.SelectedLocales");
            CustomLocaleField.Text = TextHelper.GetString("Label.AddCustomLocale");
            LocalesGroupBox.Text = TextHelper.GetString("Label.Locales");
            Text = " " + TextHelper.GetString("Title.LocaleManager");
        }

        /// <summary>
        /// 
        /// </summary>
        private void LoadDefaultLocales()
        {
            bool isDefaultLocale;
            // Add defaults to available list
            AvailableListBox.Items.AddRange(_defaultLocales);
            // Check each selected against defaults
            foreach (string locale in _applicationLocales)
            {
                isDefaultLocale = false;
                foreach (ListItem defaultLocale in _defaultLocales)
                {
                    if (locale.ToLower().Equals(defaultLocale.Value.ToLower()))
                    {
                        SelectedListBox.Items.Add(defaultLocale);
                        AvailableListBox.Items.Remove(defaultLocale);
                        isDefaultLocale = true;
                        break;
                    }
                }
                if (!isDefaultLocale)
                {
                    SelectedListBox.Items.Add(new ListItem(locale, locale));
                }
            }
        }

        private void AddDefaultLocale()
        {
            ListItem selectedLocale;
            if (AvailableListBox.SelectedIndex >= 0)
            {
                selectedLocale = (ListItem)AvailableListBox.SelectedItem;
                SelectedListBox.Items.Add(selectedLocale);
                _applicationLocales.Add(selectedLocale.Value);
                AvailableListBox.Items.Remove(selectedLocale);
            }
        }

        private void RemoveLocale()
        {
            ListItem selectedLocale;
            if (SelectedListBox.SelectedIndex >= 0)
            {
                selectedLocale = (ListItem)SelectedListBox.SelectedItem;
                SelectedListBox.Items.Remove(selectedLocale);
                _applicationLocales.Remove(selectedLocale.Value);
                // Only re-add default locales back to the available list
                foreach (ListItem defaultLocale in _defaultLocales)
                {
                    if (selectedLocale.Name.Equals(defaultLocale.Name))
                    {
                        AvailableListBox.Items.Add(selectedLocale);
                    }
                }
            }
        }

        private void OKButton_Click(object sender, EventArgs e)
        {
            if (ValidateChildren())
            {
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void AddNewButton_Click(object sender, EventArgs e)
        {
            string newLocale = CustomLocaleField.Text.Trim();
            if (newLocale.Length > 0)
            {
                if (!_applicationLocales.Contains(newLocale) && _customLocaleIsValid)
                {
                    SelectedListBox.Items.Add(new ListItem(newLocale, newLocale));
                    _applicationLocales.Add(newLocale);
                    CustomLocaleField.Text = "";
                }
            }
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            AddDefaultLocale();
        }

        private void AvailableListBox_DoubleClick(object sender, EventArgs e)
        {
            AddDefaultLocale();
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            RemoveLocale();
        }

        private void SelectedListBox_DoubleClick(object sender, EventArgs e)
        {
            RemoveLocale();
        }

        // Simple validation to check if new locale is an extension of a default locale
        private void CustomLocaleField_Validating(object sender, CancelEventArgs e)
        {
            string newLocale = CustomLocaleField.Text.Trim();
            string baseLocale;
            bool isValid = false;
            if (newLocale.Length > 0 && newLocale != TextHelper.GetString("Label.AddCustomLocale"))
            {
                foreach (ListItem locale in _defaultLocales)
                {
                    baseLocale = locale.Value + "-";
                    if (newLocale.StartsWithOrdinal(baseLocale) && newLocale.Length > baseLocale.Length)
                    {
                        isValid = true;
                        break;
                    }
                }
                if (!isValid)
                {
                    ValidationErrorProvider.SetError(CustomLocaleField, TextHelper.GetString("Validation.InvalidLocale"));
                    e.Cancel = true;
                }
                else ValidationErrorProvider.SetError(CustomLocaleField, "");
            }
            else ValidationErrorProvider.SetError(CustomLocaleField, "");
            _customLocaleIsValid = isValid;
        }

    }

}

