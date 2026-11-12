Scripts/
|-- AbilitySystem/ 
|-- CollisionSystem/
|-- CombatSystem/
|-- ConditionSystem/
|-- FileLoadingSystem/
|-- GameConfigSystem/
|-- GameFlowSystem/
|-- GameplayDataSystem/
|-- GamePlaySystem/
|-- InputSystem/
|-- Items/  #Rename to ItemSystem
|   |-- Equipment/ #Consolidate with Shipsystem Equipment. Only one should stay
|   |-- Hotbar/
|   |-- Inventory/ 
|-- LoggingSystem/
|-- MessageSystem/
|-- MovementSystem/
|-- NetworkSystem/
|   |-- NetworkInternal/
|   |-- SynchronisationSystem/
|-- Parallax/
|-- PlayerController/
|-- ProjectileSystem/
|-- ShipSystem/
|   |-- Equipment/
|   |-- Hotbar/
|-- StartMenuSystem/
|   |-- LobbyList/
|-- StatSystem/
|-- EnergySystem/
|-- ShieldSystem/
|-- EngineSystem/
|-- HullSystem/
|-- Steam/
|-- TagSystem/
|   |-- Definitions/
|   |-- Exceptions/
|   |-- Ids/
|   |-- Internal/
|   |-- Queries/
|-- UiSystem/
|   |-- HudSystem/
|   |   -- Hotbar/
|   |-- Items/
|   |-- MessageStackSystem/
|   |-- OverlayMenuSystem/
|       |-- InventoryMenu/
|-- Util/
|   |-- Mocks/
|   |-- UiComponents/
`-- World/

Use the abstract GameplayDataSystem for Items and ItemCategories as well. 

Make a proper SynchronizationSystem where all gets bundled and synced