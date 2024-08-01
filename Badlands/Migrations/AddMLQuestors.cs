// Copyright (C) 2024 Reetus
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using Org.BouncyCastle.Asn1.X509;
using Server;
using Server.Engines.Spawners;

namespace Badlands.Migrations;

public class AddMLQuestors : IMigration
{
    public DateTime MigrationTime { get; set; } = DateTime.Parse( "2024-05-19" );
    public List<Serial> Up()
    {
        CommandSystem.Handle(World.Mobiles.FirstOrDefault().Value, $"[GenerateSpawners Assemblies/Data/mlquestors.json");

        return new List<Serial>();
    }

    public void Down()
    {
    }
}