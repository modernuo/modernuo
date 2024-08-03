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

using Server;
using Server.Engines.Spawners;

namespace Badlands.Migrations;

public class AddDeluciaBulls : IMigration
{
    public DateTime MigrationTime { get; set; }

    public List<Serial> Up()
    {
        var serials = new List<Serial>();

        foreach ( var map in new[] { Map.Felucca, Map.Trammel } )
        {
            var spawner = new Spawner(
                10,
                TimeSpan.FromMinutes( 5 ),
                TimeSpan.FromMinutes( 10 ),
                0,
                20,
                "Bull",
                "Cow",
                "Sheep",
                "Goat"
            );

            spawner.MoveToWorld( new WorldLocation( new Point3D( 5170, 3989, 40 ), map ) );

            serials.Add( spawner.Serial );
        }

        return serials;
    }

    public void Down()
    {
    }
}