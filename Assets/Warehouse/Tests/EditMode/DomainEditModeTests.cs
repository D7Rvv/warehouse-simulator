using NUnit.Framework;
using UnityEngine;
using Warehouse.Domain;

namespace Warehouse.Tests
{
    public sealed class DomainEditModeTests
    {
        [TestCaseSource(typeof(DomainScenarios), nameof(DomainScenarios.Names))]
        public void PrototypeNavigationAndGhostInvariants(string name)
        {
            var fixture = Resources.Load<TextAsset>("Prototype/prototype_station_fixture_v0_1");
            Assert.That(fixture, Is.Not.Null, "Committed fixture resource missing");
            DomainScenarios.Run(name, () => JsonUtility.FromJson<PrototypeFixtureData>(fixture.text));
        }
    }
}
