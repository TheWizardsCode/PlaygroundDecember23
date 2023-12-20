The player is imbued with Nanobots. These serve three primary functions. 

* They heal injuries, ensuring the player's survival in this dangerous world. 
* They can craft ammunition, weapons, armor, and other essential items from resources scavenged in the world. 
* They are responsible for ensuring the player never actually dies and can respawn, although without any equipment collected, to allow another attempt to escape.

# Death Avoidance

The nanobots can give the player extraordinary abilities. One such ability, which is always available, is a form of invincibility. When the player is on the brink of death, the nanobots spring into action, disassembling the player's organic matter into individual cells. They then transport these cells to a safe location and reassemble the player.

This process, while saving the player's life, leaves behind any non-organic matter, including weapons and equipment. Furthermore, the player wakes up in an unfamiliar location, disoriented and without any knowledge of their whereabouts. Fortunately, the nanobots will have scavenged some starting resources and will create a small inventory that will give the player a start on another attempt at escape.

# Healing

Another ability that the nanobots always provide is the ability to heal the player. They need resources to do it, which can be gathered from destroyed items in the world. The nanobots will prioritize this action above all other buildign activities (see below). However, when the player is at full strength they will attempt to build new equipment and skills.

# Recipes and In Game Builds

The nanobots can learn recipes from fallen enemies and various locations in the world. Once they have learned a recipe they will craft items that they believe will aid the player. 

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
