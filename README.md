# Simple Triggers
[![Version](https://img.shields.io/github/v/release/Brolijah/SimpleTriggers)](https://github.com/Brolijah/SimpleTriggers/releases)
[![Downloads](https://img.shields.io/endpoint?url=https://qzysathwfhebdai6xgauhz4q7m0mzmrf.lambda-url.us-east-1.on.aws/SimpleTriggers)](https://github.com/Brolijah/SimpleTriggers)
![Lines of Code](https://aschey.tech/tokei/github/Brolijah/SimpleTriggers?category=code)
![Last Commit](https://img.shields.io/github/last-commit/Brolijah/SimpleTriggers)

## About
This plugin aims to provide a simple means of creating in-game triggers to chat messages.
Responses can be done in one of the below methods or any combination of the three:
* Chat Message
* In-Game SFX
* Text-to-Speech 
  * Kokoro
  * Windows System

New Triggers can be created by either clicking the "Add" button on the Triggers tab or saving a chat log message on the Chat History tab.
Triggers may also be shared and imported using the Import/Export buttons.
Chat logging is disabled by default every time the plugin starts, and once enabled you can search for specific messages it has recorded. This may be useful if you're unsure of the exact message you want to create a trigger for but only know some of the words. You can also configure how many messages should be stored at a time.
Message history is never saved outside of the plugin running and is lost when the plugin is disabled.

* Commands:
  * `/simpletriggers` or `/strig`
    * `enable`/`on` or `disable`/`off` to activate or deactivate the whole trigger system.
    * `toggle` flips the trigger system to active or inactive (same as above but as a 1-command toggle)
    * `speak <phrase>` will read aloud the requested phrase using your configured TTS.
  * `/stspeak <phrase>` same as the above speak argument, just as a shorter command.


### Images
![](https://i.imgur.com/QCiTMNO.png) ![](https://i.imgur.com/mlLyTKy.png)


## Known Issues
* With Kokoro, if espeak is disabled, it only supports speaking in English. I would like to remedy this in the future by loading other phonetic dictionaries. Or by finding a g2p alternative that doesn't rely on espeak.
  * With espeak enabled, you can choose between the languages it supports. You can also mix languages with different voices but YMMV.
* Kokoro may cause minor hitching. In my testing, this was near negligible (3-5 FPS). It seems to be related to playing the sound wave. Needs further investigation.

## Credits
* Kokoro C# package from [Lyrcaxis/KokoroSharp](https://github.com/Lyrcaxis/KokoroSharp)
  * Original model: [Kokoro TTS](https://huggingface.co/hexgrad/Kokoro-82M)
* Sound Effects functions from [Ottermandias/ChatAlerts](https://github.com/Ottermandias/ChatAlerts)
* Extra chat types from [karashiiro/TextToTalk](https://github.com/karashiiro/TextToTalk)
* Pre-Built English IPA Dictionary from [open-dict-data/ipa-dict](https://github.com/open-dict-data/ipa-dict)
* Text-to-IPA from [qkmaxware/CsPhonetics](https://github.com/qkmaxware/CsPhonetics) but modified for this project.
