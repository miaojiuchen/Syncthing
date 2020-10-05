# Syncthing

File transfer tool between devices

# Architecture

- Single master.
- Config in settings.ini with same master IP:Port.
- Get all device list from master and connect every two node pair.
- Use TCP connection to transfer data, using `System.IO.Pipelines`.
- Breakpoint, md5 sum check.
- Task queue.
- Notify all nodes when new device added or lost.
- <del>Revote new master when master down.</del>

# System Info Display

|Name|OS|IP|Status|Mac|WorkingDirectory|
|-|-|-|-|-|-|
| Jiuchen's Macbook Pro | macOS, High Serina |192.168.0.105| <span style="color:green">Connected</span> | xxx | /Users/jiuchenm/syncthing |
| Desktop-U7D8NX | Windows 10, Version 2004|192.168.0.106| <span style="color:red">Disconnected</span> | xxx | D:\syncthing |
| Desktop-P9YXN8 | Windows 10, Version 2004|192.168.0.107| <span style="color:green">Connected</span> | xxx | D:\syncthing |