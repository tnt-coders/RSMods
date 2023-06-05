#include "Windows.h"
#include <string>
#include <mmdeviceapi.h>
#include <endpointvolume.h>
#include <Functiondiscoverykeys_devpkey.h>

#include "../Log.hpp"
#include "../MemHelpers.hpp"
#include "../Offsets.hpp"
#include "../MemUtil.hpp"
#include "../Wwise/SoundEngine.hpp"


namespace VolumeControl {
	void SetVolume(float volume, std::string mixerSetting);
	void IncreaseVolume(int amountToIncrease, std::string mixerToIncrease);
	void DecreaseVolume(int amountToDecrease, std::string mixerToDecrease);
	void MutePlayer(bool player2 = false);
	void UnmutePlayer(bool player2 = false);
	void DisableSongPreviewAudio();
	void EnableSongPreviewAudio();
	void AllowAltTabbingWithAudio();
	void DisableAltTabbingWithAudio();
	inline bool disabledSongPreviewAudio = false;
	inline bool allowedAltTabbingWithAudio = false;

	inline bool player1Muted = false;
	inline bool player2Muted = false;
}

inline float player1VolumeBeforeMute = 100.f;
inline float player2VolumeBeforeMute = 100.f;


inline std::vector<std::string> mixerNames = {
	{"Master_Volume"}, // Master Volume
	{"Mixer_Music"}, // Song Volume
	{"Mixer_Player1"}, // Player 1 Guitar & Bass (both are handled with this singular name)
	{"Mixer_Player2"}, // Player 2 Guitar & Bass (both are handled with this singular name)
	{"Mixer_Mic"}, // My Microphone Volume
	{"Mixer_VO"}, // Rocksmith Dad Voice Over
	{"Mixer_SFX"}, // Menu SFX Volume
};