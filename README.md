# ChatRelay

##Introduction

ChatRelay is a cross service chat relay.

It currently supports relaying messages between channels on the following service types:

 * [Slack](https://slack.com)
 * [JabbR](https://github.com/davidfowl/JabbR)
 * [IRC](http://en.wikipedia.org/wiki/Internet_Relay_Chat).

##Commands

 * Messages beginning with tilde (~) will not be relayed. Ex: "~This message will not be sent to other services."

##Known Issues

 * Emotes are not yet supported.

##Disclaimer

Please get the approval of the users who administer the channels/rooms you intend to relay prior to setting up a relay!

The relay makes no use of encryption and is probably not be suitable for use with any messages that should remain private.
