# Simple Triggers
![Version](https://img.shields.io/github/v/release/Brolijah/SimpleTriggers?label=version)
![Downloads](https://img.shields.io/github/downloads/Brolijah/SimpleTriggers/total)
![Lines of Code](https://aschey.tech/tokei/github/Brolijah/SimpleTriggers?category=code)
![Last Commit](https://img.shields.io/github/last-commit/Brolijah/SimpleTriggers)

## About
This plugin aims to provide a simple means of creating in-game responses to chat messages.
Responses can be done in one of the below methods or any combination of the three:
* Chat Message
* In-Game SFX
* Text-to-Speech 
  * Kokoro
  * Windows System

New Triggers can be created by either clicking the "Add New" button on the Triggers tab or saving a chat log message on the Chat History tab. You can also import triggers that are exported via the plugin.
Chat logging is disabled by default every time the plugin starts, and once enabled you can search for specific messages it has recorded. This may be useful if you're unsure of the exact message you want to create a trigger for but only know some of the words. You can also configure how many messages should be stored at a time.
Message history is never saved outside of the plugin running and is lost when the plugin is disabled.

* Commands:
  * `/simpletriggers` or `/strig`
    * `enable`/`on` or `disable`/`off` to activate or deactivate the whole trigger system.
    * `speak <phrase>` will read aloud the requested phrase using your configured TTS.
  * `/stspeak <phrase>` same as the above speak argument, just as a shorter command.


### Images
![](https://i.imgur.com/KuZnQRM.png) ![](https://i.imgur.com/MDOUnRy.png)


## Known Issues
* With Kokoro, if espeak is disabled, it only supports speaking in English. I would like to remedy this in the future by loading other phonetic dictionaries. Or, alternatively, if KokoroSharp removes its dependency on espeak, it *may* just work in a future update.
* Kokoro may cause minor hitching. In my testing, this was near negligible (3-5 FPS). It seems to be related to playing the sound wave. Needs further investigation.

## Credits
* Kokoro C# package from [Lyrcaxis/KokoroSharp](https://github.com/Lyrcaxis/KokoroSharp)
  * Original model: [Kokoro TTS](https://huggingface.co/hexgrad/Kokoro-82M)
* Sound Effects functions from [Ottermandias/ChatAlerts](https://github.com/Ottermandias/ChatAlerts)
* Pre-Built English IPA Dictionary from [open-dict-data/ipa-dict](https://github.com/open-dict-data/ipa-dict)
* Text-to-IPA from [qkmaxware/CsPhonetics](https://github.com/qkmaxware/CsPhonetics) but modified for this project.