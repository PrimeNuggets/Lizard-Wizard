Heal-N-Hazards

Unity Package by Xavier McIntosh
Last Update: 04-06-2026

High-Quality ReadMe: https://docs.google.com/document/d/11FrwkOinUqPaHJzZXrsIjmcz9Xce6wQQBT-jEvmjNdE/edit?usp=sharing

=============================================
    Content
=============================================
    Overview
=============================================
    Heal-N-Hazards is a Unity Package aimed at helping designers create and manage interactive elements such as traps, heal spots, and environmental hazards. The kit includes tools to easily create interactables using scriptable objects, simpler management with a single script, and other quality-of-life tools like a hazard spawner and an example scene. Interactions between traps is supported as well (more advanced).

    The kit consists of two main systems:
    - Hazards: Static environmental traps (e.g., spikes, pits) that affect entities entering their trigger area.
    - Collision Effects: Dynamic effects triggered by collisions (e.g., projectile impacts, moving hazard interactions).

=============================================
    Notice
=============================================
    - This kit is intended as a tool for designers to build upon and not something to be integrated midway through a project. The reason revolves mostly around the player script. As a designer, you may have to enter your existing player script to properly integrate damage and a few other things if you don’t plan on using the example player.
    - This kit was designed and tested in 2D. Therefore, 3D integration, while possible through modification, is unsupported by this kit. 

=============================================
    [Basic Kit Usage] Hazards / Heal Spots
=============================================
    Creation
=============================================
- To create a new interactable, first enter a folder through Unity where you want to create the asset.
- Next, right-click the folder and select Create > HazardManager > Hazard.
- From there, fill out the data as it appears.
- Type is a dropdown to determine the interactable. This is mainly used to group interactables without tagging the object. Existing types besides None include:
    - Arrow
    - Fire
    - Fountain
    - Hazardous Gas
    - Heal Spot
    - Medicine Herb Patch
    - Pitfall
    - Poison Floor
    - Spikes
    - Thorny Bramble
- Damage is self-explanatory. Set to negative to heal an entity.
- Damage Type is a dropdown to determine how damage is dealt to an entity: Instant, Damage over Time (DoT), Delayed, and Summon.
    - Instant damages the entity once its hitbox collides with the interactable.
    - DoT damages the entity while they’re inside the interactable's hitbox. This damages the entity every [Damage Interval] second.
    - Delayed damages the entity after the number of seconds specified in the Delay Time variable. The entity has to stay in the trap’s collider to work as intended.
    - Summon is the only type besides None that actually doesn’t do damage. Instead, when a “Player” tagged object enters the trap, it summons a game object at the specified position and rotates it as specified.
- Despawn Time is approximately how many seconds it takes for the interactable to disappear.

=============================================
    Application
=============================================
    Hazards are for static scene objects that act as traps or healing areas.
    - To bring your newly created interactable into your scene, first create a game object for it and add a 2D collider to it.
        - Toggle the collider’s IsTrigger boolean to true.
    - Drag the HazardManager script (located under Scripts/InScene) onto this object. This script comes with 2 fields. The first is for the interactable you just made, and the second is the summon point.
    - The summon point is a referenced GameObject for an interactable with a Summon damage type to spawn their object rather than hardcoding a position into the hazard itself.
    - Do this for every interactable variant you plan to have.

=============================================
    [Advanced Kit Usage] Collision Effects
=============================================
    Creation
=============================================
    - To create a new collision effect, first enter a folder through Unity where you want to create the asset.
    - Next, right-click the folder and select Create > CollisionFX.
    - From there, fill out the data as it appears.
    - Effect Type is a dropdown to determine the effect. This is mainly used to group effects without tagging the object. Existing types besides None include:
        - Explosion
        - Fire Burst
        - Heal Effect
        - Poison Cloud
        - Spark
    - Damage is self-explanatory. Set to negative to heal an entity.
    - Damage Type is a dropdown to determine how damage is dealt to an entity: Instant or DoT.
        - Instant damages the entity once its hitbox collides with the effect.
        - DoT damages the entity while they’re inside the effect’s hitbox. This damages the entity every [DoT Interval] second.
    - Collision Life is approximately how many seconds it takes for the effect to disappear.

=============================================
	Application
