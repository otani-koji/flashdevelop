﻿using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using FlashDebugger.Properties;
using PluginCore;
using PluginCore.Localization;
using PluginCore.Managers;

namespace FlashDebugger
{
    internal class MenusHelper
    {
        public static ImageListManager imageList;
        private readonly ToolStripItem[] m_ToolStripButtons;
        private readonly ToolStripSeparator m_ToolStripSeparator;
        private readonly ToolStripSeparator m_ToolStripSeparator2;
        private readonly ToolStripButton StartContinueButton;
        private readonly ToolStripButton PauseButton;
        private readonly ToolStripButton StopButton;
        private readonly ToolStripButton CurrentButton;
        private readonly ToolStripButton RunToCursorButton;
        private readonly ToolStripButton StepButton;
        private readonly ToolStripButton NextButton;
        private readonly ToolStripButton FinishButton;
        private readonly ToolStripMenuItem StartContinueMenu;
        private readonly ToolStripMenuItem PauseMenu;
        private readonly ToolStripMenuItem StopMenu;
        private readonly ToolStripMenuItem CurrentMenu;
        private readonly ToolStripMenuItem RunToCursorMenu;
        private readonly ToolStripMenuItem StepMenu;
        private readonly ToolStripMenuItem NextMenu;
        private readonly ToolStripMenuItem FinishMenu;
        private readonly ToolStripMenuItem ToggleBreakPointMenu;
        private readonly ToolStripMenuItem ToggleBreakPointEnableMenu;
        private readonly ToolStripMenuItem DeleteAllBreakPointsMenu;
        private readonly ToolStripMenuItem DisableAllBreakPointsMenu;
        private readonly ToolStripMenuItem EnableAllBreakPointsMenu;
        private readonly ToolStripMenuItem StartRemoteDebuggingMenu;
        private readonly ToolStripMenuItem BreakOnAllMenu;
        private DebuggerState CurrentState = DebuggerState.Initializing;

