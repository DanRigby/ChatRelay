# ChatRelay

##Introduction

ChatRelay is a cross service chat relay.

It currently supports relaying messages between channels on the following service types:

 * [Slack](https://slack.com)
 * [JabbR](https://github.com/davidfowl/JabbR)
 * [IRC](http://en.wikipedia.org/wiki/Internet_Relay_Chat).

##Known Issues

 * Emotes are not yet supported.
 * New users joining Slack currently crash the relay (though it should restart automatically).
   * This is due to a bug in the library I'm currently using to interact with Slack.

##Disclaimer

Please get the approval of the users who administer the channels/rooms you intend to relay prior to setting up a relay!

The relay makes no use of encryption and is probably not be suitable for use with any messages that should remain private.
