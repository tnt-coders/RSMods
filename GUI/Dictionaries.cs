﻿using System.Windows.Forms;
using System.Collections.Generic;


namespace RSMods
{
    public partial class MainForm : Form
    {
        #region Tooltips
        private Dictionary<Control, string> TooltipDictionary = new Dictionary<Control, string>() {};

        private void FillToolTipDictionary()
        {
            // INI Edits
            // Checkboxes
            TooltipDictionary.Add(checkBox_ToggleLoft, "Disables the game background, amps and noise reactive speaker rings.\nBest used with Venue Mode off (setting in game).\nUsed by a lot of Rocksmith Streamers to make it easy to Luma Key out the game background.\nPlayer just sees an all black background when this is enabled.\nOptions for turning the loft off only when in a song, when the game first starts up, or on a key press.");
            TooltipDictionary.Add(checkBox_SongTimer, "Experimental.\nIntent is to show a box with your timestamp position through the song.");
            TooltipDictionary.Add(checkBox_ExtendedRange, "Alters the string and note colors to make it easier to play a 5 string bass or 7 string guitar.");
            TooltipDictionary.Add(checkBox_ForceEnumeration, "Game will automatically start an Enumeration sequence when a new psarc file is detected as having been added to the dlc folder.\nNot necesary to enable if you're already using Rocksniffer to do the same thing.");
            TooltipDictionary.Add(checkBox_RemoveHeadstock, "Stops the Headstock of the guitar being drawn.\n“Headless” guitar mode. Just cleans up some more of the UI.");
            TooltipDictionary.Add(checkBox_RemoveSkyline, "Removes the purple and orange bars from the top of the display in LAS.\nUse in conjunction with No Loft for a cleaner UI.\nOptions for always off, only off when in a song, or only when toggled by key press.");
            TooltipDictionary.Add(checkBox_GreenScreen, "Changes just a section of the game background to all black, amusing for a selective “green screen” stream experience.\nInvalidated by \"No Loft\".");
            TooltipDictionary.Add(checkBox_AutoLoadProfile, "Essentially holds down the ENTER key until the game has reached the main menu.\nLets you auto load the last used profile without needing to interact with the game at all.\nAlso allows you to specify what profile you want to always load");
            TooltipDictionary.Add(checkBox_Fretless, "Removes the Fret Wire from the neck, making your instrument appear to be fretless.");
            TooltipDictionary.Add(checkBox_RemoveInlays, "Disables the guitar neck inlay display entirely.\nNote: This only works with the standard dot inlays.");
            TooltipDictionary.Add(checkBox_ControlVolume, "Allows you to control how loud the game is using the in-game mixer without needing to open it.\nAlso includes a hidden \"Master Volume\" control.\nPress the key to go up by the interval set in the \"Misc\" sub-tab.\nHold Control and press the key to go down by the interval.");
            TooltipDictionary.Add(checkBox_GuitarSpeak, "Use your guitar to control the menus!");
            TooltipDictionary.Add(checkBox_RemoveLyrics, "Disables the display of song lyrics while in Learn-A-Song mode.");
            TooltipDictionary.Add(checkBox_RainbowStrings, "Experimental.\nHow Pro are you? This makes the players guitar strings constantly cycling through colors.");
            TooltipDictionary.Add(checkBox_RainbowNotes, "Experimental.\nHow Pro are you? This makes all the notes constantly cycle through colors.");
            TooltipDictionary.Add(checkBox_CustomColors, "Lets you define the string / note colors you want.\nSaves a normal set and a Colorblind mode set.");
            TooltipDictionary.Add(checkBox_RemoveLaneMarkers, "Removes the additional lane marker lines seen in the display.\nWhen used with No Loft, provides a cleaner Luma Key.");
            TooltipDictionary.Add(checkBox_ScreenShotScores, "We will automatically take a steam screenshot whenever you finish a song");
            TooltipDictionary.Add(checkBox_RiffRepeaterSpeedAboveOneHundred, "Allow you to play a song faster than 100% speed in Riff Repeater.\nPress keybinding for the speed to go up.\nPress Control + keybinding for the speed to go down.");
            TooltipDictionary.Add(checkBox_ChangeTheme, "Use this feature to customize the colors used in this GUI.");
            TooltipDictionary.Add(checkBox_useMidiAutoTuning, "If you have a drop tuning pedal with a MIDI port, we will attempt to automatically tune.");
            TooltipDictionary.Add(checkBox_ShowCurrentNote, "Shows the note you are currently playing on screen.");
            TooltipDictionary.Add(checkBox_CustomHighway, "This setting lets you change the colors of the noteway in game.\nThis can be useful for streamers if they want to make the noteway invisible.");
            TooltipDictionary.Add(checkBox_SecondaryMonitor, "Check this if you want Rocksmith to run on your second monitor.\nThis will only work in WINDOWED MODE inside of Rocksmith.\nSet Rocksmith to the full resolution of the monitor but keep it in windowed mode.\nYou will still need to set the location in the mod settings (Misc tab)\nPlease also read the tooltip there.");
            TooltipDictionary.Add(checkBox_ModsLog, "Having an issue with a mod?\nNeed to ask the devs a question about a mod turning on / off when it shouldn't?\nTurn this on, and reproduce the issue.\nSend the RSMods devs your \"RSMods_debug.txt\" after you close the game.\nIt is recommended to not leave this on.");
            TooltipDictionary.Add(checkBox_AllowAudioInBackground, "Allows you to listen to Rocksmith with the game in the background.\nThe game will give you about half a second of leeway to alt+tab without opening the pause menu.\nThe best spot to open is when the song starts, as you have around 3-4 seconds.");
            TooltipDictionary.Add(checkBox_BypassTwoRTCMessageBox, "Allows you to have two Real Tone Cables plugged in while playing singleplayer.\nWith this mod disabled, Rocksmith will stop you from doing this.\nIf you are using RS_ASIO v0.5.7, this will always be enabled.");
            TooltipDictionary.Add(checkBox_LinearRiffRepeater, "By default, the speed for Riff Repeater is not linear.\nEnabling this mod will fix that.\nIn standard Rocksmith 2014: 68% speed in Riff Repeater = 50% real speed.\nWith this mod: 68% speed in Riff Repeater = 68% real speed.");
            TooltipDictionary.Add(checkBox_UseAltSampleRate_Output, "Tells Rocksmith to look for headphones / speakers using a sample rate that isn't 48kHz.\nThis can be used to play with bluetooth headphones (there will be latency).\nSupport for this mod is \"as-is\" as we cannot help with every headset / speaker configuration.\nChanges made to this setting won't take effect until you restart Rocksmith.");
            TooltipDictionary.Add(checkBox_EnableLooping, "Allows you to loop sections of songs.\nThis differs from Riff Repeater as we let you pick sections by the amount of time.\nSet two keybindings in the \"Keybindings\" tab.\nOne specifies when the loop should start, and the other when the loop should end.\nPress the key to place the loop, and press Control + key to remove the loop.");
            TooltipDictionary.Add(checkBox_AllowRewind, "Allows you to go back a set number of seconds in a song.\nThis can be useful if you mess up a section, and want to retry it.");
            TooltipDictionary.Add(checkBox_FixOculusCrash, "When you try to open Rocksmith with a Oculus / Meta headset connected to your PC, it typically crashes.\nThis mod tries to avoid the crash by preventing the bad code from running.\nThis may fix other audio-related crashes when Rocksmith opens.\nThis mod must be enabled when you start the game for it to take effect.");
            TooltipDictionary.Add(checkBox_FixBrokenTones, "When you are playing some songs, the tone system may die.\nWhen the tone system dies, you normally have to restart your game to get tones working again.\nThis mod tries to prevent the tone system from dying.\nThis mod must be enabled when you start the game for it to take effect.");
            TooltipDictionary.Add(checkBox_CustomNSPTimer, "The timer between songs in Non-stop play is 10.9 seconds by default.\nA lot of people find this timer to be too long.\nWith this mod, you can change the amount of time between each song (down to 2 seconds due to technical limitations).\nFind the box to change the amount of time in the Automation tab.");

            // Mods
            TooltipDictionary.Add(groupBox_HowToEnumerate, "Choose to Enumerate on key press,\nor automatically scan for changes every X seconds and start enumeration if a new file has been added.");
            TooltipDictionary.Add(groupBox_LoftOffWhen, "Turn the loft off via hotkey, as soon as the game starts up or only when in a song.");
            TooltipDictionary.Add(radio_ER1StringColors, "When ER1 mode is enabled, these are the colors that the strings will be changed to.");
            TooltipDictionary.Add(radio_ER2StringColors, "When ER2 mode is enabled, these are the colors that the strings will be changed to.");
            TooltipDictionary.Add(groupBox_ToggleSkylineWhen, "Turn the skyline (Purple and Orange DD level bars) as soon as the game starts up, or only when in a song.");
            TooltipDictionary.Add(groupBox_ToggleLyricsOffWhen, "How or when do you want the lyric display disabled, always, or toggled by a hotkey only?");
            TooltipDictionary.Add(radio_LyricsAlwaysOff, "Lyrics display will always be disabled in Learn-A-Song game mode.");
            TooltipDictionary.Add(radio_LyricsOffHotkey, "Lyrics can be toggled on or off by a defined hotkey.");
            TooltipDictionary.Add(checkBox_GuitarSpeakWhileTuning, "For Advanced Users Only!\nUse Guitar Speak in tuning menus.\nThis can potentially stop you from tuning, or playing songs if setup improperly.");
            TooltipDictionary.Add(groupBox_MidiAutoTuneDevice, "Select the MIDI device that goes to your drop tuning pedal.\nWe will send a signal to the pedal to try to automatically tune it.");
            TooltipDictionary.Add(checkBox_WhammyFiveChordsMode, "If you are using the Whammy 5 or Whammy Bass.\nAre you using the pedal in Chords Mode or Classic Mode.\nClassic Mode = UnChecked, Chords Mode = Checked.");
            TooltipDictionary.Add(groupBox_OnScreenFont, "If RSMods needs to show text in game, what font should we use?");
            TooltipDictionary.Add(groupBox_AutoLoadProfiles, "If you play with another person, but want to always load into your account this is the place for you.\nThis gets the same benefits of \"Autoload Last Used Profile\" but allows you to pick which profile will always load first");
            TooltipDictionary.Add(listBox_AutoLoadProfiles, "A list of all the profiles you have saved inside of Rocksmith 2014");
            TooltipDictionary.Add(checkBox_BackupProfile, "Everytime you play Rocksmith there is an extremely small chance your save can get corrupted.\nWhen your save gets corrupted, most of the time you can't recover and need to start anew.\nThis mod will create a backup of your save everytime you open this GUI.");
            TooltipDictionary.Add(groupBox_Backups, "If you open the RSMods GUI a lot, and are low on disk space, this is the spot for you.\nThis section allows you to set how many backups we store before we start deleting older backups.\nSet this to 0 to allow us to store as many backups as possible.");
            TooltipDictionary.Add(checkBox_UnlimitedBackups, "If you have an insane amount of disk space, and want to save all your backups, check this box.\nWith this checked, we save every backup when you open RSMods and will never delete them.");
            TooltipDictionary.Add(groupBox_RRSpeed, "Note this interval is what the internal value is set to.\nFor the most control, set the interval to 2.");
            TooltipDictionary.Add(button_SecondaryMonitorStartPos, "PLEASE READ THIS!\nMove this window over to your secondary monitor and make it full screen.\nPress this button when it is full screen so we can save the resolution.\nAfter you press the button you can go back to windowed mode.");
            TooltipDictionary.Add(checkBox_NoteColors_UseRocksmithColors, "Check this box if you want normal Rocksmith colored notes.");
            TooltipDictionary.Add(listBox_MidiAutoTuningOffset, "What tuning is your guitar / bass set to?\nWe can adjust how we auto tune based on what you specify here.\nThis value can be changed in game by setting the \"Change Tuning Offset\" keybind.\nIf you press the \"Change Tuning Offset\" keybind it will go down in tuning.\nIf you hold Control while pressing your \"Change Tuning Offset\" keybind, it will go up in tuning.");
            TooltipDictionary.Add(checkBox_RemoveSongPreviews, "Check this box if you want to turn off the song previews that play when you hover over a song.");
            TooltipDictionary.Add(checkBox_OverrideInputVolume, "Enable this to allow you to turn your guitar or bass up to 11!\nRocksmith sets what volume it wants to listen to your cable at.\nThis mod allows you to bypass that restriction by changing it to whatever you set.");
            TooltipDictionary.Add(listBox_AvailableInputDevices, "This is a list of your available microphones.\nPlease select the one you use in Rocksmith so you can override the maximum volume.");
            TooltipDictionary.Add(nUpDown_OverrideInputVolume, "Set this value from 0-100 to change how loud your guitar is in Rocksmith.\nDefault value in Rocksmith is 17.\nIt is recommended to keep this value below 50.\n0 does not mean no audio, as Rocksmith will bypass the volume if you set it to 0.");

            // Misc
            TooltipDictionary.Add(groupBox_Songlist, "Custom names for the 6 \"SONG LISTS\" shown in game.");
            TooltipDictionary.Add(groupBox_Keybindings_MODS, "Set keybindings for the toggle on / off by keypress modifications.\nYou need to press ENTER after setting the key for it to be saved.");
            TooltipDictionary.Add(groupBox_Keybindings_AUDIO, "Set keybindings for changing the volume in game.\nPress the keybinding to increase the volume.\nPress control and the keybinding to decrease the volume.\nYou need to press ENTER after setting the key for it to be saved.");
            TooltipDictionary.Add(button_ResetModsToDefault, "Resets all RSMods values to defaults");
            TooltipDictionary.Add(button_AutoLoadProfile_ClearSelection, "Clears out profile field of \"Autoload Last Used Profile\" to always open the profile you ended on last play session");
            TooltipDictionary.Add(groupBox_Profiles_RevertBackup, "Use this section if your profile gets corrupted.\nThis will revert to a backup of your profile(s), so some newer data may be lost.\nMake sure to always make backups :)");
            TooltipDictionary.Add(button_Profiles_RevertBackup, "Click this button with a date selected to revert to that backup.\nThe above dates are of all the backups we've taken.");
            TooltipDictionary.Add(listBox_Profiles_ListBackups, "This is a list of all the backups of your profile(s) we've taken.\nWe enable backups by default to lower the risk of \"bricking\" a profile.\nIt is always good to make backups, just in-case the inevitable happens.");
            TooltipDictionary.Add(radio_AutoTuningWhenManual, "We will tell your pedal how to tune when you enter the tuner and press \"DELETE\" to skip tuning! Your guitar or bass will NOT have the altered tuning in the tuner.");
            TooltipDictionary.Add(radio_AutoTuningWhenTuner, "We will tell your pedal how to tune when you enter the tuner. Your guitar or bass WILL have the altered tuning in the tuner.");

            // Set & Forget Mods (Cache.psarc Modifications)
            // Tones
            TooltipDictionary.Add(button_LoadTones, "Step 1.\nClick this to load the tones that are saved in your profile.");
            TooltipDictionary.Add(listBox_ProfileTones, "Step2.\n Highlight a tone name.");
            TooltipDictionary.Add(radio_DefaultRhythmTone, "Set Highlighted Tone As New Default Rhythm Tone.");
            TooltipDictionary.Add(radio_DefaultLeadTone, "Set Highlighted Tone As New Default Lead Tone.");
            TooltipDictionary.Add(radio_DefaultBassTone, "Set Highlighted Tone As New Default Bass Tone.");
            TooltipDictionary.Add(button_AssignNewDefaultTone, "Assign the currently highlighted tone to the chosen path.");

            // Custom Tuning
            TooltipDictionary.Add(listBox_Tunings, "Shows the list of tuning definitions currently in Rocksmith.");
            TooltipDictionary.Add(button_AddTuning, "Adds the tuning as defined above.");
            TooltipDictionary.Add(button_RemoveTuning, "Removes the highlighted tuning.");
            TooltipDictionary.Add(nUpDown_String0, "Set the offset for the low-E string.");
            TooltipDictionary.Add(nUpDown_String1, "Set the offset for the A string.");
            TooltipDictionary.Add(nUpDown_String2, "Set the offset for the D string.");
            TooltipDictionary.Add(nUpDown_String3, "Set the offset for the G string.");
            TooltipDictionary.Add(nUpDown_String4, "Set the offset for the B string.");
            TooltipDictionary.Add(nUpDown_String5, "Set the offset for the high-E string.");
            TooltipDictionary.Add(button_SaveTuningChanges, "Saves the tuning list to Rocksmith.");
            TooltipDictionary.Add(button_LoadSongsToWorkOn, "Loads all the songs in your dlc folder to see what tunings you might need to add.");

            // One Click Mods
            TooltipDictionary.Add(button_AddExitGame, "Replaces UPLAY on the main menu with an EXIT GAME option.");
            TooltipDictionary.Add(button_AddDCInput, "Adds the Direct Connect mode - microphone mode with tone simulations.");
            TooltipDictionary.Add(button_AddCustomTunings, "Adds some preset definitions for the most common Custom Tunings.");
            TooltipDictionary.Add(button_AddFastLoad, "Requires: SSD drive or faster.\nSkips some of the intro sequences.\nThis may cause the game to not launch properly.\nCombined with Auto Load Last Profile and huzzah!");
            TooltipDictionary.Add(button_TurnItUpToEleven, "Makes Rocksmith +6dB louder overall, with an additional +6dB increase to tone volume.");

            // Misc
            TooltipDictionary.Add(button_RemoveTemp, "Removes the temporary files used by RSMods.");
            TooltipDictionary.Add(button_RestoreCacheBackup, "Restores the original cache.psarc file\nUndoes all \"Set-and-forget\" mods.");
            TooltipDictionary.Add(button_CleanUpUnpackedCache, "Removes temporary files and un-packs cache.psarc as it is being used now, again.");
            TooltipDictionary.Add(button_ResetToDefaultCachePsarc, "Woah, hang on there!\nHave you tried pressing the \"Restore Cache Backup\" button?\nThis should be a last resort.\nWe call home to Steam to redownload all modified files.\nThis will only break the mods in this section, nothing else.");
            TooltipDictionary.Add(button_UpdateRSMods, "Update RSMods to the newest version.\nPatch Notes: " + CheckForUpdates_GetPatchNotes());
            TooltipDictionary.Add(checkBox_TurnOffAllMods, "Press this button to turn off all mods but keep your settings saved for later.\nThis can be used if you need to test if RSMods in causing an issue.\nWhen you want to use your mods again, just uncheck this box.");

            // Twitch Bot
            TooltipDictionary.Add(button_TwitchReAuthorize, "Click this to get the authorisation key needed to let these mods listen to your twitch alerts.\nIt is possible this button may need to be clicked to re-anable the triggers.");
            TooltipDictionary.Add(button_SolidNoteColorPicker, "Choose a color for this event trigger.");
            TooltipDictionary.Add(button_SolidNoteColorRandom, "This will choose a random color for you. \nThe color does not change per activation, what you see here is how it is set for good.");
            TooltipDictionary.Add(button_AddSelectedReward, "Add the configured event trigger.");
            TooltipDictionary.Add(button_RemoveReward, "Remove the selected event trigger.");
            TooltipDictionary.Add(button_TestTwitchReward, "Manually activate the mod without needing to have recieved a donation.");
            TooltipDictionary.Add(dgv_DefaultRewards, "Lists the possible events you can use to set a trigger.");
            TooltipDictionary.Add(dgv_EnabledRewards, "Lists the events you have configured - how long they are activated for - and their cost.");
            TooltipDictionary.Add(textBox_TwitchLog, "Shows notifications from Twitch - and what got triggered from these tools.");
            TooltipDictionary.Add(label_TwitchAuthorized, "Please take care to make sure none of these entries are shown on your stream.");
            TooltipDictionary.Add(label_TwitchUsername, "Please take care to make sure none of these entries are shown on your stream.");
            TooltipDictionary.Add(label_TwitchChannelID, "Please take care to make sure none of these entries are shown on your stream.");
            TooltipDictionary.Add(label_TwitchAccessTokenVal, "Please make sure this value is never shown live.\nClick to copy this to your clipboard.\nThis value is needed when asking for Twitch support from the RSMods devs.");
            TooltipDictionary.Add(label_TwitchUsernameVal, "Please make sure this value is never shown live.\nClick to copy this to your clipboard.\nThis value is needed when asking for Twitch support from the RSMods devs.");
            TooltipDictionary.Add(label_TwitchChannelIDVal, "Please make sure this value is never shown live.\nClick to copy this to your clipboard.\nThis value is needed when asking for Twitch support from the RSMods devs.");
            TooltipDictionary.Add(checkBox_RevealTwitchAuthToken, "Only reveal this when asked by RSMods developers.\nThis is how we look to see when events happen in your stream.");

            // Custom Noteway Colors
            TooltipDictionary.Add(button_ChangeNumberedFrets, "This will change the color of all numbered frets in game.\n3, 5, 7, 9, 12, 15, 17, 19, 21, 24.\nThis needs to be paired with Change UnNumbered Frets or it won't work!");
            TooltipDictionary.Add(button_ChangeUnNumberedFrets, "This will change the color of all NON-numbered frets in game.\nThis needs to be paired with Change Numbered Frets or it won't work!");
            TooltipDictionary.Add(button_ChangeNotewayGutter, "This will change what the sides of the noteway are colored with.");
            TooltipDictionary.Add(button_ChangeFretNumber, "This will change the color of the fret numbers that show up on the noteway.\nThis doesn't change the orange note numbers!");
            TooltipDictionary.Add(groupBox_CustomHighway, "These colors are just the \"Base\" colors.\nRocksmith tampers with these colors.\nAim for a lighter color to try to counteract the tampering");

            // RS_ASIO
            TooltipDictionary.Add(groupBox_ASIO_Output, "If your headphones support ASIO, this is where you would manage them.");
            TooltipDictionary.Add(groupBox_ASIO_Input0, "If you use an audio interface, this is where you would manage it.\nThis is meant for the Player 1 \"Cable\"");
            TooltipDictionary.Add(groupBox_ASIO_Input1, "If you use an audio interface, this is where you would manage it.\nThis is meant for the Player 2 \"Cable\"");
            TooltipDictionary.Add(groupBox_ASIO_InputMic, "This section requires RS_ASIO version 0.5.5 or greater to work.\nIf you use an audio interface, this is where you would manage it.\nThis is meant for singing in game.");
            TooltipDictionary.Add(groupBox_ASIO_BufferSize, "Use this to box to change how much latency there is.\nThe lower you go, you have a higher chance of getting crackling audio.");
            TooltipDictionary.Add(listBox_AvailableASIODevices_Output, "A list of all connected ASIO devices.\nClick to save the selected device as your Output device (headphones)");
            TooltipDictionary.Add(listBox_AvailableASIODevices_Input0, "A list of all connected ASIO devices.\nClick to save the selected device as your Input0 device (guitar / bass | Player 1)");
            TooltipDictionary.Add(listBox_AvailableASIODevices_Input1, "A list of all connected ASIO devices.\nClick to save the selected device as your Input1 device (guitar / bass | Player 2)");
            TooltipDictionary.Add(listBox_AvailableASIODevices_InputMic, "A list of all connected ASIO devices.\nClick to save the selected device as your InputMic device (Singing)");
            TooltipDictionary.Add(radio_ASIO_BufferSize_Driver, "Respect the buffer size setting set by your device");
            TooltipDictionary.Add(radio_ASIO_BufferSize_Host, "Respect the buffer size setting of Rocksmith");
            TooltipDictionary.Add(radio_ASIO_BufferSize_Custom, "Respect the buffer size in the Custom Buffer Size field");
            TooltipDictionary.Add(label_ASIO_CustomBufferSize, "The lower this value goes, the lower the latency.\nWhile bringing down the latency, you have a higher chance of crackling audio.\nTry to find the sweet spot.");
            TooltipDictionary.Add(checkBox_ASIO_ASIO, "This is the main reason people use RS_ASIO.\nEnable this if you have an audio interface to potentially lower latency.");
            TooltipDictionary.Add(checkBox_ASIO_WASAPI_Input, "Enable this if you want to play with a USB cable AND your audio interface in multiplayer");
            TooltipDictionary.Add(checkBox_ASIO_WASAPI_Output, "Enable this if you have headphones that don't go through your audio interface.");
            TooltipDictionary.Add(checkBox_ASIO_Output_ControlEndpointVolume, "The EndpointVolume API enables specialized clients to control\nand monitor the volume levels of audio endpoint devices.");
            TooltipDictionary.Add(checkBox_ASIO_Input0_ControlEndpointVolume, "The EndpointVolume API enables specialized clients to control\nand monitor the volume levels of audio endpoint devices.");
            TooltipDictionary.Add(checkBox_ASIO_Input1_ControlEndpointVolume, "The EndpointVolume API enables specialized clients to control\nand monitor the volume levels of audio endpoint devices.");
            TooltipDictionary.Add(checkBox_ASIO_InputMic_ControlEndpointVolume, "The EndpointVolume API enables specialized clients to control\nand monitor the volume levels of audio endpoint devices.");
            TooltipDictionary.Add(label_ASIO_Input0_Channel, "This is what channel we look for audio on.\nA good way to find this value is to see how many inputs are before and subtract 1.\nEx: My cable is plugged into the 2nd input, so my channel is 1.");
            TooltipDictionary.Add(label_ASIO_Input1_Channel, "This is what channel we look for audio on.\nA good way to find this value is to see how many inputs are before and subtract 1.\nEx: My cable is plugged into the 2nd input, so my channel is 1.");
            TooltipDictionary.Add(label_ASIO_InputMic_Channel, "This is what channel we look for audio on.\nA good way to find this value is to see how many inputs are before and subtract 1.\nEx: My cable is plugged into the 2nd input, so my channel is 1.");
            TooltipDictionary.Add(label_ASIO_Output_BaseChannel, "This is what channel we want to send the audio to.\nA good way to find this value is to see how many outputs are before and subtract 1.\nEx: My headphones is plugged into the 2nd output, so my base channel is 1.");
            TooltipDictionary.Add(label_ASIO_Output_AltBaseChannel, "Requires RS_ASIO v0.5.6 or greater to work.\nThis is what channel we want to send mirrored audio to.\nA good way to find this value is to see how many outputs are before and subtract 1.\nEx: My headphones is plugged into the 2nd output, so my base channel is 1.");

            // Rocksmith Settings
            TooltipDictionary.Add(checkBox_Rocksmith_EnableMicrophone, "Check this box to enable singing.");
            TooltipDictionary.Add(checkBox_Rocksmith_ExclusiveMode, "Check this box to give Rocksmith 2014 exclusive control of PC Audio.\nThis will cause Rocksmith to take over all audio, but will cause latency if turned off.");
            TooltipDictionary.Add(label_Rocksmith_LatencyBuffer, "This value allows you to adjust the number of audio buffers used in one area of the Rocksmith 2014 audio engine.\nA smaller value will use fewer buffers.\nFewer buffers mean lower latency, but increase the demands on your PC to avoid audio crackling.");
            TooltipDictionary.Add(checkBox_Rocksmith_ForceWDM, "Check this box if you've tried the fine tuning configuration options and still cannot get good audio latency or have audio issues you cannot resolve.\nThis will force the game to use the previous Windows mechanism to control your audio devices.\nIt can impose higher latency than the default system, but is a good fallback.");
            TooltipDictionary.Add(checkBox_Rocksmith_ForceDirextXSink, "Check this box if you've tried all other configuration options and still cannot get good audio.\nThis forces the game to use an old Windows mechanism to control your audio devices.\nIt will almost always impose high latency, but should allow you to run the game.\nUse this as your last option.");
            TooltipDictionary.Add(checkBox_Rocksmith_DumpAudioLog, "Check this box if you need to send debugging information to Ubisoft.\nIt will create a text file called audiodump.txt, located in the same directory as the Rocksmith application.\nHaving this value on will hurt performance so leave it unchecked for normal gameplay.");
            TooltipDictionary.Add(label_Rocksmith_MaxOutputBuffer, "A few audio devices have been found to have very large output buffers.\nIn this case, the game does its best to choose a reliable audio buffer size.\nHowever, you might find that setting this variable will help to resolve audio issues.\nIn its default setting of 0, it leaves the configuration of this value up to Rocksmith.\nMost audio cards end up using an audio buffer size of 1024.\nFast PCs can usually run with this at 512.\nIf you have disabled Exclusive Mode, you may need to use a higher setting for this.\nWe haven’t run into any specific issues caused by the choice of values for MaxOutputBufferSize, but you may have better luck using multiples of 8 or 32.");
            TooltipDictionary.Add(checkBox_Rocksmith_RTCOnly, "Check this box to only allow official Ubisoft cables to work.\nThis will prevent Rocksmith from hijacking your microphone while you're playing.");
            TooltipDictionary.Add(checkBox_Rocksmith_LowLatencyMode, "Uncheck this box if you’re having trouble getting the game to have good audio performance.\nThis will drop the game back to the original Rocksmith audio settings and may resolve some audio crackling issues at the cost of some of the Rocksmith 2014 latency improvements.");
            TooltipDictionary.Add(checkBox_Rocksmith_GamepadUI, "Check this box to show the controller UI in game.");
            TooltipDictionary.Add(label_Rocksmith_ScreenWidth, "Screen horizontal resolution, in pixels.");
            TooltipDictionary.Add(label_Rocksmith_ScreenHeight, "Screen vertical resolution, in pixels.");
            TooltipDictionary.Add(groupBox_Rocksmith_Fullscreen, "Check whether you want Rocksmith to run in windowed, non-exclusive fullscreen, or exclusive fullscreen.");
            TooltipDictionary.Add(radio_Rocksmith_NonExclusiveFullScreen, "Non-exclusive mode is helpful if you have multiple monitors and want Rocksmith 2014 to be fullscreen on one monitor and other windows available on your second monitor.");
            TooltipDictionary.Add(groupBox_Rocksmith_VisualQuality, "Set this value to reflect the visual quality setting you’d like to use.");
            TooltipDictionary.Add(label_Rocksmith_RenderWidth, "Set this value to whatever width you want to the game to render at, in pixels.\nSet it to 0 to override this effect.");
            TooltipDictionary.Add(label_Rocksmith_RenderHeight, "Set this value to whatever height you want to the game to render at, in pixels.\nSet it to 0 to override this effect.");
            TooltipDictionary.Add(checkBox_Rocksmith_PostEffects, "Check this box to enable Bloom, Glow, and Color Correction.");
            TooltipDictionary.Add(checkBox_Rocksmith_Shadows, "Check this box to enable realtime shadows.");
            TooltipDictionary.Add(checkBox_Rocksmith_HighResScope, "Check this box to show the high resolution audio visualizer in game.");
            TooltipDictionary.Add(checkBox_Rocksmith_DepthOfField, "Check this box to enable the Depth of Field effect.");
            TooltipDictionary.Add(checkBox_Rocksmith_PerPixelLighting, "Check this box to color every pixel accurately.");
            TooltipDictionary.Add(checkBox_Rocksmith_MSAASamples, "Check this box if you want to use anti-aliasing.");
            TooltipDictionary.Add(checkBox_Rocksmith_DisableBrowser, "Check this box to stop Rocksmith from opening their website every two weeks.");
            TooltipDictionary.Add(checkBox_Rocksmith_UseProxy, "Use a proxy to connect to the Rocksmith servers?\nReal usage is unknown.");

            // Soundpacks
            TooltipDictionary.Add(button_UnpackAudioPsarc, "This mod requires editing the game's audio file.\nWe have to extract every file which can take a lot of space.");
            TooltipDictionary.Add(button_ResetSoundpack, "If you don't want your custom sounds anymore, click this button and we will reset them back to the default");
            TooltipDictionary.Add(button_ImportSoundPack, "Did a friend give you their soundpack?\nIf so, click this button and point to the file to add all their custom sounds.");
            TooltipDictionary.Add(button_ExportSoundPack, "Want to share your soundpack with your friends?\nIf so, click this button and make a file that you can share with them.\nTell them to open RSMods and go to this menu and click the \"Import Soundpack\" button");
            TooltipDictionary.Add(button_RepackAudioPsarc, "This mod requires editing the game's audio file.\nWe have to put all your sounds into that file or they won't be heard.\nThis will take a couple minutes.\nDon't close the application if it hangs, as it's working with a major game file");
            TooltipDictionary.Add(button_RemoveUnpackedAudioPsarc, "If you are done messing with your sounds, click this button and we'll remove our copy of your game's audio file.\nNote: You will have to unpack the file again if you want to change any sounds");
        }
        #endregion
        #region Fill Color Textboxes
        public Dictionary<int, TextBox> stringNumberToColorTextBox = new Dictionary<int, TextBox>(){}; // Can't put variables into it until after we create it.
        private void StringColors_FillStringNumberToColorDictionary()
        {
            stringNumberToColorTextBox.Clear();

            stringNumberToColorTextBox.Add(0, textBox_String0Color);
            stringNumberToColorTextBox.Add(1, textBox_String1Color);
            stringNumberToColorTextBox.Add(2, textBox_String2Color);
            stringNumberToColorTextBox.Add(3, textBox_String3Color);
            stringNumberToColorTextBox.Add(4, textBox_String4Color);
            stringNumberToColorTextBox.Add(5, textBox_String5Color);
        }

