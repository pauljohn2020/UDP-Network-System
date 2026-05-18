========================================
UDP POSITION SYNC SYSTEM - WINDOWS BUILD
========================================

A lightweight UDP networking system for real-time position sync.

========================================
HOW TO TEST MULTIPLAYER (1 PC)
========================================

1. Run UDP_Project.exe TWICE (two separate windows)

2. FIRST window (SERVER):
   - Check "Run as Server" in the top-left corner
   - Select "Blue" or "Red" for the team

3. SECOND window (CLIENT):
   - UNcheck "Run as Server"
   - Select the opposite team as compared to the first window

4. Click Play in both windows

5. Move with WASD keys

6. Press SPACE to send "Enemy Spotted" ping

========================================
WHAT YOU SHOULD SEE
========================================

- Console shows position updates
- Server window shows: "Player joined Blue/Red team"
- Pressing SPACE shows: "Enemy spotted by team X"

========================================
TROUBLESHOOTING
========================================

- Client won't connect? Ensure Server window is running FIRST
- "Port already in use"? Close Skype/Discord or change port in code
- Firewall prompt? Click "Allow access"

========================================
TECH DETAILS (For Your Review)
========================================

- UDP protocol (fast, no confirmation)
- Server tracks teams (Blue/Red auto-assigned)
- Multi-threaded listening (game doesn't freeze)
- Thread-safe dispatcher for Unity main thread

========================================
SOURCE CODE
========================================

Full source code available on GitHub:
https://github.com/pauljohn2020/UDP-Network-System

========================================
CONTACT
========================================

Paul John Shaji
pauljohn.shaji@yahoo.com