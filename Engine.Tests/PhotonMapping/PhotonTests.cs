using Core.Algebra;
using Engine.PhotonMapping;
using FluentAssertions;

namespace Engine.Tests.PhotonMapping;

public class PhotonTests
{
    [Fact]
    public void Photon_StoresAllProperties()
    {
        var position = new Vector3(1, 2, 3);
        var direction = new Vector3(0, -1, 0);
        var power = new Vector3(0.5, 0.5, 0.5);
        var pathType = PhotonPathType.Caustic;

        var photon = new Photon(position, direction, power, pathType);

        photon.Position.Should().Be(position);
        photon.Direction.Should().Be(direction);
        photon.Power.Should().Be(power);
        photon.PathType.Should().Be(pathType);
    }

    [Fact]
    public void PhotonPathType_HasThreeValues()
    {
        var values = Enum.GetValues<PhotonPathType>();
        values.Should().HaveCount(3);
        values.Should().Contain(PhotonPathType.Direct);
        values.Should().Contain(PhotonPathType.Caustic);
        values.Should().Contain(PhotonPathType.Indirect);
    }
}