        /// <summary>
        /// Creates a menu item for the plugin and adds a ignored key
        /// </summary>
        public MenusHelper(Image pluginImage, DebuggerManager debugManager)
        {
            imageList = new ImageListManager();
            imageList.ColorDepth = ColorDepth.Depth32Bit;
            imageList.Populate += ImageList_Populate;
            imageList.Initialize();

            Image imgStartContinue = PluginBase.MainForm.GetAutoAdjustedImage(Resource.StartContinue);
            Image imgPause = PluginBase.MainForm.GetAutoAdjustedImage(Resource.Pause);
            Image imgStop = PluginBase.MainForm.GetAutoAdjustedImage(Resource.Stop);
            Image imgCurrent = PluginBase.MainForm.GetAutoAdjustedImage(Resource.Current);
            Image imgRunToCursor = PluginBase.MainForm.GetAutoAdjustedImage(Resource.RunToCursor);
            Image imgStep = PluginBase.MainForm.GetAutoAdjustedImage(Resource.Step);
            Image imgNext = PluginBase.MainForm.GetAutoAdjustedImage(Resource.Next);
            Image imgFinish = PluginBase.MainForm.GetAutoAdjustedImage(Resource.Finish);

            ToolStripMenuItem viewMenu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("ViewMenu");
            var tempItem = new ToolStripMenuItem(TextHelper.GetString("Label.ViewBreakpointsPanel"), pluginImage, OpenBreakPointPanel);
            PluginBase.MainForm.RegisterShortcutItem("ViewMenu.ShowBreakpoints", tempItem);
            viewMenu.DropDownItems.Add(tempItem);
            tempItem = new ToolStripMenuItem(TextHelper.GetString("Label.ViewLocalVariablesPanel"), pluginImage, OpenLocalVariablesPanel);
            PluginBase.MainForm.RegisterShortcutItem("ViewMenu.ShowLocalVariables", tempItem);
            viewMenu.DropDownItems.Add(tempItem);
            tempItem = new ToolStripMenuItem(TextHelper.GetString("Label.ViewStackframePanel"), pluginImage, OpenStackframePanel);
            PluginBase.MainForm.RegisterShortcutItem("ViewMenu.ShowStackframe", tempItem);
            viewMenu.DropDownItems.Add(tempItem);
            tempItem = new ToolStripMenuItem(TextHelper.GetString("Label.ViewWatchPanel"), pluginImage, OpenWatchPanel);
            PluginBase.MainForm.RegisterShortcutItem("ViewMenu.ShowWatch", tempItem);
            viewMenu.DropDownItems.Add(tempItem);
            tempItem = new ToolStripMenuItem(TextHelper.GetString("Label.ViewImmediatePanel"), pluginImage, OpenImmediatePanel);
            PluginBase.MainForm.RegisterShortcutItem("ViewMenu.ShowImmediate", tempItem);
            viewMenu.DropDownItems.Add(tempItem);
            tempItem = new ToolStripMenuItem(TextHelper.GetString("Label.ViewThreadsPanel"), pluginImage, OpenThreadsPanel);
            PluginBase.MainForm.RegisterShortcutItem("ViewMenu.ShowThreads", tempItem);
            viewMenu.DropDownItems.Add(tempItem);

            // Menu           
            var debugMenu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("DebugMenu");
            if (debugMenu is null)
            {
                debugMenu = new ToolStripMenuItem(TextHelper.GetString("Label.Debug"));
                var insertMenu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("InsertMenu");
                var idx = PluginBase.MainForm.MenuStrip.Items.IndexOf(insertMenu);
                if (idx < 0) idx = PluginBase.MainForm.MenuStrip.Items.Count - 1;
                PluginBase.MainForm.MenuStrip.Items.Insert(idx, debugMenu);
            }

            StartContinueMenu = new ToolStripMenuItem(TextHelper.GetString("Label.Start"), imgStartContinue, StartContinue_Click, Keys.F5);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.Start", StartContinueMenu);
            PauseMenu = new ToolStripMenuItem(TextHelper.GetString("Label.Pause"), imgPause, debugManager.Pause_Click, Keys.Control | Keys.Shift | Keys.F5);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.Pause", PauseMenu);
            StopMenu = new ToolStripMenuItem(TextHelper.GetString("Label.Stop"), imgStop, debugManager.Stop_Click, Keys.Shift | Keys.F5);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.Stop", StopMenu);
            BreakOnAllMenu = new ToolStripMenuItem(TextHelper.GetString("Label.BreakOnAllErrors"), null, BreakOnAll_Click, Keys.Control | Keys.Alt | Keys.E);
            BreakOnAllMenu.Checked = PluginMain.settingObject.BreakOnThrow;
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.BreakOnAllErrors", BreakOnAllMenu);
            CurrentMenu = new ToolStripMenuItem(TextHelper.GetString("Label.Current"), imgCurrent, debugManager.Current_Click, Keys.Shift | Keys.F10);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.Current", CurrentMenu);
            RunToCursorMenu = new ToolStripMenuItem(TextHelper.GetString("Label.RunToCursor"), imgRunToCursor, ScintillaHelper.RunToCursor_Click, Keys.Control | Keys.F10);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.RunToCursor", RunToCursorMenu);
            StepMenu = new ToolStripMenuItem(TextHelper.GetString("Label.Step"), imgStep, debugManager.Step_Click, Keys.F11);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.StepInto", StepMenu);
            NextMenu = new ToolStripMenuItem(TextHelper.GetString("Label.Next"), imgNext, debugManager.Next_Click, Keys.F10);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.StepOver", NextMenu);
            FinishMenu = new ToolStripMenuItem(TextHelper.GetString("Label.Finish"), imgFinish, debugManager.Finish_Click, Keys.Shift | Keys.F11);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.StepOut", FinishMenu);

            ToggleBreakPointMenu = new ToolStripMenuItem(TextHelper.GetString("Label.ToggleBreakpoint"), null, ScintillaHelper.ToggleBreakPoint_Click, Keys.F9);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.ToggleBreakpoint", ToggleBreakPointMenu);
            DeleteAllBreakPointsMenu = new ToolStripMenuItem(TextHelper.GetString("Label.DeleteAllBreakpoints"), null, ScintillaHelper.DeleteAllBreakPoints_Click, Keys.Control | Keys.Shift | Keys.F9);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.DeleteAllBreakpoints", DeleteAllBreakPointsMenu);
            ToggleBreakPointEnableMenu = new ToolStripMenuItem(TextHelper.GetString("Label.ToggleBreakpointEnabled"), null, ScintillaHelper.ToggleBreakPointEnable_Click, Keys.None);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.ToggleBreakpointEnabled", ToggleBreakPointEnableMenu);
            DisableAllBreakPointsMenu = new ToolStripMenuItem(TextHelper.GetString("Label.DisableAllBreakpoints"), null, ScintillaHelper.DisableAllBreakPoints_Click, Keys.None);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.DisableAllBreakpoints", DisableAllBreakPointsMenu);
            EnableAllBreakPointsMenu = new ToolStripMenuItem(TextHelper.GetString("Label.EnableAllBreakpoints"), null, ScintillaHelper.EnableAllBreakPoints_Click, Keys.None);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.EnableAllBreakpoints", EnableAllBreakPointsMenu);

            StartRemoteDebuggingMenu = new ToolStripMenuItem(TextHelper.GetString("Label.StartRemoteDebugging"), null, StartRemote_Click, Keys.None);
            PluginBase.MainForm.RegisterShortcutItem("DebugMenu.StartRemoteDebugging", StartRemoteDebuggingMenu);

            debugMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                StartContinueMenu, PauseMenu, StopMenu, BreakOnAllMenu, new ToolStripSeparator(),
                CurrentMenu, RunToCursorMenu, StepMenu, NextMenu, FinishMenu, new ToolStripSeparator(),
                ToggleBreakPointMenu, DeleteAllBreakPointsMenu, ToggleBreakPointEnableMenu ,DisableAllBreakPointsMenu, EnableAllBreakPointsMenu, new ToolStripSeparator(),
                StartRemoteDebuggingMenu
            });

