#if WAREHOUSE_STANDALONE_TESTS
using System;
using System.IO;
using System.Web.Script.Serialization;
using Warehouse.Domain;

namespace Warehouse.Tests
{
    internal static class RunDomainTests
    {
        private static int Main(string[] arguments)
        {
            var json = File.ReadAllText(arguments[0]);
            Func<PrototypeFixtureData> fixture = () => new JavaScriptSerializer().Deserialize<PrototypeFixtureData>(json);
            var failed = 0;
            foreach (var name in DomainScenarios.Names)
            {
                try { DomainScenarios.Run(name, fixture); Console.WriteLine("PASS " + name); }
                catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
            }
            foreach (var name in PointerScenarios.Names)
            {
                try { PointerScenarios.Run(name); Console.WriteLine("PASS " + name); }
                catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e.Message); }
            }
            var count = DomainScenarios.Names.Length + PointerScenarios.Names.Length;
            Console.WriteLine("DOMAIN AND POINTER LOGIC: " + (count - failed) + "/" + count + " passed; Unity/phone execution NOT_RUN");
            return failed == 0 ? 0 : 1;
        }
    }
}
#endif
