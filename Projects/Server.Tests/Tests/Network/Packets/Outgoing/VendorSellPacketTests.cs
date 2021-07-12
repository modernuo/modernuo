using System;
using System.Collections.Generic;
using Server.Network;
using Xunit;

namespace Server.Tests.Network
{
    [Collection("Sequential Tests")]
    public class VendorSellPacketTests : IClassFixture<ServerFixture>
    {
        [Fact]
        public void TestVendorSellList()
        {
            var vendor = new Mobile(0x1024u);
            vendor.DefaultMobileInit();

            var item1 = new Item(World.NewItem);
            var item2 = new Item(World.NewItem) { Name = "Second Item" };
            var item3 = new Item(World.NewItem);

            var sellStates = new List<SellItemState>
            {
                new(item1, 100, "Item 1"),
                new(item2, 100000, "Item 2"),
                new(item3, 1, "Item 3")
            };

            var expected = new VendorSellList(vendor, sellStates).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendVendorSellList(vendor.Serial, sellStates);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }

        [Fact]
        public void TestEndVendorSell()
        {
            var vendor = new Mobile(0x1024u);
            vendor.DefaultMobileInit();

            var expected = new EndVendorBuy(vendor.Serial).Compile();

            var ns = PacketTestUtilities.CreateTestNetState();
            ns.SendEndVendorSell(vendor.Serial);

            var result = ns.SendPipe.Reader.TryRead();
            AssertThat.Equal(result.Buffer[0].AsSpan(0), expected);
        }
    }
}
