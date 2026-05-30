namespace SharpOpenGl.Engine.Economy;

/// <summary>
/// The four resource types in the game economy.
/// </summary>
public enum ResourceType
{
    /// <summary>Plasma Cores — regenerating energy from solar collectors.</summary>
    Energy,

    /// <summary>Astrium Ore — finite minerals harvested from asteroid fields.</summary>
    Minerals,

    /// <summary>Quantum Fragments — research data from derelict scans.</summary>
    Data,

    /// <summary>Personnel — crew trained at facilities and used to man ships.</summary>
    Crew
}
