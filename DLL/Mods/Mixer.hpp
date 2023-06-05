#include <chrono>
#include <string>
#include <vector>

// Volume adjustment mod
inline bool displayMixer = false;
inline bool displayCurrentVolume = false;
inline auto displayVolumeStartTime = std::chrono::steady_clock::time_point(); // Defaults to epoch time
inline unsigned int currentVolumeIndex = 0; // Mixer volume to change. 0 - Master, 1 - Song, 2 - P1, 3 - P2, 4 - Mic, 5 - VO, 6 - SFX

inline std::vector<std::string> mixerInternalNames = { // Needs to be char* as that's what SetRTPCValue needs.
		{"Master_Volume"}, // Master Volume
		{"Mixer_Music"}, // Song Volume
		{"Mixer_Player1"}, // Player 1 Guitar & Bass (both are handled with this singular name)
		{"Mixer_Player2"}, // Player 2 Guitar & Bass (both are handled with this singular name)
		{"Mixer_Mic"}, // My Microphone Volume
		{"Mixer_VO"}, // Rocksmith Dad Voice Over
		{"Mixer_SFX"}, // Menu SFX Volume
};

inline std::vector<std::string> drawMixerTextName = {
	{"Master Volume: "},
	{"Song Volume: "},
	{"Player 1 Volume: "},
	{"Player 2 Volume: "},
	{"Microphone Volume: "},
	{"Voice-Over Volume: "},
	{"SFX Volume: "},
};