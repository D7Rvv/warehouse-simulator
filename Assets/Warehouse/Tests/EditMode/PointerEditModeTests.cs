using NUnit.Framework;

namespace Warehouse.Tests
{
    public sealed class PointerEditModeTests
    {
        [TestCaseSource(typeof(PointerScenarios), nameof(PointerScenarios.Names))]
        public void OwnershipCancellationAndGestureIsolation(string name)
        { PointerScenarios.Run(name); }
    }
}