        public Dictionary<Control, Control> notewayButtonToColorTextbox = new Dictionary<Control, Control>() {};

        private void NotewayColors_FillNotewayButtonToColorDictionary()
        {
            notewayButtonToColorTextbox.Clear();

            notewayButtonToColorTextbox.Add(button_ChangeNumberedFrets, textBox_ShowNumberedFrets);
            notewayButtonToColorTextbox.Add(button_ChangeUnNumberedFrets, textBox_ShowUnNumberedFrets);
            notewayButtonToColorTextbox.Add(button_ChangeNotewayGutter, textBox_ShowNotewayGutter);
            notewayButtonToColorTextbox.Add(button_ChangeFretNumber, textBox_ShowFretNumber);
        }

        private void StringColors_FillNoteNumberToColorDictionary()
        {
            stringNumberToColorTextBox.Clear();

            stringNumberToColorTextBox.Add(0, textBox_Note0Color);
            stringNumberToColorTextBox.Add(1, textBox_Note1Color);
            stringNumberToColorTextBox.Add(2, textBox_Note2Color);
            stringNumberToColorTextBox.Add(3, textBox_Note3Color);
            stringNumberToColorTextBox.Add(4, textBox_Note4Color);
            stringNumberToColorTextBox.Add(5, textBox_Note5Color);
        }
        #endregion
    };

