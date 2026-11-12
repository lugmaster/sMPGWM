Scripts/
|-- AbilitySystem/
|-- CollisionSystem/
|-- CombatSystem/
|-- ConditionSystem/
|-- EnergySystem/
|-- EquipmentSystem/
|-- FileLoadingSystem/
|-- GameConfigSystem/
|-- GameFlowSystem/
|-- GameplayDataSystem/
|-- GamePlaySystem/
|-- InputSystem/
|-- InventorySystem/
|-- ItemSystem/
|-- LoggingSystem/
|-- MessageSystem/
|-- MovementSystem/
|-- NetworkSystem/
|-- ParallaxSystem/
|-- PlayerControllerSystem/
|-- ProjectileSystem/
|-- ShieldSystem/
|-- ShipSystem/
|-- StartMenuSystem/
|-- StatSystem/
|-- SteamSystem/
|-- TagSystem/
|-- UiSystem/
|-- UtilitySystem/
|-- WorldSystem/

Items, item categories, and runtime ship data inherit GameplayDefinition and use DefinitionRegistry. ShipDefinition remains the serialized ship input model.

GamePlaySystem owns session orchestration. ShipSystem owns ship state and resource pools; AbilitySystem owns admission and effects; MovementSystem owns movement capability; ShieldSystem and EnergySystem own shield and power simulation.

Runtime icons use IconDefinition objects from Icons. Authored icon keys are resolved at the loading boundary. Gameplay catalogue icons retain typed accessors so loading definitions does not require the Godot scene tree; textures are accessed when rendered. 

Make a proper SynchronizationSystem where all gets bundled and synced