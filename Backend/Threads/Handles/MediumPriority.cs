using Mod.DynamicEncounters.Features.Spawner.Behaviors.Interfaces;

namespace Mod.DynamicEncounters.Threads.Handles;

public class MediumPriority(int framesPerSecond, BehaviorTaskCategory category, bool fixedStep = false)
    : ConstructBehaviorLoop(framesPerSecond, category, fixedStep);