    class Dictionaries
    {
        #region Guitar Speak
        public static Dictionary<string, string> GuitarSpeakKeyPressDictionary = new Dictionary<string, string>()
        {
            {"Delete", ReadSettings.GuitarSpeakDeleteIdentifier},
            {"Space", ReadSettings.GuitarSpeakSpaceIdentifier},
            {"Enter", ReadSettings.GuitarSpeakEnterIdentifier},
            {"Tab", ReadSettings.GuitarSpeakTabIdentifier},
            {"Page Up", ReadSettings.GuitarSpeakPGUPIdentifier},
            {"Page Down", ReadSettings.GuitarSpeakPGDNIdentifier},
            {"Up Arrow", ReadSettings.GuitarSpeakUPIdentifier},
            {"Down Arrow", ReadSettings.GuitarSpeakDNIdentifier},
            {"Escape", ReadSettings.GuitarSpeakESCIdentifier},
            {"Open Bracket", ReadSettings.GuitarSpeakOBracketIdentifier},
            {"Close Bracket", ReadSettings.GuitarSpeakCBracketIdentifier},
            {"Tilde / Tilda", ReadSettings.GuitarSpeakTildeaIdentifier},
            {"Forward Slash", ReadSettings.GuitarSpeakForSlashIdentifier},
            {"Alt", ReadSettings.GuitarSpeakAltIdentifier},
            {"Close Guitar Speak", ReadSettings.GuitarSpeakCloseIdentifier}
        };

