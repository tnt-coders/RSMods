﻿using System;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Text;
using RSMods.Data;
using RSMods.Util;
using System.Reflection;
using System.Collections.Generic;
using RocksmithToolkitLib.Extensions;
using RSMods.Twitch;
using System.Threading;
using System.Xml.Serialization;
using System.Xml;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Data;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.ComponentModel;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using RocksmithToolkitLib.DLCPackage;
using RocksmithToolkitLib.DLCPackage.Manifest2014.Tone;
using RocksmithToolkitLib.Ogg;
using NAudio.Wave;
using NAudio.CoreAudioApi;
using SevenZip;
using Rocksmith2014PsarcLib.Psarc.Models.Json;
using ArrangementTuning = Rocksmith2014PsarcLib.Psarc.Models.Json.SongArrangement.ArrangementAttributes.ArrangementTuning;

namespace RSMods
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// Leaving this boolean in-case we need to quickly disable the Profiles tab due to a bug.
        /// </summary>
        bool shipProfileEdits = true;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr FindWindow(string strClassName, string strWindowName);

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);
        struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        TabPage ProfileEditsTab;
        int ProfileEditsTabIndex;

        string github_UpdateResponse;

        bool AllowSaving = false;

        public MainForm()
        {

            // Locate Rocksmith Folder
            Startup_LocateRocksmith(GenUtil.GetRSDirectory());

            // Read Ini, or create an default one.
            Startup_ReadIniOrCreateDefault();

            // Load saved credidentials and enable PubSub
            PrepTwitch_LoadSettings();

            // Initialize WinForms
            Startup_InitWinForms();

            // Locate Rocksmith Saves
            Startup_LocateSaves(GenUtil.GetSaveFolder());

            // Check if the GUI settings, and DLL settings already exist
            Startup_VerifyGUIInstall();

            // Setup bindings for Twitch events
            Twitch_Setup();

            // Fix Legacy Songlist Bug
            Startup_FixLegacySonglistBug();

            // Fill Mod Keybindings List (Fill list box)
            Startup_LoadKeybindingModNames();

            // Fill Audio Keybinding List
            Startup_LoadAudioKeybindings();

            // Load Mod Keybinding Values
            Startup_ShowCurrentKeybindingValues();

            // Load Audio Keybinding Values
            Startup_ShowCurrentAudioKeybindingValues();

            // Load Guitar Speak Preset Values
            GuitarSpeak_ResetPresets();

            // Load Default String Colors
            StringColors_LoadDefaultStringColors();

            // Load Default Note Colors
            StringColors_LoadDefaultNoteColors();

            // Load Default Noteway Colors
            NotewayColors_LoadDefaultStringColors();

            // Load Colors Saved as Theme Colors.
            CustomTheme_LoadCustomColors();

            // Load Input Devices for Override Input Device Volume mod
            Startup_LoadInputDevices();

            // Load Midi Devices
            Midi_LoadDevices();

            // Load RS_ASIO
            Startup_VerifyInstallOfASIO();

            // Load RS_ASIO Settings
            PriorSettings_LoadASIOSettings();

            // Load Rocksmith Settings
            PriorSettings_LoadRocksmithSettings();

            // Load All Available Rocksmith Profiles
            Startup_LoadRocksmithProfiles();

            // Unpack Cache.psarc
            Startup_UnpackCachePsarc();

            // Load Set And Forget Mods
            SetForget_LoadSetAndForgetMods();

            // Load All System Fonts
            Fonts_Load();

            // Backup Profiles Just In Case
            Startup_BackupProfiles();

            // Load Checkbox Values From RSMods.ini
            PriorSettings_LoadModSettings();

            // Delete Old Backups To Save Space (if user specifies)
            Startup_DeleteOldBackups(GenUtil.StrToIntDef(ReadSettings.ProcessSettings(ReadSettings.NumberOfBackupsIdentifier), 50));

            // Lock the profile edits tab if backups are disabled
            Startup_LockProfileEdits();

            // Get list of all backups so we can revert to one if needed
            Startup_ListAllBackups();

            // Check For Updates
            CheckForUpdates_CallGithubAPI();

            // Should we show the Update button?
            Startup_ShowUpdateButton();

            // Is Audio.psarc unpacked?
            Startup_CheckStatusAudioPsarc();

            // Load SoundPacks Result Voice Over list
            SoundPacks_LoadResultVoiceOverList();

            AllowSaving = true;
        }

        #region Startup Functions

        private void Startup_ReadIniOrCreateDefault() => WriteSettings.LoadSettingsFromINI();

        private void Startup_InitWinForms()
        {
            InitializeComponent();
            Text = $"{Text}-{Assembly.GetExecutingAssembly().GetName().Version}"; // Show version number in the title of the application.
        }

        private void Startup_FixLegacySonglistBug()
        {
            if (ReadSettings.ProcessSettings(ReadSettings.Songlist1Identifier) == String.Empty)
                SaveSettings_Save(ReadSettings.Songlist1Identifier, "Define Song List 1 Here");
        }

        private void Startup_LocateRocksmith(string RSFolder)
        {

            if (RSFolder == String.Empty)
            {
                string newRSFolder = GenUtil.AskUserForRSFolder();

                if (newRSFolder == string.Empty)
                {
                    MessageBox.Show("We cannot detect where you have Rocksmith located. Please try reinstalling your game on Steam.", "Error: RSLocation Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }

                Constants.RSFolder = newRSFolder;
            }
            else
            {
                if (!Directory.Exists(RSFolder))
                {
                    MessageBox.Show("It looks like your current Rocksmith2014 install folder cannot be found. Please tell us where it is located!", "Error: RSLocation Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    string newRSFolder = GenUtil.AskUserForRSFolder();
                    if (newRSFolder == string.Empty)
                    {
                        MessageBox.Show("We cannot detect where you have Rocksmith located. Please try reinstalling your game on Steam.", "Error: RSLocation Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(1);
                    }

                    Constants.RSFolder = newRSFolder;
                }
                else
                    Constants.RSFolder = RSFolder;
            }

        }

        private void Startup_LocateSaves(string SavePath)
        {
            if (Constants.BypassSavePrompt == "true")
            {
                button_SetSavePath.Visible = true;
                return;
            }

            if (SavePath == string.Empty)
            {
                MessageBox.Show("It looks like your Rocksmith 2014 save folder cannot be found. Please tell us where it is located!\nThis can be found in your Steam install folder.\n<Path To Steam Install>/userdata/#/221680/remote", "Error: SavePath Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                string newSavePath = GenUtil.AskUserForSavePath();

                if (newSavePath == string.Empty)
                {
                    MessageBox.Show("We cannot detect where you have Rocksmith2014 saves are located.", "Error: SavePath Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }

                Constants.SavePath = newSavePath;
                Constants.BypassSavePrompt = "false";
            }
            else
            {
                if (!Directory.Exists(SavePath))
                {
                    string newSavePath = Profiles.GetSaveDirectory(true);

                    if (newSavePath == string.Empty)
                    {
                        MessageBox.Show("It looks like your Rocksmith 2014 save folder cannot be found. Please tell us where it is located!\nThis can be found in your Steam install folder.\n<Path To Steam Install>/userdata/#/221680/remote", "Error: SavePath Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        newSavePath = GenUtil.AskUserForSavePath();

                        if (newSavePath == string.Empty)
                        {
                            MessageBox.Show("We cannot detect where you have Rocksmith2014 saves are located.", "Error: SavePath Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Environment.Exit(1);
                        }
                    }

                    Constants.SavePath = newSavePath;
                    Constants.BypassSavePrompt = "false";
                }
                else
                {
                    Constants.SavePath = SavePath;
                    Constants.BypassSavePrompt = "false";
                }
            }
        }

        private void Startup_VerifyGUIInstall()
        {
            WriteSettings.IsVoid(GenUtil.GetRSDirectory());
            if (!File.Exists(Path.Combine(GenUtil.GetRSDirectory(), "RSMods.ini")))
                WriteSettings.WriteINI(WriteSettings.saveSettingsOrDefaults); // Creates Settings File

            List<string> settings = new List<string>() { $"RSPath = {Constants.RSFolder}", $"SavePath = {Constants.SavePath}", $"BypassSavePrompt = {Constants.BypassSavePrompt}" };

            File.WriteAllLines(Constants.SettingsPath, settings);
        }

        private void Startup_LoadSonglists()
        {
            foreach (string songlist in Dictionaries.refreshSonglists())
                listBox_Songlist.Items.Add(songlist);
        }

        private void Startup_LoadKeybindingModNames()
        {
            foreach (string mod in Dictionaries.currentModKeypressList)
                listBox_Modlist_MODS.Items.Add(mod);
        }

        private void Startup_LoadAudioKeybindings()
        {
            foreach (string volume in Dictionaries.currentAudioKeypressList)
                listBox_Modlist_AUDIO.Items.Add(volume);
        }

        private void Startup_ShowCurrentKeybindingValues()
        {
            label_ToggleLoftKey.Text = "Toggle Loft: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.ToggleLoftIdentifier));
            label_SongTimerKey.Text = "Show Song Timer: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.ShowSongTimerIdentifier));
            label_ReEnumerationKey.Text = "Force ReEnumeration: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.ForceReEnumerationIdentifier));
            label_RainbowStringsKey.Text = "Rainbow Strings: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.RainbowStringsIdentifier));
            label_RainbowNotesKey.Text = "Rainbow Notes: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.RainbowNotesIdentifier));
            label_RemoveLyricsKey.Text = "Remove Lyrics: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.RemoveLyricsKeyIdentifier));
            label_RRSpeedKey.Text = "RR Speed: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.RRSpeedKeyIdentifier));
            label_TuningOffsetKey.Text = "Tuning Offset: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.TuningOffsetKeyIdentifier));
            label_ToggleExtendedRangeKey.Text = "Toggle Extended Range: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.ToggleExtendedRangeKeyIdentifier));
            label_LoopStartKey.Text = "Start Loop: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.LoopStartKeyIdentifier));
            label_LoopEndKey.Text = "End Loop: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.LoopEndKeyIdentifier));
            label_RewindKey.Text = "Rewind Song: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.RewindKeyIdentifier));
        }

        private void Startup_ShowCurrentAudioKeybindingValues()
        {
            label_MasterVolumeKey.Text = "Master Volume: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.MasterVolumeKeyIdentifier));
            label_SongVolumeKey.Text = "Song Volume: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.SongVolumeKeyIdentifier));
            label_Player1VolumeKey.Text = "Player1 Volume: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.Player1VolumeKeyIdentifier));
            label_Player2VolumeKey.Text = "Player2 Volume: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.Player2VolumeKeyIdentifier));
            label_MicrophoneVolumeKey.Text = "Microphone Volume: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.MicrophoneVolumeKeyIdentifier));
            label_VoiceOverVolumeKey.Text = "Voice-Over Volume: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.VoiceOverVolumeKeyIdentifier));
            label_SFXVolumeKey.Text = "SFX Volume: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.SFXVolumeKeyIdentifier));
            label_DisplayMixerKey.Text = "Display Mixer: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.DisplayMixerKeyIdentifier));
            label_MutePlayer1Key.Text = "Mute / Unmute Player1: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.MutePlayer1KeyIdentifier));
            label_MutePlayer2Key.Text = "Mute / Unmute Player2: " + KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(ReadSettings.MutePlayer2KeyIdentifier));
        }

        private void Startup_LoadInputDevices()
        {
            MMDeviceEnumerator enumerator = new MMDeviceEnumerator();
            MMDeviceCollection devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            foreach (MMDevice device in devices)
            {
                listBox_AvailableInputDevices.Items.Add(device.FriendlyName);
            }
        }

        private void Startup_LoadASIODevices()
        {
            foreach (ASIO.Devices.DriverInfo device in ASIO.Devices.FindDevices())
            {
                listBox_AvailableASIODevices_Input0.Items.Add(device.deviceName);
                listBox_AvailableASIODevices_Input1.Items.Add(device.deviceName);
                listBox_AvailableASIODevices_Output.Items.Add(device.deviceName);
                listBox_AvailableASIODevices_InputMic.Items.Add(device.deviceName);
            }
        }

        private void Startup_VerifyInstallOfASIO()
        {
            if (!ASIO.ReadSettings.VerifySettingsExist())
                TabController.TabPages.Remove(tab_RSASIO);
            else
                Startup_LoadASIODevices();
        }

        private void Startup_LoadRocksmithProfiles()
        {
            Dictionary<string, string> AvailableProfiles = Profiles.AvailableProfiles();

            foreach (KeyValuePair<string, string> profileData in AvailableProfiles)
            {
                listBox_AutoLoadProfiles.Items.Add(profileData.Key);
                listBox_Profiles_AvailableProfiles.Items.Add(profileData.Key);
            }

            try
            {
                if (AvailableProfiles != null && AvailableProfiles.Count > 0)
                {
                    int MaxSongLists = 6;
                    foreach (string prf in AvailableProfiles.Keys)
                    {
                        // Decrypt Profile
                        JObject decPrf = JObject.Parse(Profiles.DecryptProfiles(Profiles_GetProfilePathFromName(prf)));

                        // Check how many song lists
                        int TotalSongLists = decPrf["SongListsRoot"]["SongLists"].ToObject<List<List<string>>>().Count;
                        decPrf = null;

                        if (TotalSongLists > MaxSongLists)
                            MaxSongLists = TotalSongLists;
                    }

                    label_TotalSonglists.Text = MaxSongLists.ToString();

                    Profiles_Helper_GenerateValidSonglists(MaxSongLists);
                }
                // We cannot load the Rocksmith profiles.
                else
                {
                    Startup_LoadSonglists();
                }
            }
            catch
            {
                Startup_LoadSonglists();
            }
        }

        private void Startup_DeleteOldBackups(int maxAmountOfBackups)
        {

            if (maxAmountOfBackups == 0) // User says they want all the backups.
                return;

            string backupFolder = Path.Combine(RSMods.Data.Constants.RSFolder, "Profile_Backups");

            if (!Directory.Exists(backupFolder))
                return;

            DirectoryInfo[] backups = new DirectoryInfo(backupFolder).GetDirectories().OrderBy(f => f.LastWriteTime).ToArray();

            int foldersLeftToRemove = backups.Length - maxAmountOfBackups;

            foreach (DirectoryInfo backup in backups)
            {
                if (foldersLeftToRemove == 0)
                    break;

                if (Array.IndexOf(backups, backup.Name) < backups.Length - maxAmountOfBackups)
                {
                    foreach (string file in Directory.GetFiles(backup.FullName))
                    {
                        File.Delete(file);
                    }
                    Directory.Delete(backup.FullName);
                    foldersLeftToRemove--;
                }

            }
        }

        private void Startup_BackupProfiles()
        {
            if (ReadSettings.ProcessSettings(ReadSettings.BackupProfileIdentifier) == "on")
                Profiles.SaveProfile();
        }

        private void Startup_UnlockProfileEdits()
        {
            if (ReadSettings.ProcessSettings(ReadSettings.BackupProfileIdentifier) == "on" && shipProfileEdits)
                TabController.TabPages.Insert(ProfileEditsTabIndex, tab_Profiles);
        }

        private void Startup_LockProfileEdits()
        {
            ProfileEditsTab = tab_Profiles;
            ProfileEditsTabIndex = TabController.TabPages.IndexOf(ProfileEditsTab);

            if (ReadSettings.ProcessSettings(ReadSettings.BackupProfileIdentifier) != "on" || !shipProfileEdits || !Constants.SavePath.IsSavePath())
                TabController.TabPages.Remove(tab_Profiles);
        }

        private void Startup_ListAllBackups()
        {
            try
            {
                List<string> backups = new List<string>();
                foreach (string backup in Directory.GetDirectories(Path.Combine(GenUtil.GetRSDirectory(), "Profile_Backups")))
                {
                    string folderName = Path.GetFileNameWithoutExtension(backup);
                    string date = folderName.Split('_')[0];
                    string time = folderName.Split('_')[1];

                    int month = Convert.ToInt32(date.Split('-')[0]);
                    int day = Convert.ToInt32(date.Split('-')[1]);
                    int year = Convert.ToInt32(date.Split('-')[2]);

                    string userFriendlyName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month) + " " + day + " " + year + " @ " + time.Replace('-', ':');

                    backups.Add(userFriendlyName);
                }
                backups.Reverse();
                backups.ForEach(b => listBox_Profiles_ListBackups.Items.Add(b));
            }
            catch // Folder doesn't exist
            {
            }
        }

        private void Startup_ShowUpdateButton() => button_UpdateRSMods.Visible = CheckForUpdates_IsUpdateAvailable();

        private void Startup_CheckStatusAudioPsarc() => SoundPacks_ChangeUIForUnpackedFolder(Directory.Exists("audio_psarc"));

        private void Startup_UnpackCachePsarc() => SetAndForgetMods.UnpackCachePsarc();

        #endregion
        #region Show Prior Settings In GUI
        private void PriorSettings_LoadModSettings()
        {
            if (ReadSettings.ProcessSettings(ReadSettings.ToggleLoftEnabledIdentifier) == "on") // Toggle Loft Enabled / Disabled
            {
                checkBox_ToggleLoft.Checked = true;
                radio_LoftAlwaysOff.Visible = true;
                radio_LoftOffHotkey.Visible = true;
                radio_LoftOffInSong.Visible = true;
                groupBox_LoftOffWhen.Visible = true;

                if (ReadSettings.ProcessSettings(ReadSettings.ToggleLoftWhenIdentifier) == "startup")
                    radio_LoftAlwaysOff.Checked = true;
                else if (ReadSettings.ProcessSettings(ReadSettings.ToggleLoftWhenIdentifier) == "manual")
                    radio_LoftOffHotkey.Checked = true;
                else if (ReadSettings.ProcessSettings(ReadSettings.ToggleLoftWhenIdentifier) == "song")
                    radio_LoftOffInSong.Checked = true;
            }

            if (ReadSettings.ProcessSettings(ReadSettings.VolumeControlEnabledIdentifier) == "on") // Add Volume Enabled / Disabled
            {
                checkBox_ControlVolume.Checked = true;
                groupBox_Keybindings_AUDIO.Visible = true;
                groupBox_ControlVolumeIncrement.Visible = true;

                string valStr = ReadSettings.ProcessSettings(ReadSettings.VolumeControlIntervalIdentifier);
                int intVal = 0;

                if (int.TryParse(valStr, out intVal))
                    nUpDown_VolumeInterval.Value = intVal;
            }

            if (ReadSettings.ProcessSettings(ReadSettings.ShowSongTimerEnabledIdentifier) == "on") // Show Song Timer Enabled / Disabled
            {
                checkBox_SongTimer.Checked = true;
                groupBox_SongTimer.Visible = true;
                if (ReadSettings.ProcessSettings(ReadSettings.ShowSongTimerWhenIdentifier) == "automatic")
                    radio_SongTimerAlways.Checked = true;
                else
                    radio_SongTimerManual.Checked = true;
            }

            if (ReadSettings.ProcessSettings(ReadSettings.ForceReEnumerationEnabledIdentifier) != "off") // Force Enumeration Settings
            {
                radio_ForceEnumerationAutomatic.Visible = true;
                radio_ForceEnumerationManual.Visible = true;
                groupBox_HowToEnumerate.Visible = true;
                if (ReadSettings.ProcessSettings(ReadSettings.ForceReEnumerationEnabledIdentifier) == "automatic")
                    radio_ForceEnumerationAutomatic.Checked = true;
                else
                    radio_ForceEnumerationManual.Checked = true;
            }

            if (ReadSettings.ProcessSettings(ReadSettings.ExtendedRangeEnabledIdentifier) == "on") // Extended Range Enabled / Disabled
            {
                checkBox_ExtendedRange.Checked = true;
            }

            if (ReadSettings.ProcessSettings(ReadSettings.CustomStringColorNumberIndetifier) != "0") // Custom String Colors
            {
                checkBox_CustomColors.Checked = true;
                groupBox_StringColors.Visible = true;
            }

            /* Disco Mode: Deprecated, as of now, because you can't toggle it off easily.
                DiscoModeCheckbox.Checked = ReadSettings.ProcessSettings(ReadSettings.DiscoModeIdentifier) == "on";
            */

            if (ReadSettings.ProcessSettings(ReadSettings.RemoveHeadstockIdentifier) == "on") // Remove Headstock Enabled / Disabled
            {
                checkBox_RemoveHeadstock.Checked = true;
                groupBox_ToggleHeadstockOffWhen.Visible = true;

                if (ReadSettings.ProcessSettings(ReadSettings.RemoveHeadstockWhenIdentifier) == "startup")
                    radio_HeadstockAlwaysOff.Checked = true;
                else if (ReadSettings.ProcessSettings(ReadSettings.RemoveHeadstockWhenIdentifier) == "song")
                    radio_HeadstockOffInSong.Checked = true;
            }

            if (ReadSettings.ProcessSettings(ReadSettings.RemoveSkylineIdentifier) == "on") // Remove Skyline Enabled / Disabled
            {
                checkBox_RemoveSkyline.Checked = true;
                groupBox_ToggleSkylineWhen.Visible = true;

                if (ReadSettings.ProcessSettings(ReadSettings.ToggleSkylineWhenIdentifier) == "song") // Remove Skyline on Song Load
                    radio_SkylineOffInSong.Checked = true;
                else if (ReadSettings.ProcessSettings(ReadSettings.ToggleSkylineWhenIdentifier) == "startup") // Remove Skyline on Game Startup 
                    radio_SkylineAlwaysOff.Checked = true;
            }

            if (ReadSettings.ProcessSettings(ReadSettings.ForceProfileEnabledIdentifier) == "on") // Force Load Profile On Game Boot Enabled / Disabled
            {
                checkBox_AutoLoadProfile.Checked = true;
                if (ReadSettings.ProcessSettings(ReadSettings.ProfileToLoadIdentifier) != "")
                    listBox_AutoLoadProfiles.SelectedItem = ReadSettings.ProcessSettings(ReadSettings.ProfileToLoadIdentifier);
            }

            if (ReadSettings.ProcessSettings(ReadSettings.RemoveLyricsIdentifier) == "on") // Remove Lyrics
            {
                checkBox_RemoveLyrics.Checked = true;
                groupBox_ToggleLyricsOffWhen.Visible = true;

                if (ReadSettings.ProcessSettings(ReadSettings.RemoveLyricsWhenIdentifier) == "startup") // Remove Lyrics When ...
                    radio_LyricsAlwaysOff.Checked = true;
                else if (ReadSettings.ProcessSettings(ReadSettings.RemoveLyricsWhenIdentifier) == "manual") // Remove Lyrics When ...
                    radio_LyricsOffHotkey.Checked = true;
            }

            if (ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakIdentifier) == "on")
            {
                checkBox_GuitarSpeak.Checked = true;
                groupBox_GuitarSpeak.Visible = true;
                checkBox_GuitarSpeakWhileTuning.Visible = true;
            }

            if (ReadSettings.ProcessSettings(ReadSettings.RiffRepeaterAboveHundredIdentifier) == "on")
            {
                checkBox_RiffRepeaterSpeedAboveOneHundred.Checked = true;
                groupBox_RRSpeed.Visible = true;

                string val = ReadSettings.ProcessSettings(ReadSettings.RiffRepeaterSpeedIntervalIdentifier);
                decimal decimalVal = 0;

                if (Decimal.TryParse(val, out decimalVal))
                {
                    nUpDown_RiffRepeaterSpeed.Value = decimalVal;
                    SaveSettings_Save(ReadSettings.RiffRepeaterSpeedIntervalIdentifier, decimalVal.ToString());
                }

            }

            if (ReadSettings.ProcessSettings(ReadSettings.MidiAutoTuningIdentifier) == "on")
            {
                checkBox_useMidiAutoTuning.Checked = true;
                groupBox_MidiAutoTuneDevice.Visible = true;
                label_SelectedMidiOutDevice.Text = "Midi Device: " + ReadSettings.ProcessSettings(ReadSettings.MidiAutoTuningDeviceIdentifier);
                groupBox_MidiAutoTuningOffset.Visible = true;
                listBox_MidiAutoTuningOffset.SelectedIndex = GenUtil.StrToIntDef(ReadSettings.ProcessSettings(ReadSettings.MidiTuningOffsetIdentifier), 0) + 3;
                groupBox_MidiAutoTuningWhen.Visible = true;
                //groupBox_MidiInDevice.Visible = true;
                label_SelectedMidiInDevice.Text = "Midi Device: " + ReadSettings.ProcessSettings(ReadSettings.MidiInDeviceIdentifier);

                if (ReadSettings.ProcessSettings(ReadSettings.TuningPedalIdentifier) != "")
                {
                    int tuningPedal = GenUtil.StrToIntDef(ReadSettings.ProcessSettings(ReadSettings.TuningPedalIdentifier), 0);

                    switch (tuningPedal)
                    {
                        case 1:
                            radio_WhammyDT.Checked = true;
                            break;
                        case 2:
                            radio_WhammyBass.Checked = true;
                            checkBox_WhammyChordsMode.Visible = true;
                            break;
                        case 3:
                            radio_Whammy.Checked = true;
                            checkBox_WhammyChordsMode.Visible = true;
                            break;
                        case 4:
                            radio_SoftwarePedal.Checked = true;
                            break;
                        default:
                            break;
                    }

                }

                switch (ReadSettings.ProcessSettings(ReadSettings.MidiAutoTuningWhenIdentifier))
                {
                    default: // Intentional fall-through
                    case "manual":
                        radio_AutoTuningWhenManual.Checked = true;
                        break;
                    case "tuner":
                        radio_AutoTuningWhenTuner.Checked = true;
                        break;
                }
            }

            if (ReadSettings.ProcessSettings(ReadSettings.BackupProfileIdentifier) == "on")
            {
                nUpDown_NumberOfBackups.Value = GenUtil.StrToIntDef(ReadSettings.ProcessSettings(ReadSettings.NumberOfBackupsIdentifier), 50);
                groupBox_Backups.Visible = true;
            }

            if (ReadSettings.ProcessSettings(ReadSettings.OverrideInputVolumeEnabledIdentifier) == "on")
            {
                checkBox_OverrideInputVolume.Checked = true;
                groupBox_OverrideInputVolume.Visible = true;
            }

            checkBox_EnableLooping.Checked = ReadSettings.ProcessSettings(ReadSettings.AllowLoopingIdentifier) == "on";
            groupBox_LoopingLeadUp.Visible = checkBox_EnableLooping.Checked;
            nUpDown_LoopingLeadUp.Value = GenUtil.EstablishMaxValue((GenUtil.StrToDecDef(ReadSettings.ProcessSettings(ReadSettings.LoopingLeadUpIdentifier), 0) / 1000), 5.000m);
            checkBox_GuitarSpeakWhileTuning.Checked = ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakTuningIdentifier) == "on";
            checkBox_ChangeTheme.Checked = ReadSettings.ProcessSettings(ReadSettings.CustomGUIThemeIdentifier) == "on";
            checkBox_ScreenShotScores.Checked = ReadSettings.ProcessSettings(ReadSettings.ScreenShotScoresIdentifier) == "on";
            checkBox_Fretless.Checked = ReadSettings.ProcessSettings(ReadSettings.FretlessModeEnabledIdentifier) == "on";
            checkBox_RemoveInlays.Checked = ReadSettings.ProcessSettings(ReadSettings.RemoveInlaysIdentifier) == "on";
            checkBox_RemoveLaneMarkers.Checked = ReadSettings.ProcessSettings(ReadSettings.RemoveLaneMarkersIdentifier) == "on";
            checkBox_GreenScreen.Checked = ReadSettings.ProcessSettings(ReadSettings.GreenScreenWallIdentifier) == "on";
            checkBox_RainbowStrings.Checked = ReadSettings.ProcessSettings(ReadSettings.RainbowStringsEnabledIdentifier) == "on";
            checkBox_RainbowNotes.Checked = ReadSettings.ProcessSettings(ReadSettings.RainbowNotesEnabledIdentifier) == "on";
            checkBox_WhammyChordsMode.Checked = ReadSettings.ProcessSettings(ReadSettings.ChordsModeIdentifier) == "on";
            checkBox_ShowCurrentNote.Checked = ReadSettings.ProcessSettings(ReadSettings.ShowCurrentNoteOnScreenIdentifier) == "on";
            checkBox_CustomHighway.Checked = ReadSettings.ProcessSettings(ReadSettings.CustomHighwayColorsIdentifier) == "on";
            checkBox_SecondaryMonitor.Checked = ReadSettings.ProcessSettings(ReadSettings.SecondaryMonitorIdentifier) == "on";
            checkBox_NoteColors_UseRocksmithColors.Checked = ReadSettings.ProcessSettings(ReadSettings.SeparateNoteColorsModeIdentifier) == "1";
            checkBox_RemoveSongPreviews.Checked = ReadSettings.ProcessSettings(ReadSettings.RemoveSongPreviewsIdentifier) == "on";
            checkBox_AllowAudioInBackground.Checked = ReadSettings.ProcessSettings(ReadSettings.AllowAudioInBackgroundIdentifier) == "on";
            checkBox_BypassTwoRTCMessageBox.Checked = ReadSettings.ProcessSettings(ReadSettings.BypassTwoRTCMessageBoxIdentifier) == "on";
            checkBox_LinearRiffRepeater.Checked = ReadSettings.ProcessSettings(ReadSettings.LinearRiffRepeaterIdentifier) == "on";
            checkBox_UseAltSampleRate_Output.Checked = ReadSettings.ProcessSettings(ReadSettings.UseAlternativeOutputSampleRateIdentifier) == "on";
            groupBox_SampleRateOutput.Visible = checkBox_UseAltSampleRate_Output.Checked;
            listBox_AltSampleRatesOutput.SelectedItem = ReadSettings.ProcessSettings(ReadSettings.AlternativeOutputSampleRateIdentifier) + " Hz";
            nUpDown_ForceEnumerationXMS.Value = GenUtil.StrToIntDef(ReadSettings.ProcessSettings(ReadSettings.CheckForNewSongIntervalIdentifier), 5000) / 1000; // Loads old settings for enumeration every x ms
            listBox_AvailableInputDevices.SelectedItem = ReadSettings.ProcessSettings(ReadSettings.OverrideInputVolumeDeviceIdentifier);
            nUpDown_OverrideInputVolume.Value = GenUtil.StrToIntDef(ReadSettings.ProcessSettings(ReadSettings.OverrideInputVolumeIdentifier), 17);
            checkBox_ER_SeparateNoteColors.Checked = ReadSettings.ProcessSettings(ReadSettings.SeparateNoteColorsIdentifier) == "on";
            groupBox_NoteColors.Visible = checkBox_ER_SeparateNoteColors.Checked;
            checkBox_BackupProfile.Checked = ReadSettings.ProcessSettings(ReadSettings.BackupProfileIdentifier) == "on";
            checkBox_ModsLog.Checked = File.Exists(Path.Combine(GenUtil.GetRSDirectory(), "RSMods_debug.txt"));
            checkBox_TurnOffAllMods.Checked = !File.Exists(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll")) && File.Exists(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll.off"));
            checkBox_ForceEnumeration.Checked = ReadSettings.ProcessSettings(ReadSettings.ForceReEnumerationEnabledIdentifier) != "off";
            checkBox_AllowRewind.Checked = ReadSettings.ProcessSettings(ReadSettings.AllowRewindIdentifier) == "on";
            groupBox_RewindBy.Visible = checkBox_AllowRewind.Checked;
            nUpDown_RewindBy.Value = GenUtil.EstablishMaxValue((GenUtil.StrToDecDef(ReadSettings.ProcessSettings(ReadSettings.RewindByIdentifier), 0) / 1000), 90.000m);
            checkBox_FixOculusCrash.Checked = ReadSettings.ProcessSettings(ReadSettings.FixOculusCrashIdentifier) == "on";
            checkBox_FixBrokenTones.Checked = ReadSettings.ProcessSettings(ReadSettings.FixBrokenTonesIdentifier) == "on";
            checkBox_CustomNSPTimer.Checked = ReadSettings.ProcessSettings(ReadSettings.UseCustomNSPTimerIdentifier) == "on";
            groupBox_NSPTimer.Visible = checkBox_CustomNSPTimer.Checked;
            nUpDown_NSPTimer.Value = GenUtil.EstablishMaxValue((GenUtil.StrToDecDef(ReadSettings.ProcessSettings(ReadSettings.CustomNSPTimeLimitIdentifier), 10000) / 1000), 60.000m);
        }

        private void PriorSettings_LoadASIOSettings()
        {
            if (!ASIO.ReadSettings.VerifySettingsExist())
                return;

            // Config
            checkBox_ASIO_WASAPI_Output.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableWasapiOutputsIdentifier, ASIO.ReadSettings.Sections.Config), 0));
            checkBox_ASIO_WASAPI_Input.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableWasapiInputsIdentifier, ASIO.ReadSettings.Sections.Config), 0));
            checkBox_ASIO_ASIO.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableAsioIdentifier, ASIO.ReadSettings.Sections.Config), 0));

            // Asio
            if (ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.BufferSizeModeIdentifier, ASIO.ReadSettings.Sections.Asio) == "custom")
            {
                radio_ASIO_BufferSize_Custom.Checked = true;
                nUpDown_ASIO_CustomBufferSize.Value = GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.CustomBufferSizeIdentifier, ASIO.ReadSettings.Sections.Asio), 48);
            }
            if (ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.BufferSizeModeIdentifier, ASIO.ReadSettings.Sections.Asio) == "driver")
                radio_ASIO_BufferSize_Driver.Checked = true;
            if (ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.BufferSizeModeIdentifier, ASIO.ReadSettings.Sections.Asio) == "host")
                radio_ASIO_BufferSize_Host.Checked = true;

            // Output
            nUpDown_ASIO_Output_BaseChannel.Value = GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.BaseChannelIdentifier, ASIO.ReadSettings.Sections.Output), 0);
            nUpDown_ASIO_Output_AltBaseChannel.Value = GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.AltBaseChannelIdentifier, ASIO.ReadSettings.Sections.Output), 0);
            checkBox_ASIO_Output_ControlEndpointVolume.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableSoftwareEndpointVolumeControlIdentifier, ASIO.ReadSettings.Sections.Output), 0));
            checkBox_ASIO_Output_ControlMasterVolume.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableSoftwareMasterVolumeControlIdentifier, ASIO.ReadSettings.Sections.Output), 0));
            nUpDown_ASIO_Output_MaxVolume.Value = GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.SoftwareMasterVolumePercentIdentifier, ASIO.ReadSettings.Sections.Output), 0);
            checkBox_ASIO_Output_Disabled.Checked = ASIO.ReadSettings.DisabledOutput;
            listBox_AvailableASIODevices_Output.SelectedItem = ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Output);
            checkBox_ASIO_Output_EnableRefHack.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableRefCountHackIdentifier, ASIO.ReadSettings.Sections.Output), 0));

            // Input0
            nUpDown_ASIO_Input0_Channel.Value = GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.ChannelIdentifier, ASIO.ReadSettings.Sections.Input0), 0);
            checkBox_ASIO_Input0_ControlEndpointVolume.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableSoftwareEndpointVolumeControlIdentifier, ASIO.ReadSettings.Sections.Input0), 0));
            checkBox_ASIO_Input0_ControlMasterVolume.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableSoftwareMasterVolumeControlIdentifier, ASIO.ReadSettings.Sections.Input0), 0));
            nUpDown_ASIO_Input0_MaxVolume.Value = GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.SoftwareMasterVolumePercentIdentifier, ASIO.ReadSettings.Sections.Input0), 0);
            checkBox_ASIO_Input0_Disabled.Checked = ASIO.ReadSettings.DisabledInput0;
            listBox_AvailableASIODevices_Input0.SelectedItem = ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Input0);
            checkBox_ASIO_Input0_EnableRefHack.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableRefCountHackIdentifier, ASIO.ReadSettings.Sections.Input0), 0));

            // Input1
            nUpDown_ASIO_Input1_Channel.Value = GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.ChannelIdentifier, ASIO.ReadSettings.Sections.Input1), 0);
            checkBox_ASIO_Input1_ControlEndpointVolume.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableSoftwareEndpointVolumeControlIdentifier, ASIO.ReadSettings.Sections.Input1), 0));
            checkBox_ASIO_Input1_ControlMasterVolume.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableSoftwareMasterVolumeControlIdentifier, ASIO.ReadSettings.Sections.Input1), 0));
            nUpDown_ASIO_Input1_MaxVolume.Value = GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.SoftwareMasterVolumePercentIdentifier, ASIO.ReadSettings.Sections.Input1), 0);
            checkBox_ASIO_Input1_Disabled.Checked = ASIO.ReadSettings.DisabledInput1;
            listBox_AvailableASIODevices_Input1.SelectedItem = ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Input1);
            checkBox_ASIO_Input1_EnableRefHack.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableRefCountHackIdentifier, ASIO.ReadSettings.Sections.Input1), 0));

            // InputMic
            nUpDown_ASIO_InputMic_Channel.Value = GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.ChannelIdentifier, ASIO.ReadSettings.Sections.InputMic), 0);
            checkBox_ASIO_InputMic_ControlEndpointVolume.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableSoftwareEndpointVolumeControlIdentifier, ASIO.ReadSettings.Sections.InputMic), 0));
            checkBox_ASIO_InputMic_ControlMasterVolume.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableSoftwareMasterVolumeControlIdentifier, ASIO.ReadSettings.Sections.InputMic), 0));
            nUpDown_ASIO_InputMic_MaxVolume.Value = GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.SoftwareMasterVolumePercentIdentifier, ASIO.ReadSettings.Sections.InputMic), 0);
            checkBox_ASIO_InputMic_Disabled.Checked = ASIO.ReadSettings.DisabledInputMic;
            listBox_AvailableASIODevices_InputMic.SelectedItem = ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.InputMic);
            checkBox_ASIO_InputMic_EnableRefHack.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(ASIO.ReadSettings.ProcessSettings(ASIO.ReadSettings.EnableRefCountHackIdentifier, ASIO.ReadSettings.Sections.InputMic), 0));
        }

        private void PriorSettings_LoadRocksmithSettings()
        {
            // Audio Settings

            checkBox_Rocksmith_EnableMicrophone.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.EnableMicrophoneIdentifier), 1));
            checkBox_Rocksmith_ExclusiveMode.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.ExclusiveModeIdentifier), 1));
            if (GenUtil.StrToDecDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.LatencyBufferIdentifier), 16) <= 0 || GenUtil.StrToDecDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.LatencyBufferIdentifier), 16) > 16)
                SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.LatencyBufferIdentifier, "4");
            nUpDown_Rocksmith_LatencyBuffer.Value = GenUtil.StrToDecDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.LatencyBufferIdentifier), 4);
            checkBox_Rocksmith_ForceWDM.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.ForceWDMIdentifier), 0));
            checkBox_Rocksmith_ForceDirextXSink.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.ForceDirectXSinkIdentifier), 0));
            checkBox_Rocksmith_DumpAudioLog.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.DumpAudioLogIdentifier), 0));
            if (Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.MaxOutputBufferSizeIdentifier), 0)))
                nUpDown_Rocksmith_MaxOutputBuffer.Value = GenUtil.StrToDecDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.MaxOutputBufferSizeIdentifier), 0);
            else
                checkBox_Rocksmith_Override_MaxOutputBufferSize.Checked = true;
            checkBox_Rocksmith_RTCOnly.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.RealToneCableOnlyIdentifier), 0));
            checkBox_Rocksmith_LowLatencyMode.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.Win32UltraLowLatencyModeIdentifier), 1));
            // Visual Settings

            checkBox_Rocksmith_GamepadUI.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.ShowGamepadUIIdentifier), 0));
            nUpDown_Rocksmith_ScreenWidth.Value = GenUtil.StrToDecDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.ScreenWidthIdentifier), 0);
            nUpDown_Rocksmith_ScreenHeight.Value = GenUtil.StrToDecDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.ScreenHeightIdentifier), 0);
            switch (GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.FullscreenIdentifier), 2))
            {
                case 0:
                    radio_Rocksmith_Windowed.Checked = true;
                    break;
                case 1:
                    radio_Rocksmith_NonExclusiveFullScreen.Checked = true;
                    break;
                case 2:
                    radio_Rocksmith_ExclusiveFullScreen.Checked = true;
                    break;
                default:
                    break;
            }
            nUpDown_Rocksmith_RenderWidth.Value = GenUtil.StrToDecDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.RenderingWidthIdentifier), 0);
            nUpDown_Rocksmith_RenderHeight.Value = GenUtil.StrToDecDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.RenderingHeightIdentifier), 0);
            checkBox_Rocksmith_PostEffects.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.EnablePostEffectsIdentifier), 1));
            checkBox_Rocksmith_Shadows.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.EnableShadowsIdentifier), 1));
            checkBox_Rocksmith_HighResScope.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.EnableHighResScopeIdentifier), 1));
            checkBox_Rocksmith_DepthOfField.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.EnableDepthOfFieldIdentifier), 1));
            checkBox_Rocksmith_PerPixelLighting.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.EnablePerPixelLightingIdentifier), 1));
            checkBox_Rocksmith_MSAASamples.Checked = GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.MsaaSamplesIdentifier), 4) == 4;
            checkBox_Rocksmith_DisableBrowser.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.DisableBrowserIdentifier), 0));
            checkBox_Rocksmith_EnableRenderRes.Checked = (Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.RenderingWidthIdentifier) != "0" || Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.RenderingHeightIdentifier) != "0");

            switch (GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.VisualQualityIdentifier), 1))
            {
                case 0:
                    radio_Rocksmith_LowQuality.Checked = true;
                    break;
                case 1:
                    radio_Rocksmith_MediumQuality.Checked = true;
                    break;
                case 2:
                    radio_Rocksmith_HighQuality.Checked = true;
                    break;
                case 3:
                    radio_Rocksmith_CustomQuality.Checked = true;
                    break;
                default:
                    break;
            }

            // Network Settings
            checkBox_Rocksmith_UseProxy.Checked = Convert.ToBoolean(GenUtil.StrToIntDef(Rocksmith.ReadSettings.ProcessSettings(Rocksmith.ReadSettings.UseProxyIdentifier), 1));
        }

        #endregion
        #region Custom Themes

        // Not taken from here :O https://stackoverflow.com/a/3419209
        private List<Control> ControlList = new List<Control>(); // Don't make this readonly
        private void GetAllControls(Control container)
        {
            foreach (Control c in container.Controls)
            {
                GetAllControls(c);
                if (c is ListBox || c is GroupBox || c is TabPage || c is Button)
                    ControlList.Add(c);
            }
        }

        private void CustomTheme_ChangeTheme(Color backgroundColor, Color textColor, Color buttonColor)
        {
            GetAllControls(TabController);
            BackColor = backgroundColor; // MainForm BackColor
            ForeColor = textColor; // MainForm ForeColor

            foreach (Control controlToChange in ControlList)
            {
                controlToChange.ForeColor = textColor;

                if (controlToChange is Button)
                    controlToChange.BackColor = buttonColor;
                else
                    controlToChange.BackColor = backgroundColor;
            }

            CustomTheme_DataGridView(dgv_DefaultRewards, backgroundColor, textColor);
            CustomTheme_DataGridView(dgv_EnabledRewards, backgroundColor, textColor);
            CustomTheme_DataGridView(dgv_Profiles_Songlists, backgroundColor, textColor);

            // Twitch Log. Can't be done automatically or it will break other text boxes :(
            textBox_TwitchLog.ForeColor = textColor;
            textBox_TwitchLog.BackColor = backgroundColor;
        }

        private void CustomTheme_DataGridView(DataGridView grid, Color backgroundColor, Color textColor)
        {
            grid.EnableHeadersVisualStyles = false; // Allows us to customize the color scheme

            // Background Colors
            grid.BackgroundColor = backgroundColor;
            grid.ColumnHeadersDefaultCellStyle.BackColor = backgroundColor;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = backgroundColor;
            grid.DefaultCellStyle.SelectionBackColor = backgroundColor;

            // Foreground Colors
            grid.ForeColor = textColor;
            grid.DefaultCellStyle.SelectionForeColor = textColor;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = textColor;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = textColor;
        }

        private void CustomTheme_LoadCustomColors()
        {
            Color backColor = WriteSettings.defaultBackgroundColor, foreColor = WriteSettings.defaultTextColor, buttonColor = WriteSettings.defaultButtonColor;

            if (ReadSettings.ProcessSettings(ReadSettings.CustomGUIThemeIdentifier) == "on") // Users uses a custom theme.
            {
                if (ReadSettings.ProcessSettings(ReadSettings.CustomGUIBackgroundColorIdentifier) != String.Empty)
                    backColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.CustomGUIBackgroundColorIdentifier));

                if (ReadSettings.ProcessSettings(ReadSettings.CustomGUITextColorIdentifier) != String.Empty)
                    foreColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.CustomGUITextColorIdentifier));

                if (ReadSettings.ProcessSettings(ReadSettings.CustomGUIButtonColorIdentifier) != String.Empty)
                    buttonColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.CustomGUIButtonColorIdentifier));
            }

            textBox_ChangeBackgroundColor.BackColor = backColor;
            textBox_ChangeTextColor.BackColor = foreColor;
            textBox_ChangeButtonColor.BackColor = buttonColor;

            CustomTheme_ChangeTheme(textBox_ChangeBackgroundColor.BackColor, textBox_ChangeTextColor.BackColor, textBox_ChangeButtonColor.BackColor);
        }

        private void CustomTheme_ChangeTheme(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.CustomGUIThemeIdentifier, checkBox_ChangeTheme.Checked.ToString().ToLower());
            groupBox_ChangeTheme.Visible = checkBox_ChangeTheme.Checked;

            if (!checkBox_ChangeTheme.Checked) // Turning off custom themes
                CustomTheme_ChangeTheme(WriteSettings.defaultBackgroundColor, WriteSettings.defaultTextColor, WriteSettings.defaultButtonColor);
        }

        private void CustomTheme_ChangeBackgroundColor(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                ShowHelp = false,
                Color = WriteSettings.defaultBackgroundColor
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSettings_Save(ReadSettings.CustomGUIBackgroundColorIdentifier, (colorDialog.Color.ToArgb() & 0x00ffffff).ToString("X6"));
                textBox_ChangeBackgroundColor.BackColor = colorDialog.Color;
            }
        }

        private void CustomTheme_ChangeTextColor(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                ShowHelp = false,
                Color = WriteSettings.defaultTextColor
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSettings_Save(ReadSettings.CustomGUITextColorIdentifier, (colorDialog.Color.ToArgb() & 0x00ffffff).ToString("X6"));
                textBox_ChangeTextColor.BackColor = colorDialog.Color;
            }
        }

        private void CustomTheme_ChangeButtonColor(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                ShowHelp = false,
                Color = WriteSettings.defaultButtonColor
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSettings_Save(ReadSettings.CustomGUIButtonColorIdentifier, (colorDialog.Color.ToArgb() & 0x00ffffff).ToString("X6"));
                textBox_ChangeButtonColor.BackColor = colorDialog.Color;
            }
        }

        private void CustomTheme_Apply(object sender, EventArgs e) => CustomTheme_ChangeTheme(textBox_ChangeBackgroundColor.BackColor, textBox_ChangeTextColor.BackColor, textBox_ChangeButtonColor.BackColor);

        private void CustomTheme_Reset(object sender, EventArgs e)
        {
            CustomTheme_ChangeTheme(WriteSettings.defaultBackgroundColor, WriteSettings.defaultTextColor, WriteSettings.defaultButtonColor);

            SaveSettings_Save(ReadSettings.CustomGUIBackgroundColorIdentifier, (WriteSettings.defaultBackgroundColor.ToArgb() & 0x00ffffff).ToString("X6"));
            SaveSettings_Save(ReadSettings.CustomGUITextColorIdentifier, (WriteSettings.defaultTextColor.ToArgb() & 0x00ffffff).ToString("X6"));
            SaveSettings_Save(ReadSettings.CustomGUIButtonColorIdentifier, (WriteSettings.defaultButtonColor.ToArgb() & 0x00ffffff).ToString("X6"));

            textBox_ChangeBackgroundColor.BackColor = WriteSettings.defaultBackgroundColor;
            textBox_ChangeTextColor.BackColor = WriteSettings.defaultTextColor;
            textBox_ChangeButtonColor.BackColor = WriteSettings.defaultButtonColor;
        }

        #endregion
        #region Check For Keypresses (Keybindings)
        private void Keypress_CheckDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) // If enter is pressed
            {
                e.SuppressKeyPress = true; // Turns off the windows beep for pressing an invalid key.
                Save_Songlists_Keybindings(sender, e);
            }

            else if (((TextBox)sender) == textBox_NewKeyAssignment_MODS)
            {
                e.SuppressKeyPress = true; // Turns off the windows beep for pressing an invalid key.

                if (KeyConversion.KeyDownDictionary.Contains(e.KeyCode))
                    textBox_NewKeyAssignment_MODS.Text = e.KeyCode.ToString();

                else if ((e.KeyValue > 47 && e.KeyValue < 60) || (e.KeyValue > 64 && e.KeyValue < 91)) // Number or Letter was pressed (Will be overrided by text input)
                {
                    if (MessageBox.Show("The key you entered is currently used by Rocksmith and may interfere with being able to use the game properly. Are you sure you want to use this keybinding?", "Keybinding Warning!", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                        return;
                    else
                        textBox_NewKeyAssignment_MODS.Text = "";
                }
            }
            else if (((TextBox)sender) == textBox_NewKeyAssignment_AUDIO)
            {
                e.SuppressKeyPress = true; // Turns off the windows beep for pressing an invalid key.

                if (KeyConversion.KeyDownDictionary.Contains(e.KeyCode))
                    textBox_NewKeyAssignment_AUDIO.Text = e.KeyCode.ToString();

                else if ((e.KeyValue > 47 && e.KeyValue < 60) || (e.KeyValue > 64 && e.KeyValue < 91)) // Number or Letter was pressed (Will be overrided by text input)
                {
                    if (MessageBox.Show("The key you entered is currently used by Rocksmith and may interfere with being able to use the game properly. Are you sure you want to use this keybinding?", "Keybinding Warning!", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                        return;
                    else
                        textBox_NewKeyAssignment_AUDIO.Text = "";
                }
            }
        }

        private void Keypress_CheckUp(object sender, KeyEventArgs e)
        {
            if (KeyConversion.KeyUpDictionary.Contains(e.KeyCode))
            {
                if (sender == textBox_NewKeyAssignment_MODS)
                    textBox_NewKeyAssignment_MODS.Text = e.KeyCode.ToString();
                else if (sender == textBox_NewKeyAssignment_AUDIO)
                    textBox_NewKeyAssignment_AUDIO.Text = e.KeyCode.ToString();
            }

        }

        private void Keypress_CheckMouse(object sender, MouseEventArgs e)
        {
            if (KeyConversion.MouseButtonDictionary.Contains(e.Button))
            {
                if (sender == textBox_NewKeyAssignment_MODS)
                    textBox_NewKeyAssignment_MODS.Text = e.Button.ToString();
                else if (sender == textBox_NewKeyAssignment_AUDIO)
                    textBox_NewKeyAssignment_AUDIO.Text = e.Button.ToString();
            }

        }

        private void Keypress_LoadKeys(object sender, EventArgs e) => textBox_NewKeyAssignment_MODS.Text = Dictionaries.refreshKeybindingList()[listBox_Modlist_MODS.SelectedIndex];
        private void Keypress_LoadVolumes(object sender, EventArgs e) => textBox_NewKeyAssignment_AUDIO.Text = Dictionaries.refreshAudioKeybindingList()[listBox_Modlist_AUDIO.SelectedIndex];
        #endregion
        #region Reset To Default
        private void Reset_DefaultSettings(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to reset your mod settings to their defaults?", "WARNING: RESET TO DEFAULT?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                File.Delete(Path.Combine(GenUtil.GetRSDirectory(), "RSMods.ini"));
                WriteSettings.LoadSettingsFromINI(); // Create the default INI
                WriteSettings.WriteINI(WriteSettings.saveSettingsOrDefaults); // Refresh Form will regenerate all the settings, so we need to overwrite them.
                Reset_RefreshForm();
            }
            else
                MessageBox.Show("All your settings have been saved, and nothing was reset");
        }

        private void Reset_RefreshForm()
        {
            Hide();
            var newForm = new MainForm();
            newForm.Closed += (s, args) => Close();
            newForm.Show();
        }
        #endregion
        #region Save Settings
        private void SaveSettings_Save(string IdentifierToChange, string ChangedSettingValue)
        {
            if (!AllowSaving)
                return;

            // Right before launch, we switched from the boolean names of (true / false) to (on / off) for users to be able to edit the mods without the GUI (by hand).
            if (ChangedSettingValue == "true")
                ChangedSettingValue = "on";
            else if (ChangedSettingValue == "false")
                ChangedSettingValue = "off";

            foreach (string section in WriteSettings.saveSettingsOrDefaults.Keys)
            {
                foreach (KeyValuePair<string, string> entry in WriteSettings.saveSettingsOrDefaults[section])
                {
                    if (IdentifierToChange == entry.Key)
                    {
                        WriteSettings.saveSettingsOrDefaults[section][IdentifierToChange] = ChangedSettingValue;
                        break; // We found what we need, so let's leave.
                    }
                }
            }

            Debug.WriteLine(new StackFrame(1, true).GetMethod().Name);

            WriteSettings.WriteINI(WriteSettings.saveSettingsOrDefaults);
            SaveSettings_ShowLabel();
            WinMsgUtil.SendMsgToRS("update all");
        }

        private void SaveSettings_ShowLabel()
        {
            label_SettingsSaved.Visible = true;
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1500;
            timer.Tick += (source, e) => { label_SettingsSaved.Visible = false; timer.Stop(); };
            timer.Start();
        }

        private void Save_Songlists_Keybindings(object sender, EventArgs e) // Save Songlists and Keybindings when pressing Enter
        {
            if (!AllowSaving)
                return;


            TextBox textBox = ((TextBox)sender);

            // Song Lists
            if (textBox.Name == textBox_NewSonglistName.Name)
            {

                foreach (string currentSongList in Dictionaries.SongListIndexToINISetting)
                {
                    int index = Dictionaries.SongListIndexToINISetting.IndexOf(currentSongList);

                    if (textBox_NewSonglistName.Text.Trim() == "") // The game UI will break with a blank name.
                    {
                        MessageBox.Show("You cannot save a blank song list name as the game will break", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }
                    else if (index == listBox_Songlist.SelectedIndex)
                    {
                        SaveSettings_Save(currentSongList, textBox_NewSonglistName.Text);
                        listBox_Songlist.Items[index] = textBox_NewSonglistName.Text;
                        break;
                    };
                }

                Profiles_RefreshSonglistNames();
            }

            // Mod Keybindings
            if (textBox.Name == textBox_NewKeyAssignment_MODS.Name)
            {
                foreach (string currentKeybinding in Dictionaries.KeybindingsIndexToINISetting)
                {
                    int index = Dictionaries.KeybindingsIndexToINISetting.IndexOf(currentKeybinding);

                    if (textBox_NewKeyAssignment_MODS.Text == "")
                    {
                        MessageBox.Show("You cannot set a blank keybind", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }
                    else if (index == listBox_Modlist_MODS.SelectedIndex)
                    {
                        SaveSettings_Save(currentKeybinding, KeyConversion.VirtualKey(textBox_NewKeyAssignment_MODS.Text));
                        break;
                    }
                }

                textBox_NewKeyAssignment_MODS.Text = String.Empty;
            }

            // Audio Keybindings
            if (textBox.Name == textBox_NewKeyAssignment_AUDIO.Name)
            {
                foreach (string currentKeybinding in Dictionaries.AudioKeybindingsIndexToINISetting)
                {
                    int index = Dictionaries.AudioKeybindingsIndexToINISetting.IndexOf(currentKeybinding);

                    if (textBox_NewKeyAssignment_AUDIO.Text == "")
                    {
                        MessageBox.Show("You cannot set a blank keybind", "Warning!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }
                    else if (index == listBox_Modlist_AUDIO.SelectedIndex)
                    {
                        SaveSettings_Save(currentKeybinding, KeyConversion.VirtualKey(textBox_NewKeyAssignment_AUDIO.Text));
                        break;
                    }
                }

                textBox_NewKeyAssignment_AUDIO.Text = String.Empty;
            }
            Startup_ShowCurrentKeybindingValues();
            Startup_ShowCurrentAudioKeybindingValues();
        }

        private void SaveSettings_ASIO_Middleware(string identifierToChange, ASIO.ReadSettings.Sections section, string ChangedSettingValue)
        {
            if (!AllowSaving)
                return;


            ASIO.WriteSettings.SaveChanges(identifierToChange, section, ChangedSettingValue, checkBox_ASIO_Output_Disabled.Checked, checkBox_ASIO_Input0_Disabled.Checked, checkBox_ASIO_Input1_Disabled.Checked, checkBox_ASIO_InputMic_Disabled.Checked);
            SaveSettings_ShowLabel();
        }

        private void SaveSettings_Rocksmith_Middleware(string identifierToChange, string ChangedSettingValue)
        {
            if (!AllowSaving)
                return;

            Rocksmith.WriteSettings.SaveChanges(identifierToChange, ChangedSettingValue);
            SaveSettings_ShowLabel();
        }

        #endregion
        #region String Colors

        private void StringColors_ChangeStringColor(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                ShowHelp = false
            };

            int stringColorKey = 0;
            if (radio_ER1StringColors.Checked)
            {
                stringColorKey = 1;
            }
            else if (radio_ER2StringColors.Checked)
            {
                stringColorKey = 2;
            }
            string stringColorButtonIdentifier = String.Empty;
            int stringNumber = 0;
            StringColors_FillStringNumberToColorDictionary();

            foreach (KeyValuePair<string, string> stringColorButton in Dictionaries.stringColorButtonsToSettingIdentifiers[stringColorKey])
            {
                if (sender.ToString().Contains(stringColorButton.Key.ToString()))
                {
                    stringColorButtonIdentifier = stringColorButton.Value.ToString();
                    break; // We have the one value we need, so we can leave.
                }
                stringNumber++;
            }

            colorDialog.Color = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(stringColorButtonIdentifier));

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSettings_Save(ReadSettings.CustomStringColorNumberIndetifier, "2"); // Tell the game to use custom colors
                SaveSettings_Save(stringColorButtonIdentifier, (colorDialog.Color.ToArgb() & 0x00ffffff).ToString("X6"));
                stringNumberToColorTextBox[stringNumber].BackColor = colorDialog.Color;
            }
        }

        private void StringColors_ChangeNoteColor(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                ShowHelp = false
            };
            bool isNormalNotes = radio_DefaultNoteColors.Checked; // True = Normal, False = Colorblind
            string noteColorButtonIdentifier = String.Empty;
            int noteNumber = 0;

            StringColors_FillNoteNumberToColorDictionary();

            foreach (KeyValuePair<string, string> noteColorButton in Dictionaries.noteColorButtonsToSettingIdentifiers[isNormalNotes])
            {
                if (sender.ToString().Contains(noteColorButton.Key.ToString()))
                {
                    noteColorButtonIdentifier = noteColorButton.Value.ToString();
                    break; // We have the one value we need, so we can leave.
                }
                noteNumber++;
            }

            colorDialog.Color = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(noteColorButtonIdentifier));

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSettings_Save(ReadSettings.SeparateNoteColorsModeIdentifier, "2"); // Tell the game to use custom note colors
                SaveSettings_Save(noteColorButtonIdentifier, (colorDialog.Color.ToArgb() & 0x00ffffff).ToString("X6"));
                stringNumberToColorTextBox[noteNumber].BackColor = colorDialog.Color;
            }
        }

        private void StringColors_LoadDefaultStringColors()
        {
            if (ReadSettings.ProcessSettings(ReadSettings.String0Color_N_Identifier) != String.Empty) // Fixes a small use case where the GUI moves faster than the writing of the INI.
            {
                textBox_String0Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String0Color_N_Identifier));
                textBox_String1Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String1Color_N_Identifier));
                textBox_String2Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String2Color_N_Identifier));
                textBox_String3Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String3Color_N_Identifier));
                textBox_String4Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String4Color_N_Identifier));
                textBox_String5Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String5Color_N_Identifier));
            }
            else
                WriteSettings.WriteINI(WriteSettings.saveSettingsOrDefaults);
        }

        private void StringColors_LoadER1StringColors()
        {
            if (ReadSettings.ProcessSettings(ReadSettings.String0Color_ER1_Identifier) != String.Empty) // Fixes a small use case where the GUI moves faster than the writing of the INI.
            {
                textBox_String0Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String0Color_ER1_Identifier));
                textBox_String1Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String1Color_ER1_Identifier));
                textBox_String2Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String2Color_ER1_Identifier));
                textBox_String3Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String3Color_ER1_Identifier));
                textBox_String4Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String4Color_ER1_Identifier));
                textBox_String5Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String5Color_ER1_Identifier));
            }
            else
                WriteSettings.WriteINI(WriteSettings.saveSettingsOrDefaults);
        }

        private void StringColors_LoadER2StringColors()
        {
            if (ReadSettings.ProcessSettings(ReadSettings.String0Color_ER2_Identifier) != String.Empty) // Fixes a small use case where the GUI moves faster than the writing of the INI.
            {
                textBox_String0Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String0Color_ER2_Identifier));
                textBox_String1Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String1Color_ER2_Identifier));
                textBox_String2Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String2Color_ER2_Identifier));
                textBox_String3Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String3Color_ER2_Identifier));
                textBox_String4Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String4Color_ER2_Identifier));
                textBox_String5Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String5Color_ER2_Identifier));
            }
            else
                WriteSettings.WriteINI(WriteSettings.saveSettingsOrDefaults);
        }

        private void StringColors_LoadDefaultNoteColors()
        {
            if (ReadSettings.ProcessSettings(ReadSettings.Note0Color_N_Identifier) != String.Empty) // Fixes a small use case where the GUI moves faster than the writing of the INI.
            {
                textBox_Note0Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.Note0Color_N_Identifier));
                textBox_Note1Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.Note1Color_N_Identifier));
                textBox_Note2Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.Note2Color_N_Identifier));
                textBox_Note3Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.Note3Color_N_Identifier));
                textBox_Note4Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.Note4Color_N_Identifier));
                textBox_Note5Color.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.Note5Color_N_Identifier));
            }
            else
                WriteSettings.WriteINI(WriteSettings.saveSettingsOrDefaults);
        }

        private void StringColors_DefaultStringColors(object sender, EventArgs e) => StringColors_LoadDefaultStringColors();

        private void StringColors_ER1StringColors(object sender, EventArgs e) => StringColors_LoadER1StringColors();

        private void StringColors_ER2StringColors(object sender, EventArgs e) => StringColors_LoadER2StringColors();

        private void StringColors_DefaultNoteColors(object sender, EventArgs e) => StringColors_LoadDefaultNoteColors();

        #endregion
        #region Noteway Colors
        private void NotewayColors_ChangeNotewayColor(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                ShowHelp = false
            };

            string notewayColorButtonIdentifier = String.Empty;
            int notewayObject = 0;
            NotewayColors_FillNotewayButtonToColorDictionary();

            foreach (KeyValuePair<string, string> notewayColorButton in Dictionaries.notewayColorButtonsToSettingIdentifier)
            {
                if (sender.ToString().Contains(notewayColorButton.Key.ToString()))
                {
                    notewayColorButtonIdentifier = notewayColorButton.Value.ToString();
                    break; // We have the one value we need, so we can leave.
                }
                notewayObject++;
            }

            if (ReadSettings.ProcessSettings(notewayColorButtonIdentifier) != "")
                colorDialog.Color = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(notewayColorButtonIdentifier));

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                SaveSettings_Save(notewayColorButtonIdentifier, (colorDialog.Color.ToArgb() & 0x00ffffff).ToString("X6"));
                notewayButtonToColorTextbox[((Button)sender)].BackColor = colorDialog.Color;
            }
        }

        private void NotewayColors_LoadDefaultStringColors()
        {
            if (ReadSettings.ProcessSettings(ReadSettings.CustomHighwayNumberedIdentifier) != "")
                textBox_ShowNumberedFrets.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.CustomHighwayNumberedIdentifier));
            if (ReadSettings.ProcessSettings(ReadSettings.CustomHighwayUnNumberedIdentifier) != "")
                textBox_ShowUnNumberedFrets.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.CustomHighwayUnNumberedIdentifier));
            if (ReadSettings.ProcessSettings(ReadSettings.CustomHighwayGutterIdentifier) != "")
                textBox_ShowNotewayGutter.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.CustomHighwayGutterIdentifier));
            if (ReadSettings.ProcessSettings(ReadSettings.CustomFretNubmersIdentifier) != "")
                textBox_ShowFretNumber.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.CustomFretNubmersIdentifier));
        }

        #endregion
        #region Prep Set And Forget Mods
        // SetAndForget Mods

        private void SetForget_FillUI()
        {
            listBox_Tunings.Items.Clear();
            SetAndForgetMods.TuningsCollection = SetAndForgetMods.LoadTuningsCollection();

            listBox_Tunings.Items.Add("<New>");
            foreach (var key in SetAndForgetMods.TuningsCollection.Keys)
                listBox_Tunings.Items.Add(key);
        }

        private TuningDefinitionInfo SetForget_GetCurrentTuningInfo()
        {
            var tuningDefinition = new TuningDefinitionInfo();
            var strings = new Dictionary<string, int>();

            for (int strIdx = 0; strIdx < 6; strIdx++)
                strings[$"string{strIdx}"] = (int)((NumericUpDown)tabPage_SetAndForget_CustomTunings.Controls[$"nUpDown_String{strIdx}"]).Value;

            tuningDefinition.Strings = strings;
            tuningDefinition.UIName = String.Format("$[{0}]{1}", nUpDown_UIIndex.Value.ToString(), textBox_UIName.Text);

            return tuningDefinition;
        }
        private void SetForget_LoadSetAndForgetMods()
        {
            SetAndForgetMods.LoadDefaultFiles();
            SetAndForgetMods.UnpackCachePsarc(); // We need to unpack the cache AGAIN in-case the user resets their psarc, and we don't know.
            SetForget_FillUI();
            SetForget_SetTunerColors();
        }
        #endregion
        #region Set And Forget UI Functions
        private void SetForget_SetTunerColors(int string_num = -1, int extendedRange = 0)
        {
            switch (string_num)
            {
                case 0: // Set low E string color
                    if (extendedRange == 1)
                        nUpDown_String0.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String0Color_ER1_Identifier));
                    else if (extendedRange == 2)
                        nUpDown_String0.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String0Color_ER2_Identifier));
                    else
                        nUpDown_String0.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String0Color_N_Identifier));
                    break;
                case 1: // Set A string Color
                    if (extendedRange == 1)
                        nUpDown_String1.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String1Color_ER1_Identifier));
                    else if (extendedRange == 2)
                        nUpDown_String1.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String1Color_ER2_Identifier));
                    else
                        nUpDown_String1.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String1Color_N_Identifier));
                    break;
                case 2: // Set D string color
                    if (extendedRange == 1)
                        nUpDown_String2.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String2Color_ER1_Identifier));
                    else if (extendedRange == 1)
                        nUpDown_String2.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String2Color_ER2_Identifier));
                    else
                        nUpDown_String2.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String2Color_N_Identifier));
                    break;
                case 3: // Set G string color
                    if (extendedRange == 1)
                        nUpDown_String3.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String3Color_ER1_Identifier));
                    else if (extendedRange == 1)
                        nUpDown_String3.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String3Color_ER2_Identifier));
                    else
                        nUpDown_String3.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String3Color_N_Identifier));
                    break;
                case 4: // Set B string color
                    if (extendedRange == 1)
                        nUpDown_String4.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String4Color_ER1_Identifier));
                    else if (extendedRange == 2)
                        nUpDown_String4.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String4Color_ER2_Identifier));
                    else
                        nUpDown_String4.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String4Color_N_Identifier));
                    break;
                case 5: // Set high e string color
                    if (extendedRange == 1)
                        nUpDown_String5.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String5Color_ER1_Identifier));
                    else if (extendedRange == 2)
                        nUpDown_String5.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String5Color_ER2_Identifier));
                    else
                        nUpDown_String5.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String5Color_N_Identifier));
                    break;
                default: // Set all string colors
                    if (extendedRange == 1)
                    {
                        nUpDown_String0.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String0Color_ER1_Identifier));
                        nUpDown_String1.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String1Color_ER1_Identifier));
                        nUpDown_String2.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String2Color_ER1_Identifier));
                        nUpDown_String3.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String3Color_ER1_Identifier));
                        nUpDown_String4.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String4Color_ER1_Identifier));
                        nUpDown_String5.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String5Color_ER1_Identifier));
                    }
                    else if (extendedRange == 2)
                    {
                        nUpDown_String0.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String0Color_ER2_Identifier));
                        nUpDown_String1.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String1Color_ER2_Identifier));
                        nUpDown_String2.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String2Color_ER2_Identifier));
                        nUpDown_String3.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String3Color_ER2_Identifier));
                        nUpDown_String4.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String4Color_ER2_Identifier));
                        nUpDown_String5.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String5Color_ER2_Identifier));
                    }
                    else
                    {
                        nUpDown_String0.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String0Color_N_Identifier));
                        nUpDown_String1.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String1Color_N_Identifier));
                        nUpDown_String2.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String2Color_N_Identifier));
                        nUpDown_String3.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String3Color_N_Identifier));
                        nUpDown_String4.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String4Color_N_Identifier));
                        nUpDown_String5.BackColor = ColorTranslator.FromHtml("#" + ReadSettings.ProcessSettings(ReadSettings.String5Color_N_Identifier));
                    }
                    break;
            }
        }


        private void SetForget_RestoreDefaults(object sender, EventArgs e)
        {
            if (SetAndForgetMods.RestoreDefaults())
                SetForget_FillUI();
        }

        private void SetForget_ResetCache(object sender, EventArgs e)
        {
            if (MessageBox.Show("Woah, hang on there!\nHave you tried pressing the \"Restore Cache Backup\" button?\nThis should be a last resort.\nWe call home to Steam to redownload all modified files.\nThis will only break the mods in this section, nothing else.", "HANG ON!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                System.Diagnostics.Process.Start("steam://validate/221680");
                SetAndForgetMods.RemoveTempFolders();
            }

        }
        private void SetForget_TurnItUpToEleven(object sender, EventArgs e) => SetAndForgetMods.AddIncreasedVolumeWwiseBank();

        private void SetForget_UnpackCacheAgain(object sender, EventArgs e) => SetAndForgetMods.CleanUnpackedCache();

        private void SetForget_AddCustomTunings(object sender, EventArgs e) => SetAndForgetMods.AddCustomTunings();

        private void SetForget_AddFastLoadMod(object sender, EventArgs e) => SetAndForgetMods.AddFastLoadMod();

        private void SetForget_ListTunings(object sender, EventArgs e)
        {
            if (sender == null || listBox_Tunings.SelectedItem == null)
                return;

            string selectedItem = listBox_Tunings.SelectedItem.ToString();

            if (selectedItem == "<New>")
            {
                textBox_InternalTuningName.Text = "";
                nUpDown_UIIndex.Value = 0;
                textBox_UIName.Text = "";
                listBox_SetAndForget_SongsWithSelectedTuning.Items.Clear();
                return;
            }

            var selectedTuning = SetAndForgetMods.TuningsCollection[selectedItem];
            var uiName = SetAndForgetMods.SplitTuningUIName(selectedTuning.UIName);

            textBox_InternalTuningName.Text = selectedItem;
            nUpDown_UIIndex.Value = Convert.ToInt32(uiName.Item1);
            textBox_UIName.Text = uiName.Item2;

            for (int strIdx = 0; strIdx < 6; strIdx++) // If you are lazy and don't want to list each string separately, just do this sexy two-liner
                ((NumericUpDown)tabPage_SetAndForget_CustomTunings.Controls[$"nUpDown_String{strIdx}"]).Value = selectedTuning.Strings[$"string{strIdx}"];

            SetForget_FillSongsWithSelectedTuningList();
        }

        private void SetForget_SaveTuningChanges(object sender, EventArgs e)
        {
            if (listBox_Tunings.SelectedIndex != -1) // If we are saving a change to the currently selected tuning, perform a change in the collection, otherwise directly go to saving
            {
                string selectedItem = listBox_Tunings.SelectedItem.ToString();

                if (selectedItem != "<New>")
                {
                    SetAndForgetMods.TuningsCollection[selectedItem] = SetForget_GetCurrentTuningInfo();

                    if (listBox_SetAndForget_SongsWithCustomTuning.Items.Count > 0)
                        SetForget_LoadSongsToWorkOn(sender, e);
                }
            }

            SetAndForgetMods.SaveTuningsJSON();

            MessageBox.Show("Saved current tuning, don't forget to press \"Add Custom Tunings\" button when you are done!", "Success");
        }

        private void SetForget_RemoveTuning(object sender, EventArgs e)
        {
            if (listBox_Tunings.SelectedIndex == -1)
                return;

            string selectedItem = listBox_Tunings.SelectedItem.ToString();

            if (selectedItem == "<New>")
                return;

            SetAndForgetMods.TuningsCollection.Remove(selectedItem); // I guess we would be better here using BindingSource on Listbox + ObservableCollection instead of Dict to make changes reflect automatically, but... one day
            listBox_Tunings.Items.Remove(selectedItem);

            if (listBox_SetAndForget_SongsWithCustomTuning.Items.Count > 0)
                SetForget_LoadSongsToWorkOn(sender, e);
        }

        private void SetForget_AddTuning(object sender, EventArgs e)
        {
            if (listBox_Tunings.SelectedIndex == -1)
                listBox_Tunings.SelectedIndex = 0;

            if (listBox_Tunings.SelectedItem.ToString() != "<New>")
                return;

            var currTuning = SetForget_GetCurrentTuningInfo();
            string internalName = textBox_InternalTuningName.Text;

            if (internalName.Trim() == "")
            {
                MessageBox.Show("You cannot have a blank internal name.");
                return;
            }

            if (!SetAndForgetMods.TuningsCollection.ContainsKey(internalName)) // Unlikely to happen, but still... prevent users accidentaly trying to add existing stuff
            {
                SetAndForgetMods.TuningsCollection.Add(internalName, currTuning);
                listBox_Tunings.Items.Add(internalName);

                if (listBox_SetAndForget_SongsWithCustomTuning.Items.Count > 0)
                    SetForget_LoadSongsToWorkOn(sender, e);
            }
            else
                MessageBox.Show("You already have a tuning with the same internal name");
        }

        private void SetForget_AddCustomMenu(object sender, EventArgs e) => SetAndForgetMods.AddExitGameMenuOption();

        private void SetForget_AddDCMode(object sender, EventArgs e) => SetAndForgetMods.AddDirectConnectModeOption();

        private void SetForget_RemoveTempFolders(object sender, EventArgs e) => SetAndForgetMods.RemoveTempFolders();

        private void SetForget_SetDefaultTones(object sender, EventArgs e)
        {
            if (listBox_ProfileTones.SelectedItem == null)
                return;

            int selectedToneType = -1;
            if (radio_DefaultRhythmTone.Checked)
                selectedToneType = 0;
            else if (radio_DefaultLeadTone.Checked)
                selectedToneType = 1;
            else if (radio_DefaultBassTone.Checked)
                selectedToneType = 2;

            string selectedToneName = listBox_ProfileTones.SelectedItem.ToString();

            SetAndForgetMods.SetDefaultTones(selectedToneName, selectedToneType);
        }

        private void SetForget_LoadTonesFromProfiles(object sender, EventArgs e)
        {
            var profileTones = SetAndForgetMods.GetSteamProfilesTones();

            if (profileTones.Count > 0)
            {
                listBox_ProfileTones.Items.Clear();

                profileTones.ForEach(t => listBox_ProfileTones.Items.Add(t));
            }
        }

        private void SetForget_ImportExistingSettings(object sender, EventArgs e)
        {
            if (SetAndForgetMods.ImportExistingSettings())
                SetForget_FillUI();
        }

        private void SetForget_AssignNewGuitarArcadeTone(object sender, EventArgs e)
        {
            if (listBox_ProfileTones.SelectedItem == null)
                return;

            int selectedToneType = -1;

            var gaRadioControls = new List<RadioButton>() {
                radio_TempleOfBendsTone, radio_ScaleWarriorsTone, radio_StringsSkipSaloonTone,
                radio_ScaleRacerTone, radio_NinjaSlideNTone, radio_HurtlinHurdlesTone, radio_HarmonicHeistTone,
                radio_DucksReduxTone, radio_RainbowLaserTone, radio_GoneWailinTone};

            for (int i = 0; i < 10; i++)
            {
                if (gaRadioControls[i].Checked)
                {
                    selectedToneType = i;
                    break;
                }
            }

            string selectedToneName = listBox_ProfileTones.SelectedItem.ToString();

            SetAndForgetMods.SetGuitarArcadeTone(selectedToneName, selectedToneType);
        }

        private void SetForget_LoadSongsToWorkOn(object sender, EventArgs e)
        {
            Songs = SongManager.ExtractSongData(progressBar_FillSongsWithCustomTunings); // Load all the data from the songs

            SetForget_ShowLoadedSongs(); // Makes the listboxes and labels visible for songs with tunings.
            SetForget_FillDefinedTunings(); // Get a list of all of our non-"Custom Tuning"s.
            SetForget_FillCustomTuningList(); // Get a list of all song & arrangement combos that will show up as "Custom Tuning" if not dealt with.
            // SetForget_FillSongsWithBadBassTuningsList(); // DISABLED: Not integrated with the automated fixing | Get a list of all song & arrangement combos that have a bass tuning that is not in the Rocksmith tuning format.
            SetForget_FillSongsWithSelectedTuningList(); // Get a list of all song & arrangement combos that have the same tuning as selected in listBox_Tunings.
        }

        private void SetForget_ShowLoadedSongs()
        {
            // Songs with Selected Tuning
            label_SongsWithSelectedTuning.Visible = true;
            listBox_SetAndForget_SongsWithSelectedTuning.Visible = true;

            // Songs that may show up as "Custom Tuning".
            label_SetAndForget_MayShowUpAsCustomTuning.Visible = true;
            listBox_SetAndForget_SongsWithCustomTuning.Visible = true;
        }

        private TuningDefinitionInfo SetForget_ConvertTuningStandards(ArrangementTuning tuning, string name)
        {
            TuningDefinitionInfo convertedTuning = new TuningDefinitionInfo();

            convertedTuning.UIName = name;
            convertedTuning.Strings.Add("string0", tuning.String0);
            convertedTuning.Strings.Add("string1", tuning.String1);
            convertedTuning.Strings.Add("string2", tuning.String2);
            convertedTuning.Strings.Add("string3", tuning.String3);
            convertedTuning.Strings.Add("string4", tuning.String4);
            convertedTuning.Strings.Add("string5", tuning.String5);

            return convertedTuning;
        }

        private ArrangementTuning SetForget_ConvertTuningStandards(TuningDefinitionInfo tuning)
        {
            ArrangementTuning convertedTuning = new ArrangementTuning();

            foreach (KeyValuePair<string, int> t in tuning.Strings)
            {
                switch (t.Key)
                {
                    case "string0":
                        convertedTuning.String0 = t.Value;
                        break;
                    case "string1":
                        convertedTuning.String1 = t.Value;
                        break;
                    case "string2":
                        convertedTuning.String2 = t.Value;
                        break;
                    case "string3":
                        convertedTuning.String3 = t.Value;
                        break;
                    case "string4":
                        convertedTuning.String4 = t.Value;
                        break;
                    case "string5":
                        convertedTuning.String5 = t.Value;
                        break;
                }
            }

            return convertedTuning;
        }

        List<ArrangementTuning> definedTunings = new List<ArrangementTuning>();

        private void SetForget_FillDefinedTunings()
        {
            definedTunings.Clear();

            foreach (TuningDefinitionInfo tuningDefinition in SetAndForgetMods.TuningsCollection.Values)
            {
                definedTunings.Add(SetForget_ConvertTuningStandards(tuningDefinition));
            }
        }

        private void SetForget_FillCustomTuningList()
        {
            customTunings.Clear();

            foreach (SongData song in Songs)
            {
                foreach (SongArrangement arrangement in song.Arrangements)
                {
                    string formatting = string.Empty;

                    if (arrangement.Attributes.ArrangementProperties.Represent == 0)
                        formatting += "Alt ";
                    else if (arrangement.Attributes.ArrangementProperties.BonusArr == 1)
                        formatting += "Bonus ";

                    formatting += arrangement.Attributes.ArrangementName + " for " + song.Artist + " - " + song.Title;
                    if (!definedTunings.Contains(arrangement.Attributes.Tuning) && !customTunings.ContainsKey(formatting))
                    {
                        customTunings.Add(formatting, arrangement.Attributes.Tuning);
                    }
                }
            }

            listBox_SetAndForget_SongsWithCustomTuning.Items.Clear();
            listBox_SetAndForget_SongsWithCustomTuning.Items.AddRange(customTunings.Keys.ToArray());
        }

        private void SetForget_FillSongsWithSelectedTuningList()
        {
            listBox_SetAndForget_SongsWithSelectedTuning.Items.Clear();

            List<string> songsWithTuning = new List<string>();

            if (listBox_Tunings.SelectedIndex == -1 || listBox_Tunings.SelectedItem.ToString() == "<New>")
                return;

            ArrangementTuning selectedTuning = SetForget_ConvertTuningStandards(SetAndForgetMods.TuningsCollection[listBox_Tunings.SelectedItem.ToString()]);

            foreach (SongData song in Songs)
            {
                foreach (SongArrangement arrangement in song.Arrangements)
                {
                    string formatting = string.Empty;

                    if (arrangement.Attributes.ArrangementProperties.Represent == 0)
                        formatting += "Alt ";
                    else if (arrangement.Attributes.ArrangementProperties.BonusArr == 1)
                        formatting += "Bonus ";

                    formatting += arrangement.Attributes.ArrangementName + " for " + song.Artist + " - " + song.Title;
                    if (arrangement.Attributes.Tuning.Equals(selectedTuning))
                    {
                        songsWithTuning.Add(formatting);
                    }
                }
            }

            songsWithTuning.Sort();
            listBox_SetAndForget_SongsWithSelectedTuning.Items.AddRange(songsWithTuning.ToArray());
        }

        private void SetForget_FillSongsWithBadBassTuningsList()
        {
            listBox_SetAndForget_SongsWithBadBassTuning.Items.Clear();
            List<string> songsBeingChanged = new List<string>();

            foreach (SongData song in Songs)
            {
                foreach (SongArrangement arrangement in song.Arrangements)
                {

                    string formatting = string.Empty;

                    if (arrangement.Attributes.ArrangementProperties.Represent == 0)
                        formatting += "Alt ";
                    else if (arrangement.Attributes.ArrangementProperties.BonusArr == 1)
                        formatting += "Bonus ";

                    formatting += arrangement.Attributes.ArrangementName + " for " + song.Artist + " - " + song.Title;
                    ArrangementTuning arrangementTuning = arrangement.Attributes.Tuning;
                    if (arrangement.Attributes.ArrangementName.ToLower().Contains("bass")) // Should be formatted this way or we will lose alt / bonus bass.
                    {
                        if (!SetForget_IsTuningStandard(arrangement.Attributes.Tuning) && !SetForget_IsTuningDrop(arrangement.Attributes.Tuning) && !songsBeingChanged.Contains(formatting))
                        {
                            if (!song.ODLC && !(arrangementTuning.String0 == 0 || arrangementTuning.String1 == 0 || arrangementTuning.String2 == 0 || arrangementTuning.String3 == 0) && ((arrangementTuning.String4 == 0 && arrangementTuning.String5 == 0) || (arrangementTuning.String4 == 12 && arrangementTuning.String5 == 12)))
                            {
                                songsBeingChanged.Add(formatting);
                            }
                        }
                    }
                }
            }
            listBox_SetAndForget_SongsWithBadBassTuning.Items.AddRange(songsBeingChanged.ToArray());
        }

        private bool SetForget_IsTuningStandard(object tuning, bool forceBass = false)
        {
            if (tuning is ArrangementTuning)
            {
                ArrangementTuning arrTuning = ((ArrangementTuning)tuning);
                if (forceBass)
                    return arrTuning.String0 == arrTuning.String1 && arrTuning.String1 == arrTuning.String2 && arrTuning.String2 == arrTuning.String3;
                else
                    return arrTuning.String0 == arrTuning.String1 && arrTuning.String1 == arrTuning.String2 && arrTuning.String2 == arrTuning.String3 && arrTuning.String3 == arrTuning.String4 && arrTuning.String4 == arrTuning.String5;
            }
            else if (tuning is TuningDefinitionInfo)
                return SetForget_IsTuningStandard(SetForget_ConvertTuningStandards((TuningDefinitionInfo)tuning), forceBass);
            else
                return false;
        }

        private bool SetForget_IsTuningDrop(object tuning, bool forceBass = false)
        {
            if (tuning is ArrangementTuning)
            {
                ArrangementTuning arrTuning = ((ArrangementTuning)tuning);
                if (forceBass)
                    return arrTuning.String0 + 2 == arrTuning.String1 && arrTuning.String1 == arrTuning.String2 && arrTuning.String2 == arrTuning.String3;
                else
                    return arrTuning.String0 + 2 == arrTuning.String1 && arrTuning.String1 == arrTuning.String2 && arrTuning.String2 == arrTuning.String3 && arrTuning.String3 == arrTuning.String4 && arrTuning.String4 == arrTuning.String5;
            }
            else if (tuning is TuningDefinitionInfo)
                return SetForget_IsTuningDrop(SetForget_ConvertTuningStandards((TuningDefinitionInfo)tuning), forceBass);
            else
                return false;
        }

        SortedDictionary<string, ArrangementTuning> customTunings = new SortedDictionary<string, ArrangementTuning>();

        private void SetForget_LoadCustomTuningFromSong(object sender, EventArgs e)
        {
            if (listBox_SetAndForget_SongsWithCustomTuning.SelectedIndex < 0)
                return;

            ArrangementTuning customTuning = customTunings[listBox_SetAndForget_SongsWithCustomTuning.SelectedItem.ToString()];

            nUpDown_String0.Value = customTuning.String0;
            nUpDown_String1.Value = customTuning.String1;
            nUpDown_String2.Value = customTuning.String2;
            nUpDown_String3.Value = customTuning.String3;
            nUpDown_String4.Value = customTuning.String4;
            nUpDown_String5.Value = customTuning.String5;

            listBox_Tunings.SelectedIndex = 0; // "<New>"
        }

        private void SetForget_TuningOffsets(object sender, EventArgs e)
        {
            string nupName = ((NumericUpDown)sender).Name;
            int stringNumber = Int32.Parse(nupName[nupName.Length - 1].ToString()); // Returns the current sender's name.
            switch (stringNumber)
            {
                case 0:
                    int offset = 40; // E2 (Midi)
                    label_CustomTuningLowEStringLetter.Text = GuitarSpeak.GuitarSpeakNoteOctaveMath(Convert.ToString(Convert.ToInt32(nUpDown_String0.Value) + offset));
                    break;
                case 1:
                    offset = 45; // A2 (Midi)
                    label_CustomTuningAStringLetter.Text = GuitarSpeak.GuitarSpeakNoteOctaveMath(Convert.ToString(Convert.ToInt32(nUpDown_String1.Value) + offset));
                    break;
                case 2:
                    offset = 50; // D3 (Midi)
                    label_CustomTuningDStringLetter.Text = GuitarSpeak.GuitarSpeakNoteOctaveMath(Convert.ToString(Convert.ToInt32(nUpDown_String2.Value) + offset));
                    break;
                case 3:
                    offset = 55;// G3 (Midi)
                    label_CustomTuningGStringLetter.Text = GuitarSpeak.GuitarSpeakNoteOctaveMath(Convert.ToString(Convert.ToInt32(nUpDown_String3.Value) + offset));
                    break;
                case 4:
                    offset = 59; // B3 (Midi)
                    label_CustomTuningBStringLetter.Text = GuitarSpeak.GuitarSpeakNoteOctaveMath(Convert.ToString(Convert.ToInt32(nUpDown_String4.Value) + offset));
                    break;
                case 5:
                    offset = 64; // E4 (Midi)
                    label_CustomTuningHighEStringLetter.Text = GuitarSpeak.GuitarSpeakNoteOctaveMath(Convert.ToString(Convert.ToInt32(nUpDown_String5.Value) + offset)).ToLower();
                    break;
                default: // Yeah we don't know wtf happened here
                    MessageBox.Show("Invalid String Number! Please report this to the GUI devs!");
                    break;
            }

            // Change string color if the user if it would be "extended range" of that string.
            if (ReadSettings.ProcessSettings(ReadSettings.ExtendedRangeEnabledIdentifier) == "on" && int.Parse(ReadSettings.ProcessSettings(ReadSettings.ExtendedRangeTuningIdentifier)) >= ((NumericUpDown)sender).Value)
                SetForget_SetTunerColors(stringNumber, 1);
            else
                SetForget_SetTunerColors(stringNumber);
        }

        #endregion
        #region Save Setting Middleware

        private void Save_ToggleLoft(object sender, EventArgs e) // Toggle Loft Enabled/ Disabled
        {
            SaveSettings_Save(ReadSettings.ToggleLoftEnabledIdentifier, checkBox_ToggleLoft.Checked.ToString().ToLower());
            checkBox_ToggleLoft.Checked = checkBox_ToggleLoft.Checked;
            radio_LoftAlwaysOff.Visible = checkBox_ToggleLoft.Checked;
            radio_LoftOffHotkey.Visible = checkBox_ToggleLoft.Checked;
            radio_LoftOffInSong.Visible = checkBox_ToggleLoft.Checked;
            groupBox_LoftOffWhen.Visible = checkBox_ToggleLoft.Checked;
        }

        private void Save_SongTimer(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.ShowSongTimerEnabledIdentifier, checkBox_SongTimer.Checked.ToString().ToLower());
            groupBox_SongTimer.Visible = checkBox_SongTimer.Checked;
        }

        private void Save_ForceEnumeration(object sender, EventArgs e)
        {
            checkBox_ForceEnumeration.Checked = checkBox_ForceEnumeration.Checked;
            radio_ForceEnumerationAutomatic.Visible = checkBox_ForceEnumeration.Checked;
            radio_ForceEnumerationManual.Visible = checkBox_ForceEnumeration.Checked;
            groupBox_HowToEnumerate.Visible = checkBox_ForceEnumeration.Checked;

            if (checkBox_ForceEnumeration.Checked)
                SaveSettings_Save(ReadSettings.ForceReEnumerationEnabledIdentifier, "manual");
            else
                SaveSettings_Save(ReadSettings.ForceReEnumerationEnabledIdentifier, "false");
        }

        private void Save_EnumerateEveryXMS(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.CheckForNewSongIntervalIdentifier, (nUpDown_ForceEnumerationXMS.Value * 1000).ToString());

        private void Save_ForceEnumerationAutomatic(object sender, EventArgs e)
        {
            label_ForceEnumerationXMS.Visible = true;
            nUpDown_ForceEnumerationXMS.Visible = true;
            SaveSettings_Save(ReadSettings.ForceReEnumerationEnabledIdentifier, "automatic");
        }

        private void Save_ForceEnumerationManual(object sender, EventArgs e)
        {
            label_ForceEnumerationXMS.Visible = false;
            nUpDown_ForceEnumerationXMS.Visible = false;
            SaveSettings_Save(ReadSettings.ForceReEnumerationEnabledIdentifier, "manual");
        }

        private void Save_RainbowStrings(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.RainbowStringsEnabledIdentifier, checkBox_RainbowStrings.Checked.ToString().ToLower());

        private void Save_RainbowNotes(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.RainbowNotesEnabledIdentifier, checkBox_RainbowNotes.Checked.ToString().ToLower());

        private void Save_ExtendedRange(object sender, EventArgs e)
        {

            checkBox_CustomColors.Checked = checkBox_ExtendedRange.Checked;

            SaveSettings_Save(ReadSettings.ExtendedRangeEnabledIdentifier, checkBox_ExtendedRange.Checked.ToString().ToLower());

            if (checkBox_ExtendedRange.Checked)
                SaveSettings_Save(ReadSettings.CustomStringColorNumberIndetifier, "2");
            else
                SaveSettings_Save(ReadSettings.CustomStringColorNumberIndetifier, "0");
        }

        private void Save_CustomStringColors(object sender, EventArgs e)
        {
            groupBox_StringColors.Visible = checkBox_CustomColors.Checked;

            if (checkBox_CustomColors.Checked)
                SaveSettings_Save(ReadSettings.CustomStringColorNumberIndetifier, "2");
            else
                SaveSettings_Save(ReadSettings.CustomStringColorNumberIndetifier, "0");
        }

        // private void Save_DiscoMode(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.DiscoModeIdentifier, DiscoModeCheckbox.Checked.ToString().ToLower());

        private void Save_RemoveHeadstockCheckbox(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.RemoveHeadstockIdentifier, checkBox_RemoveHeadstock.Checked.ToString().ToLower());
            groupBox_ToggleHeadstockOffWhen.Visible = checkBox_RemoveHeadstock.Checked;
        }

        private void Save_RemoveSkyline(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.RemoveSkylineIdentifier, checkBox_RemoveSkyline.Checked.ToString().ToLower());
            groupBox_ToggleSkylineWhen.Visible = checkBox_RemoveSkyline.Checked;
        }

        private void Save_GreenScreenWall(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.GreenScreenWallIdentifier, checkBox_GreenScreen.Checked.ToString().ToLower());

        private void Save_AutoLoadLastProfile(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.ForceProfileEnabledIdentifier, checkBox_AutoLoadProfile.Checked.ToString().ToLower());
            groupBox_AutoLoadProfiles.Visible = checkBox_AutoLoadProfile.Checked;
        }

        private void Save_Fretless(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.FretlessModeEnabledIdentifier, checkBox_Fretless.Checked.ToString().ToLower());

        private void Save_RemoveInlays(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.RemoveInlaysIdentifier, checkBox_RemoveInlays.Checked.ToString().ToLower());

        private void Save_ToggleLoftWhenManual(object sender, EventArgs e)
        {
            if (radio_LoftOffHotkey.Checked)
                SaveSettings_Save(ReadSettings.ToggleLoftWhenIdentifier, "manual");
        }

        private void Save_ToggleLoftWhenSong(object sender, EventArgs e)
        {
            if (radio_LoftOffInSong.Checked)
                SaveSettings_Save(ReadSettings.ToggleLoftWhenIdentifier, "song");
        }

        private void Save_ToggleLoftWhenStartup(object sender, EventArgs e)
        {
            if (radio_LoftAlwaysOff.Checked)
                SaveSettings_Save(ReadSettings.ToggleLoftWhenIdentifier, "startup");
        }

        private void Save_RemoveLaneMarkers(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.RemoveLaneMarkersIdentifier, checkBox_RemoveLaneMarkers.Checked.ToString().ToLower());
        private void Save_ToggleSkylineSong(object sender, EventArgs e)
        {
            if (radio_SkylineOffInSong.Checked)
                SaveSettings_Save(ReadSettings.ToggleSkylineWhenIdentifier, "song");
        }

        private void Save_ToggleSkylineStartup(object sender, EventArgs e)
        {
            if (radio_SkylineAlwaysOff.Checked)
                SaveSettings_Save(ReadSettings.ToggleSkylineWhenIdentifier, "startup");
        }
        private void Delete_Keybind_MODS(object sender, EventArgs e)
        {
            textBox_NewKeyAssignment_MODS.Text = "";

            foreach (string currentMod in Dictionaries.KeybindingsIndexToINISetting)
            {
                int index = Dictionaries.KeybindingsIndexToINISetting.IndexOf(currentMod);
                if (index == listBox_Modlist_MODS.SelectedIndex)
                {
                    SaveSettings_Save(currentMod, "");
                    break;
                }
            }
            Startup_ShowCurrentKeybindingValues();
        }

        private void Delete_Keybind_AUDIO(object sender, EventArgs e)
        {
            textBox_NewKeyAssignment_AUDIO.Text = "";

            foreach (string currentMod in Dictionaries.AudioKeybindingsIndexToINISetting)
            {
                int index = Dictionaries.AudioKeybindingsIndexToINISetting.IndexOf(currentMod);
                if (index == listBox_Modlist_AUDIO.SelectedIndex)
                {
                    SaveSettings_Save(currentMod, "");
                    break;
                }
            }
            Startup_ShowCurrentAudioKeybindingValues();
        }

        private void Save_RemoveLyrics(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.RemoveLyricsIdentifier, checkBox_RemoveLyrics.Checked.ToString().ToLower());
            radio_LyricsAlwaysOff.Visible = checkBox_RemoveLyrics.Checked;
            radio_LyricsOffHotkey.Visible = checkBox_RemoveLyrics.Checked;
            groupBox_ToggleLyricsOffWhen.Visible = checkBox_RemoveLyrics.Checked;
        }

        private void Fill_Songlist_Name(object sender, EventArgs e)
        {
            if (listBox_Songlist.SelectedIndex >= 0)
                textBox_NewSonglistName.Text = listBox_Songlist.SelectedItem.ToString();
        }

        private void Save_ToggleLyricsStartup(object sender, EventArgs e)
        {
            if (radio_LyricsAlwaysOff.Checked)
                SaveSettings_Save(ReadSettings.RemoveLyricsWhenIdentifier, "startup");
        }

        private void Save_ToggleLyricsManual(object sender, EventArgs e)
        {
            if (radio_LyricsOffHotkey.Checked)
                SaveSettings_Save(ReadSettings.RemoveLyricsWhenIdentifier, "manual");
        }

        private void Save_VolumeControls(object sender, EventArgs e)
        {
            groupBox_Keybindings_AUDIO.Visible = checkBox_ControlVolume.Checked;
            groupBox_ControlVolumeIncrement.Visible = checkBox_ControlVolume.Checked;
            SaveSettings_Save(ReadSettings.VolumeControlEnabledIdentifier, checkBox_ControlVolume.Checked.ToString().ToLower());
        }

        private void Save_RiffRepeaterSpeedInterval(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.RiffRepeaterSpeedIntervalIdentifier, nUpDown_RiffRepeaterSpeed.Value.ToString());

        private void Save_RiffRepeaterSpeedAboveOneHundred(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.RiffRepeaterAboveHundredIdentifier, checkBox_RiffRepeaterSpeedAboveOneHundred.Checked.ToString().ToLower());
            groupBox_RRSpeed.Visible = checkBox_RiffRepeaterSpeedAboveOneHundred.Checked;
        }

        private void Save_UseMidiAutoTuning(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.MidiAutoTuningIdentifier, checkBox_useMidiAutoTuning.Checked.ToString().ToLower());
            groupBox_MidiAutoTuneDevice.Visible = checkBox_useMidiAutoTuning.Checked;
            groupBox_MidiAutoTuningOffset.Visible = checkBox_useMidiAutoTuning.Checked;
            groupBox_MidiAutoTuningWhen.Visible = checkBox_useMidiAutoTuning.Checked;
        }
        private void Save_AutoTuneDevice(object sender, EventArgs e)
        {
            if (listBox_ListMidiOutDevices.SelectedItem != null)
            {
                SaveSettings_Save(ReadSettings.MidiAutoTuningDeviceIdentifier, listBox_ListMidiOutDevices.SelectedItem.ToString());
                label_SelectedMidiOutDevice.Text = "Midi Device: " + listBox_ListMidiOutDevices.SelectedItem.ToString();
            }
        }

        private void Save_MidiInDevice(object sender, EventArgs e)
        {
            if (listBox_ListMidiInDevices.SelectedItem != null)
            {
                SaveSettings_Save(ReadSettings.MidiInDeviceIdentifier, listBox_ListMidiInDevices.SelectedItem.ToString());
                label_SelectedMidiInDevice.Text = "Midi Device: " + listBox_ListMidiInDevices.SelectedItem.ToString();
            }
        }

        private void Save_WhammyDT(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.TuningPedalIdentifier, "1");

        private void Save_WhammyBass(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.TuningPedalIdentifier, "2");
            checkBox_WhammyChordsMode.Visible = radio_WhammyBass.Checked;
        }

        private void Save_Whammy(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.TuningPedalIdentifier, "3");
            checkBox_WhammyChordsMode.Visible = radio_Whammy.Checked;
        }

        private void Save_SoftwarePedal(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.TuningPedalIdentifier, "4");

        private void Save_WhammyChordsMode(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.ChordsModeIdentifier, checkBox_WhammyChordsMode.Checked.ToString().ToLower());

        private void Save_ShowCurrentNote(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.ShowCurrentNoteOnScreenIdentifier, checkBox_ShowCurrentNote.Checked.ToString().ToLower());

        private void Save_ScreenShotScores(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.ScreenShotScoresIdentifier, checkBox_ScreenShotScores.Checked.ToString().ToLower());

        private void Save_HeadStockAlwaysOffButton(object sender, EventArgs e)
        {
            if (radio_HeadstockAlwaysOff.Checked)
                SaveSettings_Save(ReadSettings.RemoveHeadstockWhenIdentifier, "startup");
        }
        private void Save_HeadstockOffInSongOnlyButton(object sender, EventArgs e)
        {
            if (radio_HeadstockOffInSong.Checked)
                SaveSettings_Save(ReadSettings.RemoveHeadstockWhenIdentifier, "song");
        }

        private void Save_VolumeInterval(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.VolumeControlIntervalIdentifier, Convert.ToInt32(nUpDown_VolumeInterval.Value).ToString());

        private void Save_AutoLoadProfile(object sender, EventArgs e)
        {
            if (listBox_AutoLoadProfiles.SelectedIndex == -1)
                SaveSettings_Save(ReadSettings.ProfileToLoadIdentifier, "");
            else
                SaveSettings_Save(ReadSettings.ProfileToLoadIdentifier, listBox_AutoLoadProfiles.SelectedItem.ToString());
        }

        private void AutoLoadProfile_ClearSelection(object sender, EventArgs e) => listBox_AutoLoadProfiles.ClearSelected();
        private void Save_BackupProfile(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.BackupProfileIdentifier, checkBox_BackupProfile.Checked.ToString().ToLower());
            groupBox_Backups.Visible = checkBox_BackupProfile.Checked;


            if (checkBox_BackupProfile.Checked)
            {
                Profiles.SaveProfile();

                if (Profiles.GetSaveDirectory() == String.Empty)
                {
                    MessageBox.Show("It looks like your profile(s) can't be found :(\nWe are disabling the Backup Profile mod so it doesn't look like we're lying to you.");
                    checkBox_BackupProfile.Checked = false;
                }
                else
                {
                    Startup_UnlockProfileEdits();
                }
            }
            else if (AllowSaving)
            {
                Startup_LockProfileEdits();
            }
        }

        private void UnlimitedBackups(object sender, EventArgs e)
        {
            if (checkBox_UnlimitedBackups.Checked)
                nUpDown_NumberOfBackups.Value = 0;
            else
            {
                nUpDown_NumberOfBackups.Value = 50;
                nUpDown_NumberOfBackups.Enabled = true;
            }
        }

        private void Save_NumberOfBackups(object sender, EventArgs e)
        {
            if (nUpDown_NumberOfBackups.Value == 0)
            {
                nUpDown_NumberOfBackups.Enabled = false;
                checkBox_UnlimitedBackups.Checked = true;
            }
            SaveSettings_Save(ReadSettings.NumberOfBackupsIdentifier, nUpDown_NumberOfBackups.Value.ToString());
        }

        private void Save_CustomHighway(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.CustomHighwayColorsIdentifier, checkBox_CustomHighway.Checked.ToString().ToLower());
            groupBox_CustomHighway.Visible = checkBox_CustomHighway.Checked;
        }

        private void ResetNotewayColors(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.CustomHighwayNumberedIdentifier, "");
            SaveSettings_Save(ReadSettings.CustomHighwayUnNumberedIdentifier, "");
            SaveSettings_Save(ReadSettings.CustomHighwayGutterIdentifier, "");
            SaveSettings_Save(ReadSettings.CustomFretNubmersIdentifier, "");

            textBox_ShowNumberedFrets.BackColor = SystemColors.Control;
            textBox_ShowUnNumberedFrets.BackColor = SystemColors.Control;
            textBox_ShowNotewayGutter.BackColor = SystemColors.Control;
            textBox_ShowFretNumber.BackColor = SystemColors.Control;
        }

        private void Save_SongTimerAlways(object sender, EventArgs e)
        {
            if (radio_SongTimerAlways.Checked)
                SaveSettings_Save(ReadSettings.ShowSongTimerWhenIdentifier, "automatic");
        }

        private void Save_SongTimerManual(object sender, EventArgs e)
        {
            if (radio_SongTimerManual.Checked)
                SaveSettings_Save(ReadSettings.ShowSongTimerWhenIdentifier, "manual");
        }

        private void Save_SecondaryMonitorStartPosition(object sender, EventArgs e)
        {
            Process guiProcess = Process.GetProcessesByName("RSMods")[0];
            IntPtr ptr = guiProcess.MainWindowHandle;
            Rect guiLocation = new Rect();
            GetWindowRect(ptr, ref guiLocation);

            SaveSettings_Save(ReadSettings.SecondaryMonitorXPositionIdentifier, (guiLocation.Left + 8).ToString());
            SaveSettings_Save(ReadSettings.SecondaryMonitorYPositionIdentifier, (guiLocation.Top + 8).ToString());
        }

        private void Save_SecondaryMonitor(object sender, EventArgs e)
        {
            button_SecondaryMonitorStartPos.Visible = checkBox_SecondaryMonitor.Checked;
            SaveSettings_Save(ReadSettings.SecondaryMonitorIdentifier, checkBox_SecondaryMonitor.Checked.ToString().ToLower());
        }

        private void Save_ER_SeparateNoteColors(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.SeparateNoteColorsIdentifier, checkBox_ER_SeparateNoteColors.Checked.ToString().ToLower());
            groupBox_NoteColors.Visible = checkBox_ER_SeparateNoteColors.Checked;

            if (!checkBox_ER_SeparateNoteColors.Checked)
                SaveSettings_Save(ReadSettings.SeparateNoteColorsModeIdentifier, (0).ToString());
            else
                SaveSettings_Save(ReadSettings.SeparateNoteColorsModeIdentifier, (2).ToString());
        }

        private void Save_NoteColors_UseRocksmithColors(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.SeparateNoteColorsModeIdentifier, (2 - Convert.ToInt32(checkBox_NoteColors_UseRocksmithColors.Checked)).ToString());

            button_Note0ColorButton.Enabled = !checkBox_NoteColors_UseRocksmithColors.Checked;
            button_Note1ColorButton.Enabled = !checkBox_NoteColors_UseRocksmithColors.Checked;
            button_Note2ColorButton.Enabled = !checkBox_NoteColors_UseRocksmithColors.Checked;
            button_Note3ColorButton.Enabled = !checkBox_NoteColors_UseRocksmithColors.Checked;
            button_Note4ColorButton.Enabled = !checkBox_NoteColors_UseRocksmithColors.Checked;
            button_Note5ColorButton.Enabled = !checkBox_NoteColors_UseRocksmithColors.Checked;

            if (checkBox_NoteColors_UseRocksmithColors.Checked)
            {
                textBox_Note0Color.BackColor = DefaultBackColor;
                textBox_Note1Color.BackColor = DefaultBackColor;
                textBox_Note2Color.BackColor = DefaultBackColor;
                textBox_Note3Color.BackColor = DefaultBackColor;
                textBox_Note4Color.BackColor = DefaultBackColor;
                textBox_Note5Color.BackColor = DefaultBackColor;
            }
            else
                StringColors_LoadDefaultNoteColors();

            radio_DefaultNoteColors.Enabled = !checkBox_NoteColors_UseRocksmithColors.Checked;
            radio_colorBlindERNoteColors.Enabled = !checkBox_NoteColors_UseRocksmithColors.Checked;
        }

        private void Save_DumpRSModsLogToFile(object sender, EventArgs e)
        {
            if (!AllowSaving)
                return;

            if (checkBox_ModsLog.Checked)
                File.Create(Path.Combine(GenUtil.GetRSDirectory(), "RSMods_debug.txt"));
            else
            {
                // First we have to do garbage cleanup since it keeps the file in use way too long.
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // We can finally delete the file :)
                File.Delete(Path.Combine(GenUtil.GetRSDirectory(), "RSMods_debug.txt"));
            }
        }

        private void Save_MidiAutoTuningOffset(object sender, EventArgs e)
        {
            if (listBox_MidiAutoTuningOffset.SelectedIndex > -1)
                SaveSettings_Save(ReadSettings.MidiTuningOffsetIdentifier, (listBox_MidiAutoTuningOffset.SelectedIndex - 3).ToString());
        }

        private void Save_AutoTuningWhenManual(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.MidiAutoTuningWhenIdentifier, "manual");

        private void Save_AutoTuningWhenTuner(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.MidiAutoTuningWhenIdentifier, "tuner");

        private void Save_TurnOffAllMods(object sender, EventArgs e)
        {

            if (File.Exists(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll")) && !File.Exists(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll.off"))) // Has DLL enabled and doesn't have DLL turned off
                File.Move(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll"), Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll.off"));
            else if (File.Exists(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll.off")) && !File.Exists(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll"))) // Has DLL turned off and doesn't have DLL enabled
                File.Move(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll.off"), Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll"));
            else if (File.Exists(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll")) && File.Exists(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll.off"))) // Has DLL enabled AND turned off.
            {
                File.Delete(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll.off"));

                File.Move(Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll"), Path.Combine(GenUtil.GetRSDirectory(), "xinput1_3.dll.off"));
            }
        }

        private void Save_RemoveSongPreviews(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.RemoveSongPreviewsIdentifier, checkBox_RemoveSongPreviews.Checked.ToString().ToLower());

        private void Save_OverrideInputVolumeEnabled(object sender, EventArgs e)
        {
            groupBox_OverrideInputVolume.Visible = checkBox_OverrideInputVolume.Checked;
            SaveSettings_Save(ReadSettings.OverrideInputVolumeEnabledIdentifier, checkBox_OverrideInputVolume.Checked.ToString().ToLower());
        }

        private void Save_OverrideInputVolume(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.OverrideInputVolumeIdentifier, nUpDown_OverrideInputVolume.Value.ToString());

        private void Save_OverrideInputVolumeDevice(object sender, EventArgs e)
        {
            if (listBox_AvailableInputDevices.SelectedItem != null)
            {
                SaveSettings_Save(ReadSettings.OverrideInputVolumeDeviceIdentifier, listBox_AvailableInputDevices.SelectedItem.ToString());
            }
        }

        private void Save_AllowAudioInBackground(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.AllowAudioInBackgroundIdentifier, checkBox_AllowAudioInBackground.Checked.ToString().ToLower());

        private void Save_BypassTwoRTCMessageBox(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.BypassTwoRTCMessageBoxIdentifier, checkBox_BypassTwoRTCMessageBox.Checked.ToString().ToLower());
        private void Save_LinearRiffRepeater(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.LinearRiffRepeaterIdentifier, checkBox_LinearRiffRepeater.Checked.ToString().ToLower());

        private void Save_UseAlternativeSampleRate_Output(object sender, EventArgs e)
        {
            groupBox_SampleRateOutput.Visible = checkBox_UseAltSampleRate_Output.Checked;
            SaveSettings_Save(ReadSettings.UseAlternativeOutputSampleRateIdentifier, checkBox_UseAltSampleRate_Output.Checked.ToString().ToLower());
        }

        private void Save_AltSampleRatesOutput(object sender, EventArgs e)
        {
            if (listBox_AltSampleRatesOutput.SelectedItem != null)
            {
                SaveSettings_Save(ReadSettings.AlternativeOutputSampleRateIdentifier, listBox_AltSampleRatesOutput.SelectedItem.ToString().Split(' ')[0]);
            }
        }

        private void Save_EnableLooping(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.AllowLoopingIdentifier, checkBox_EnableLooping.Checked.ToString().ToLower());
            groupBox_LoopingLeadUp.Visible = checkBox_EnableLooping.Checked;
        }

        private void Save_LoopingLeadUp(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.LoopingLeadUpIdentifier, ((int)(nUpDown_LoopingLeadUp.Value * 1000)).ToString());

        private void Save_AllowRewind(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.AllowRewindIdentifier, checkBox_AllowRewind.Checked.ToString().ToLower());
            groupBox_RewindBy.Visible = checkBox_AllowRewind.Checked;
        }

        private void Save_RewindBy(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.RewindByIdentifier, ((int)(nUpDown_RewindBy.Value * 1000)).ToString());

        private void Save_FixOculusCrash(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.FixOculusCrashIdentifier, checkBox_FixOculusCrash.Checked.ToString().ToLower());

        private void Save_FixBrokenTones(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.FixBrokenTonesIdentifier, checkBox_FixBrokenTones.Checked.ToString().ToLower());

        private void Save_UseCustomNSPTimer(object sender, EventArgs e)
        {
            SaveSettings_Save(ReadSettings.UseCustomNSPTimerIdentifier, checkBox_CustomNSPTimer.Checked.ToString().ToLower());
            groupBox_NSPTimer.Visible = checkBox_CustomNSPTimer.Checked;
        }

        private void Save_NSPTimer(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.CustomNSPTimeLimitIdentifier, ((int)(nUpDown_NSPTimer.Value * 1000)).ToString());

        private void Save_SetSavePath(object sender, EventArgs e)
        {
            string saveFolder = GenUtil.GetSaveFolder(true);

            if (saveFolder == string.Empty)
            {
                MessageBox.Show("We did not save a Save Folder");
                return;
            }

            // Backup profiles & prevent the user from pressing this button again
            checkBox_BackupProfile.Checked = true;
            button_SetSavePath.Visible = false;

            // Save some constants
            Constants.SavePath = saveFolder;
            Constants.BypassSavePrompt = "false";
            List<string> settings = new List<string>() { $"RSPath = {Constants.RSFolder}", $"SavePath = {Constants.SavePath}", $"BypassSavePrompt = {Constants.BypassSavePrompt}" };
            File.WriteAllLines(Constants.SettingsPath, settings);

            // Refresh the Profile Edits UI, now that we have the information we need.
            Startup_LoadRocksmithProfiles();
        }

        #endregion
        #region ToolTips

        bool CreatedToolTipYet = false;

        private void ToolTips_Hide(object sender, EventArgs e)
        {
            if (ActiveForm != null) // This fixes a glitch where if you are hovering over a Control that calls the tooltip, and alt-tab, the program will crash since ActiveFrame turns to null... If the user is highlighting something, and the window becomes null, we need to refrain from trying to hide the tooltip that "does not exist".
            {
                ToolTip.Hide(ActiveForm);
                ToolTip.Active = false;
            };
        }

        public ToolTip currentTooltip = new ToolTip(); // Fixes toolTip duplication glitch.

        private void ToolTips_Show(object sender, EventArgs e)
        {
            if (CreatedToolTipYet) // Do we already have a filled tooltip? If so, clear it.
            {
                currentTooltip.Dispose();
                currentTooltip = new ToolTip();
            }

            currentTooltip.Active = true;

            TooltipDictionary.Clear();
            FillToolTipDictionary();

            foreach (Control ControlHoveredOver in TooltipDictionary.Keys)
            {
                if (ControlHoveredOver == sender)
                {
                    TooltipDictionary.TryGetValue(ControlHoveredOver, out string toolTipString);
                    currentTooltip.Show(toolTipString, ControlHoveredOver, 5000000); // Don't change the duration number, even if it's higher. It works as it is, and changing it to even Int32.MaxValue causes it to go back to the 5-second max.
                    CreatedToolTipYet = true;
                    break; // We found what we needed, now GTFO of here.
                }
            }
        }

        #endregion
        #region Guitar Speak

        private void GuitarSpeak_Enable(object sender, EventArgs e)
        {
            checkBox_GuitarSpeak.Checked = checkBox_GuitarSpeak.Checked;
            groupBox_GuitarSpeak.Visible = checkBox_GuitarSpeak.Checked;
            checkBox_GuitarSpeakWhileTuning.Visible = checkBox_GuitarSpeak.Checked;
            SaveSettings_Save(ReadSettings.GuitarSpeakIdentifier, checkBox_GuitarSpeak.Checked.ToString().ToLower());
        }

        private void GuitarSpeak_Save(object sender, EventArgs e)
        {
            if (listBox_GuitarSpeakNote.SelectedIndex >= 0 && listBox_GuitarSpeakOctave.SelectedIndex >= 0 && listBox_GuitarSpeakKeypress.SelectedIndex >= 0)
            {
                int inputNote = listBox_GuitarSpeakNote.SelectedIndex + 36; // We skip the first 3 octaves to give an accurate representation of the notes being played
                int inputOctave = listBox_GuitarSpeakOctave.SelectedIndex - 3; // -1 for the offset, and -2 for octave offset in DLL.
                int outputNoteOctave = inputNote + (inputOctave * 12);
                MessageBox.Show(listBox_GuitarSpeakNote.SelectedItem.ToString() + listBox_GuitarSpeakOctave.SelectedItem.ToString() + " was saved to " + listBox_GuitarSpeakKeypress.SelectedItem.ToString(), "Note Saved!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                foreach (KeyValuePair<string, string> entry in Dictionaries.GuitarSpeakKeyPressDictionary)
                {
                    if (listBox_GuitarSpeakKeypress.SelectedItem.ToString() == entry.Key)
                    {
                        SaveSettings_Save(entry.Value, outputNoteOctave.ToString());
                        listBox_GuitarSpeakSaved.ClearSelected();

                        foreach (string guitarSpeakItem in listBox_GuitarSpeakSaved.Items)
                        {
                            if (guitarSpeakItem.Contains(listBox_GuitarSpeakKeypress.SelectedItem.ToString()))
                            {
                                listBox_GuitarSpeakSaved.Items.Remove(guitarSpeakItem);
                                break;
                            }
                        }
                        listBox_GuitarSpeakSaved.Items.Add(listBox_GuitarSpeakKeypress.SelectedItem.ToString() + ": " + GuitarSpeak.GuitarSpeakNoteOctaveMath(outputNoteOctave.ToString()));
                        GuitarSpeak_ResetPresets();
                    }
                }

                listBox_GuitarSpeakNote.ClearSelected();
                listBox_GuitarSpeakOctave.ClearSelected();
                listBox_GuitarSpeakKeypress.ClearSelected();
            }
            else
                MessageBox.Show("One, or more, of the Guitar Speak boxes not selected", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void GuitarSpeak_WhileTuning(object sender, EventArgs e) => SaveSettings_Save(ReadSettings.GuitarSpeakTuningIdentifier, checkBox_GuitarSpeakWhileTuning.Checked.ToString().ToLower());

        private void GuitarSpeak_Help(object sender, EventArgs e) => System.Diagnostics.Process.Start("https://pastebin.com/raw/PZ0FQTn0");


        private void GuitarSpeak_ResetPresets()
        {
            listBox_GuitarSpeakSaved.Items.Clear();

            foreach (KeyValuePair<string, string> guitarSpeakKeypress in Dictionaries.RefreshGuitarSpeakPresets())
                listBox_GuitarSpeakSaved.Items.Add(guitarSpeakKeypress.Key + guitarSpeakKeypress.Value);
        }

        private void GuitarSpeak_ClearSavedValue(object sender, EventArgs e)
        {
            int valueToRemove = listBox_GuitarSpeakSaved.SelectedIndex;

            if (valueToRemove == -1)
                return;

            listBox_GuitarSpeakSaved.SelectedIndex = -1;

            SaveSettings_Save(Dictionaries.GuitarSpeakIndexToINISetting[valueToRemove], "");

            GuitarSpeak_ResetPresets();
        }

        #endregion
        #region Prep Twitch
        private void Twitch_Show()
        {
            foreach (Control ctrl in tab_Twitch.Controls)
                ctrl.Visible = true;

            foreach (DataGridViewRow row in dgv_EnabledRewards.Rows)
            {
                if (row.Cells[1].Value.ToString() == "Solid color notes")
                {
                    var selectedReward = Twitch_GetSelectedReward(row);

                    if (selectedReward.AdditionalMsg != null && selectedReward.AdditionalMsg != string.Empty && selectedReward.AdditionalMsg != "Random")
                        row.DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#" + selectedReward.AdditionalMsg);

                    Twitch_CheckForTurboSpeed(selectedReward);
                }
            }

            Twitch_SolidNoteColor_Show(false);
        }

        private void Twitch_Setup()
        {
            label_TwitchUsernameVal.DataBindings.Add(new Binding("Text", TwitchSettings.Get, "Username", false, DataSourceUpdateMode.OnPropertyChanged));
            label_TwitchChannelIDVal.DataBindings.Add(new Binding("Text", TwitchSettings.Get, "ChannelID", false, DataSourceUpdateMode.OnPropertyChanged));
            label_TwitchAccessTokenVal.DataBindings.Add(new Binding("Text", TwitchSettings.Get, "AccessToken", false, DataSourceUpdateMode.OnPropertyChanged));

            // Hide values by default (Security just incase the streamer is live with RSMods on screen)
            label_TwitchUsernameVal.DataBindings.Add(new Binding("Visible", checkBox_RevealTwitchAuthToken, "Checked", false, DataSourceUpdateMode.OnPropertyChanged));
            label_TwitchChannelIDVal.DataBindings.Add(new Binding("Visible", checkBox_RevealTwitchAuthToken, "Checked", false, DataSourceUpdateMode.OnPropertyChanged));
            label_TwitchAccessTokenVal.DataBindings.Add(new Binding("Visible", checkBox_RevealTwitchAuthToken, "Checked", false, DataSourceUpdateMode.OnPropertyChanged));

            textBox_TwitchLog.DataBindings.Add(new Binding("Text", TwitchSettings.Get, "Log"));

            Binding listeningToTwitchBinding = new Binding("Text", TwitchSettings.Get, "Authorized");
            listeningToTwitchBinding.Format += (s, e) =>
            {
                if ((bool)e.Value && TwitchSettings.Get.Reauthorized) // If we are authorized
                {
                    PubSub.Get.SetUp(); // Well... this is probably not the best place since it's called a lot, but wing it
                    TwitchSettings.Get.Reauthorized = false;
                    timerValidateTwitch.Enabled = true;
                    Twitch_Show();
                }

                e.Value = (bool)e.Value ? "Listening to Twitch events" : "Not listening to twitch events";
            };
            label_IsListeningToEvents.DataBindings.Add(listeningToTwitchBinding);

            checkBox_TwitchForceReauth.Checked = TwitchSettings.Get.ForceReauth;

            foreach (var defaultReward in TwitchSettings.Get.DefaultRewards) // BindingList... yeah, not yet
                dgv_DefaultRewards.Rows.Add(defaultReward.Name, defaultReward.Description);

            foreach (var enabledReward in TwitchSettings.Get.Rewards)
                Twitch_AddRewardToEnabled(enabledReward);

        }

        private void PrepTwitch_LoadSettings()
        {
            TwitchSettings.Get._context = SynchronizationContext.Current;
            TwitchSettings.Get.LoadSettings();
            TwitchSettings.Get.LoadDefaultEffects();
            TwitchSettings.Get.LoadEnabledEffects();
        }
        #endregion
        #region Twitch
        private void Twitch_ReAuthorize(object sender, EventArgs e)
        {
            ImplicitAuth auth = new ImplicitAuth();

            string authRes = auth.MakeAuthRequest();

            if (!authRes.Equals("OK"))
            {
                MessageBox.Show($"Please open the following link in your browser: {authRes}", "Can't open your browser!");
            }

            // string authToken = TwitchSettings.Get.AccessToken;
            // while (TwitchSettings.Get.AccessToken == authToken || TwitchSettings.Get.Username == String.Empty) {} // We want to get the new value so we are waiting until this breaks
            // label_AuthorizedAs.Text = $"{TwitchSettings.Get.Username} with channel ID: {TwitchSettings.Get.ChannelID} and access token: {TwitchSettings.Get.AccessToken}";
        }

        private void Twitch_NewAccessToken(object sender, EventArgs e) => checkBox_RevealTwitchAuthToken.Checked = false;

        private void Twitch_AutoScrollLog(object sender, EventArgs e)
        {
            textBox_TwitchLog.SelectionStart = textBox_TwitchLog.TextLength;
            textBox_TwitchLog.ScrollToCaret();
        }

        private void Twitch_CheckForTurboSpeed(TwitchReward selectedReward)
        {
            if (selectedReward.Name.Contains("TurboSpeed"))
            {
                if (selectedReward.Enabled)
                    WinMsgUtil.SendMsgToRS("enable TurboSpeed");
                else
                    WinMsgUtil.SendMsgToRS("disable TurboSpeed");
            }
        }

        private async void Twitch_SaveRewards()
        {
            await Task.Run(() =>
            {
                XmlSerializer xs = new XmlSerializer(TwitchSettings.Get.Rewards.GetType());
                using (var sww = new StringWriter())
                {
                    using (XmlWriter writer = XmlWriter.Create(sww, new XmlWriterSettings { Indent = true }))
                    {
                        xs.Serialize(writer, TwitchSettings.Get.Rewards);
                        File.WriteAllText("TwitchEnabledEffects.xml", sww.ToString());
                    }
                }
            });
        }

        private void Twitch_AddReward(object sender, EventArgs e)
        {
            if (dgv_DefaultRewards.SelectedRows.Count < 1)
                return;

            var selectedRow = dgv_DefaultRewards.SelectedRows[0];
            var selectedReward = TwitchSettings.Get.DefaultRewards.FirstOrDefault(r => r.Name == selectedRow.Cells["colDefaultRewardsName"].Value.ToString());
            int rewardID = -1;

            if (selectedReward == null)
                return;

            if (dgv_EnabledRewards.Rows.Count == 0)
                rewardID = 1;
            else
                rewardID = Convert.ToInt32(dgv_EnabledRewards.Rows[dgv_EnabledRewards.Rows.Count - 1].Cells["colEnabledRewardsID"].Value) + 1;

            MessageBoxManager.Yes = "Subs";
            MessageBoxManager.No = "Bits";
            MessageBoxManager.Cancel = "Points";
            MessageBoxManager.Register();

            var dialogResult = MessageBox.Show("Do you wish to add selected reward for subs, bits, channel points?" + Environment.NewLine + "NOTE: changing the amount of subs won't have an effect, as sub \"bombs\" are sent separately!", "Subs or Bits or Channel points?", MessageBoxButtons.YesNoCancel);
            if (dialogResult == DialogResult.Yes)
            {
                var reward = new SubReward();
                reward.Map(selectedReward);
                reward.SubID = rewardID;

                TwitchSettings.Get.Rewards.Add(reward);
                Twitch_AddRewardToEnabled(reward);
            }
            else if (dialogResult == DialogResult.No)
            {
                var reward = new BitsReward();
                reward.Map(selectedReward);
                reward.BitsID = rewardID;

                TwitchSettings.Get.Rewards.Add(reward);
                Twitch_AddRewardToEnabled(reward);
            }
            else
            {
                var reward = new ChannelPointsReward();
                reward.Map(selectedReward);
                reward.PointsID = rewardID;

                TwitchSettings.Get.Rewards.Add(reward);
                Twitch_AddRewardToEnabled(reward);
            }

            MessageBoxManager.Unregister(); // Just making sure our custom msg buttons don't stay enabled
            Twitch_SaveRewards();
        }

        private void Twitch_AddRewardToEnabled(TwitchReward reward) // Just imagine this was a bound list :P
        {
            if (reward is BitsReward)
                dgv_EnabledRewards.Rows.Add(reward.Enabled, reward.Name, reward.Length, ((BitsReward)reward).BitsAmount, "Bits", ((BitsReward)reward).BitsID);
            else if (reward is ChannelPointsReward)
                dgv_EnabledRewards.Rows.Add(reward.Enabled, reward.Name, reward.Length, ((ChannelPointsReward)reward).PointsAmount, "Points", ((ChannelPointsReward)reward).PointsID);
            else if (reward is SubReward)
                dgv_EnabledRewards.Rows.Add(reward.Enabled, reward.Name, reward.Length, 1, "Sub", ((SubReward)reward).SubID);
        }

        private TwitchReward Twitch_GetSelectedReward(DataGridViewRow selectedRow)
        {
            TwitchReward selectedReward;

            if (selectedRow.Cells["colEnabledRewardsType"].Value.ToString() == "Bits")
                selectedReward = TwitchSettings.Get.Rewards.FirstOrDefault(r => r is BitsReward && ((BitsReward)r).BitsID.ToString() == selectedRow.Cells["colEnabledRewardsID"].Value.ToString());
            else if (selectedRow.Cells["colEnabledRewardsType"].Value.ToString() == "Sub")
                selectedReward = TwitchSettings.Get.Rewards.FirstOrDefault(r => r is SubReward && ((SubReward)r).SubID.ToString() == selectedRow.Cells["colEnabledRewardsID"].Value.ToString());
            else
                selectedReward = TwitchSettings.Get.Rewards.FirstOrDefault(r => r is ChannelPointsReward && ((ChannelPointsReward)r).PointsID.ToString() == selectedRow.Cells["colEnabledRewardsID"].Value.ToString());

            return selectedReward;
        }

        private void Twitch_EnabledRewards_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (dgv_EnabledRewards.IsCurrentCellDirty && dgv_EnabledRewards.CurrentCell.ColumnIndex == 0 && dgv_EnabledRewards.CurrentCell.RowIndex != -1)
            {
                dgv_EnabledRewards.CommitEdit(DataGridViewDataErrorContexts.Commit);
                dgv_EnabledRewards.EndEdit();
            }
        }

        private void Twitch_EnabledRewards_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var selectedRow = dgv_EnabledRewards.SelectedRows[0];
            var selectedReward = Twitch_GetSelectedReward(selectedRow);

            if (selectedReward == null)
                return;

            selectedReward.Enabled = Convert.ToBoolean(selectedRow.Cells["colEnabledRewardsEnabled"].Value);
            if (selectedRow.Cells["colEnabledRewardsLength"].Value == null || !(Int32.TryParse(selectedRow.Cells["colEnabledRewardsLength"].Value.ToString(), out int rewardLength)))
            {
                selectedRow.Cells["colEnabledRewardsLength"].Value = 0;
                MessageBox.Show("You need to put a number, not a text value.");
                return;
            }

            if (selectedRow.Cells["colEnabledRewardsAmount"].Value == null || !(Int32.TryParse(selectedRow.Cells["colEnabledRewardsAmount"].Value.ToString(), out int rewardAmount)))
            {
                selectedRow.Cells["colEnabledRewardsAmount"].Value = 0;
                MessageBox.Show("You need to put a number, not a text value.");
                return;
            }

            selectedReward.Length = rewardLength;

            if (selectedReward is BitsReward)
                ((BitsReward)selectedReward).BitsAmount = Convert.ToInt32(selectedRow.Cells["colEnabledRewardsAmount"].Value);
            else if (selectedReward is ChannelPointsReward)
                ((ChannelPointsReward)selectedReward).PointsAmount = Convert.ToInt32(selectedRow.Cells["colEnabledRewardsAmount"].Value);

            Twitch_CheckForTurboSpeed(selectedReward);

            Twitch_SaveRewards();
        }

        private void Twitch_SelectEnabledReward(object sender, EventArgs e)
        {
            if (dgv_EnabledRewards.SelectedRows.Count < 1)
                return;

            var selectedRow = dgv_EnabledRewards.SelectedRows[0];
            var selectedReward = Twitch_GetSelectedReward(selectedRow);
            Twitch_SolidNoteColor_Show(false);

            if (selectedReward.Name != "Solid color notes")
                return;

            if (selectedReward.AdditionalMsg == null || selectedReward.AdditionalMsg == string.Empty || selectedReward.AdditionalMsg == "Random")
            {
                Twitch_SetAdditionalMessage("Random");
                textBox_SolidNoteColorPicker.BackColor = Color.White;
                textBox_SolidNoteColorPicker.Text = "Random";
                dgv_EnabledRewards.SelectedRows[0].DefaultCellStyle.BackColor = Color.White;
            }
            else
            {
                textBox_SolidNoteColorPicker.BackColor = ColorTranslator.FromHtml("#" + selectedReward.AdditionalMsg);
                textBox_SolidNoteColorPicker.Text = "";
                dgv_EnabledRewards.SelectedRows[0].DefaultCellStyle.BackColor = ColorTranslator.FromHtml("#" + selectedReward.AdditionalMsg);
            }

            Twitch_SolidNoteColor_Show(true);
        }


        private void Twitch_RemoveReward(object sender, EventArgs e)
        {
            if (dgv_EnabledRewards.SelectedRows.Count < 1)
                return;

            var selectedRow = dgv_EnabledRewards.SelectedRows[0];
            var selectedReward = Twitch_GetSelectedReward(selectedRow);

            if (selectedReward.Name == "Solid color notes")
                Twitch_SolidNoteColor_Show(false);

            if (selectedReward != null)
                TwitchSettings.Get.Rewards.Remove(selectedReward);

            dgv_EnabledRewards.Rows.RemoveAt(selectedRow.Index);

            Twitch_SaveRewards();
        }

        private void Twitch_SetAdditionalMessage(string msg)
        {
            var selectedRow = dgv_EnabledRewards.SelectedRows[0];
            var selectedReward = Twitch_GetSelectedReward(selectedRow);

            if (selectedReward.Name != "Solid color notes")
                return;

            selectedReward.AdditionalMsg = msg;
        }

        private void Twitch_SolidNoteColor_Pick(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog
            {
                AllowFullOpen = true,
                ShowHelp = false
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                string colorHex = (colorDialog.Color.ToArgb() & 0x00ffffff).ToString("X6");
                textBox_SolidNoteColorPicker.BackColor = colorDialog.Color;
                textBox_SolidNoteColorPicker.Text = String.Empty;
                dgv_EnabledRewards.SelectedRows[0].DefaultCellStyle.BackColor = colorDialog.Color;

                Twitch_SetAdditionalMessage(colorHex);
                Twitch_SaveRewards();
            }
        }

        private void Twitch_SolidNoteColor_Random(object sender, EventArgs e)
        {
            textBox_SolidNoteColorPicker.BackColor = Color.White;
            textBox_SolidNoteColorPicker.Text = "Random";

            Twitch_SetAdditionalMessage("Random");
            Twitch_SaveRewards();

            dgv_EnabledRewards.SelectedRows[0].DefaultCellStyle.BackColor = Color.White;
        }

        private void Twitch_SendFakeReward()
        {
            if (dgv_EnabledRewards.CurrentCell == null)
                return;

            PubSub.SendMessageToRocksmith(TwitchSettings.Get.Rewards[dgv_EnabledRewards.CurrentCell.RowIndex]);
        }

        private void Twitch_TestReward(object sender, EventArgs e)
        {
            if (Process.GetProcessesByName("Rocksmith2014").Length == 0)
            {
                TwitchSettings.Get.AddToLog("The game does not appear to be running!");
                return;
            }

            Twitch_SendFakeReward();
        }
        private void Twitch_SolidNoteColor_Show(bool show)
        {
            button_SolidNoteColorPicker.Visible = show;
            textBox_SolidNoteColorPicker.Visible = show;
            button_SolidNoteColorRandom.Visible = show;
        }

        private void Twitch_timerValidate(object sender, EventArgs e)
        {
            if (checkBox_TwitchForceReauth.Checked)
            {
                TwitchSettings.Get.AddToLog("Reauthorizing...");
                TwitchSettings.Get.AddToLog("----------------");

                var auth = new ImplicitAuth(); // Force the issue
                auth.MakeAuthRequest(true); // When the request finishes, it will trigger PropertyChanged & set Reauthorized, which in turn will reset PubSub
            }
            else
                PubSub.Get.Resub();
        }

        private static void Twitch_SaveSettings() => TwitchSettings.Get.SaveSettings();

        private void Twitch_ForceReauth(object sender, EventArgs e)
        {
            TwitchSettings.Get.ForceReauth = checkBox_TwitchForceReauth.Checked;
            Twitch_SaveSettings();
        }

        private void Twitch_SaveLog(object sender, EventArgs e)
        {
            try
            {
                File.WriteAllText("twitchLog.txt", TwitchSettings.Get.Log);
                MessageBox.Show("Saved log to RS folder/RSMods/twitchLog.txt!", "Saved!");
            }
            catch (IOException ioex)
            {
                MessageBox.Show($"Unable to save log, error: {ioex.Message}");
            }
        }

        private void Twitch_CopyCredentialsForDevs(object sender, MouseEventArgs e) => Clipboard.SetText("Send to RSMod Developers ( Discord Ffio#2221 or LovroM8#9999 )\nUsername: " + TwitchSettings.Get.Username + "\nChannel ID: " + TwitchSettings.Get.ChannelID + "\nAccess Token: " + TwitchSettings.Get.AccessToken);

        /*private void dgv_EnabledRewards_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
       {
           if (!(dgv_EnabledRewards.Columns[e.ColumnIndex] is DataGridViewComboBoxColumn))
               return;

           dgv_EnabledRewards.BeginEdit(false);
           var ec = dgv_EnabledRewards.EditingControl as DataGridViewComboBoxEditingControl;
           if (ec != null && ec.Width - e.X < SystemInformation.VerticalScrollBarWidth)
               ec.DroppedDown = true;

           SaveEnabledRewardsToFile();
       }*/
        #endregion
        #region Custom Fonts
        private void Fonts_Load() // Not modified from here: https://stackoverflow.com/a/8657854 :eyes:
        {
            InstalledFontCollection fontList = new InstalledFontCollection();
            FontFamily[] fontFamilies = fontList.Families;

            foreach (FontFamily font in fontFamilies)
            {
                listBox_AvailableFonts.Items.Add(font.Name);
            }

            listBox_AvailableFonts.SelectedItem = ReadSettings.ProcessSettings(ReadSettings.OnScreenFontIdentifier);
        }

        private void Fonts_Change(object sender, EventArgs e)
        {
            string fontName = listBox_AvailableFonts.SelectedItem.ToString();
            Font newFontSelected = new Font(fontName, 10.0f, Font.Style, Font.Unit);
            label_FontTestCAPITALS.Font = newFontSelected;
            label_FontTestlowercase.Font = newFontSelected;
            label_FontTestNumbers.Font = newFontSelected;

            SaveSettings_Save(ReadSettings.OnScreenFontIdentifier, fontName);
        }
        #endregion
        #region RS_ASIO

        // Config
        private void ASIO_WASAPI_Output(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableWasapiOutputsIdentifier, ASIO.ReadSettings.Sections.Config, Convert.ToInt32(checkBox_ASIO_WASAPI_Output.Checked).ToString());
        private void ASIO_WASAPI_Input(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableWasapiInputsIdentifier, ASIO.ReadSettings.Sections.Config, Convert.ToInt32(checkBox_ASIO_WASAPI_Input.Checked).ToString());
        private void ASIO_ASIO(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableAsioIdentifier, ASIO.ReadSettings.Sections.Config, Convert.ToInt32(checkBox_ASIO_ASIO.Checked).ToString());

        // Driver
        private void ASIO_ListAvailableInput0(object sender, EventArgs e)
        {
            if (listBox_AvailableASIODevices_Input0.SelectedItem != null)
            {
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Input0, listBox_AvailableASIODevices_Input0.SelectedItem.ToString());
            }
        }

        private void ASIO_ListAvailableInput1(object sender, EventArgs e)
        {
            if (listBox_AvailableASIODevices_Input1.SelectedItem != null)
            {
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Input1, listBox_AvailableASIODevices_Input1.SelectedItem.ToString());
            }
        }
        private void ASIO_ListAvailableOutput(object sender, EventArgs e)
        {
            if (listBox_AvailableASIODevices_Output.SelectedItem != null)
            {
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Output, listBox_AvailableASIODevices_Output.SelectedItem.ToString());
            }
        }
        private void ASIO_ListAvailableInputMic(object sender, EventArgs e)
        {
            if (listBox_AvailableASIODevices_InputMic.SelectedItem != null)
            {
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.InputMic, listBox_AvailableASIODevices_InputMic.SelectedItem.ToString());
            }
        }

        // Disable / Comment Out Driver
        private void ASIO_Output_Disable(object sender, EventArgs e)
        {
            if (listBox_AvailableASIODevices_Output.SelectedItem != null)
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Output, listBox_AvailableASIODevices_Output.SelectedItem.ToString());
            else
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Output, "");
        }
        private void ASIO_Input0_Disable(object sender, EventArgs e)
        {
            if (listBox_AvailableASIODevices_Input0.SelectedItem != null)
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Input0, listBox_AvailableASIODevices_Input0.SelectedItem.ToString());
            else
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Input0, "");
        }
        private void ASIO_Input1_Disable(object sender, EventArgs e)
        {
            if (listBox_AvailableASIODevices_Input1.SelectedItem != null)
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Input1, listBox_AvailableASIODevices_Input1.SelectedItem.ToString());
            else
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.Input1, "");
        }
        private void ASIO_InputMic_Disable(object sender, EventArgs e)
        {
            if (listBox_AvailableASIODevices_InputMic.SelectedItem != null)
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.InputMic, listBox_AvailableASIODevices_InputMic.SelectedItem.ToString());
            else
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, ASIO.ReadSettings.Sections.InputMic, "");
        }

        // Buffer Size
        private void ASIO_BufferSize_Driver(object sender, EventArgs e)
        {
            if (radio_ASIO_BufferSize_Driver.Checked)
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.BufferSizeModeIdentifier, ASIO.ReadSettings.Sections.Asio, "driver");
        }
        private void ASIO_BufferSize_Host(object sender, EventArgs e)
        {
            if (radio_ASIO_BufferSize_Host.Checked)
                SaveSettings_ASIO_Middleware(ASIO.ReadSettings.BufferSizeModeIdentifier, ASIO.ReadSettings.Sections.Asio, "host");
        }
        private void ASIO_BufferSize_Custom(object sender, EventArgs e)
        {
            label_ASIO_CustomBufferSize.Visible = radio_ASIO_BufferSize_Custom.Checked;
            nUpDown_ASIO_CustomBufferSize.Visible = radio_ASIO_BufferSize_Custom.Checked;
            SaveSettings_ASIO_Middleware(ASIO.ReadSettings.BufferSizeModeIdentifier, ASIO.ReadSettings.Sections.Asio, "custom");
        }
        private void ASIO_CustomBufferSize(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.CustomBufferSizeIdentifier, ASIO.ReadSettings.Sections.Asio, nUpDown_ASIO_CustomBufferSize.Value.ToString());

        // Input0 Settings
        private void ASIO_Input0_Channel(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.ChannelIdentifier, ASIO.ReadSettings.Sections.Input0, nUpDown_ASIO_Input0_Channel.Value.ToString());
        private void ASIO_Input0_MaxVolume(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.SoftwareMasterVolumePercentIdentifier, ASIO.ReadSettings.Sections.Input0, nUpDown_ASIO_Input0_MaxVolume.Value.ToString());
        private void ASIO_Input0_MasterVolume(object sender, EventArgs e)
        {
            SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableSoftwareMasterVolumeControlIdentifier, ASIO.ReadSettings.Sections.Input0, Convert.ToInt32(checkBox_ASIO_Input0_ControlMasterVolume.Checked).ToString());
            label_ASIO_Input0_MaxVolume.Visible = checkBox_ASIO_Input0_ControlMasterVolume.Checked;
            nUpDown_ASIO_Input0_MaxVolume.Visible = checkBox_ASIO_Input0_ControlMasterVolume.Checked;
        }
        private void ASIO_Input0_EndpointVolume(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableSoftwareEndpointVolumeControlIdentifier, ASIO.ReadSettings.Sections.Input0, Convert.ToInt32(checkBox_ASIO_Input0_ControlEndpointVolume.Checked).ToString());
        private void ASIO_Input0_EnableRefHack(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableRefCountHackIdentifier, ASIO.ReadSettings.Sections.Input0, Convert.ToInt32(checkBox_ASIO_Input0_EnableRefHack.Checked).ToString());

        // Input1 Settings
        private void ASIO_Input1_Channel(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.ChannelIdentifier, ASIO.ReadSettings.Sections.Input1, nUpDown_ASIO_Input1_Channel.Value.ToString());
        private void ASIO_Input1_MaxVolume(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.SoftwareMasterVolumePercentIdentifier, ASIO.ReadSettings.Sections.Input1, nUpDown_ASIO_Input1_MaxVolume.Value.ToString());
        private void ASIO_Input1_MasterVolume(object sender, EventArgs e)
        {
            SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableSoftwareMasterVolumeControlIdentifier, ASIO.ReadSettings.Sections.Input1, Convert.ToInt32(checkBox_ASIO_Input1_ControlMasterVolume.Checked).ToString());
            label_ASIO_Input1_MaxVolume.Visible = checkBox_ASIO_Input1_ControlMasterVolume.Checked;
            nUpDown_ASIO_Input1_MaxVolume.Visible = checkBox_ASIO_Input1_ControlMasterVolume.Checked;
        }
        private void ASIO_Input1_EndpointVolume(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableSoftwareEndpointVolumeControlIdentifier, ASIO.ReadSettings.Sections.Input1, Convert.ToInt32(checkBox_ASIO_Input1_ControlEndpointVolume.Checked).ToString());
        private void ASIO_Input1_EnableRefHack(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableRefCountHackIdentifier, ASIO.ReadSettings.Sections.Input1, Convert.ToInt32(checkBox_ASIO_Input1_EnableRefHack.Checked).ToString());

        // Output Settings
        private void ASIO_Output_BaseChannel(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.BaseChannelIdentifier, ASIO.ReadSettings.Sections.Output, nUpDown_ASIO_Output_BaseChannel.Value.ToString());
        private void ASIO_Output_AltBaseChannel(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.AltBaseChannelIdentifier, ASIO.ReadSettings.Sections.Output, nUpDown_ASIO_Output_AltBaseChannel.Value.ToString());
        private void ASIO_Output_MaxVolume(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.SoftwareMasterVolumePercentIdentifier, ASIO.ReadSettings.Sections.Output, nUpDown_ASIO_Output_MaxVolume.Value.ToString());
        private void ASIO_Output_MasterVolume(object sender, EventArgs e)
        {
            SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableSoftwareMasterVolumeControlIdentifier, ASIO.ReadSettings.Sections.Output, Convert.ToInt32(checkBox_ASIO_Output_ControlMasterVolume.Checked).ToString());
            label_ASIO_Output_MaxVolume.Visible = checkBox_ASIO_Output_ControlMasterVolume.Checked;
            nUpDown_ASIO_Output_MaxVolume.Visible = checkBox_ASIO_Output_ControlMasterVolume.Checked;
        }
        private void ASIO_Output_EndpointVolume(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableSoftwareEndpointVolumeControlIdentifier, ASIO.ReadSettings.Sections.Output, Convert.ToInt32(checkBox_ASIO_Output_ControlEndpointVolume.Checked).ToString());
        private void ASIO_Output_EnableRefHack(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableRefCountHackIdentifier, ASIO.ReadSettings.Sections.Output, Convert.ToInt32(checkBox_ASIO_Output_EnableRefHack.Checked).ToString());

        // InputMic Settings
        private void ASIO_InputMic_Channel(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.ChannelIdentifier, ASIO.ReadSettings.Sections.InputMic, nUpDown_ASIO_InputMic_Channel.Value.ToString());
        private void ASIO_InputMic_MaxVolume(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.SoftwareMasterVolumePercentIdentifier, ASIO.ReadSettings.Sections.InputMic, nUpDown_ASIO_InputMic_MaxVolume.Value.ToString());
        private void ASIO_InputMic_MasterVolume(object sender, EventArgs e)
        {
            SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableSoftwareMasterVolumeControlIdentifier, ASIO.ReadSettings.Sections.InputMic, Convert.ToInt32(checkBox_ASIO_InputMic_ControlMasterVolume.Checked).ToString());
            label_ASIO_InputMic_MaxVolume.Visible = checkBox_ASIO_InputMic_ControlMasterVolume.Checked;
            nUpDown_ASIO_InputMic_MaxVolume.Visible = checkBox_ASIO_InputMic_ControlMasterVolume.Checked;
        }
        private void ASIO_InputMic_EndpointVolume(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableSoftwareEndpointVolumeControlIdentifier, ASIO.ReadSettings.Sections.InputMic, Convert.ToInt32(checkBox_ASIO_InputMic_ControlEndpointVolume.Checked).ToString());
        private void ASIO_InputMic_EnableRefHack(object sender, EventArgs e) => SaveSettings_ASIO_Middleware(ASIO.ReadSettings.EnableRefCountHackIdentifier, ASIO.ReadSettings.Sections.InputMic, Convert.ToInt32(checkBox_ASIO_InputMic_EnableRefHack.Checked).ToString());

        // Clear Selection
        private void ASIO_ClearSelectedDevice(ListBox deviceList, EventHandler e, ASIO.ReadSettings.Sections section)
        {
            deviceList.SelectedIndexChanged -= e;
            deviceList.SelectedIndex = -1;
            SaveSettings_ASIO_Middleware(ASIO.ReadSettings.DriverIdentifier, section, "");
            deviceList.SelectedIndexChanged += e;
        }
        private void ASIO_Input0_ClearSelection(object sender, EventArgs e) => ASIO_ClearSelectedDevice(listBox_AvailableASIODevices_Input0, ASIO_ListAvailableInput0, ASIO.ReadSettings.Sections.Input0);
        private void ASIO_Input1_ClearSelection(object sender, EventArgs e) => ASIO_ClearSelectedDevice(listBox_AvailableASIODevices_Input1, ASIO_ListAvailableInput1, ASIO.ReadSettings.Sections.Input1);
        private void ASIO_Output_ClearSelection(object sender, EventArgs e) => ASIO_ClearSelectedDevice(listBox_AvailableASIODevices_Output, ASIO_ListAvailableOutput, ASIO.ReadSettings.Sections.Output);
        private void ASIO_InputMic_ClearSelection(object sender, EventArgs e) => ASIO_ClearSelectedDevice(listBox_AvailableASIODevices_InputMic, ASIO_ListAvailableInputMic, ASIO.ReadSettings.Sections.InputMic);
        private void ASIO_OpenGithub(object sender, EventArgs e) => Process.Start("https://github.com/mdias/rs_asio");

        #endregion
        #region Rocksmith Settings
        // Audio Settings
        private void Rocksmith_EnableMicrophone(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.EnableMicrophoneIdentifier, checkBox_Rocksmith_EnableMicrophone.Checked.ToString().ToLower());
        private void Rocksmith_ExclusiveMode(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.ExclusiveModeIdentifier, checkBox_Rocksmith_ExclusiveMode.Checked.ToString().ToLower());
        private void Rocksmith_LatencyBuffer(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.LatencyBufferIdentifier, nUpDown_Rocksmith_LatencyBuffer.Value.ToString());
        private void Rocksmith_ForceWDM(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.ForceWDMIdentifier, checkBox_Rocksmith_ForceWDM.Checked.ToString().ToLower());
        private void Rocksmith_ForceDirextXSink(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.ForceDirectXSinkIdentifier, checkBox_Rocksmith_ForceDirextXSink.Checked.ToString().ToLower());
        private void Rocksmith_DumpAudioLog(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.DumpAudioLogIdentifier, checkBox_Rocksmith_DumpAudioLog.Checked.ToString().ToLower());
        private void Rocksmith_MaxBufferSize(object sender, EventArgs e)
        {
            SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.MaxOutputBufferSizeIdentifier, nUpDown_Rocksmith_MaxOutputBuffer.Value.ToString());
            if (nUpDown_Rocksmith_MaxOutputBuffer.Value == 0)
                checkBox_Rocksmith_Override_MaxOutputBufferSize.Checked = true;
        }
        private void Rocksmith_RTCOnly(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.RealToneCableOnlyIdentifier, checkBox_Rocksmith_RTCOnly.Checked.ToString().ToLower());
        private void Rocksmith_LowLatencyMode(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.Win32UltraLowLatencyModeIdentifier, checkBox_Rocksmith_LowLatencyMode.Checked.ToString().ToLower());

        private void Rocksmith_AutomateMaxBufferSize(object sender, EventArgs e)
        {
            nUpDown_Rocksmith_MaxOutputBuffer.Enabled = !checkBox_Rocksmith_Override_MaxOutputBufferSize.Checked;
            if (checkBox_Rocksmith_Override_MaxOutputBufferSize.Checked)
                nUpDown_Rocksmith_MaxOutputBuffer.Value = 0;
            else
                nUpDown_Rocksmith_MaxOutputBuffer.Value = 32;
        }

        // Visual Settings
        private void Rocksmith_GamepadUI(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.ShowGamepadUIIdentifier, checkBox_Rocksmith_GamepadUI.Checked.ToString().ToLower());
        private void Rocksmith_ScreenWidth(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.ScreenWidthIdentifier, nUpDown_Rocksmith_ScreenWidth.Value.ToString());
        private void Rocksmith_ScreenHeight(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.ScreenHeightIdentifier, nUpDown_Rocksmith_ScreenHeight.Value.ToString());
        private void Rocksmith_Windowed(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.FullscreenIdentifier, "0");
        private void Rocksmith_NonExclusiveFullScreen(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.FullscreenIdentifier, "1");
        private void Rocksmith_ExclusiveFullScreen(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.FullscreenIdentifier, "2");
        private void Rocksmith_LowQuality(object sender, EventArgs e)
        {
            checkBox_Rocksmith_DepthOfField.Checked = false;
            checkBox_Rocksmith_PostEffects.Checked = true;
            checkBox_Rocksmith_HighResScope.Checked = true;

            checkBox_Rocksmith_DepthOfField.Enabled = false;
            checkBox_Rocksmith_PostEffects.Enabled = false;
            checkBox_Rocksmith_HighResScope.Enabled = false;

            SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.VisualQualityIdentifier, "0");
        }
        private void Rocksmith_MediumQuality(object sender, EventArgs e)
        {
            checkBox_Rocksmith_DepthOfField.Checked = true;
            checkBox_Rocksmith_PostEffects.Checked = true;
            checkBox_Rocksmith_HighResScope.Checked = true;

            checkBox_Rocksmith_DepthOfField.Enabled = false;
            checkBox_Rocksmith_PostEffects.Enabled = false;
            checkBox_Rocksmith_HighResScope.Enabled = false;


            SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.VisualQualityIdentifier, "1");
        }
        private void Rocksmith_HighQuality(object sender, EventArgs e)
        {
            checkBox_Rocksmith_DepthOfField.Checked = true;
            checkBox_Rocksmith_PostEffects.Checked = true;
            checkBox_Rocksmith_HighResScope.Checked = true;

            checkBox_Rocksmith_DepthOfField.Enabled = false;
            checkBox_Rocksmith_PostEffects.Enabled = false;
            checkBox_Rocksmith_HighResScope.Enabled = false;

            SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.VisualQualityIdentifier, "2");
        }

        private void Rocksmith_CustomQuality(object sender, EventArgs e)
        {
            checkBox_Rocksmith_DepthOfField.Enabled = true;
            checkBox_Rocksmith_PostEffects.Enabled = true;
            checkBox_Rocksmith_HighResScope.Enabled = true;

            SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.VisualQualityIdentifier, "3");
        }
        private void Rocksmith_RenderWidth(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.RenderingWidthIdentifier, nUpDown_Rocksmith_RenderWidth.Value.ToString());
        private void Rocksmith_RenderHeight(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.RenderingHeightIdentifier, nUpDown_Rocksmith_RenderHeight.Value.ToString());
        private void Rocksmith_PostEffects(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.EnablePostEffectsIdentifier, checkBox_Rocksmith_PostEffects.Checked.ToString().ToLower());
        private void Rocksmith_Shadows(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.EnableShadowsIdentifier, checkBox_Rocksmith_Shadows.Checked.ToString().ToLower());
        private void Rocksmith_HighResScope(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.EnableHighResScopeIdentifier, checkBox_Rocksmith_HighResScope.Checked.ToString().ToLower());
        private void Rocksmith_DepthOfField(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.EnableDepthOfFieldIdentifier, checkBox_Rocksmith_DepthOfField.Checked.ToString().ToLower());
        private void Rocksmith_PerPixelLighting(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.EnablePerPixelLightingIdentifier, checkBox_Rocksmith_PerPixelLighting.Checked.ToString().ToLower());
        private void Rocksmith_MSAA(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.MsaaSamplesIdentifier, (((Convert.ToInt32(checkBox_Rocksmith_MSAASamples.Checked) * 3) + 1).ToString()));
        private void Rocksmith_DisableBrowser(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.DisableBrowserIdentifier, checkBox_Rocksmith_DisableBrowser.Checked.ToString().ToLower());

        private void Rocksmith_EnableRenderRes(object sender, EventArgs e)
        {
            label_Rocksmith_RenderWidth.Visible = checkBox_Rocksmith_EnableRenderRes.Checked;
            label_Rocksmith_RenderHeight.Visible = checkBox_Rocksmith_EnableRenderRes.Checked;
            nUpDown_Rocksmith_RenderWidth.Visible = checkBox_Rocksmith_EnableRenderRes.Checked;
            nUpDown_Rocksmith_RenderHeight.Visible = checkBox_Rocksmith_EnableRenderRes.Checked;

            if (!checkBox_Rocksmith_EnableRenderRes.Checked)
            {
                nUpDown_Rocksmith_RenderWidth.Value = 0;
                nUpDown_Rocksmith_RenderHeight.Value = 0;
            }
        }

        // Network Settings
        private void Rocksmith_UseProxy(object sender, EventArgs e) => SaveSettings_Rocksmith_Middleware(Rocksmith.ReadSettings.UseProxyIdentifier, checkBox_Rocksmith_UseProxy.Checked.ToString().ToLower());

        #endregion
        #region Profiles

        List<SongData> Songs = new List<SongData>();

        private string currentUnpackedProfile = String.Empty;

        private void Profiles_RefreshSonglistNames()
        {
            int startIndex = 3;

            for (int i = 0; i < Dictionaries.SongListIndexToINISetting.Count; i++)
            {
                dgv_Profiles_Songlists.Columns[startIndex + i].HeaderText = ReadSettings.ProcessSettings(Dictionaries.SongListIndexToINISetting[i]);
            }
        }

        private void Profiles_LoadSongs(object sender, EventArgs e)
        {
            dgv_Profiles_Songlists.ClearSelection();
            dgv_Profiles_Songlists.Rows.Clear();

            dgv_Profiles_Songlists.Visible = false;
            button_Profiles_SaveSonglist.Visible = false;

            Songs = SongManager.ExtractSongData(progressBar_Profiles_LoadPsarcs);

            // Add RS1 owned DLC
            List<string> ownedRS1DLC = new List<string>();
            List<JToken> DLCTags = Profiles.DecryptedProfile["Stats"]["DLCTag"].ToList();
            foreach (JProperty DLCTag in DLCTags)
            {
                ownedRS1DLC.Add(DLCTag.Name);
            }

            List<List<string>> dlcKeyArrayList = new List<List<string>>();
            dlcKeyArrayList.Add(Profiles.DecryptedProfile["FavoritesListRoot"]["FavoritesList"].ToObject<List<string>>());

            List<List<string>> SongLists = Profiles.DecryptedProfile["SongListsRoot"]["SongLists"].ToObject<List<List<string>>>();

            foreach (List<string> songlist in SongLists)
            {
                dlcKeyArrayList.Add(songlist);
            }

            foreach (SongData song in Songs.ToList())
            {

                if ((song.RS1AppID != 0 && !ownedRS1DLC.Contains(song.RS1AppID.ToString())) || song.Artist == String.Empty || song.Title == String.Empty || !song.Shipping)
                {
                    Songs.Remove(song);
                    continue;
                }

                // Default profile
                bool inFavorites = false, inSonglist1 = false, inSonglist2 = false, inSonglist3 = false, inSonglist4 = false, inSonglist5 = false, inSonglist6 = false;

                // Modified profile
                bool inSonglist7 = false, inSonglist8 = false, inSonglist9 = false, inSonglist10 = false, inSonglist11 = false, inSonglist12 = false, inSonglist13 = false;
                bool inSonglist14 = false, inSonglist15 = false, inSonglist16 = false, inSonglist17 = false, inSonglist18 = false, inSonglist19 = false, inSonglist20 = false;

                foreach (List<string> dlcKeyArray in dlcKeyArrayList)
                {
                    if (dlcKeyArray.Contains(song.DLCKey))
                    {
                        int songlist = dlcKeyArrayList.IndexOf(dlcKeyArray);

                        switch (songlist)
                        {
                            case 0:
                                inFavorites = true;
                                break;
                            case 1:
                                inSonglist1 = true;
                                break;
                            case 2:
                                inSonglist2 = true;
                                break;
                            case 3:
                                inSonglist3 = true;
                                break;
                            case 4:
                                inSonglist4 = true;
                                break;
                            case 5:
                                inSonglist5 = true;
                                break;
                            case 6:
                                inSonglist6 = true;
                                break;
                            case 7:
                                inSonglist7 = true;
                                break;
                            case 8:
                                inSonglist8 = true;
                                break;
                            case 9:
                                inSonglist9 = true;
                                break;
                            case 10:
                                inSonglist10 = true;
                                break;
                            case 11:
                                inSonglist11 = true;
                                break;
                            case 12:
                                inSonglist12 = true;
                                break;
                            case 13:
                                inSonglist13 = true;
                                break;
                            case 14:
                                inSonglist14 = true;
                                break;
                            case 15:
                                inSonglist15 = true;
                                break;
                            case 16:
                                inSonglist16 = true;
                                break;
                            case 17:
                                inSonglist17 = true;
                                break;
                            case 18:
                                inSonglist18 = true;
                                break;
                            case 19:
                                inSonglist19 = true;
                                break;
                            case 20:
                                inSonglist20 = true;
                                break;
                            default:
                                break;
                        }
                    }

                }

                dgv_Profiles_Songlists.Rows.Add(song.Artist, song.Title, inFavorites, inSonglist1, inSonglist2, inSonglist3, inSonglist4, inSonglist5, inSonglist6,
                                                inSonglist7, inSonglist8, inSonglist9, inSonglist10, inSonglist11, inSonglist12, inSonglist13, inSonglist14, inSonglist15,
                                                inSonglist16, inSonglist17, inSonglist18, inSonglist19, inSonglist20);
            }

            Profiles_RefreshSonglistNames();

            // Hide songlists that the user has not enabled yet.
            // Unhide songlists that the user has enabled.
            for (int songlist = 20; songlist > SongLists.Count; songlist--)
            {
                dgv_Profiles_Songlists.Columns[$"SongList{songlist}"].Visible = false;
            }
            for (int songlist = 1; songlist <= SongLists.Count; songlist++)
            {
                dgv_Profiles_Songlists.Columns[$"SongList{songlist}"].Visible = true;
            }

            dgv_Profiles_Songlists.Visible = true;
            button_Profiles_SaveSonglist.Visible = true;
        }

        private void Profiles_UnpackProfile()
        {
            if (currentUnpackedProfile != listBox_Profiles_AvailableProfiles.SelectedItem.ToString())
            {
                currentUnpackedProfile = listBox_Profiles_AvailableProfiles.SelectedItem.ToString();

                Profiles.DecryptedProfile = JObject.Parse(Profiles.DecryptProfiles(Profiles_GetProfilePathFromName(listBox_Profiles_AvailableProfiles.SelectedItem.ToString())));
            }
        }

        private void Profiles_ChangeSelectedProfile(object sender, EventArgs e)
        {
            button_Profiles_LoadSongs.Visible = true;
            groupBox_Profiles_Rewards.Visible = true;
            groupBox_Profile_MoreSongLists.Visible = true;
            groupBox_ImportJsonTones.Visible = true;

            Profiles_UnpackProfile();

            List<List<string>> SongLists = Profiles.DecryptedProfile["SongListsRoot"]["SongLists"].ToObject<List<List<string>>>();
            label_TotalSonglists.Text = SongLists.Count.ToString();
        }

        private string Profiles_GetProfilePathFromName(string profileName) => Path.Combine(Profiles.GetSaveDirectory(), Profiles.AvailableProfiles()[profileName] + "_PRFLDB");

        private void Profiles_SaveSonglists(object sender, EventArgs e)
        {
            if (listBox_Profiles_AvailableProfiles.SelectedIndex < 0)
                return;

            Profiles_ENCRYPT();
            MessageBox.Show("Your songlists and favorites have been saved!");
        }

        private void Profiles_UnlockAllRewards(object sender, EventArgs e)
        {
            if (listBox_Profiles_AvailableProfiles.SelectedIndex > -1)
            {
                if (MessageBox.Show("Are you sure you want to unlock all rewards?\nThat defeats the grind for in-game rewards.", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    Profiles.ChangeRewardStatus(true);
                    Profiles_SaveRewardsToProfile();
                }
            }
            else
                MessageBox.Show("Make sure you have a profile selected!");
        }

        private void Profiles_LockAllRewards(object sender, EventArgs e)
        {
            if (listBox_Profiles_AvailableProfiles.SelectedIndex > -1)
            {
                if (MessageBox.Show("Are you sure you want to lock all rewards?\nThis will remove all access to in-game rewards.", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    Profiles.ChangeRewardStatus(false);
                    Profiles_SaveRewardsToProfile();
                }
            }
            else
                MessageBox.Show("Make sure you have a profile selected!");
        }

        private void Profile_AddNewSongList(object sender, EventArgs e)
        {
            if (listBox_Profiles_AvailableProfiles.SelectedIndex > -1)
            {
                List<List<string>> SongLists = Profiles.DecryptedProfile["SongListsRoot"]["SongLists"].ToObject<List<List<string>>>();

                if (SongLists.Count >= 20)
                {
                    MessageBox.Show("We cannot complete your request!\nHaving more than 20 song lists is extremely unrealistic.\nPlease reach out to the RSMods dev team and we can change this restriction");
                    return;
                }

                SongLists.Add(new List<string>());

                Profiles.DecryptedProfile["SongListsRoot"]["SongLists"] = JToken.FromObject(SongLists);

                Profiles_GenerateNewSonglistsLists();

                // Reload the song list readout to show the new songlist.
                if (dgv_Profiles_Songlists.Visible)
                {
                    Profiles_LoadSongs(sender, e);
                }

                label_TotalSonglists.Text = SongLists.Count.ToString();

                Profiles_ENCRYPT();
                MessageBox.Show("Your new song list is present in game!");
            }
            else
                MessageBox.Show("Make sure you have a profile selected!");
        }


        private void Profile_RemoveNewestSongList(object sender, EventArgs e)
        {
            if (listBox_Profiles_AvailableProfiles.SelectedIndex > -1)
            {
                List<List<string>> SongLists = Profiles.DecryptedProfile["SongListsRoot"]["SongLists"].ToObject<List<List<string>>>();

                if (SongLists.Count <= 6)
                {
                    MessageBox.Show("We cannot remove anymore songlists.");
                    return;
                }

                SongLists.Remove(SongLists[SongLists.Count - 1]);

                Profiles.DecryptedProfile["SongListsRoot"]["SongLists"] = JToken.FromObject(SongLists);

                Profiles_GenerateNewSonglistsLists();

                // Reload the song list readout to hide the removed songlist.
                if (dgv_Profiles_Songlists.Visible)
                {
                    Profiles_LoadSongs(sender, e);
                }

                label_TotalSonglists.Text = SongLists.Count.ToString();

                Profiles_ENCRYPT();
                MessageBox.Show("The newest songlist has been removed!");
            }
            else
                MessageBox.Show("Make sure you have a profile selected!");
        }

        private void Profiles_GenerateNewSonglistsLists()
        {
            if (Profiles.DecryptedProfile != null)
            {
                List<List<string>> SongLists = Profiles.DecryptedProfile["SongListsRoot"]["SongLists"].ToObject<List<List<string>>>();

                Profiles_Helper_GenerateValidSonglists(SongLists.Count);
            }
        }

        private void Profiles_Helper_GenerateValidSonglists(int TotalSonglists)
        {
            List<string> SongListReferences = new List<string>();

            switch (TotalSonglists)
            {

                case 20:
                    SongListReferences.Add(ReadSettings.Songlist20Identifier);
                    goto case 19;
                case 19:
                    SongListReferences.Add(ReadSettings.Songlist19Identifier);
                    goto case 18;
                case 18:
                    SongListReferences.Add(ReadSettings.Songlist18Identifier);
                    goto case 17;
                case 17:
                    SongListReferences.Add(ReadSettings.Songlist17Identifier);
                    goto case 16;
                case 16:
                    SongListReferences.Add(ReadSettings.Songlist16Identifier);
                    goto case 15;
                case 15:
                    SongListReferences.Add(ReadSettings.Songlist15Identifier);
                    goto case 14;
                case 14:
                    SongListReferences.Add(ReadSettings.Songlist14Identifier);
                    goto case 13;
                case 13:
                    SongListReferences.Add(ReadSettings.Songlist13Identifier);
                    goto case 12;
                case 12:
                    SongListReferences.Add(ReadSettings.Songlist12Identifier);
                    goto case 11;
                case 11:
                    SongListReferences.Add(ReadSettings.Songlist11Identifier);
                    goto case 10;
                case 10:
                    SongListReferences.Add(ReadSettings.Songlist10Identifier);
                    goto case 9;
                case 9:
                    SongListReferences.Add(ReadSettings.Songlist9Identifier);
                    goto case 8;
                case 8:
                    SongListReferences.Add(ReadSettings.Songlist8Identifier);
                    goto case 7;
                case 7:
                    SongListReferences.Add(ReadSettings.Songlist7Identifier);
                    goto case 6;
                case 6:
                    SongListReferences.Add(ReadSettings.Songlist6Identifier);
                    goto case 5;
                case 5:
                    SongListReferences.Add(ReadSettings.Songlist5Identifier);
                    goto case 4;
                case 4:
                    SongListReferences.Add(ReadSettings.Songlist4Identifier);
                    goto case 3;
                case 3:
                    SongListReferences.Add(ReadSettings.Songlist3Identifier);
                    goto case 2;
                case 2:
                    SongListReferences.Add(ReadSettings.Songlist2Identifier);
                    goto case 1;

                case 1:
                    SongListReferences.Add(ReadSettings.Songlist1Identifier);
                    break;

                case 0:
                default:
                    break;
            }

            SongListReferences.Reverse();

            Dictionaries.SongListIndexToINISetting = SongListReferences;

            Dictionaries.refreshSonglists();
            listBox_Songlist.Items.Clear();

            foreach (string SongList in Dictionaries.songlists)
            {
                listBox_Songlist.Items.Add(SongList);
            }
        }

        private void Profiles_ENCRYPT()
        {
            Profiles.EncryptProfile(Profiles.DecryptedProfile.ToString(Newtonsoft.Json.Formatting.None), Profiles_GetProfilePathFromName(currentUnpackedProfile));
        }

        private void Profiles_SaveRewardsToProfile()
        {
            Profiles_ENCRYPT();
            MessageBox.Show("Changes to Rewards have been saved!");
        }

        private void Profiles_SongToSonglist(int songlistNumber, bool add = true)
        {
            int rowIndex = dgv_Profiles_Songlists.SelectedCells[0].RowIndex;
            string commonName = $"{dgv_Profiles_Songlists[0, rowIndex].Value.ToString()} - {dgv_Profiles_Songlists[1, rowIndex].Value.ToString()}";
            string DLCKey = Songs.FirstOrDefault(song => song.CommonName == commonName).DLCKey;

            List<string> SongList = Profiles.DecryptedProfile["SongListsRoot"]["SongLists"][songlistNumber - 1].ToObject<List<string>>();

            if (add && !SongList.Contains(DLCKey))
                SongList.Add(DLCKey);
            else if (!add && SongList.Contains(DLCKey))
                SongList.Remove(DLCKey);

            Profiles.DecryptedProfile["SongListsRoot"]["SongLists"][songlistNumber - 1] = JToken.FromObject(SongList);
        }

        private void Profiles_SongToFavorites(bool add = true)
        {
            int rowIndex = dgv_Profiles_Songlists.SelectedCells[0].RowIndex;
            string commonName = $"{dgv_Profiles_Songlists[0, rowIndex].Value.ToString()} - {dgv_Profiles_Songlists[1, rowIndex].Value.ToString()}";
            string DLCKey = Songs.FirstOrDefault(song => song.CommonName == commonName).DLCKey;

            List<string> FavoritesList = Profiles.DecryptedProfile["FavoritesListRoot"]["FavoritesList"].ToObject<List<string>>();

            if (add && !FavoritesList.Contains(DLCKey))
                FavoritesList.Add(DLCKey);
            else if (!add && FavoritesList.Contains(DLCKey))
                FavoritesList.Remove(DLCKey);

            Profiles.DecryptedProfile["FavoritesListRoot"]["FavoritesList"] = JToken.FromObject(FavoritesList);
        }

        private void Profiles_Songlists_DirtyState(object sender, EventArgs e)
        {
            if (dgv_Profiles_Songlists.IsCurrentCellDirty)
                dgv_Profiles_Songlists.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void Profiles_Songlists_ChangedValue(object sender, DataGridViewCellEventArgs e)
        {
            if (dgv_Profiles_Songlists.Columns[e.ColumnIndex].CellType == typeof(DataGridViewCheckBoxCell) && e.RowIndex > -1)
            {
                bool isChecked = Convert.ToBoolean(dgv_Profiles_Songlists[e.ColumnIndex, e.RowIndex].Value.ToString().ToLower());

                // Favorites
                if (e.ColumnIndex == 8)
                {
                    Profiles_SongToFavorites(isChecked);
                }
                // Songlists
                else
                {
                    Profiles_SongToSonglist(e.ColumnIndex - 1, isChecked);
                }
            }
        }

        private void Profiles_RevertToBackup(object sender, EventArgs e)
        {
            if (listBox_Profiles_ListBackups.SelectedIndex < 0)
                return;

            string localizedName = listBox_Profiles_ListBackups.SelectedItem.ToString();

            int monthNumber = DateTime.ParseExact(localizedName.Split(' ')[0], "MMM", CultureInfo.CurrentCulture).Month;

            string[] localizedSplit = localizedName.Split(' ');

            string time = localizedSplit[4];

            time = time.Replace(':', '-');

            string month = monthNumber.ToString();

            if (monthNumber < 10)
                month = "0" + monthNumber.ToString();

            string backupName = month + '-' + localizedSplit[1] + '-' + localizedSplit[2] + '_' + time;

            foreach (string profile in Directory.GetFiles(Path.Combine(GenUtil.GetRSDirectory(), "Profile_Backups", backupName)))
                File.Copy(profile, Path.Combine(Profiles.GetSaveDirectory(), Path.GetFileName(profile)), true);


            MessageBox.Show($"Reverted to the backup: {localizedName}");
        }

        private void Profiles_ImportToneManifest(object sender, EventArgs e)
        {
            if (listBox_Profiles_AvailableProfiles.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a profile!");
                return;
            }

            List<string> filenames = new List<string>();

            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Filter = "JSON|*.json";

                if (checkBox_ImportTonesBulk.Checked)
                {
                    fileDialog.Multiselect = true;
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    filenames = fileDialog.FileNames.ToList();
                }
            }

            if (filenames.Count == 0)
            {
                MessageBox.Show("No JSON manifests were imported.");
                return;
            }

            List<object> tonesToImport_Guitar = new List<object>();
            List<object> tonesToImport_Bass = new List<object>();

            foreach (string filename in filenames)
            {
                string manifestContents = File.ReadAllText(filename);

                JToken manifest = JToken.Parse(manifestContents);

                if (manifest["Entries"] == null)
                {
                    MessageBox.Show($"Input Tone Manifest doesn't have ENTRIES.\nFilename: {filename}");
                    continue;
                }

                JToken entries = manifest["Entries"];

                if (entries.First == null)
                {
                    MessageBox.Show($"Input Tone Manifest ENTRIES doesn't have children.\nFilename: {filename}");
                    continue;
                }

                string arrId = string.Empty;

                foreach (var pair in JObject.Parse(entries.ToString()))
                {
                    arrId = pair.Key;
                }

                if (arrId == string.Empty)
                {
                    MessageBox.Show($"Input Tone Manifest has no ArrangementId.\nFilename: {filename}");
                    continue;
                }
                if (entries[arrId] == null)
                {
                    MessageBox.Show($"Input Tone Manifest has invalid ArrangementId.\nFilename: {filename}");
                    continue;
                }

                JToken arrangement = entries[arrId];

                if (arrangement["Attributes"] == null)
                {
                    MessageBox.Show($"Input Tone Manifest has no arrangement attributes.\nFilename: {filename}");
                    continue;
                }

                JToken attributes = arrangement["Attributes"];

                if (attributes["ArrangementName"] == null)
                {
                    MessageBox.Show($"Input Tone Manifest has no arrangement name.\nFilename: {filename}");
                    continue;
                }

                string arrangementName = attributes["ArrangementName"].ToString();

                if (attributes["Tones"] == null)
                {
                    MessageBox.Show($"Input Tone Manifest has no tones.\nFilename: {filename}");
                    continue;
                }

                JToken tones = attributes["Tones"];

                List<object> tonesInSong = tones.ToObject<List<object>>();

                foreach (object tone in tonesInSong)
                {
                    // Bass
                    if (arrangementName.Contains("Bass"))
                    {
                        tonesToImport_Bass.Add(tone);
                    }
                    // Guitar
                    else
                    {
                        tonesToImport_Guitar.Add(tone);
                    }
                }
            }

            List<object> GuitarTones = Profiles.DecryptedProfile["CustomTones"].ToObject<List<object>>();
            List<object> BassTones = Profiles.DecryptedProfile["BassTones"].ToObject<List<object>>();

            foreach (object tone in tonesToImport_Guitar)
            {
                GuitarTones.Add(tone);
            }
            foreach (object tone in tonesToImport_Bass)
            {
                BassTones.Add(tone);
            }

            Profiles.DecryptedProfile["CustomTones"] = JToken.FromObject(GuitarTones);
            Profiles.DecryptedProfile["BassTones"] = JToken.FromObject(BassTones);

            Profiles_ENCRYPT();

            MessageBox.Show($"Added {tonesToImport_Guitar.Count + tonesToImport_Bass.Count} tone(s) to profile!");
        }

        private void Profiles_ImportTone2014(object sender, EventArgs e)
        {
            if (listBox_Profiles_AvailableProfiles.SelectedIndex == -1)
            {
                MessageBox.Show("Please select a profile!");
                return;
            }

            List<string> filenames = new List<string>();

            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Filter = "XML|*.tone2014.xml";

                if (checkBox_ImportTonesBulk.Checked)
                {
                    fileDialog.Multiselect = true;
                }

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    filenames = fileDialog.FileNames.ToList();
                }
            }

            if (filenames.Count == 0)
            {
                MessageBox.Show("No XML tones were imported.");
                return;
            }

            List<Tone2014> tonesToImport_Guitar = new List<Tone2014>();
            List<Tone2014> tonesToImport_Bass = new List<Tone2014>();

            MessageBoxManager.OK = "Guitar";
            MessageBoxManager.Cancel = "Bass";
            MessageBoxManager.Register();

            foreach (string filename in filenames)
            {
                Tone2014 tone = Tone2014.LoadFromXmlTemplateFile(filename);

                DialogResult arrangementResult = MessageBox.Show($"Do you want to save {tone.Name} as a guitar tone, or a bass tone?", "Question", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

                if (arrangementResult == DialogResult.Cancel)
                {
                    tonesToImport_Bass.Add(tone);
                }
                else
                {
                    tonesToImport_Guitar.Add(tone);
                }
            }

            MessageBoxManager.Unregister();
            MessageBoxManager.OK = "OK";
            MessageBoxManager.Cancel = "Cancel";

            List<object> GuitarTones = Profiles.DecryptedProfile["CustomTones"].ToObject<List<object>>();
            List<object> BassTones = Profiles.DecryptedProfile["BassTones"].ToObject<List<object>>();

            foreach (Tone2014 tone in tonesToImport_Guitar)
            {
                GuitarTones.Add(tone);
            }

            foreach (Tone2014 tone in tonesToImport_Bass)
            {
                BassTones.Add(tone);
            }


            Profiles.DecryptedProfile["CustomTones"] = JToken.FromObject(GuitarTones);
            Profiles.DecryptedProfile["BassTones"] = JToken.FromObject(BassTones);

            Profiles_ENCRYPT();

            MessageBox.Show($"Added {tonesToImport_Guitar.Count + tonesToImport_Bass.Count} tone(s) to your profile!");
        }

        #endregion
        #region Check For Updates
        private async void CheckForUpdates_CallGithubAPI()
        {
            try
            {
                // Setup HTTP Client
                string latestRelease_API = "https://api.github.com/repos/Lovrom8/RSMods/releases/latest";
                HttpClient client = new HttpClient();

                // Github API won't let us through without a User-Agent.
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RSMods", Application.ProductVersion));

                // Read latest release from Github API.
                HttpResponseMessage response = client.GetAsync(latestRelease_API).Result;

                string jsonResponse = await response.Content.ReadAsStringAsync();

                if (jsonResponse.Contains("limit exceeded"))
                {
                    github_UpdateResponse = "null";
                    return;
                }

                github_UpdateResponse = jsonResponse;
            }
            catch
            {
                github_UpdateResponse = "null"; // User doesn't have a stable internet connection.
            }
        }

        private bool CheckForUpdates_IsUpdateAvailable()
        {
            string jsonResponse = github_UpdateResponse;

            // Couldn't connect to Github API.
            if (jsonResponse == "null")
                return false;

            // Get Version Number From Github API.
            try
            {
                string github_versionNumber = JToken.Parse(jsonResponse).SelectToken("name").ToString().Replace("RSModsInstaller-v", "");

                // Return true if an update is available, and false if it isn't.
                return github_versionNumber != Application.ProductVersion;
            }
            catch // Unable to check for updates.
            {
                return false;
            }
        }

        private string CheckForUpdates_GetPatchNotes()
        {
            string jsonResponse = github_UpdateResponse;

            // Couldn't connect to Github API.
            if (jsonResponse == "null")
                return "";

            return JToken.Parse(jsonResponse).SelectToken("body").ToString();
        }

        private void CheckForUpdates_GetInstaller()
        {
            string jsonResponse = github_UpdateResponse;

            if (jsonResponse == "null")
                return;

            // Get Download Link For New Installer.
            string github_newRelease = JToken.Parse(jsonResponse).SelectToken("assets")[0].SelectToken("browser_download_url").ToString();
            string github_installerName = JToken.Parse(jsonResponse).SelectToken("assets")[0].SelectToken("name").ToString();

            // Download New Installer And Run It
            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(CheckForUpdates_RunInstaller);
                webClient.DownloadFileAsync(new Uri(github_newRelease), github_installerName);
            }
        }

        private void CheckForUpdates_RunInstaller(object sender, AsyncCompletedEventArgs e)
        {
            // Download was canceled.
            if (e.Cancelled)
                return;

            // Run Installer and close GUI so we don't cause conflicts / errors.
            if (e.Error == null)
            {
                Task.Run(() => Process.Start("RS2014-Mod-Installer.exe"));
                Application.Exit();
            }

            // Error detected
            else
                MessageBox.Show(e.Error.Message + "\n" + e.Error.StackTrace);
        }

        private void CheckForUpdates_UpdateRSMods(object sender, EventArgs e) => CheckForUpdates_GetInstaller();
        #endregion
        #region Sound Packs

        static string soundPackLocationPrefix = "audio_psarc\\audio_psarc_RS2014_Pc\\audio\\windows\\";
        static string soundPackEnglishPrefix = "english(us)\\";
        string voiceLine_BadPerformance = "2066953778.wem";
        string voiceLine_DisappointingPerformance = "2067218742.wem";
        string voiceLine_SubparPerformance = "2066826048.wem";
        string voiceLine_CouldBeBetter = "2068001585.wem";
        string voiceLine_DecentPerformance = "2068133176.wem";
        string voiceLine_AlrightPerformance = "2068002869.wem";
        string voiceLine_ExcellentPerformance = "2067285052.wem";
        string voiceLine_TopNotchPerformance = "2067281979.wem";
        string voiceLine_SuperbPerformance = "2067350856.wem";
        string voiceLine_DazzlingPerformance = "2068132687.wem";
        string voiceLine_YoureGonnaBeASuperstar = "2068199486.wem";
        string voiceLine_WonderfulPerformance = "2067154245.wem";
        string voiceLine_ExceptionalPerformance = "2067153482.wem";
        string voiceLine_AmazingPerformance = "2067871807.wem";
        string voiceLine_ExemplaryPerformance = "2067022644.wem";
        string voiceLine_MasterfulPerformance_98 = "2068137287.wem";
        string voiceLine_MasterfulPerformance_99 = "2067870540.wem";
        string voiceLine_FlawlessPerformance = "2068002100.wem";


        private void SoundPacks_UnpackAudioPsarc(object sender, EventArgs e)
        {
            if (MessageBox.Show("For us to do song packs we need to unpack a huge game file. This will take up about 1.3 gigabytes.\nPress OK if you are fine with that, or Cancel if you are not.", "Please Read!!!", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK)
                return;

            SoundPacks_PleaseWaitMessage(true);

            string audioPsarcLocation = Path.Combine(GenUtil.GetRSDirectory(), "audio.psarc");

            GlobalExtension.UpdateProgress = progressBar_RepackAudioPsarc;
            GlobalExtension.CurrentOperationLabel = label_AudioPsarcPleaseWait;

            Packer.Unpack(audioPsarcLocation, Path.Combine(GenUtil.GetRSDirectory(), "RSMods/", "audio_psarc"));

            SoundPacks_PleaseWaitMessage(false);
            SoundPacks_ChangeUIForUnpackedFolder(true);
            MessageBox.Show("You may now mess around with custom sound packs");
        }

        private void SoundPacks_DownloadWwise(object sender, EventArgs e)
        {
            Process.Start("https://ignition4.customsforge.com/cfsm/wwise/");
            MessageBox.Show("After you download and install Wwise, make sure to open it at least once to ensure the EULA is agreed to!", "Wwise EULA", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool SoundPacks_PleaseWaitMessage(bool show)
        {
            label_AudioPsarcPleaseWait.Visible = show;
            progressBar_RepackAudioPsarc.Visible = show;

            if (show)
                progressBar_RepackAudioPsarc.Value = 0;

            return show;
        }

        private void SoundPacks_RepackAudioPsarc(object sender, EventArgs e)
        {
            if (!Directory.Exists(Path.Combine(GenUtil.GetRSDirectory(), "RSMods/", "audio_psarc\\audio_psarc_RS2014_Pc")))
            {
                MessageBox.Show("We detect no audio.psarc is decompiled. Give us some time to try to fix that.");
                SoundPacks_UnpackAudioPsarc(sender, e);
            }

            MessageBox.Show("This will take a couple minutes!\nGo do something while this is working it's magic.\nIf RSMods looks like it crashed, it didn't, do NOT attempt to close it or you may need to verify your game files");
            SoundPacks_PleaseWaitMessage(true);
            GlobalExtension.CurrentOperationLabel = label_AudioPsarcPleaseWait;
            GlobalExtension.UpdateProgress = progressBar_RepackAudioPsarc;
            GlobalExtension.UpdateProgress.Maximum = 110;
            Packer.Pack(Path.Combine(Application.StartupPath, "audio_psarc\\audio_psarc_RS2014_Pc"), Path.Combine(GenUtil.GetRSDirectory(), "audio.psarc"));
            GlobalExtension.UpdateProgress.Value = 0;
            GlobalExtension.UpdateProgress.Maximum = 100;
            SoundPacks_PleaseWaitMessage(false);
            MessageBox.Show("Open your game, and see if the sound works!");
            GC.Collect(); // We use a lot of memory here, so let's take out the garbage.
        }

        private void SoundPacks_RemoveUnpackedAudioPsarc(object sender, EventArgs e)
        {
            Directory.Delete("audio_psarc", true);
            SoundPacks_ChangeUIForUnpackedFolder(false);
        }

        private void SoundPacks_ChangeUIForUnpackedFolder(bool isUnpacked)
        {
            button_UnpackAudioPsarc.Visible = !isUnpacked;
            groupBox_SoundPacks.Visible = isUnpacked;
        }

        private void SoundPacks_ReplaceSound(string soundToReplace)
        {

            if (!Directory.Exists(Path.Combine(Application.StartupPath, "audio_psarc")))
            {
                MessageBox.Show("Audio PSARC not unpacked");
                SoundPacks_ChangeUIForUnpackedFolder(false);
                return;
            }

            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.Filter = "Mp3 Files|*.mp3|Ogg Files|*.ogg|Wav Files|*.wav|Wem Files|*.wem";
                fileDialog.RestoreDirectory = true;

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    if (Path.GetExtension(fileDialog.FileName) == ".mp3")
                        fileDialog.FileName = SoundPacks_ConvertMP3ToWav(fileDialog.FileName);
                    if (Path.GetExtension(fileDialog.FileName) == ".ogg")
                        fileDialog.FileName = SoundPacks_ConvertOGGToWem(fileDialog.FileName);
                    if (Path.GetExtension(fileDialog.FileName) == ".wav")
                        fileDialog.FileName = SoundPacks_ConvertWAVToWem(fileDialog.FileName);

                    if (fileDialog.FileName != "null")
                    {
                        File.Delete(Path.Combine(Application.StartupPath, soundToReplace));
                        GC.Collect(); // Need to take out the garbage or it'll crash
                        File.Move(fileDialog.FileName, Path.Combine(Application.StartupPath, soundToReplace));
                        MessageBox.Show("Don't forget to hit \"Repack Audio Psarc\" when you're done.");
                    }
                    else
                        MessageBox.Show("An error occured when converting your file.\nPlease contact the RSMods devs.");

                }
            }
        }

        private string SoundPacks_ConvertMP3ToWav(string mp3File)
        {
            string wavFile = Path.Combine(Path.GetDirectoryName(mp3File), Path.GetFileNameWithoutExtension(mp3File) + ".wav");

            using (Mp3FileReader mp3FileReader = new Mp3FileReader(mp3File))
            {
                WaveFileWriter.CreateWaveFile(wavFile, mp3FileReader);
            }

            return wavFile;
        }

        private void SoundPacks_Beta(object sender, EventArgs e) => Process.Start("https://github.com/Lovrom8/RSMods/issues/new");

        private string SoundPacks_ConvertWAVToWem(string wavFile)
        {
            string wemFile = "null";
            try
            {
                string previewWav = Path.Combine(Path.GetDirectoryName(wavFile), Path.GetFileNameWithoutExtension(wavFile) + "_preview.wav"); // Previw needs to be made or Wav2Wem crashes.

                if (File.Exists(previewWav))
                    File.Delete(previewWav);

                File.Copy(wavFile, previewWav);
                Wwise.Wav2Wem(wavFile, Path.Combine(Application.StartupPath, Path.GetFileNameWithoutExtension(wavFile) + ".wem"), 4);
                wemFile = Path.Combine(Application.StartupPath, Path.GetFileNameWithoutExtension(wavFile) + ".wem");
                File.Delete(previewWav);
                File.Delete(Path.Combine(Path.GetDirectoryName(wemFile), Path.GetFileNameWithoutExtension(wemFile) + "_preview.wem"));
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show($"Your WWISEROOT environment variable can't be found.\nPlease change it to a folder that exists.\nThe error is: {ex.Message}.\nPlease reboot your computer after you fix this.");
            }

            return wemFile;
        }

        private string SoundPacks_ConvertOGGToWem(string oggFile)
        {
            SoundPacks_PleaseWaitMessage(true);
            string wemFile = "null";
            try
            {
                string oggFileName = Path.GetFileNameWithoutExtension(oggFile);
                string oggFullPath = Path.Combine(Path.GetDirectoryName(oggFile), oggFileName);
                string directory = Path.GetDirectoryName(oggFile);
                string oggPreviewName = oggFullPath + "_preview";

                wemFile = OggFile.Convert2Wem(oggFile);

                File.Delete(oggFullPath + ".wav");
                File.Delete(oggPreviewName + ".ogg");
                File.Delete(oggPreviewName + ".wav");
                File.Delete(oggPreviewName + ".wem");

                if (File.Exists(Path.Combine(Application.StartupPath, Path.GetFileNameWithoutExtension(wemFile) + ".wem")))
                    File.Delete(Path.Combine(Application.StartupPath, Path.GetFileNameWithoutExtension(wemFile) + ".wem"));

                GC.Collect(); // Gotta clean up the garbage or it'll try to crash.

                File.Move(wemFile, Path.Combine(Application.StartupPath, Path.GetFileNameWithoutExtension(wemFile) + ".wem"));
                wemFile = Path.Combine(Application.StartupPath, Path.GetFileNameWithoutExtension(wemFile) + ".wem");
            }
            catch (DirectoryNotFoundException ex)
            {
                MessageBox.Show($"Your WWISEROOT environment variable can't be found.\nPlease change it to a folder that exists.\nThe error is: {ex.Message}.\nPlease reboot your computer after you fix this.");
            }

            SoundPacks_PleaseWaitMessage(false);
            return wemFile;
        }

        private void SoundPacks_Import_Dialog(object sender, EventArgs e)
        {
            using (OpenFileDialog fileDialog = new OpenFileDialog())
            {
                fileDialog.RestoreDirectory = true;
                fileDialog.Filter = "RS2014 Soundpack|*.rs_soundpack";

                if (fileDialog.ShowDialog() == DialogResult.OK)
                    SoundPacks_Import_File(fileDialog.FileName);
            }
        }

        private void SoundPacks_Import_File(string fileName)
        {
            SevenZipExtractor.SetLibraryPath("7z64.dll");
            using (SevenZipExtractor extractor = new SevenZipExtractor(fileName))
            {
                extractor.ExtractArchive(soundPackLocationPrefix);
                MessageBox.Show("Don't forget to hit \"Repack Audio Psarc\" when you're done.");
            }
        }

        private void SoundPacks_Export_Dialog(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.Filter = "RS2014 Soundpack|*.rs_soundpack";
            fileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (fileDialog.ShowDialog() == DialogResult.OK)
                SoundPacks_Export_File(fileDialog.FileName);
        }

        private void SoundPacks_Export_File(string fileName)
        {
            SevenZipCompressor.SetLibraryPath("7z64.dll");

            SevenZipCompressor compressor = new SevenZipCompressor
            {
                CompressionMethod = CompressionMethod.Deflate,
                CompressionLevel = SevenZip.CompressionLevel.Normal,
                CompressionMode = SevenZip.CompressionMode.Create,
                DirectoryStructure = true,
                PreserveDirectoryRoot = false,
                ArchiveFormat = OutArchiveFormat.Zip
            };

            Dictionary<string, string> exportedFiles = new Dictionary<string, string>()
                {
                    { soundPackEnglishPrefix + voiceLine_BadPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_BadPerformance },
                    { soundPackEnglishPrefix + voiceLine_DisappointingPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_DisappointingPerformance },
                    { soundPackEnglishPrefix + voiceLine_SubparPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_SubparPerformance },
                    { soundPackEnglishPrefix + voiceLine_CouldBeBetter, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_CouldBeBetter },
                    { soundPackEnglishPrefix + voiceLine_DecentPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_DecentPerformance },
                    { soundPackEnglishPrefix + voiceLine_AlrightPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_AlrightPerformance },
                    { soundPackEnglishPrefix + voiceLine_ExcellentPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_ExcellentPerformance },
                    { soundPackEnglishPrefix + voiceLine_TopNotchPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_TopNotchPerformance },
                    { soundPackEnglishPrefix + voiceLine_SuperbPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_SuperbPerformance },
                    { soundPackEnglishPrefix + voiceLine_DazzlingPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_DazzlingPerformance },
                    { soundPackEnglishPrefix + voiceLine_YoureGonnaBeASuperstar, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_YoureGonnaBeASuperstar },
                    { soundPackEnglishPrefix + voiceLine_WonderfulPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_WonderfulPerformance },
                    { soundPackEnglishPrefix + voiceLine_ExceptionalPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_ExceptionalPerformance },
                    { soundPackEnglishPrefix + voiceLine_AmazingPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_AmazingPerformance },
                    { soundPackEnglishPrefix + voiceLine_ExemplaryPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_ExemplaryPerformance },
                    { soundPackEnglishPrefix + voiceLine_MasterfulPerformance_98, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_MasterfulPerformance_98 },
                    { soundPackEnglishPrefix + voiceLine_MasterfulPerformance_99, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_MasterfulPerformance_99 },
                    { soundPackEnglishPrefix + voiceLine_FlawlessPerformance, soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_FlawlessPerformance },
                };

            compressor.CompressFileDictionary(exportedFiles, fileName);
            MessageBox.Show("You can now share your sound pack with your friends!\nTell them to open this menu and click \"Import Soundpack\" button, and point to this file.");
        }

        private void SoundPacks_Reset(object sender, EventArgs e)
        {
            GenUtil.ExtractEmbeddedResource(Application.StartupPath, Assembly.GetExecutingAssembly(), "RSMods.Resources", new string[] { "original.rs_soundpack" });
            SoundPacks_Import_File("original.rs_soundpack");
            File.Delete("original.rs_soundpack");
        }

        private void SoundPacks_ReplaceBadPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_BadPerformance);
        private void SoundPacks_ReplaceDisappointingPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_DisappointingPerformance);
        private void SoundPacks_ReplaceSubparPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_SubparPerformance);
        private void SoundPacks_ReplaceCouldBeBetter(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_CouldBeBetter);
        private void SoundPacks_ReplaceDecentPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_DecentPerformance);
        private void SoundPacks_ReplaceAlrightPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_AlrightPerformance);
        private void SoundPacks_ReplaceExcellentPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_ExcellentPerformance);
        private void SoundPacks_ReplaceTopNotchPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_TopNotchPerformance);
        private void SoundPacks_ReplaceSuperbPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_SuperbPerformance);
        private void SoundPacks_ReplaceDazzlingPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_DazzlingPerformance);
        private void SoundPacks_ReplaceSuperstar(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_YoureGonnaBeASuperstar);
        private void SoundPacks_ReplaceWonderfulPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_WonderfulPerformance);
        private void SoundPacks_ReplaceExceptionalPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_ExceptionalPerformance);
        private void SoundPacks_ReplaceAmazingPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_AmazingPerformance);
        private void SoundPacks_ReplaceExemplaryPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_ExemplaryPerformance);
        private void SoundPacks_ReplaceMasterfulPerformance_98(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_MasterfulPerformance_98);
        private void SoundPacks_ReplaceMasterfulPerformance_99(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_MasterfulPerformance_99);
        private void SoundPacks_ReplaceFlawlessPerformance(object sender, EventArgs e) => SoundPacks_ReplaceSound(soundPackLocationPrefix + soundPackEnglishPrefix + voiceLine_FlawlessPerformance);


        private void SoundPacks_LoadResultVoiceOverList()
        {
            listBox_Result_VOs.Items.Clear();
            Dictionaries.ResultVoiceOverDictionary.Keys.ToList().ForEach(key => listBox_Result_VOs.Items.Add(key));
        }

        private void SoundPacks_PlayResultVoiceOver(object sender, EventArgs e)
        {
            if (listBox_Result_VOs.SelectedIndex == -1)
                return;

            WinMsgUtil.SendMsgToRS($"WwiseEvent {Dictionaries.ResultVoiceOverDictionary[listBox_Result_VOs.SelectedItem.ToString()]}");
        }
        #endregion
        #region Midi

        private void Midi_LoadDevices()
        {
            this.listBox_ListMidiOutDevices.Items.Clear();
            this.listBox_ListMidiInDevices.Items.Clear();

            uint numberOfMidiOutDevices = Midi.midiOutGetNumDevs();
            uint numberOfMidiInDevices = Midi.midiInGetNumDevs();

            for (uint deviceNumber = 0; deviceNumber < numberOfMidiOutDevices; deviceNumber++)
            {
                Midi.MIDIOUTCAPS temp = new Midi.MIDIOUTCAPS { };
                Midi.midiOutGetDevCaps(deviceNumber, ref temp, (uint)Marshal.SizeOf(typeof(Midi.MIDIOUTCAPS)));
                this.listBox_ListMidiOutDevices.Items.Add(temp.szPname);
            }

            for (uint deviceNumber = 0; deviceNumber < numberOfMidiInDevices; deviceNumber++)
            {
                Midi.MIDIINCAPS temp = new Midi.MIDIINCAPS { };
                Midi.midiInGetDevCaps(deviceNumber, ref temp, (uint)Marshal.SizeOf(typeof(Midi.MIDIINCAPS)));
                this.listBox_ListMidiInDevices.Items.Add(temp.szPname);
            }

            if (ReadSettings.ProcessSettings(ReadSettings.MidiAutoTuningDeviceIdentifier) != "")
                listBox_ListMidiOutDevices.SelectedItem = ReadSettings.ProcessSettings(ReadSettings.MidiAutoTuningDeviceIdentifier);

            if (ReadSettings.ProcessSettings(ReadSettings.MidiInDeviceIdentifier) != "")
                listBox_ListMidiInDevices.SelectedItem = ReadSettings.ProcessSettings(ReadSettings.MidiInDeviceIdentifier);
        }

        private void MidiInProc(int hMidiIn, Midi.Responses wMsg, uint dwInstance, uint midiMessage, uint timeStamp)
        {
            switch (wMsg)
            {
                case Midi.Responses.MIM_OPEN:
                    Debug.WriteLine("wMsg=MIM_OPEN");
                    break;
                case Midi.Responses.MIM_CLOSE:
                    Debug.WriteLine("wMsg=MIM_CLOSE");
                    break;
                case Midi.Responses.MIM_DATA:
                    byte[] Data = BitConverter.GetBytes(midiMessage);
                    byte[] Timestamp = BitConverter.GetBytes(timeStamp);

                    // This should always be the case, but we should check just in case.
                    // 0xFE - Keep-Alive signal. We don't need to log that.
                    if (Data.Length == 4)
                    {
                        Midi.Status Status = (Midi.Status)Data[0];
                        byte Channel = (byte)(Data[0] % 16);

                        // Note Off
                        if (Status >= Midi.Status.NoteOff && Status < Midi.Status.NoteOn)
                        {
                            Debug.WriteLine($"Note Off received on channel {Channel}. Key = {Data[1]}. Velocity = {Data[2]}");
                        }

                        // Note On
                        else if (Status >= Midi.Status.NoteOn && Status < Midi.Status.AfterTouch)
                        {
                            Debug.WriteLine($"Note On received on channel {Channel}. Key = {Data[1]}. Velocity = {Data[2]}");
                        }

                        // Aftertouch
                        else if (Status >= Midi.Status.AfterTouch && Status < Midi.Status.CC)
                        {
                            Debug.WriteLine($"Aftertouch received on channel {Channel}. Key = {Data[1]}. Touch = {Data[2]}");
                        }

                        // CC
                        else if (Status >= Midi.Status.CC && Status < Midi.Status.PC)
                        {
                            Debug.WriteLine($"CC received on channel {Channel}. Bank = {Data[1]}. Value = {Data[2]}");
                        }

                        // PC
                        else if (Status >= Midi.Status.PC && Status < Midi.Status.Pressure)
                        {
                            Debug.WriteLine($"PC received on channel {Channel}. Program = {Data[1]}");
                        }

                        // Pressure
                        else if (Status >= Midi.Status.Pressure && Status < Midi.Status.PitchBend)
                        {
                            Debug.WriteLine($"Pressure received on channel {Channel}.");
                        }

                        // Pitch Bend
                        else if (Status >= Midi.Status.PitchBend && Status < Midi.Status.SystemEx)
                        {
                            Debug.WriteLine($"Pitch Bend received on channel {Channel}. LSB = {Data[1]}. MSB = {Data[2]}");
                        }

                        // SystemEx
                        else if (Status >= Midi.Status.SystemEx)
                        {
                            // Keep-Alive status. Don't log this as it will spam the console.
                            if (Status == (Midi.Status)0xFE)
                            {
                                return;
                            }

                            Debug.WriteLine($"SystemEX received on channel {Channel}. Data1 = {Data[1]}. Data2 = {Data[2]}");
                        }

                        // Unknown status
                        else
                        {
                            Debug.WriteLine($"Unknown MIDI status received on channel {Channel}! Status = {Data[0]}. Data1 = {Data[1]}. Data2 = {Data[2]}");
                        }
                    }

                    break;
                case Midi.Responses.MIM_LONGDATA:
                    Debug.WriteLine("wMsg=MIM_LONGDATA");
                    break;
                case Midi.Responses.MIM_ERROR:
                    Debug.WriteLine("wMsg=MIM_ERROR");
                    break;
                case Midi.Responses.MIM_LONGERROR:
                    Debug.WriteLine("wMsg=MIM_LONGERROR");
                    break;
                case Midi.Responses.MIM_MOREDATA:
                    Debug.WriteLine("wMsg=MIM_MOREDATA");
                    break;
                default:
                    Debug.WriteLine("wMsg = unknown");
                    break;
            }
        }

        private void checkBox_EnabledMidiIn_CheckedChanged(object sender, EventArgs e)
        {

            // Enable
            if (checkBox_EnabledMidiIn.Checked)
            {
                if (listBox_ListMidiInDevices.SelectedIndex == -1)
                    return;

                uint numberOfMidiInDevices = Midi.midiInGetNumDevs();

                Debug.WriteLine("Looking for device");

                for (uint deviceNumber = 0; deviceNumber < numberOfMidiInDevices; deviceNumber++)
                {
                    Midi.MIDIINCAPS temp = new Midi.MIDIINCAPS { };
                    Midi.midiInGetDevCaps(deviceNumber, ref temp, (uint)Marshal.SizeOf(typeof(Midi.MIDIINCAPS)));

                    if (temp.szPname == listBox_ListMidiInDevices.SelectedItem.ToString())
                    {
                        Debug.WriteLine($"Found device: {temp.szPname}");
                        Midi.SelectedMidiInDeviceId = deviceNumber;
                        break;
                    }
                }

                if (Midi.SelectedMidiInDeviceId == 2014)
                    return;

                Midi.MidiInProcessing = MidiInProc;
                Debug.WriteLine(Midi.midiInOpen(ref Midi.MidiInHandle, Midi.SelectedMidiInDeviceId, Midi.MidiInProcessing, 0, 0x30000));

                Debug.WriteLine(Midi.midiInStart(Midi.MidiInHandle));

                Debug.WriteLine("Set up Midi In");
            }
            // Disable
            else
            {
                Midi.midiInStop(Midi.MidiInHandle);
                Midi.midiInClose(Midi.MidiInHandle);

                Midi.MidiInHandle = (IntPtr)0;

                Debug.WriteLine("Shutdown Midi In");
            }
        }
    }



    public class Midi
    {
        public enum Status : byte
        {
            NoteOff = 0x80,
            NoteOn = 0x90,
            AfterTouch = 0xA0,
            CC = 0xB0,
            PC = 0xC0,
            Pressure = 0xD0,
            PitchBend = 0xE0,
            SystemEx = 0xF0
        }


        public static IntPtr MidiInHandle = (IntPtr)0;
        public static uint SelectedMidiInDeviceId = 2014;
        public static MidiInProc MidiInProcessing = null;

        public enum Responses : uint
        {
            MIM_OPEN = 0x3C1,
            MIM_CLOSE = 0x3C2,
            MIM_DATA = 0x3C3,
            MIM_LONGDATA = 0x3C4,
            MIM_ERROR = 0x3C5,
            MIM_LONGERROR = 0x3C6,
            MIM_MOREDATA = 0x3CC
        }
        public enum MMRESULT : uint
        {
            MMSYSERR_NOERROR,
            MMSYSERR_ERROR,
            MMSYSERR_BADDEVICEID,
            MMSYSERR_NOTENABLED,
            MMSYSERR_ALLOCATED,
            MMSYSERR_INVALHANDLE,
            MMSYSERR_NODRIVER,
            MMSYSERR_NOMEM,
            MMSYSERR_NOTSUPPORTED,
            MMSYSERR_BADERRNUM,
            MMSYSERR_INVALFLAG,
            MMSYSERR_INVALPARAM,
            MMSYSERR_HANDLEBUSY,
            MMSYSERR_INVALIDALIAS,
            MMSYSERR_BADDB,
            MMSYSERR_KEYNOTFOUND,
            MMSYSERR_READERROR,
            MMSYSERR_WRITEERROR,
            MMSYSERR_DELETEERROR,
            MMSYSERR_VALNOTFOUND,
            MMSYSERR_NODRIVERCB,
            WAVERR_BADFORMAT = 32,
            WAVERR_STILLPLAYING = 33,
            WAVERR_UNPREPARED = 34
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void MidiInProc(int hMidiIn, Midi.Responses wMsg, uint dwInstance, uint dwParam1, uint dwParam2);

        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIOUTCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public ushort wTechnology;
            public ushort wVoices;
            public ushort wNotes;
            public ushort wChannelMask;
            public uint dwSupport;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MIDIINCAPS
        {
            public ushort wMid;
            public ushort wPid;
            public uint vDriverVersion;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szPname;
            public uint dwSupport;
        }

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiOutGetDevCaps(uint uDeviceID, ref MIDIOUTCAPS lpMidiOutCaps, uint cbMidiOutCaps);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint midiOutGetNumDevs();

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern MMRESULT midiInGetDevCaps(uint uDeviceID, ref MIDIINCAPS pmic, uint cbmic);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint midiInGetNumDevs();

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint midiInOpen(ref IntPtr hmi, uint uDeviceID, [MarshalAs(UnmanagedType.FunctionPtr)] MidiInProc dwCallback, uint dwInstance, uint fdwOpen);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint midiInStart(IntPtr hmi);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint midiInStop(IntPtr hmi);

        [DllImport("winmm.dll", SetLastError = true)]
        public static extern uint midiInClose(IntPtr hmi);
    }
    #endregion
}