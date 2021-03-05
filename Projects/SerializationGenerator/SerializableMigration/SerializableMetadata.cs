/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableMigration.cs                                        *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace SerializationGenerator
{
    public record SerializableMetadata
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("type")]
        public INamedTypeSymbol Type { get; set; }

        [JsonPropertyName("properties")]
        public List<SerializableProperty> Properties { get; set; }
    }
}
