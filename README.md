# The Feegs
The Feegs is a multiplayer, first person shooter game I desgined in Unity. The name comes froma nickname for my dog Figo who is the face of the app's icon.

## Running The Application
The playable desktop app is located in the Builds directory. Mac users should run the `Build.app` file, and Windows users should run `The Feegs.exe` from the folder titled `The Feegs WINDOWS`.

The first player to run the game should create a lobby, and remaining players can join the existing room.

## Controls
Many of the controls are not standard for FPS games. I selected controls based on my own setup of playing on a Macbook Pro without a mouse. As such, some bindings may feel odd or abnormal for more experienced or traditional PC gamers.
### Movement
- Player movement: WASD keys
- Sprint: Left Shift (HOLD)
- Crouch: Left Control (HOLD)
- Slide: F while sprinting
### Weapon Mechanics
- Fire: Left click
- Aim down sights: C
- Swap to primary/secondary: 1/2
- Reload: R
### Miscellaneous
- Inflict damage: U

## Motivation
I designed this game out of a love of playing FPS games and an interest in the physics and mechanics behind making one. The weapons are projecticle based (as opposed to hit-scan) and are subject to travel-time and bullet drop. These were features I wanted to include out of interest in how physics mechanics are used in game making. I have played this game with friends of mine and have had shared lots of fun and laughs as a result of it.

## Credits
Inspiration for the game comes from Welton King on YouTube. I watched him design a multiplayer FPS game from scratch and took inspiration when doing it myself.

All code, models, designs, and maps were developed solely by me. Sound effects were pulled from copyright-free sound libraries. The multiplayer aspect of the game runs off of Photon.

## Known Issues
This game is far from perfect. Many bugs exist (more than listed below), and as this game is not published, they will likely continue to persist. The game is still enjoyable despite these!
- Rarely after respawning, some players become invincible and lose ADS functionality until they quit/rejoin
- Sliding does not work consistently
- Running up slopes leaves the player suspended in the air for a short period of time
- Jumping while running up slopes will launch the player high up
- Jumping while on a steep enough slope will not have any effect
- Rendering arms while sprinting and sliding is inconsistent