=============================================
    Collision Effects are for dynamic effects triggered by collisions, such as projectile impacts or moving hazard interactions.
    - Once you have a Collision Effect setup, create a new object in your scene and add a 2D collider to it.
        - Toggle the collider’s IsTrigger boolean to true.
    - Drag the InteractionCollision (located in Scripts/InScene) script onto the object.
    - Apply your Collision Effect in the field and save your object as a prefab. Repeat these steps for all of your desired collision effects.

    - Once the prefabs are finished, bring the InteractionsManager script (located in Scripts/InScene) into your scene on an object that should remain enabled throughout the scene, such as the Main Camera. Only one is required per scene; adding more can cause unintended bugs.
    - Once you have the script in the scene, you can make as many interactions as you feel necessary.
    - There are 3 required sections per interaction: First Participant, Second Participant, and the Outcome.
    - Participants:
        - The first field is the participant's trap type. For projectiles, use the same type of trap that summoned them.
        - Second is whether the participant is a projectile or an interactable.
        - The final 2 fields are whether the projectile is on fire/poison, respectively.
    - Outcome:
        - The first boolean determines whether the order of collision matters.
            - True means the order doesn’t matter.
        - The next field is where you drag your collision prefab into.
        - The last 2 booleans indicate whether the participants should be destroyed upon collision.
            - True means they are destroyed

=============================================
    Demo
=============================================
    Entities
=============================================
	Overview
=============================================
    - This resource was created to ensure consistency across player-created entities. Notable entities in the example scene include Arrows and the Player.
    - As the designer, you can edit stats such as HP, Speed, and Jump Height.
    - Internally, this resource also handles status flags like burn and poison. There is no functionality like health loss with these effects, as for the sake of the kit, they are meant as purely informational variables.
=============================================
	Creation
=============================================
    - To create an entity, first enter a folder through Unity where you want to create the asset.
    Next, right-click the folder and select Create > Entity.
    - From there, fill out the data as it appears.

=============================================
	Player
=============================================
	Overview
=============================================
    - This resource illustrates how the player should operate this kit.
    - Note that if you wish to avoid programming, use the PlayerExample script (located in Examples/Scripts) as the base script for your player.
=============================================
	Application
=============================================
    - Make your player’s GameObject and attach the PlayerExample script (located in Examples/Scripts) to it.
        - Make sure your player has an Animator, 2D Collider, and a Rigidbody 2D attached.
        - Make sure the animator’s ApplyRootMotion boolean is True
            - When false, this resulted in an error during testing, where the player would gradually float offscreen, preventing movement.
    - Apply the Entity Data for the entity to actually function with interactables.
    - Add any other data for the following fields
    - Note that the pitfall animation requires both a start and end animation. A full animation will not play without modification to the script.

=============================================
	Projectiles
=============================================
	Overview
=============================================
    - This resource was created to ensure consistency across player-created projectiles.
    - Projectiles move in accordance with their z-rotation. For example, 0 would move up, 45 = up-right, 90 = right, etc.
=============================================
	Application
=============================================
    - Make your projectile’s GameObject and attach the Projectiles script (located in Scripts/InScene) to it.
        - Make sure your projectile has a 2D Collider and a Rigidbody 2D attached.
        - Make sure the collider’s IsTrigger boolean is True
        - Recommended: set the Rigidbody’s Gravity Scale to 0
        The kit does not delete the projectile if it comes in contact with the ground.
    - Attach your entity data for your projectile in the Entity Data field, and assign the boundaries (-x, x, -y, y) of your worldspace for the projectile to be deleted.

=============================================
	Interactable Spawning
=============================================
    Application
=============================================
    - Append one instance of the HazardSpawner script (located in Example/Scripts) into your scene and fill out the appropriate data.
    - Spawnable Ground is a list of objects where interactables can spawn.
        - All interactibles will spawn along the object’s top collision boundary and can spawn on any x-coordinate within that collider.
    - UseGroundTag is a boolean that adds all objects tagged “Ground” to the Spawnable Ground list.
    - Screen Bounds defines the bounds (-x, x, -y, y) within which interactables can spawn. No interactables can spawn outside of these boundaries.
    - Hazards is a list of interactables that can spawn along with their spawn chance (0-100).
    - Spawn Rolls is the number of times the system attempts to spawn interactables.
    Finally, the last 2 variables are to prevent spawning near the player and other interactables.
    - Setting to 0 disables the prevention.


=============================================
    Video
=============================================
Link: https://youtu.be/kUxacOXm240
