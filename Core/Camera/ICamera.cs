using Core.Math;
using Core.Sampling;

namespace Core.Camera;

public interface ICamera
{
    Ray GetRay(float u, float v, Sampler sampler);
}