        public static List<string> GuitarSpeakIndexToINISetting = new List<string>()
        {
            ReadSettings.GuitarSpeakDeleteIdentifier,
            ReadSettings.GuitarSpeakSpaceIdentifier,
            ReadSettings.GuitarSpeakEnterIdentifier,
            ReadSettings.GuitarSpeakTabIdentifier,
            ReadSettings.GuitarSpeakPGUPIdentifier,
            ReadSettings.GuitarSpeakPGDNIdentifier,
            ReadSettings.GuitarSpeakUPIdentifier,
            ReadSettings.GuitarSpeakDNIdentifier,
            ReadSettings.GuitarSpeakESCIdentifier,
            ReadSettings.GuitarSpeakOBracketIdentifier,
            ReadSettings.GuitarSpeakCBracketIdentifier,
            ReadSettings.GuitarSpeakTildeaIdentifier,
            ReadSettings.GuitarSpeakForSlashIdentifier,
            ReadSettings.GuitarSpeakAltIdentifier,
            ReadSettings.GuitarSpeakCloseIdentifier
        };

        public static Dictionary<string, string> GuitarSpeakPresetDictionary = new Dictionary<string, string>();

        public static Dictionary<string, string> RefreshGuitarSpeakPresets()
        {
            GuitarSpeakPresetDictionary.Clear();

            GuitarSpeakPresetDictionary.Add("Delete: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakDeleteIdentifier)));
            GuitarSpeakPresetDictionary.Add("Space: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakSpaceIdentifier)));
            GuitarSpeakPresetDictionary.Add("Enter: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakEnterIdentifier)));
            GuitarSpeakPresetDictionary.Add("Tab: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakTabIdentifier)));
            GuitarSpeakPresetDictionary.Add("Page Up: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakPGUPIdentifier)));
            GuitarSpeakPresetDictionary.Add("Page Down: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakPGDNIdentifier)));
            GuitarSpeakPresetDictionary.Add("Up Arrow: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakUPIdentifier)));
            GuitarSpeakPresetDictionary.Add("Down Arrow: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakDNIdentifier)));
            GuitarSpeakPresetDictionary.Add("Escape: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakESCIdentifier)));
            GuitarSpeakPresetDictionary.Add("Open Bracket: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakOBracketIdentifier)));
            GuitarSpeakPresetDictionary.Add("Close Bracket: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakCBracketIdentifier)));
            GuitarSpeakPresetDictionary.Add("Tilde / Tilda: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakTildeaIdentifier)));
            GuitarSpeakPresetDictionary.Add("Forward Slash: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakForSlashIdentifier)));
            GuitarSpeakPresetDictionary.Add("Alt: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakAltIdentifier)));
            GuitarSpeakPresetDictionary.Add("Close Guitar Speak: ", GuitarSpeak.GuitarSpeakNoteOctaveMath(ReadSettings.ProcessSettings(ReadSettings.GuitarSpeakCloseIdentifier)));
            return GuitarSpeakPresetDictionary;
        }
        #endregion
        #region Index To Identifier

        public static List<string> SongListIndexToINISetting = new List<string>()
        {
            ReadSettings.Songlist1Identifier,
            ReadSettings.Songlist2Identifier,
            ReadSettings.Songlist3Identifier,
            ReadSettings.Songlist4Identifier,
            ReadSettings.Songlist5Identifier,
            ReadSettings.Songlist6Identifier
        };

        public static List<string> KeybindingsIndexToINISetting = new List<string>()
        {
            ReadSettings.ToggleLoftIdentifier,
            ReadSettings.ShowSongTimerIdentifier,
            ReadSettings.ForceReEnumerationIdentifier,
            ReadSettings.RainbowStringsIdentifier,
            ReadSettings.RainbowNotesIdentifier,
            ReadSettings.RemoveLyricsKeyIdentifier,
            ReadSettings.RRSpeedKeyIdentifier,
            ReadSettings.TuningOffsetKeyIdentifier,
            ReadSettings.ToggleExtendedRangeKeyIdentifier,
            ReadSettings.LoopStartKeyIdentifier,
            ReadSettings.LoopEndKeyIdentifier,
            ReadSettings.RewindKeyIdentifier
        };

        public static List<string> AudioKeybindingsIndexToINISetting = new List<string>()
        {
            ReadSettings.MasterVolumeKeyIdentifier,
            ReadSettings.SongVolumeKeyIdentifier,
            ReadSettings.Player1VolumeKeyIdentifier,
            ReadSettings.Player2VolumeKeyIdentifier,
            ReadSettings.MicrophoneVolumeKeyIdentifier,
            ReadSettings.VoiceOverVolumeKeyIdentifier,
            ReadSettings.SFXVolumeKeyIdentifier,
            ReadSettings.DisplayMixerKeyIdentifier,
            ReadSettings.MutePlayer1KeyIdentifier,
            ReadSettings.MutePlayer2KeyIdentifier
        };
        #endregion
        #region VoiceOver

        public static Dictionary<string, string> ResultVoiceOverDictionary = new Dictionary<string, string>()
        {
            { "Bad Performance", "play_VO_RESULTSSCREEN2_001_15NARRATOR_DRY_26312" },
            { "Disappointing Performance", "play_VO_RESULTSSCREEN2_001_13NARRATOR_DRY_26310" },
            { "Subpar Performance", "play_VO_RESULTSSCREEN2_001_17NARRATOR_DRY_26314" },
            { "Could Be Better", "play_VO_RESULTSSCREEN2_001_22NARRATOR_DRY_26319" },
            { "Decent Performance", "play_VO_RESULTSSCREEN_001_31NARRATOR_DRY_26288" },
            { "Alright Performance", "play_VO_RESULTSSCREEN_001_04NARRATOR_DRY_26328" },
            { "Excellent Performance", "play_VO_RESULTSSCREEN_001_22NARRATOR_DRY_26344" },
            { "Top Notch Performance", "play_VO_RESULTSSCREEN2_001_24NARRATOR_DRY_26322" },
            { "Superb Performance", "play_VO_RESULTSSCREEN2_001_30NARRATOR_DRY_26352" },
            { "Dazzling Performance", "play_VO_RESULTSSCREEN_001_13NARRATOR_DRY_26337" },
            { "You\'re Gonna Be A Superstar", "play_VO_RESULTSSCREEN_001_39NARRATOR_DRY_26296" },
            { "Wonderful Performance", "play_VO_RESULTSSCREEN_001_19NARRATOR_DRY_26341" },
            { "Exceptional Performance", "play_VO_RESULTSSCREEN_001_23NARRATOR_DRY_26345" },
            { "Amazing Performance", "play_VO_RESULTSSCREEN2_001_29NARRATOR_DRY_26351" },
            { "Exemplary Performance", "play_VO_RESULTSSCREEN_001_26NARRATOR_DRY_26348" },
            { "(98%) Masterful Performance", "play_VO_RESULTSSCREEN2_001_27NARRATOR_DRY_26324" },
            { "(99%) Masterful Performance", "play_VO_RESULTSSCREEN_001_20NARRATOR_DRY_26342" },
            { "Flawless Performance", "play_VO_RESULTSSCREEN_001_12NARRATOR_DRY_26336" }
        };

        #endregion
        #region Colors

        public static Dictionary<int, Dictionary<string, string>> stringColorButtonsToSettingIdentifiers = new Dictionary<int, Dictionary<string, string>>()
        {
            { 0, new Dictionary<string, string> { // Normal Colors
            
                {"E String", ReadSettings.String0Color_N_Identifier},
                {"A String", ReadSettings.String1Color_N_Identifier},
                {"D String", ReadSettings.String2Color_N_Identifier},
                {"G String", ReadSettings.String3Color_N_Identifier},
                {"B String", ReadSettings.String4Color_N_Identifier},
                {"e String", ReadSettings.String5Color_N_Identifier}
            }},

            { 1,  new Dictionary<string, string> { // ER 1 Colors
            
                {"E String", ReadSettings.String0Color_ER1_Identifier},
                {"A String", ReadSettings.String1Color_ER1_Identifier},
                {"D String", ReadSettings.String2Color_ER1_Identifier},
                {"G String", ReadSettings.String3Color_ER1_Identifier},
                {"B String", ReadSettings.String4Color_ER1_Identifier},
                {"e String", ReadSettings.String5Color_ER1_Identifier}
            }},

            { 2,  new Dictionary<string, string> { // ER 2 Colors
            
                {"E String", ReadSettings.String0Color_ER2_Identifier},
                {"A String", ReadSettings.String1Color_ER2_Identifier},
                {"D String", ReadSettings.String2Color_ER2_Identifier},
                {"G String", ReadSettings.String3Color_ER2_Identifier},
                {"B String", ReadSettings.String4Color_ER2_Identifier},
                {"e String", ReadSettings.String5Color_ER2_Identifier}
            }}
        };

        public static Dictionary<bool, Dictionary<string, string>> noteColorButtonsToSettingIdentifiers = new Dictionary<bool, Dictionary<string, string>>()
        {
            { true, new Dictionary<string, string> { // Normal Colors
            
                {"E String", ReadSettings.Note0Color_N_Identifier},
                {"A String", ReadSettings.Note1Color_N_Identifier},
                {"D String", ReadSettings.Note2Color_N_Identifier},
                {"G String", ReadSettings.Note3Color_N_Identifier},
                {"B String", ReadSettings.Note4Color_N_Identifier},
                {"e String", ReadSettings.Note5Color_N_Identifier}
            }},

            { false,  new Dictionary<string, string> { // Colorblind Colors
            
                {"E String", ReadSettings.Note0Color_CB_Identifier},
                {"A String", ReadSettings.Note1Color_CB_Identifier},
                {"D String", ReadSettings.Note2Color_CB_Identifier},
                {"G String", ReadSettings.Note3Color_CB_Identifier},
                {"B String", ReadSettings.Note4Color_CB_Identifier},
                {"e String", ReadSettings.Note5Color_CB_Identifier}
            }}
        };

        public static Dictionary<string, string> notewayColorButtonsToSettingIdentifier = new Dictionary<string, string>()
        {
            {"Change Numbered Frets", ReadSettings.CustomHighwayNumberedIdentifier },
            {"Change UnNumbered Frets", ReadSettings.CustomHighwayUnNumberedIdentifier },
            {"Change Noteway Sides", ReadSettings.CustomHighwayGutterIdentifier},
            {"Change Fret Number", ReadSettings.CustomFretNubmersIdentifier },
        };

        #endregion
        #region Current Keybind Mod Names
        public static List<string> currentModKeypressList = new List<string>()
        {
            "Toggle Loft",
            "Show Song Timer",
            "Force ReEnumeration",
            "Rainbow Strings",
            "Rainbow Notes",
            "Remove Lyrics",
            "RR Speed Change",
            "Change Tuning Offset",
            "Toggle Extended Range",
            "Start Loop",
            "End Loop",
            "Rewind Song"
        };

        public static List<string> currentAudioKeypressList = new List<string>()
        {
            "Master Volume",
            "Song Volume",
            "Player 1 Volume",
            "Player 2 Volume",
            "Microphone Volume",
            "Voice-Over Volume",
            "SFX Volume",
            "Display Mixer",
            "Mute / Unmute Player 1",
            "Mute / Unmute Player 2"
        };
        #endregion
        #region Refresh Lists
        public static List<string> songlists = new List<string>();
        public static List<string> savedKeysForModToggles = new List<string>();
        public static List<string> savedKeysForVolumes = new List<string>();

        public static List<string> refreshKeybindingList()
        {
            savedKeysForModToggles.Clear();

            foreach(string setting in KeybindingsIndexToINISetting)
            {
                savedKeysForModToggles.Add(KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(setting)));
            }

            return savedKeysForModToggles;
        }

        public static List<string> refreshSonglists()
        {
            songlists.Clear();

            // Is this the users first time opening the GUI?
            if (ReadSettings.ProcessSettings(ReadSettings.Songlist1Identifier) == string.Empty)
            {
                for (int i = 0; i < 6; i++)
                {
                    songlists.Add($"Define Song List {i + 1} Here");
                }
            }
            else
            {
                foreach(string setting in SongListIndexToINISetting)
                {
                    songlists.Add(ReadSettings.ProcessSettings(setting));
                }
            }
           
            return songlists;
        }

        public static List<string> refreshAudioKeybindingList()
        {
            savedKeysForVolumes.Clear();

            foreach(string setting in AudioKeybindingsIndexToINISetting)
            {
                savedKeysForVolumes.Add(KeyConversion.VKeyToUI(ReadSettings.ProcessSettings(setting)));
            }

            return savedKeysForVolumes;
        }
        #endregion
    }
}
