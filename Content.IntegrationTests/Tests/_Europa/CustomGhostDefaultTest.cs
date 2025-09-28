using Content.Shared._Europa.CustomGhost;
using Robust.Shared.Prototypes;

//
// License-Identifier: AGPL-3.0-or-later
//

namespace Content.IntegrationTests.Tests._Europa
{
    [TestFixture]
    public sealed class CustomGhostDefaultTest
    {
        [Test]
        public async Task CustomGhostDefaultPrototypePresent()
        {
            await using var pair = await PoolManager.GetServerClient();
            var server = pair.Server;
            var prototypeManager = server.ResolveDependency<IPrototypeManager>();
            Assert.That(prototypeManager.HasIndex<CustomGhostPrototype>("default"));
            await pair.CleanReturnAsync();
        }
    }
}
