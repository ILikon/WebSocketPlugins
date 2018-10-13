# WebSocketPlugins
A plugin for Rainmeter that implements a WebSocket client. Can be used to communicate with other programs, webpages or any device that hosts a WebSocket server.

## Measure Options
 - `Address` - address and port of the server to connect to
 - `OnOpen` - bang to execute when connection is estabilished
 - `OnClose` - bang to execute when connection is closed
 
	Note: `$message$` keyword will be replaced by the content of the closing message received 
 - `OnMessage` - bang to execute when a message is received from server
 
 	Note: `$message$` keyword will be replaced by the content of the message received 
 - `KeepAlive` - 1 or 0 - if active will try to reconnect to the server if the connection is lost or is not achieved on the first try
 - `MaxReconnectAttempts` - number of times it will try to reconnect before completly stopping 
 
 	Note: if the number is 0 then it will keep trying to reconnect indefinitely 
 - `PingServer` - 1 or 0 - if active will temporaly ping the server to assure the connection is still alive
 - `SendAsync` - 1 or 0 - if active will send messages assyncronously
 - `ParseCommands` - add commands to be automatically parsed when a message is received. Commands should be given in a string with the commands prefixes split by a `|`. For each command add a new option starting with the command prefix and the bang to be executed when the command is received.

	Note: `$message$` keyword will be replaced by the content of the command message received 
	
For more information please check the basic [example](Examples/Example.ini) provided with the plugin.
 
## Acknowledgments
 - Inspiration for plugin development was motivated by a similiar, server only, plugin:
 https://github.com/tjhrulz/MessagePassingForRainmeter
 - WebSocket implementation thanks to:
 https://github.com/sta/websocket-sharp
 
## Usage
This plugin was developed as part of a personal project. To see it in use check:
https://github.com/ILikon/TabletCompanion

