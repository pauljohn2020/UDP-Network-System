# UDP Position Sync System

A real-time multiplayer position synchronization system built in Unity using UDP sockets.

## What This Project Shows

- UDP socket programming in C#
- Multi-threaded network listening (game doesn't freeze)
- Client-server architecture
- Team-based message filtering (like Valorant pings)
- Thread-safe Unity integration

## The Scripts

- **NetworkManager.cs** - Handles UDP server/client logic, threading, and message routing
- **PlayerController.cs** - Manages player movement and input
- **UnityMainThreadDispatcher.cs** - Safely executes background thread actions on Unity's main thread

## How It Works

The server listens for UDP packets on a separate thread. Clients send their position every 100ms. The server tracks which team each client belongs to and only broadcasts "enemy spotted" messages to teammates.

## How to Test

1. Create a new Unity project
2. Create an `Assets/Scripts/` folder
3. Add these three scripts to that folder
4. Create an empty GameObject → add NetworkManager (isServer = true)
5. Create a Cube → add PlayerController
6. Press Play in the Editor (Server)
7. Build a second version or use ParrelSync to create a Client
8. Move with WASD, press SPACE for enemy ping

## Performance Notes

- UDP packets sent every 100ms
- Server processing latency under 10ms
- Network listening runs on background thread — main thread stays smooth