The player is imbued with Nanobots. These serve three primary functions. 

* They heal injuries, ensuring the player's survival in this dangerous world. 
* They can craft ammunition, weapons, armor, and other essential items from resources scavenged in the world. 
* They are responsible for ensuring the player never actually dies and can respawn, although without any equipment collected, to allow another attempt to escape.

  There are three tiers of upgrades the nanobots can build for you:

* Essentials - in-game essentials like health and ammo. Must have a recipe for these (see below) Built by nanobots when needed and instructed.
* Power - purchasing of new weapons (with recipe for ammo), more powerful ammo type recipes, upgrades to weapons (one-off and incremental ones built in-game) which are lost on death
* Skills - permanent character upgrades that the nanobots can give you between levels, no recipe needed, kept on death

# Essentials

Essential items (such as health and ammo) can be built by the nanobots in-game Nanobots will build what they think you need and drop them as pickups. So, for example, you start the game with a recipe for health and another for your starting weapons ammo. If you need health they will automatically make you a health pack. If you need ammo they make ammo. 

Building essential items costs resources. These resources are dropped by the enemies when killed.

# Power Ups

New in-game recipes are bought with resources in-level. For example the player could purchase a new weapon with ammo recipe. When a spawner is destroyed three recipes will be dropped. The player can grab any one of these three. Since they just destroyed the spawner they will have a short amount of time with no enemies attacking, enabling them to think a little about which to choose. However, enemies will still be coming from other spawners, so the player is pressured into a quick decision. 

These upgrades will increase the characters power between levels. However, these recipes will be forgotten on death.  Players are presented with a random selection of three powerups between each level. Choosing the best complement to their existing loadout will be critical to their ongoing success.

# Skill Upgrades

Between levels the player can choose one of a random set of permanent character upgrades such as speed, max health, nanobot efficiency and so on. These are not lost on death, providing a sense of progression between plays.

# Death Avoidance

The nanobots can give the player extraordinary abilities. One such ability, which is always available, is a form of invincibility. When the player is on the brink of death, the nanobots spring into action, disassembling the player's organic matter into individual cells. They then transport these cells to a safe location and reassemble the player.

This process, while saving the player's life, leaves behind any non-organic matter, including weapons and equipment. Furthermore, the player wakes up in an unfamiliar location, disoriented and without any knowledge of their whereabouts. Fortunately, the nanobots will have scavenged some starting resources and will create a small inventory that will give the player a start on another attempt at escape.

# Developer Notes

## Creating Recipes

To enable the Nanobots to create an item for the player you must first have a Pickup item for the desired upgrade, this is a standard Neo FPS pickup. In some cases it is also necessary to have a Recipe scriptable object that is aware of the type of upgrade this is, these scripts are used to decide whether or not a particular recipe is built (for example see HealthPickupRecipe and AmmoPickupRecipe).

The Nanobots will build the game object defined by the most appropriate recipe found, and drop it on the floor to the right and forward of the player is currently standing.

Once the PickupObject and Recipe scriptable objects are available you can build a new recipe as follows:

* Create a new recipe by right clicking in a Resources/Recipes folder and selecting Create > Playground > [recipe type]
* Fill in the details (tooltips should guide you)
* Open Assets/_Dev/Prefabs/Player/Playground_Character.prefab
* Find the Nanobot Manager component
* In the appropriate Recipes section click the + to add a new recipe
* Drag the recipe to the right priority position
