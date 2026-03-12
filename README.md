# Simple Triggers

## About
This plugin aims to provide a simple means of creating in-game responses to chat messages.
Responses can be done in one of the below methods or any combination of the three:
* Chat Message
* In-Game SFX
* Text-to-Speech 
  * Kokoro (recommended)
  * Windows System

New Triggers can be created by either clicking the "Add New" button on the Triggers tab or saving a chat log message on the Chat History tab.
Chat logging is disabled by default every time the plugin starts, and once enabled you can search for specific messages it has recorded. This may be useful if you're unsure of the exact message you want to create a trigger for but only know some of the words. You can also configure how many messages should be stored at a time.
Message history is never saved outside of the plugin running and is lost when the plugin is disabled.

At this point in time, the suggested method for Text-to-Speech responses is to use Kokoro. If you're on Windows you will have the option using the System voice. Windows System TTS isn't fully implemented yet.
Kokoro may also have slight performance hits but in my personal testing it was barely noticeable (3-5 FPS hitches).

* Commands: `/simpletriggers` or `/strig`
  * Optional arguments: `enable`/`on` or `disable`/`off` to activate or deactivate the whole trigger system.

### Images
![](https://i.imgur.com/q1UmdHN.png) ![](https://i.imgur.com/MDOUnRy.png)


## Known Issues
* Kokoro may cause minor hitching. In my testing, this was near negligible (3-5 FPS). It seems to be related to playing the sound wave. Needs further investigation.
* There seems to be a ~20 MB memory leak with Kokoro during destruction. Unclear of the root cause. If you find out please let me know!

## Credits
* Kokoro C# package from [Lyrcaxis/KokoroSharp](https://github.com/Lyrcaxis/KokoroSharp)
  * Original model: [Kokoro TTS](https://huggingface.co/hexgrad/Kokoro-82M)
* Sound Effects functions from [Ottermandias/ChatAlerts](https://github.com/Ottermandias/ChatAlerts)
* Pre-Built English IPA Dictionary from [open-dict-data/ipa-dict](https://github.com/open-dict-data/ipa-dict)
* Text-to-IPA from [qkmaxware/CsPhonetics](https://github.com/qkmaxware/CsPhonetics) but modified for this project.