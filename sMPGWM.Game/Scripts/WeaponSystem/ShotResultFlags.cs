using System;

namespace sMPGWM.Scripts.WeaponSystem;

[Flags]
public enum ShotResultFlags : byte { Miss = 0, Shield = 1, Hull = 2, World = 4, Wreck = 8, ShieldContact = 16 }