            // ToolStrip
            m_ToolStripSeparator = new ToolStripSeparator();
            m_ToolStripSeparator.Margin = new Padding(1, 0, 0, 0);
            m_ToolStripSeparator2 = new ToolStripSeparator();
            StartContinueButton = new ToolStripButton(TextHelper.GetString("Label.Start"), imgStartContinue, StartContinue_Click);
            StartContinueButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            PluginBase.MainForm.RegisterSecondaryItem("DebugMenu.Start", StartContinueButton);
            PauseButton = new ToolStripButton(TextHelper.GetString("Label.Pause"), imgPause, debugManager.Pause_Click);
            PauseButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            PluginBase.MainForm.RegisterSecondaryItem("DebugMenu.Pause", PauseButton);
            StopButton = new ToolStripButton(TextHelper.GetString("Label.Stop"), imgStop, debugManager.Stop_Click);
            StopButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            PluginBase.MainForm.RegisterSecondaryItem("DebugMenu.Stop", StopButton);
            CurrentButton = new ToolStripButton(TextHelper.GetString("Label.Current"), imgCurrent, debugManager.Current_Click);
            CurrentButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            PluginBase.MainForm.RegisterSecondaryItem("DebugMenu.Current", CurrentButton);
            RunToCursorButton = new ToolStripButton(TextHelper.GetString("Label.RunToCursor"), imgRunToCursor, ScintillaHelper.RunToCursor_Click);
            RunToCursorButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            PluginBase.MainForm.RegisterSecondaryItem("DebugMenu.RunToCursor", RunToCursorButton);
            StepButton = new ToolStripButton(TextHelper.GetString("Label.Step"), imgStep, debugManager.Step_Click);
            StepButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            PluginBase.MainForm.RegisterSecondaryItem("DebugMenu.StepInto", StepButton);
            NextButton = new ToolStripButton(TextHelper.GetString("Label.Next"), imgNext, debugManager.Next_Click);
            NextButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            PluginBase.MainForm.RegisterSecondaryItem("DebugMenu.StepOver", NextButton);
            FinishButton = new ToolStripButton(TextHelper.GetString("Label.Finish"), imgFinish, debugManager.Finish_Click);
            FinishButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            PluginBase.MainForm.RegisterSecondaryItem("DebugMenu.StepOut", FinishButton);
            m_ToolStripButtons = new ToolStripItem[] { m_ToolStripSeparator, StartContinueButton, PauseButton, StopButton, m_ToolStripSeparator2, CurrentButton, RunToCursorButton, StepButton, NextButton, FinishButton };

