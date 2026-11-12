using System;

namespace sMPGWM.Scripts.ShipSystem;
[Flags]
public enum ShipComponentKind { None = 0, Power = 1, Shield = 2, Movement = 4, AbilityAdmission = 8, Effects = 16, All = Power | Shield | Movement | AbilityAdmission | Effects }
