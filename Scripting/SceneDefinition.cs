using Core.Camera;
using Core.Scene;

namespace Scripting;

public sealed record SceneDefinition(Scene Scene, ICamera Camera);