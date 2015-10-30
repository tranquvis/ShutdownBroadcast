# ShutdownBroadcast
With this package you can shutdown multiple windows pcs or servers from a single pc.
It contains a client and a server app.
The server sends the broadcast and the client listens for an incoming shutdown request.
## Installation Instruction
The installation folders are in the ShutdownBroadcast folder.
### Client Application
1. copy the client app folder on your clients, that should be shut down
2. **Configuration** (edit config.txt)
  - secret: custom secret text
  - server_ip: ip address of the server
  - port:  port on which the client should listen
  
  example:
  ```
  secret = "$shutdownAllPcs$"
  server_ip = 192.168.1.10
  port = 8888
  ```
3. execute the application to listen for a shutdown request

An autostart entry will be added on the first run, so that the app will start automatically next time.

### Server Application
1. copy the server app folder on a pc, from where you want make a shutdown broadcast
2. **Configuration** (edit config.txt)
  - secret: must be equal to the clients secret
  - broadcast_ip: the broadcast ip address of your network
  - port:  port on which the client app listens (same as in client config)
  
  example:
  ```
  secret = "$shutdownAllPcs$"
  broadcast_ip = 192.168.1.255
  port = 8888
  ```
3. execute the application to run the shutdown broadcast