            // Events
            PluginMain.debugManager.StateChangedEvent += UpdateMenuState;
            PluginMain.settingObject.BreakOnThrowChanged += BreakOnThrowChanged;
        }

        private void ImageList_Populate(object sender, EventArgs e)
        {
            imageList.Images.Add("StartContinue", PluginBase.MainForm.ImageSetAdjust(Resource.StartContinue));
            imageList.Images.Add("Pause", PluginBase.MainForm.ImageSetAdjust(Resource.Pause));
            //imageList.Images.Add("Stop", PluginBase.MainForm.ImageSetAdjust(Resource.Stop));
            //imageList.Images.Add("Current", PluginBase.MainForm.ImageSetAdjust(Resource.Current));
            //imageList.Images.Add("RunToCursor", PluginBase.MainForm.ImageSetAdjust(Resource.RunToCursor));
            //imageList.Images.Add("Step", PluginBase.MainForm.ImageSetAdjust(Resource.Step));
            //imageList.Images.Add("Next", PluginBase.MainForm.ImageSetAdjust(Resource.Next));
            //imageList.Images.Add("Finish", PluginBase.MainForm.ImageSetAdjust(Resource.Finish));
        }

        public void AddToolStripItems() => PluginBase.MainForm.ToolStrip.Items.AddRange(m_ToolStripButtons);

        void OpenLocalVariablesPanel(object sender, EventArgs e) => PanelsHelper.localsPanel.Show();

        void OpenBreakPointPanel(object sender, EventArgs e) => PanelsHelper.breakPointPanel.Show();

        void OpenStackframePanel(object sender, EventArgs e) => PanelsHelper.stackframePanel.Show();

        void OpenWatchPanel(object sender, EventArgs e) => PanelsHelper.watchPanel.Show();

        void OpenImmediatePanel(object sender, EventArgs e) => PanelsHelper.immediatePanel.Show();

        void OpenThreadsPanel(object sender, EventArgs e) => PanelsHelper.threadsPanel.Show();

        void BreakOnAll_Click(object sender, EventArgs e) => PluginMain.settingObject.BreakOnThrow = !BreakOnAllMenu.Checked;

        void StartContinue_Click(object sender, EventArgs e)
        {
            if (PluginMain.debugManager.FlashInterface.isDebuggerStarted)
            {
                PluginMain.debugManager.Continue_Click(sender, e);
            }
            else PluginMain.debugManager.Start(/*false*/);
        }

        void StartRemote_Click(object sender, EventArgs e)
        {
            if (PluginMain.debugManager.FlashInterface.isDebuggerStarted)
            {
                PluginMain.debugManager.Continue_Click(sender, e);
            }
            else PluginMain.debugManager.Start(true);
        }

        #region Menus State Management

        private void BreakOnThrowChanged(object sender, EventArgs e) => BreakOnAllMenu.Checked = PluginMain.settingObject.BreakOnThrow;

        public void UpdateMenuState(object sender) => UpdateMenuState(sender, CurrentState);

        public void UpdateMenuState(object sender, DebuggerState state)
        {
            if (((Form) PluginBase.MainForm).InvokeRequired)
            {
                ((Form) PluginBase.MainForm).BeginInvoke((MethodInvoker)(() => UpdateMenuState(sender, state)));
                return;
            }
            bool hasChanged = CurrentState != state;
            CurrentState = state; // Set current now...
            if (state == DebuggerState.Initializing || state == DebuggerState.Stopped)
            {
                if (PluginMain.settingObject.StartDebuggerOnTestMovie) StartContinueButton.Text = StartContinueMenu.Text = TextHelper.GetString("Label.Continue");
                else StartContinueButton.Text = StartContinueMenu.Text = TextHelper.GetString("Label.Start");
            }
            else StartContinueButton.Text = StartContinueMenu.Text = TextHelper.GetString("Label.Continue");
            PluginBase.MainForm.ApplySecondaryShortcut(StartContinueButton);
            //
            StopButton.Enabled = StopMenu.Enabled = (state != DebuggerState.Initializing && state != DebuggerState.Stopped);
            PauseButton.Enabled = PauseMenu.Enabled = (state == DebuggerState.Running);
            //
            if (state == DebuggerState.Initializing || state == DebuggerState.Stopped)
            {
                if (PluginMain.settingObject.StartDebuggerOnTestMovie) StartContinueButton.Enabled = StartContinueMenu.Enabled = false;
                else StartContinueButton.Enabled = StartContinueMenu.Enabled = true;
            }
            else if (state == DebuggerState.BreakHalt || state == DebuggerState.ExceptionHalt || state == DebuggerState.PauseHalt)
            {
                StartContinueButton.Enabled = StartContinueMenu.Enabled = true;
            }
            else StartContinueButton.Enabled = StartContinueMenu.Enabled = false;
            //
            bool enabled = (state == DebuggerState.BreakHalt || state == DebuggerState.PauseHalt);
            CurrentButton.Enabled = CurrentMenu.Enabled = RunToCursorButton.Enabled = enabled;
            NextButton.Enabled = NextMenu.Enabled = FinishButton.Enabled = FinishMenu.Enabled = enabled;
            RunToCursorMenu.Enabled = StepButton.Enabled = StepMenu.Enabled = enabled;
            if (state == DebuggerState.Running && (!PluginMain.debugManager.FlashInterface.isDebuggerStarted || !PluginMain.debugManager.FlashInterface.isDebuggerSuspended))
            {
                PanelsHelper.localsUI.TreeControl.Nodes.Clear();
                PanelsHelper.stackframeUI.ClearItem();
                PanelsHelper.watchUI.UpdateElements();
            }
            enabled = GetLanguageIsValid();
            ToggleBreakPointMenu.Enabled = ToggleBreakPointEnableMenu.Enabled = enabled;
            DeleteAllBreakPointsMenu.Enabled = DisableAllBreakPointsMenu.Enabled = enabled;
            EnableAllBreakPointsMenu.Enabled = PanelsHelper.breakPointUI.Enabled = enabled;
            StartRemoteDebuggingMenu.Enabled = (state == DebuggerState.Initializing || state == DebuggerState.Stopped);
            //
            bool hideButtons = state == DebuggerState.Initializing || state == DebuggerState.Stopped;
            StartContinueButton.Visible = StartContinueButton.Enabled;
            PauseButton.Visible = StopButton.Visible = CurrentButton.Visible = NextButton.Visible =
            RunToCursorButton.Visible = StepButton.Visible = FinishButton.Visible = !hideButtons;
            m_ToolStripSeparator.Visible = StartContinueButton.Visible;
            m_ToolStripSeparator2.Visible = !hideButtons;
            // Notify plugins of main states when state changes...
            if (hasChanged && (state == DebuggerState.Running || state == DebuggerState.Stopped))
            {
                DataEvent de = new DataEvent(EventType.Command, "FlashDebugger." + state, null);
                EventManager.DispatchEvent(this, de);
            }
            PluginBase.MainForm.RefreshUI();
        }

        /// <summary>
        /// Gets if the language is valid for debugging
        /// </summary>
        private bool GetLanguageIsValid()
        {
            var document = PluginBase.MainForm.CurrentDocument;
            if (document is null || !document.IsEditable) return false;
            var ext = Path.GetExtension(document.FileName);
            var lang = document.SciControl.ConfigurationLanguage;
            return lang == "as3" || ext == ".mxml" || lang == "haxe";
        }

        #endregion

    }

}
