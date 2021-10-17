# IMPORTANT: This branch is discontinued
## For the current version of Jire, please refer to the Jire repository instead (https://github.com/AspectInteractive/Jire)

Due to difficulties in getting this to work as intended, the Anya pathfinder has been discontinued. This code is a carbon copy of the C++ code written and owned by (T. Uras and S. Koenig,  2015. An Empirical Comparison of Any-Angle Path-Planning Algorithms. In: Proceedings of the 8th Annual Symposium on Combinatorial Search.). The original code is available at: http://idm-lab.org/anyangle (direct link is http://idm-lab.org/code/anyangle.tar). Main files used are AnyAngleAlgorithm.cpp, AnyAngleAlgorithm.h, ANYA.h, ANYA.cpp. You will also need to refer to the paper "Optimal Any-Angle Pathfinding In Practice" that can be found here: http://www.grastien.net/ban/articles/hgoa-jair16.pdf . 

The main files to refer to in this project are:

- AnyaPathSearch.cs - The main algorithm
- AnyaPathfinderOverlay.cs - A visual overlay for testing the algorithm (use '/anyall' command to view this)
- CCPos.cs - A cell-corner, maps closely to CPos and is used heavily

I would not recommend undertaking this unless you have some experience in pathfinding algorithms and some background in math. This code does not contain any notable bugs (this has been exhaustively verified), but fails to identify the right path due to either a subtle inconsistency with the above C++ code (e.g. it uses a pre-populated clearance table, where my code checks clearances on the fly), or a bug in the above C++ code (has not been tested independently). If you would like to know more about this, feel free to contact me.

# ---

<p align="center"><img src="https://www.dropbox.com/s/9c4ovllw064gtnz/JireLogoCondensed.png?raw=1" /></p>

Jire will be an open-source RTS that features substantial modifications to the real-time strategy game engine OpenRA ([http://www.openra.net](http://www.openra.net)). The primary goal of Jire is to further enable modern RTS development within OpenRA (ORA), by incorporating modern features such as non-grid based movement and cutting edge pathfinding. This will in turn enable the creation (or re-creation) of RTS games such as Age Of Empires II, StarCraft II, C&C 3 and more with less rework required. The secondary goal of Jire is to be a retro-inspired competitive multiplayer RTS, that feels modern and takes inspiration from the all-time greats such as Age of Empires II and StarCraft/StarCraft II.

Below are a list of core features that will be developed for this purpose.

- **(DONE)** Implement Non-grid based movement using the existing Aircraft movement as a basis
- **(DONE)** Implement Non-grid based collision detection with other buildings and units
- **(DONE)** Implement Non-grid based collision detection with terrain obstacles such as cliffs and rivers
- **(IN PROGRESS)** Modify or re-implement the pathfinding module for non-grid based movement. An example of such a module can be found with [Age Of Empires II](https://www.gamasutra.com/view/feature/131720/coordinated_unit_movement.php) or StarCraft II.
- Diamond based grid for building placement as it exists in Age Of Empires II.

In addition, the below are nice-to-have features that will be developed once the core features are complete.

- A game balance framework and/or theory that aims to ensure a baseline level of balance among factions can easily be obtained, that avoids as much painstaking trial and error as possible.
- Client/server architecture, with a competitive matchmaking system that uses Elo to group players into leagues (similar to StarCraft II).
- A custom map/game module that enables people to create their own spin-off mini-games within the engine, similar to that seen in StarCraft & WarCraft custom maps.
