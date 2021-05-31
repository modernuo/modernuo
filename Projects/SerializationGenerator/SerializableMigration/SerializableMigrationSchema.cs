/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2021 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: SerializableMigrationSchema.cs                                  *
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
using System.Linq;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace SerializableMigration
{
    public static class SerializableMigrationSchema
    {
        public static JsonSerializerOptions GetJsonSerializerOptions() =>
            new()
            {
                WriteIndented = true,
                AllowTrailingCommas = true,
                IgnoreNullValues = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };

        private static Dictionary<string, SerializableMetadata> _cache = new();

        public static List<SerializableMetadata> GetMigrationsByAnalyzerConfig(
            this GeneratorExecutionContext context,
            INamedTypeSymbol typeSymbol,
            int version,
            JsonSerializerOptions options
        )
        {
            var typeName = typeSymbol.ToDisplayString();
            var migrations = new SortedSet<SerializableMetadata>(new SerializableMetadataComparer());

            foreach (var additionalText in context.AdditionalFiles)
            {
                if (!_cache.TryGetValue(additionalText.Path, out var migration))
                {
                    var text = additionalText.GetText(context.CancellationToken)?.ToString();
                    if (text == null)
                    {
                        continue;
                    }

                    migration = JsonSerializer.Deserialize<SerializableMetadata>(text, options);
                }

                if (typeName == migration!.Type && version > migration.Version)
                {
                    migrations.Add(migration);
                }
            }

            return migrations.ToList();
        }
    }
}
